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
	private sealed partial class Transport : IAsyncDisposable
	{
		#region 常量定义
		private const int BATCH_SIZE = 1024;
		private static readonly TimeSpan TICK_INTERVAL = TimeSpan.FromMilliseconds(100);
		#endregion

		#region 成员字段
		private readonly string _identifier;
		private readonly ZeroQueueRuntimeOptions _options;
		private readonly Func<string[]> _heartbeatTopics;
		private readonly NetMQQueue<Command> _commands;
		private readonly NetMQPoller _poller;
		private readonly ZeroBroadcast _broadcast;
		private readonly ZeroControl _control;
		private readonly Task _runner;
		private readonly List<StartCommand> _starters = [];
		private RequestSocket _discovery;
		private NetMQTimer _timer;
		private string _epoch;
		private ushort _incomingPort;
		private ushort _outgoingPort;
		private ushort _controlPort;
		private DateTime _discoveryDeadline;
		private DateTime _nextDiscovery;
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
			_broadcast = new ZeroBroadcast(options, identifier, _poller, heartbeatTopics, this.OnBroadcastDisconnected);
			_control = new ZeroControl(options, _poller, this.Post);
			_runner = Task.Factory.StartNew(() =>
			{
				if(Thread.CurrentThread.Name == null)
					Thread.CurrentThread.Name = $"{nameof(ZeroQueue)}#{identifier}.Actor";

				_poller.Run(new SynchronizationContext());
			}, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
		}
		#endregion

		#region 公共属性
		public bool HasControl => _controlPort > 0;
		#endregion

		#region 公共方法
		public async ValueTask<string> PublishAsync(string identifier, string topic, string identity, string tags, byte[] data, int compressionThreshold, TimeSpan expiration, MessageReliability reliability, CancellationToken cancellation)
		{
			if(reliability == MessageReliability.LeastOnce)
				return await _control.PublishAsync(identifier, topic, identity, tags, data, expiration, cancellation);

			var command = new PublishCommand(identifier, topic, identity, data, compressionThreshold, cancellation);
			await this.Enqueue(command);
			return command.Published ? identifier : null;
		}

		public ValueTask SubscribeAsync(ZeroSubscriber subscriber, string topic, CancellationToken cancellation) => this.Enqueue(new SubscribeCommand(subscriber, topic, cancellation));
		public ValueTask UnsubscribeAsync(ZeroSubscriber subscriber, CancellationToken cancellation) => this.Enqueue(new UnsubscribeCommand(subscriber, cancellation));
		public ValueTask StartAsync(CancellationToken cancellation) => this.Enqueue(new StartCommand(cancellation));
		public void Pause(ZeroSubscriber subscriber, Message message) => this.PostUnobserved(new PauseCommand(subscriber, message));
		public void Resume(ZeroSubscriber subscriber) => this.PostUnobserved(new ResumeCommand(subscriber));
		#endregion

		#region 命令处理
		private ValueTask Enqueue(Command command)
		{
			this.Post(command);
			return new ValueTask(command.Completion.Task);
		}

		private void Post(Command command)
		{
			if(Volatile.Read(ref _disposed) != 0)
			{
				command.Completion.TrySetException(new ObjectDisposedException(nameof(Transport)));
				return;
			}

			try { _commands.Enqueue(command); }
			catch(Exception exception) { command.Completion.TrySetException(exception); }
		}

		private void PostUnobserved(Command command)
		{
			if(Volatile.Read(ref _disposed) != 0)
				return;

			try { _commands.Enqueue(command); }
			catch(ObjectDisposedException) { }
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
					var completed = command switch
					{
						StartCommand start => this.Start(start),
						PublishCommand publish => this.Publish(publish),
						SubscribeCommand subscribe => this.Subscribe(subscribe),
						UnsubscribeCommand unsubscribe => this.Unsubscribe(unsubscribe.Subscriber),
						PauseCommand pause => _broadcast.Pause(pause.Subscriber, pause.Message),
						ResumeCommand resume => _broadcast.Resume(resume.Subscriber),
						ZeroControl.ControlCommand control => _control.Execute(control),
						StopCommand => this.Stop(),
						_ => true,
					};

					if(completed)
						command.Completion.TrySetResult();
				}
				catch(Exception exception)
				{
					command.Completion.TrySetException(exception);
				}
			}
		}
		#endregion

		#region 启动发现
		private bool Start(StartCommand command)
		{
			if(_broadcast.IsConnected)
				return true;

			_starters.Add(command);
			this.EnsureTimer();
			this.Discover();
			return false;
		}

		private void Discover()
		{
			if(_discovery != null || DateTime.UtcNow < _nextDiscovery)
				return;

			var requester = new RequestSocket();
			try
			{
				requester.Options.HeartbeatInterval = TimeSpan.FromSeconds(30);
				requester.ReceiveReady += this.OnDiscoveryReady;
				requester.Connect(ZeroUtility.GetTcpAddress(_options.Server, _options.Port));
				_poller.Add(requester);
				_discovery = requester;
				_discoveryDeadline = DateTime.UtcNow + _options.Timeout;
				requester.SendFrame($"{ZeroQueueServer.PROTOCOL_NAME}\nProtocol-Version:{ZeroQueueServer.PROTOCOL_VERSION}\nCommand:Discover\nInstance:{_identifier}");
			}
			catch
			{
				this.ReleaseDiscovery(requester);
				throw;
			}
		}

		private void OnDiscoveryReady(object sender, NetMQSocketEventArgs args)
		{
			try
			{
				var response = args.Socket.ReceiveFrameString();
				this.ReleaseDiscovery(args.Socket as RequestSocket);

				if(!TryParseDiscovery(response, out var epoch, out var control, out var incoming, out var outgoing))
					throw new InvalidOperationException(Properties.Resources.ZeroQueue_DiscoveryInvalid_Message);

				var changed = !string.Equals(_epoch, epoch, StringComparison.Ordinal) || _controlPort != control || _incomingPort != incoming || _outgoingPort != outgoing;
				if(changed || !_broadcast.IsConnected)
					this.Connect(epoch, control, incoming, outgoing);

				_nextDiscovery = DateTime.UtcNow + _options.ReconnectInterval;
				this.CompleteStarters();
			}
			catch(Exception exception)
			{
				this.Disconnect();
				this.FailStarters(exception);
				_nextDiscovery = DateTime.UtcNow + _options.ReconnectInterval;
			}
		}

		private void Connect(string epoch, ushort control, ushort incoming, ushort outgoing)
		{
			_control.Disconnect(false);
			_epoch = epoch;
			_controlPort = control;
			_incomingPort = incoming;
			_outgoingPort = outgoing;
			_broadcast.Connect(epoch, incoming, outgoing);
			_control.Connect(control);
		}

		private void OnBroadcastDisconnected()
		{
			_epoch = null;
			_nextDiscovery = DateTime.MinValue;
		}

		private void Disconnect()
		{
			_broadcast.Disconnect(false);
			_control.Disconnect(false);
			_epoch = null;
			_controlPort = 0;
			_incomingPort = 0;
			_outgoingPort = 0;
		}

		private void CompleteStarters()
		{
			foreach(var starter in _starters)
				starter.Completion.TrySetResult();
			_starters.Clear();
		}

		private void FailStarters(Exception exception)
		{
			foreach(var starter in _starters)
				starter.Completion.TrySetException(exception);
			_starters.Clear();
		}

		private void ReleaseDiscovery(RequestSocket requester = null)
		{
			requester ??= _discovery;
			if(requester == null)
				return;

			if(ReferenceEquals(_discovery, requester))
				_discovery = null;
			requester.ReceiveReady -= this.OnDiscoveryReady;
			if(!requester.IsDisposed)
				_poller.RemoveAndDispose(requester);
		}
		#endregion

		#region 发布订阅
		private bool Publish(PublishCommand command)
		{
			command.Published = _broadcast.Publish(command);
			return true;
		}

		private bool Subscribe(SubscribeCommand command)
		{
			if(command.Subscriber.Options?.Reliability == MessageReliability.LeastOnce)
				return _control.Subscribe(command);

			return _broadcast.Subscribe(command);
		}

		private bool Unsubscribe(ZeroSubscriber subscriber)
		{
			if(subscriber.Options?.Reliability == MessageReliability.LeastOnce)
				return _control.Unsubscribe(subscriber);

			return _broadcast.Unsubscribe(subscriber);
		}
		#endregion

		#region 维护管理
		private void EnsureTimer()
		{
			if(_timer != null)
				return;

			_timer = new NetMQTimer(TICK_INTERVAL);
			_timer.Elapsed += this.OnTick;
			_poller.Add(_timer);
			_nextDiscovery = DateTime.MinValue;
		}

		private void OnTick(object sender, NetMQTimerEventArgs args)
		{
			var now = DateTime.UtcNow;
			_control.Tick(now);
			_broadcast.Tick(now);
			if(_discovery != null && now >= _discoveryDeadline)
			{
				this.ReleaseDiscovery();
				this.Disconnect();
				this.FailStarters(new TimeoutException(Properties.Resources.ZeroQueue_DiscoveryTimeout_Message));
				_nextDiscovery = now + _options.ReconnectInterval;
			}

			if(_discovery == null && now >= _nextDiscovery)
			{
				try { this.Discover(); }
				catch(Exception exception)
				{
					this.Disconnect();
					this.FailStarters(exception);
					_nextDiscovery = now + _options.ReconnectInterval;
				}
			}
		}
		#endregion

		#region 停止释放
		private bool Stop()
		{
			_control.Stop();
			_broadcast.Stop();
			if(_timer != null)
			{
				_timer.Enable = false;
				_timer.Elapsed -= this.OnTick;
				_poller.Remove(_timer);
				_timer = null;
			}

			this.ReleaseDiscovery();
			this.FailStarters(new ObjectDisposedException(nameof(Transport)));
			return true;
		}

		public async ValueTask DisposeAsync()
		{
			if(Interlocked.Exchange(ref _disposed, 1) != 0)
				return;

			var stop = new StopCommand();
			_commands.Enqueue(stop);
			await stop.Completion.Task;

			if(_poller.IsRunning)
				_poller.Stop();

			await _runner;
			_commands.ReceiveReady -= this.OnCommandReady;
			_poller.Remove(_commands);
			_commands.Dispose();
			_poller.Dispose();
		}
		#endregion

		#region 协议解析
		private static bool TryParseDiscovery(string response, out string epoch, out ushort control, out ushort incoming, out ushort outgoing)
		{
			epoch = null;
			control = 0;
			incoming = 0;
			outgoing = 0;
			if(string.IsNullOrWhiteSpace(response))
				return false;

			var lines = response.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
			if(lines.Length < 4 || !string.Equals(lines[0], ZeroQueueServer.PROTOCOL_NAME, StringComparison.Ordinal))
				return false;

			var version = false;
			for(var index = 1; index < lines.Length; index++)
			{
				var separator = lines[index].IndexOf(':');
				if(separator <= 0 || separator == lines[index].Length - 1)
					return false;

				var name = lines[index].AsSpan(0, separator);
				var value = lines[index].AsSpan(separator + 1);
				if(name.SequenceEqual("Protocol-Version"))
					version = value.SequenceEqual(ZeroQueueServer.PROTOCOL_VERSION);
				else if(name.SequenceEqual("Epoch"))
					epoch = value.ToString();
				else if(name.SequenceEqual("Control"))
					ushort.TryParse(value, out control);
				else if(name.SequenceEqual("Incoming"))
					ushort.TryParse(value, out incoming);
				else if(name.SequenceEqual("Outgoing"))
					ushort.TryParse(value, out outgoing);
			}

			return version && epoch?.Length == 32 && incoming > 0 && outgoing > 0 && incoming != outgoing &&
				(control == 0 || incoming != control && outgoing != control);
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

		private sealed class PublishCommand(string identifier, string topic, string identity, byte[] data, int compressionThreshold, CancellationToken cancellation) : Command(cancellation)
		{
			public string Identifier { get; } = identifier;
			public string Topic { get; } = topic;
			public string Identity { get; } = identity;
			public byte[] Data { get; } = data;
			public int CompressionThreshold { get; } = compressionThreshold;
			public bool Published { get; set; }
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
