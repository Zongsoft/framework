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

using Zongsoft.Components;
using Zongsoft.Collections;
using Zongsoft.Configuration;

namespace Zongsoft.Messaging
{
	public abstract class MessageQueueBase : IMessageQueue
	{
		#region 构造函数
		protected MessageQueueBase(string name, IConnectionSettings connectionSetting = null)
		{
			this.Name = name ?? string.Empty;
			this.ConnectionSetting = connectionSetting;
		}
		#endregion

		#region 公共属性
		public string Name { get; }
		public IConnectionSettings ConnectionSetting { get; set; }
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
		public ValueTask<IMessageConsumer> SubscribeAsync(Action<Message> handler, CancellationToken cancellation = default) =>
			this.SubscribeAsync(null, null, new HandlerAdapter(handler), null, cancellation);

		public ValueTask<IMessageConsumer> SubscribeAsync(Action<Message> handler, MessageSubscribeOptions options, CancellationToken cancellation = default) =>
			this.SubscribeAsync(null, null, new HandlerAdapter(handler), options, cancellation);

		public ValueTask<IMessageConsumer> SubscribeAsync(IHandler<Message> handler, CancellationToken cancellation = default) =>
			this.SubscribeAsync(null, null, handler, null, cancellation);

		public ValueTask<IMessageConsumer> SubscribeAsync(IHandler<Message> handler, MessageSubscribeOptions options, CancellationToken cancellation = default) =>
			this.SubscribeAsync(null, null, handler, options, cancellation);

		public ValueTask<IMessageConsumer> SubscribeAsync(string topics, Action<Message> handler, CancellationToken cancellation = default) =>
			this.SubscribeAsync(topics, null, new HandlerAdapter(handler), null, cancellation);

		public ValueTask<IMessageConsumer> SubscribeAsync(string topics, Action<Message> handler, MessageSubscribeOptions options, CancellationToken cancellation = default) =>
			this.SubscribeAsync(topics, null, new HandlerAdapter(handler), options, cancellation);

		public ValueTask<IMessageConsumer> SubscribeAsync(string topics, IHandler<Message> handler, CancellationToken cancellation = default) =>
			this.SubscribeAsync(topics, null, handler, null, cancellation);

		public ValueTask<IMessageConsumer> SubscribeAsync(string topics, IHandler<Message> handler, MessageSubscribeOptions options, CancellationToken cancellation = default) =>
			this.SubscribeAsync(topics, null, handler, options, cancellation);

		public ValueTask<IMessageConsumer> SubscribeAsync(string topics, string tags, Action<Message> handler, CancellationToken cancellation = default) =>
			this.SubscribeAsync(topics, tags, new HandlerAdapter(handler), null, cancellation);

		public ValueTask<IMessageConsumer> SubscribeAsync(string topics, string tags, Action<Message> handler, MessageSubscribeOptions options, CancellationToken cancellation = default) =>
			this.SubscribeAsync(topics, tags, new HandlerAdapter(handler), options, cancellation);

		public ValueTask<IMessageConsumer> SubscribeAsync(string topics, string tags, IHandler<Message> handler, CancellationToken cancellation = default) =>
			this.SubscribeAsync(topics, tags, handler, null, cancellation);

		public abstract ValueTask<IMessageConsumer> SubscribeAsync(string topics, string tags, IHandler<Message> handler, MessageSubscribeOptions options, CancellationToken cancellation = default);
		#endregion

		#region 资源释放
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing) { }
		#endregion

		#region 嵌套子类
		private class HandlerAdapter(Action<Message> handler) : HandlerBase<Message>
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
