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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Messaging.Kafka library.
 *
 * The Zongsoft.Messaging.Kafka is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Messaging.Kafka is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Messaging.Kafka library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

using Confluent.Kafka;

namespace Zongsoft.Messaging.Kafka;

public class KafkaSubscriber : MessageConsumerBase<KafkaQueue>
{
	#region 私有变量
	private readonly NativeWorker _worker = new();
	private int _closed = 1;
	#endregion

	#region 成员字段
	private Poller _poller;
	private IConsumer<string, byte[]> _consumer;
	#endregion

	#region 构造函数
	public KafkaSubscriber(KafkaQueue queue, string topic, Components.IHandler<Message> handler, MessageSubscribeOptions options = null) : base(queue, topic, handler, options)
	{
		_poller = new Poller(this);
	}
	#endregion

	#region 重写方法
	protected override async ValueTask OnCloseAsync(CancellationToken cancellation)
	{
		if(_poller != null)
			await _poller.StopAsync(cancellation);

		if(Interlocked.Exchange(ref _closed, 1) == 0)
		{
			await _worker.InvokeAsync(() => _consumer?.Close());
		}

	}
	#endregion

	#region 内部方法
	internal async ValueTask SubscribeAsync(IConsumer<string, byte[]> consumer, CancellationToken cancellation)
	{
		ArgumentNullException.ThrowIfNull(consumer);
		await _worker.InvokeAsync(() =>
		{
			try
			{
				cancellation.ThrowIfCancellationRequested();
				if(_consumer != null)
					throw new InvalidOperationException(Properties.Resources.Messaging_TopicAlreadySubscribed_Message);

				consumer.Subscribe(this.Topic);
				_consumer = consumer;
				_closed = 0;
			}
			catch
			{
				consumer.Dispose();
				throw;
			}
		});

		await _poller.StartAsync(cancellation);
	}

	private Message Receive(MessageDequeueOptions options, CancellationToken cancellation)
	{
		CancellationTokenSource source = null;
		ConsumeResult<string, byte[]> result = null;

		if(options.Timeout > TimeSpan.Zero)
		{
			source = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
			source.CancelAfter(options.Timeout);
			cancellation = source.Token;
		}

		try
		{
			//Confluent.Kafka 仅提供同步 Consume，在本订阅拥有的原生工作线程上执行。
			if(_consumer == null)
				throw new InvalidOperationException(Properties.Resources.Messaging_TopicNotSubscribed_Message);

			result = _consumer.Consume(cancellation);
		}
		catch(OperationCanceledException)
		{
			return Message.Empty;
		}
		finally
		{
			source?.Dispose();
		}

		if(result == null || result.IsPartitionEOF)
			return Message.Empty;

		var data = result.Message.Value;
		if(result.Message.Headers?.TryGetLastBytes(KafkaQueue.COMPRESSION_HEADER, out var compression) == true && compression?.Length > 0)
			data = MessageCompression.Decompress(System.Text.Encoding.UTF8.GetString(compression), data);

		//构建接收到的消息
		return new Message(result.Message.Key, result.Topic, data, cancellation => this.CommitAsync(result, cancellation))
		{
			Timestamp = result.Message.Timestamp.UtcDateTime,
		};
	}

	private ValueTask CommitAsync(ConsumeResult<string, byte[]> result, CancellationToken cancellation) => _worker.InvokeAsync(() =>
	{
		cancellation.ThrowIfCancellationRequested();
		_consumer?.Commit(result);
	});
	#endregion

	#region 处置方法
	protected override async ValueTask DisposeAsync(bool disposing)
	{
		try
		{
			await base.DisposeAsync(disposing);
		}
		finally
		{
			var poller = Interlocked.Exchange(ref _poller, null);

			try
			{
				if(poller != null)
					await poller.DisposeAsync();
			}
			finally
			{
				try { await _worker.InvokeAsync(() => Interlocked.Exchange(ref _consumer, null)?.Dispose()); }
				finally { await _worker.DisposeAsync(); }
			}
		}
	}
	#endregion

	#region 嵌套子类
	private sealed class Poller(KafkaSubscriber subscriber) : MessagePollerBase
	{
		protected override ValueTask<Message> ReceiveAsync(MessageDequeueOptions options, CancellationToken cancellation) =>
			subscriber._worker.InvokeAsync(() => subscriber.Receive(options, cancellation));
		protected override ValueTask OnHandleAsync(Message message, CancellationToken cancellation) => subscriber.Handler?.HandleAsync(message, cancellation) ?? ValueTask.CompletedTask;
	}

	//原生消费、提交和关闭必须串行执行；此线程由订阅创建并在异步释放时排空。
	private sealed class NativeWorker : IAsyncDisposable
	{
		private readonly BlockingCollection<Action> _commands = new();
		private readonly TaskCompletionSource _stopped = new(TaskCreationOptions.RunContinuationsAsynchronously);
		private int _disposing;

		public NativeWorker()
		{
			new Thread(this.Run) { IsBackground = true, Name = nameof(KafkaSubscriber) }.Start();
		}

		public ValueTask InvokeAsync(Action action) => new(this.InvokeAsync(() => { action(); return true; }).AsTask());
		public ValueTask<T> InvokeAsync<T>(Func<T> action)
		{
			var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

			try
			{
				_commands.Add(() =>
				{
					try { completion.TrySetResult(action()); }
					catch(OperationCanceledException exception) { completion.TrySetCanceled(exception.CancellationToken); }
					catch(Exception exception) { completion.TrySetException(exception); }
				});
			}
			catch(InvalidOperationException) { completion.TrySetException(new ObjectDisposedException(nameof(KafkaSubscriber))); }

			return new ValueTask<T>(completion.Task);
		}

		private void Run()
		{
			try
			{
				foreach(var command in _commands.GetConsumingEnumerable())
					command();
				_stopped.TrySetResult();
			}
			catch(Exception exception) { _stopped.TrySetException(exception); }
		}

		public ValueTask DisposeAsync()
		{
			if(Interlocked.Exchange(ref _disposing, 1) == 0)
				_commands.CompleteAdding();
			return new ValueTask(_stopped.Task);
		}
	}
	#endregion
}
