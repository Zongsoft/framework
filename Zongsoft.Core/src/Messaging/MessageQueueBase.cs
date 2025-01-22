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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Zongsoft.Components;
using Zongsoft.Collections;
using Zongsoft.Configuration;

namespace Zongsoft.Messaging
{
	public abstract class MessageQueueBase<TSubscriber> : IMessageQueue where TSubscriber : IMessageConsumer
	{
		#region 常量定义
		private const int DISPOSED = -1;
		private const int DISPOSING = 1;
		#endregion

		#region 成员字段
		private volatile int _disposing;
		#endregion

		#region 构造函数
		protected MessageQueueBase(string name, IConnectionSettings connectionSettings = null)
		{
			this.Name = name ?? string.Empty;
			this.ConnectionSettings = connectionSettings;
			this.Subscribers = new SubscriberCollection();
		}
		#endregion

		#region 公共属性
		public string Name { get; }
		public IConnectionSettings ConnectionSettings { get; set; }
		public SubscriberCollection Subscribers { get; }
		public bool IsDisposed => _disposing == DISPOSED;
		#endregion

		#region 生产方法
		public ValueTask<string> ProduceAsync(ReadOnlyMemory<byte> data, MessageEnqueueOptions options = null, CancellationToken cancellation = default) =>
			this.ProduceAsync(null, null, data, options, cancellation);

		public ValueTask<string> ProduceAsync(ReadOnlyMemory<char> data, MessageEnqueueOptions options = null, CancellationToken cancellation = default) =>
			this.ProduceAsync(null, null, data, Encoding.UTF8, options, cancellation);

		public ValueTask<string> ProduceAsync(ReadOnlyMemory<char> data, Encoding encoding, MessageEnqueueOptions options = null, CancellationToken cancellation = default)=>
			this.ProduceAsync(null, null, data, encoding, options, cancellation);

		public ValueTask<string> ProduceAsync(string topic, ReadOnlyMemory<byte> data, MessageEnqueueOptions options = null, CancellationToken cancellation = default) =>
			this.ProduceAsync(topic, null, data, options, cancellation);

		public abstract ValueTask<string> ProduceAsync(string topic, string tags, ReadOnlyMemory<byte> data, MessageEnqueueOptions options = null, CancellationToken cancellation = default);

		public ValueTask<string> ProduceAsync(string topic, ReadOnlyMemory<char> data, MessageEnqueueOptions options = null, CancellationToken cancellation = default) =>
			this.ProduceAsync(topic, null, data, Encoding.UTF8, options, cancellation);

		public ValueTask<string> ProduceAsync(string topic, ReadOnlyMemory<char> data, Encoding encoding, MessageEnqueueOptions options = null, CancellationToken cancellation = default) =>
			this.ProduceAsync(topic, null, data, encoding, options, cancellation);

		public ValueTask<string> ProduceAsync(string topic, string tags, ReadOnlyMemory<char> data, MessageEnqueueOptions options = null, CancellationToken cancellation = default) =>
			this.ProduceAsync(topic, tags, data, Encoding.UTF8, options, cancellation);

		public ValueTask<string> ProduceAsync(string topic, string tags, ReadOnlyMemory<char> data, Encoding encoding, MessageEnqueueOptions options = null, CancellationToken cancellation = default) =>
			this.ProduceAsync(topic, tags, encoding.GetBytes(data.ToString()), options, cancellation);
		#endregion

		#region 订阅方法
		async ValueTask<IMessageConsumer> IMessageQueue.SubscribeAsync(Action<Message> handler, CancellationToken cancellation) => await this.SubscribeAsync(null, null, new HandlerAdapter(handler), null, cancellation);
		async ValueTask<IMessageConsumer> IMessageQueue.SubscribeAsync(Action<Message> handler, MessageSubscribeOptions options, CancellationToken cancellation) => await this.SubscribeAsync(null, null, new HandlerAdapter(handler), options, cancellation);
		async ValueTask<IMessageConsumer> IMessageQueue.SubscribeAsync(IHandler<Message> handler, CancellationToken cancellation) => await this.SubscribeAsync(null, null, handler, null, cancellation);
		async ValueTask<IMessageConsumer> IMessageQueue.SubscribeAsync(IHandler<Message> handler, MessageSubscribeOptions options, CancellationToken cancellation) => await this.SubscribeAsync(null, null, handler, options, cancellation);
		async ValueTask<IMessageConsumer> IMessageQueue.SubscribeAsync(string topic, Action<Message> handler, CancellationToken cancellation) => await this.SubscribeAsync(topic, null, new HandlerAdapter(handler), null, cancellation);
		async ValueTask<IMessageConsumer> IMessageQueue.SubscribeAsync(string topic, Action<Message> handler, MessageSubscribeOptions options, CancellationToken cancellation) => await this.SubscribeAsync(topic, null, new HandlerAdapter(handler), options, cancellation);
		async ValueTask<IMessageConsumer> IMessageQueue.SubscribeAsync(string topic, IHandler<Message> handler, CancellationToken cancellation) =>await this.SubscribeAsync(topic, null, handler, null, cancellation);
		async ValueTask<IMessageConsumer> IMessageQueue.SubscribeAsync(string topic, IHandler<Message> handler, MessageSubscribeOptions options, CancellationToken cancellation) => await this.SubscribeAsync(topic, null, handler, options, cancellation);
		async ValueTask<IMessageConsumer> IMessageQueue.SubscribeAsync(string topic, string tags, Action<Message> handler, CancellationToken cancellation) => await this.SubscribeAsync(topic, tags, new HandlerAdapter(handler), null, cancellation);
		async ValueTask<IMessageConsumer> IMessageQueue.SubscribeAsync(string topic, string tags, Action<Message> handler, MessageSubscribeOptions options, CancellationToken cancellation) => await this.SubscribeAsync(topic, tags, new HandlerAdapter(handler), options, cancellation);
		async ValueTask<IMessageConsumer> IMessageQueue.SubscribeAsync(string topic, string tags, IHandler<Message> handler, CancellationToken cancellation) => await this.SubscribeAsync(topic, tags, handler, null, cancellation);
		async ValueTask<IMessageConsumer> IMessageQueue.SubscribeAsync(string topic, string tags, IHandler<Message> handler, MessageSubscribeOptions options, CancellationToken cancellation) => await this.SubscribeAsync(topic, tags, handler, options, cancellation);

		public ValueTask<TSubscriber> SubscribeAsync(Action<Message> handler, CancellationToken cancellation = default) => this.SubscribeAsync(null, null, new HandlerAdapter(handler), null, cancellation);
		public ValueTask<TSubscriber> SubscribeAsync(Action<Message> handler, MessageSubscribeOptions options, CancellationToken cancellation = default) => this.SubscribeAsync(null, null, new HandlerAdapter(handler), options, cancellation);
		public ValueTask<TSubscriber> SubscribeAsync(IHandler<Message> handler, CancellationToken cancellation = default) => this.SubscribeAsync(null, null, handler, null, cancellation);
		public ValueTask<TSubscriber> SubscribeAsync(IHandler<Message> handler, MessageSubscribeOptions options, CancellationToken cancellation = default) => this.SubscribeAsync(null, null, handler, options, cancellation);
		public ValueTask<TSubscriber> SubscribeAsync(string topic, Action<Message> handler, CancellationToken cancellation = default) => this.SubscribeAsync(topic, null, new HandlerAdapter(handler), null, cancellation);
		public ValueTask<TSubscriber> SubscribeAsync(string topic, Action<Message> handler, MessageSubscribeOptions options, CancellationToken cancellation = default) => this.SubscribeAsync(topic, null, new HandlerAdapter(handler), options, cancellation);
		public ValueTask<TSubscriber> SubscribeAsync(string topic, IHandler<Message> handler, CancellationToken cancellation = default) => this.SubscribeAsync(topic, null, handler, null, cancellation);
		public ValueTask<TSubscriber> SubscribeAsync(string topic, IHandler<Message> handler, MessageSubscribeOptions options, CancellationToken cancellation = default) => this.SubscribeAsync(topic, null, handler, options, cancellation);
		public ValueTask<TSubscriber> SubscribeAsync(string topic, string tags, Action<Message> handler, CancellationToken cancellation = default) => this.SubscribeAsync(topic, tags, new HandlerAdapter(handler), null, cancellation);
		public ValueTask<TSubscriber> SubscribeAsync(string topic, string tags, Action<Message> handler, MessageSubscribeOptions options, CancellationToken cancellation = default) => this.SubscribeAsync(topic, tags, new HandlerAdapter(handler), options, cancellation);
		public ValueTask<TSubscriber> SubscribeAsync(string topic, string tags, IHandler<Message> handler, CancellationToken cancellation = default) => this.SubscribeAsync(topic, tags, handler, null, cancellation);

		public async ValueTask<TSubscriber> SubscribeAsync(string topic, string tags, IHandler<Message> handler, MessageSubscribeOptions options, CancellationToken cancellation = default)
		{
			//确保主题不为空
			topic ??= string.Empty;

			if(this.Subscribers.TryGetValue(topic, out var subscriber))
				return subscriber;

			if(this.Subscribers.TryAdd(topic, subscriber = await this.CreateSubscriberAsync(topic, tags, handler, options, cancellation)))
			{
				//执行订阅操作，如果订阅成功则挂载其订阅取消事件
				if(await this.OnSubscribeAsync(subscriber, cancellation))
					subscriber.Closed += OnClosed;

				//返回新建的订阅者
				return subscriber;
			}

			return this.Subscribers.TryGetValue(topic, out subscriber) ? subscriber : default;

			void OnClosed(object sender, EventArgs args)
			{
				if(sender is IMessageConsumer consumer)
				{
					consumer.Closed -= OnClosed;

					if(consumer.Topic != null && this.Subscribers.Remove(consumer.Topic, out var subscriber))
						this.OnUnsubscribed(subscriber);
				}
			}
		}

		protected abstract ValueTask<bool> OnSubscribeAsync(TSubscriber subscriber, CancellationToken cancellation);
		protected abstract ValueTask<TSubscriber> CreateSubscriberAsync(string topic, string tags, IHandler<Message> handler, MessageSubscribeOptions options, CancellationToken cancellation);
		protected virtual void OnUnsubscribed(TSubscriber subscriber) { }
		#endregion

		#region 资源释放
		public void Dispose()
		{
			var disposing = Interlocked.CompareExchange(ref _disposing, DISPOSING, 0);
			if(disposing != 0)
				return;

			try
			{
				this.Dispose(true);
				GC.SuppressFinalize(this);
			}
			finally
			{
				_disposing = DISPOSED;
			}
		}

		protected virtual void Dispose(bool disposing) { }
		#endregion

		#region 嵌套子类
		public sealed class SubscriberCollection : IReadOnlyCollection<TSubscriber>
		{
			#region 成员字段
			private readonly ConcurrentDictionary<string, TSubscriber> _subscribers = new();
			#endregion

			#region 公共属性
			public int Count => _subscribers.Count;
			public TSubscriber this[string topic] => _subscribers[topic];
			#endregion

			#region 公共方法
			public bool TryGetValue(string topic, out TSubscriber subscriber) => _subscribers.TryGetValue(topic, out subscriber);
			#endregion

			#region 内部方法
			internal bool TryAdd(string topic, TSubscriber subscriber) => _subscribers.TryAdd(topic, subscriber);
			internal bool Remove(string topic, out TSubscriber subscriber) => _subscribers.TryRemove(topic, out subscriber);
			#endregion

			#region 枚举遍历
			public IEnumerator<TSubscriber> GetEnumerator() => _subscribers.Values.GetEnumerator();
			IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
			#endregion
		}

		private sealed class HandlerAdapter(Action<Message> handler) : HandlerBase<Message>
		{
			private readonly Action<Message> _handler = handler ?? throw new ArgumentNullException(nameof(handler));

			protected override ValueTask OnHandleAsync(Message argument, Parameters parameters, CancellationToken cancellation)
			{
				_handler.Invoke(argument);
				return ValueTask.CompletedTask;
			}
		}
		#endregion
	}
}
