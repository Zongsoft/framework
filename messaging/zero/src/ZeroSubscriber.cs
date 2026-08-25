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
using System.Threading.Channels;

using NetMQ;
using NetMQ.Sockets;

using Zongsoft.Services;
using Zongsoft.Components;

namespace Zongsoft.Messaging.ZeroMQ;

public sealed class ZeroSubscriber : MessageConsumerBase<ZeroQueue>
{
	#region 常量定义
	private const int CAPACITY = 1000;
	#endregion

	#region 私有成员
	private readonly Channel<Message> _messages;
	private readonly CancellationTokenSource _cancellation = new();
	private readonly Task _worker;
	private SubscriberSocket _channel;
	private TaskCompletionSource _synchronization;
	private Message? _pending;
	private int _closed;
	#endregion

	#region 构造函数
	internal ZeroSubscriber(ZeroQueue queue, string topic, IHandler<Message> handler, MessageSubscribeOptions options = null) : base(queue, topic, handler, options)
	{
		_messages = System.Threading.Channels.Channel.CreateBounded<Message>(new BoundedChannelOptions(CAPACITY)
		{
			SingleReader = true,
			SingleWriter = true,
			FullMode = BoundedChannelFullMode.Wait,
		});
		_worker = Task.Run(this.ProcessAsync);
	}
	#endregion

	#region 公共属性
	internal SubscriberSocket Channel => Volatile.Read(ref _channel);
	#endregion

	#region 内部方法
	internal SubscriberSocket Attach(string topic, string address)
	{
		var channel = new SubscriberSocket();
		channel.Options.ReceiveHighWatermark = CAPACITY;
		channel.Options.HeartbeatInterval = TimeSpan.FromSeconds(30);
		channel.ReceiveReady += this.OnReceiveReady;
		channel.Subscribe(topic);
		channel.Subscribe(ZeroQueueServer.WELCOME_MESSAGE);
		channel.Connect(address);

		_synchronization = new(TaskCreationOptions.RunContinuationsAsynchronously);
		Volatile.Write(ref _channel, channel);
		return channel;
	}

	internal SubscriberSocket Detach()
	{
		Interlocked.Exchange(ref _synchronization, null)?.TrySetCanceled();
		return Interlocked.Exchange(ref _channel, null);
	}

	internal async ValueTask SynchronizeAsync(TimeSpan timeout, CancellationToken cancellation)
	{
		var synchronization = _synchronization;
		if(synchronization != null)
			await synchronization.Task.WaitAsync(timeout, cancellation);
	}

	internal void SetPending(Message message) => _pending = message;
	internal void MarkSynchronized() => Interlocked.Exchange(ref _synchronization, null)?.TrySetResult();
	internal bool Dispatch(Message message)
	{
		if(_messages.Writer.TryWrite(message))
			return true;

		this.Queue.Pause(this, message);
		return false;
	}

	internal bool TryDispatchPending()
	{
		if(!_pending.HasValue || !_messages.Writer.TryWrite(_pending.Value))
			return false;

		_pending = null;
		return true;
	}
	#endregion

	#region 重写方法
	protected override async ValueTask OnCloseAsync(CancellationToken cancellation)
	{
		if(Interlocked.Exchange(ref _closed, 1) != 0)
			return;

		Interlocked.Exchange(ref _synchronization, null)?.TrySetCanceled(cancellation);
		await this.Queue.UnsubscribeAsync(this, CancellationToken.None);
		_messages.Writer.TryComplete();
		_cancellation.Cancel();

		try { await _worker.WaitAsync(this.Queue.Timeout); }
		catch(OperationCanceledException) { }
		catch(TimeoutException exception) { Diagnostics.Logging.GetLogging(this).Warn(exception.Message); }

		_cancellation.Dispose();
	}
	#endregion

	#region 事件处理
	internal void OnReceiveReady(object sender, NetMQSocketEventArgs args)
	{
		try
		{
			var round = Math.Max(args.Socket.Options.ReceiveHighWatermark, 100);

			for(var index = 0; index < round; index++)
			{
				if(!args.Socket.TryReceiveFrameString(out var header, out var more))
					break;

				if(!more)
				{
					if(header == ZeroQueueServer.WELCOME_MESSAGE)
						Interlocked.Exchange(ref _synchronization, null)?.TrySetResult();
					continue;
				}

				if(string.IsNullOrEmpty(header) || !Packetizer.TryUnpack(header, out var identifier, out var topic, out var options))
				{
					SkipRemainingFrames(args.Socket, more);
					continue;
				}

				if(!args.Socket.TryReceiveFrameBytes(out var data, out more))
					break;

				if(more)
				{
					SkipRemainingFrames(args.Socket, true);
					continue;
				}

				if(!this.Queue.Validate(identifier))
					continue;

				if(string.IsNullOrEmpty(identifier) && (data == null || data.Length == 0))
					continue;

				if(Packetizer.Options.TryGetValue(options, Packetizer.Options.Compressor, out var compressor))
				{
					if(!string.Equals(compressor, nameof(IO.Compression.Compressor.Brotli), StringComparison.OrdinalIgnoreCase))
						continue;

					data = IO.Compression.Compressor.Decompress(compressor, data);
				}

				if(!this.Dispatch(new Message(this.Queue.GetLogicalTopic(topic), data ?? [])))
					return;

				if(!args.Socket.HasIn)
					break;
			}
		}
		catch(Exception exception)
		{
			Diagnostics.Logging.GetLogging(this).Error(exception);
		}

		static void SkipRemainingFrames(NetMQSocket socket, bool more)
		{
			while(more && socket.TrySkipFrame(out more)) { }
		}
	}
	#endregion

	#region 私有方法
	private async Task ProcessAsync()
	{
		try
		{
			await foreach(var message in _messages.Reader.ReadAllAsync(_cancellation.Token))
			{
				this.Queue.Resume(this);

				try
				{
					var handler = this.Handler;
					if(handler != null)
						await handler.HandleAsync(message, null, _cancellation.Token);
				}
				catch(OperationCanceledException) when(_cancellation.IsCancellationRequested) { }
				catch(Exception exception) { Diagnostics.Logging.GetLogging(this).Error(exception); }
			}
		}
		catch(OperationCanceledException) when(_cancellation.IsCancellationRequested) { }
	}
	#endregion
}
