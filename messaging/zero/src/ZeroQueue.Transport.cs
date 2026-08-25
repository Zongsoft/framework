/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@qq.com>
 *
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Messaging.ZeroMQ library.
 *
 * The Zongsoft.Messaging.ZeroMQ is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Messaging.ZeroMQ is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Messaging.ZeroMQ library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using NetMQ;
using NetMQ.Sockets;

namespace Zongsoft.Messaging.ZeroMQ;

public sealed partial class ZeroQueue
{
	private sealed class Transport : IAsyncDisposable
	{
		#region 常量定义
		private const int BATCH_SIZE = 1024;
		#endregion

		#region 成员字段
		private readonly string _identifier;
		private readonly ZeroQueueRuntimeOptions _options;
		private readonly Func<string[]> _heartbeatTopics;
		private readonly NetMQQueue<Command> _commands;
		private readonly NetMQPoller _poller;
		private readonly Task _runner;
		private readonly Dictionary<ZeroSubscriber, SubscriberSocket> _subscribers = new();
		private readonly HashSet<ZeroSubscriber> _paused = new();
		private PublisherSocket _publisher;
		private NetMQTimer _timer;
		private ushort _publisherPort;
		private ushort _subscriberPort;
		private int _disposed;
		#endregion

		#region 构造函数
		public Transport(ZeroQueueRuntimeOptions options, string identifier, Func<string[]> heartbeatTopics)
		{
			_options = options;
			_identifier = identifier;
			_heartbeatTopics = heartbeatTopics;
			_commands = new NetMQQueue<Command>();
			_commands.ReceiveReady += this.OnCommandReady;
			_poller = new NetMQPoller() { _commands };
			_runner = Task.Factory.StartNew(() =>
			{
				if (Thread.CurrentThread.Name == null)
					Thread.CurrentThread.Name = $"{nameof(ZeroQueue)}#{identifier}.Actor";

				_poller.Run(new SynchronizationContext());
			}, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
		}
		#endregion

		#region 公共方法
		public ValueTask StartAsync(CancellationToken cancellation) => this.Enqueue(new StartCommand(cancellation));
		public ValueTask PublishAsync(string topic, string identifier, byte[] data, int compressionThreshold, CancellationToken cancellation) => this.Enqueue(new PublishCommand(topic, identifier, data, compressionThreshold, cancellation));
		public ValueTask SubscribeAsync(ZeroSubscriber subscriber, string topic, CancellationToken cancellation) => this.Enqueue(new SubscribeCommand(subscriber, topic, cancellation));
		public ValueTask UnsubscribeAsync(ZeroSubscriber subscriber, CancellationToken cancellation) => this.Enqueue(new UnsubscribeCommand(subscriber, cancellation));
		public void Pause(ZeroSubscriber subscriber, Message message) => this.PostUnobserved(new PauseCommand(subscriber, message));
		public void Resume(ZeroSubscriber subscriber) => this.PostUnobserved(new ResumeCommand(subscriber));
		#endregion

		#region 私有方法
		private ValueTask Enqueue(Command command)
		{
			this.Post(command);
			return new ValueTask(command.Completion.Task);
		}

		private void Post(Command command)
		{
			if (Volatile.Read(ref _disposed) != 0)
			{
				command.Completion.TrySetException(new ObjectDisposedException(nameof(Transport)));
				return;
			}

			try { _commands.Enqueue(command); }
			catch (Exception exception) { command.Completion.TrySetException(exception); }
		}

		private void PostUnobserved(Command command)
		{
			if (Volatile.Read(ref _disposed) != 0)
				return;

			try { _commands.Enqueue(command); }
			catch (ObjectDisposedException) { }
		}

		private void Stop()
		{
			if(_timer != null)
			{
				_timer.Enable = false;
				_timer.Elapsed -= this.OnHeartbeat;
				_poller.Remove(_timer);
				_timer = null;
			}

			foreach(var subscriber in new List<ZeroSubscriber>(_subscribers.Keys))
				this.Unsubscribe(subscriber);

			_publisher?.Dispose();
			_publisher = null;
		}

		private void Start()
		{
			if (_publisher != null && !_publisher.IsDisposed)
				return;

			(_publisherPort, _subscriberPort) = GetExchangePorts(_options);
			if (_publisherPort == 0 || _subscriberPort == 0)
				throw new InvalidOperationException(string.Format(Properties.Resources.ZeroQueue_ExchangeUnavailable_Message, _options.Server, _options.Port));

			var publisher = new PublisherSocket();

			try
			{
				publisher.Options.HeartbeatInterval = TimeSpan.FromSeconds(30);
				publisher.Connect(ZeroUtility.GetTcpAddress(_options.Server, _subscriberPort));
				_publisher = publisher;

				if (_options.Heartbeat > TimeSpan.Zero && _timer == null)
				{
					_timer = new NetMQTimer(_options.Heartbeat);
					_timer.Elapsed += this.OnHeartbeat;
					_poller.Add(_timer);
				}
			}
			catch
			{
				publisher.Dispose();
				throw;
			}
		}

		private void Publish(PublishCommand command)
		{
			var publisher = _publisher ?? throw new InvalidOperationException(Properties.Resources.ZeroQueue_PublisherUninitialized_Message);
			var compressor = command.CompressionThreshold > 0 && command.Data.Length > command.CompressionThreshold ? nameof(IO.Compression.Compressor.Brotli) : null;
			var header = Packetizer.Pack(command.Identifier, command.Topic, compressor);
			var data = compressor == null ? command.Data : IO.Compression.Compressor.Compress(compressor, command.Data);
			publisher.SendMoreFrame(header).SendFrame(data);
		}

		private void Subscribe(SubscribeCommand command)
		{
			if (_subscribers.ContainsKey(command.Subscriber))
				return;

			var channel = command.Subscriber.Attach(command.Topic, ZeroUtility.GetTcpAddress(_options.Server, _publisherPort));
			_subscribers.Add(command.Subscriber, channel);
			_poller.Add(channel);
		}

		private void Unsubscribe(ZeroSubscriber subscriber)
		{
			var paused = _paused.Remove(subscriber);

			if (!_subscribers.Remove(subscriber, out var channel))
				channel = subscriber.Detach();
			else
				subscriber.Detach();

			if (channel == null || channel.IsDisposed)
				return;

			channel.ReceiveReady -= subscriber.OnReceiveReady;
			if (paused)
				channel.Dispose();
			else
				_poller.RemoveAndDispose(channel);
		}

		private void PauseCore(ZeroSubscriber subscriber, Message message)
		{
			if (!_subscribers.TryGetValue(subscriber, out var channel) || !_paused.Add(subscriber))
				return;

			subscriber.SetPending(message);
			_poller.Remove(channel);
		}

		private void ResumeCore(ZeroSubscriber subscriber)
		{
			if (!_paused.Contains(subscriber) || !subscriber.TryDispatchPending())
				return;

			_paused.Remove(subscriber);
			if (_subscribers.TryGetValue(subscriber, out var channel) && !channel.IsDisposed)
				_poller.Add(channel);
		}

		private void OnHeartbeat(object sender, NetMQTimerEventArgs args)
		{
			if (_publisher == null || _publisher.IsDisposed)
				return;

			try
			{
				foreach (var topic in _heartbeatTopics())
					_publisher.SendMoreFrame(Packetizer.Pack(topic)).SendFrameEmpty();
			}
			catch (Exception exception) { Diagnostics.Logging.GetLogging(this).Error(exception); }
		}

		private void OnCommandReady(object sender, NetMQQueueEventArgs<Command> args)
		{
			for(var index = 0; index < BATCH_SIZE && args.Queue.TryDequeue(out var command, TimeSpan.Zero); index++)
			{
				if(command.Cancellation.IsCancellationRequested)
				{
					command.Completion.TrySetCanceled(command.Cancellation);
					continue;
				}

				try
				{
					switch(command)
					{
						case StartCommand:
							this.Start();
							break;
						case PublishCommand publish:
							this.Publish(publish);
							break;
						case SubscribeCommand subscribe:
							this.Subscribe(subscribe);
							break;
						case UnsubscribeCommand unsubscribe:
							this.Unsubscribe(unsubscribe.Subscriber);
							break;
						case PauseCommand pause:
							this.PauseCore(pause.Subscriber, pause.Message);
							break;
						case ResumeCommand resume:
							this.ResumeCore(resume.Subscriber);
							break;
						case StopCommand:
							this.Stop();
							break;
					}

					command.Completion.TrySetResult();
				}
				catch(Exception exception)
				{
					command.Completion.TrySetException(exception);
				}
			}
		}

		private static (ushort publisherPort, ushort subscriberPort) GetExchangePorts(ZeroQueueRuntimeOptions options)
		{
			using var requester = new RequestSocket();
			requester.Connect(ZeroUtility.GetTcpAddress(options.Server, options.Port));
			requester.SendFrameEmpty();

			if (!requester.TryReceiveFrameString(options.Timeout, out var response) || string.IsNullOrEmpty(response))
				return default;

			ushort publisher = 0;
			ushort subscriber = 0;

			foreach (var entry in response.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
			{
				var index = entry.IndexOf('=');
				if (index <= 0 || index == entry.Length - 1)
					continue;

				var name = entry.AsSpan(0, index);
				var value = entry.AsSpan(index + 1);

				if (name.Equals("Publisher", StringComparison.OrdinalIgnoreCase))
					ushort.TryParse(value, out publisher);
				else if (name.Equals("Subscriber", StringComparison.OrdinalIgnoreCase))
					ushort.TryParse(value, out subscriber);
			}

			return (publisher, subscriber);
		}
		#endregion

		#region 异步释放
		public async ValueTask DisposeAsync()
		{
			if (Interlocked.Exchange(ref _disposed, 1) != 0)
				return;

			var stop = new StopCommand();
			_commands.Enqueue(stop);
			await stop.Completion.Task;


			if (_poller.IsRunning)
				_poller.Stop();

			await _runner;

			_commands.ReceiveReady -= this.OnCommandReady;
			_poller.Remove(_commands);
			_commands.Dispose();
			_poller.Dispose();
		}
		#endregion

		#region 嵌套子类
		private abstract class Command(CancellationToken cancellation = default)
		{
			public CancellationToken Cancellation { get; } = cancellation;
			public TaskCompletionSource Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		}

		private sealed class StopCommand : Command;
		private sealed class StartCommand(CancellationToken cancellation) : Command(cancellation);

		private sealed class PublishCommand(string topic, string identifier, byte[] data, int compressionThreshold, CancellationToken cancellation) : Command(cancellation)
		{
			public string Topic { get; } = topic;
			public string Identifier { get; } = identifier;
			public byte[] Data { get; } = data;
			public int CompressionThreshold { get; } = compressionThreshold;
		}
		private sealed class SubscribeCommand(ZeroSubscriber subscriber, string topic, CancellationToken cancellation) : Command(cancellation)
		{
			public ZeroSubscriber Subscriber { get; } = subscriber;
			public string Topic { get; } = topic;
		}
		private sealed class UnsubscribeCommand(ZeroSubscriber subscriber, CancellationToken cancellation) : Command(cancellation)
		{
			public ZeroSubscriber Subscriber { get; } = subscriber;
		}
		private sealed class PauseCommand(ZeroSubscriber subscriber, Message message) : Command
		{
			public ZeroSubscriber Subscriber { get; } = subscriber;
			public Message Message { get; } = message;
		}
		private sealed class ResumeCommand(ZeroSubscriber subscriber) : Command
		{
			public ZeroSubscriber Subscriber { get; } = subscriber;
		}
		#endregion
	}
}
