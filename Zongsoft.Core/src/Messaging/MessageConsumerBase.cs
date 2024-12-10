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
 * Copyright (C) 2010-2023 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Components;
using Zongsoft.Collections;
using System.Runtime.CompilerServices;

namespace Zongsoft.Messaging
{
	public abstract class MessageConsumerBase<TQueue> : IMessageConsumer where TQueue : IMessageQueue
	{
		#region 事件定义
		public event EventHandler<EventArgs> Unsubscribed;
		#endregion

		#region 成员字段
		private IHandler<Message> _handler;
		private volatile int _unsubscribed;
		#endregion

		#region 构造函数
		protected MessageConsumerBase(TQueue queue, IHandler<Message> handler, MessageSubscribeOptions options = null) : this(queue, null, [], handler, options) { }
		protected MessageConsumerBase(TQueue queue, string topic, IHandler<Message> handler, MessageSubscribeOptions options = null) : this(queue, topic, [], handler, options) { }
		protected MessageConsumerBase(TQueue queue, string topic, string tags, IHandler<Message> handler, MessageSubscribeOptions options = null) : this(queue, topic, Slice(tags), handler, options) { }
		protected MessageConsumerBase(TQueue queue, string topic, string[] tags, IHandler<Message> handler, MessageSubscribeOptions options = null)
		{
			this.Queue = queue ?? throw new ArgumentNullException(nameof(queue));
			this.Topic = topic;
			this.Tags = tags;
			this.Options = options;
			_handler = handler;
		}
		#endregion

		#region 公共属性
		public string Topic { get; }
		public string[] Tags { get; }
		public IHandler<Message> Handler => _handler;
		public MessageSubscribeOptions Options { get; }
		public bool IsUnsubscribed => _unsubscribed != 0;
		#endregion

		#region 保护属性
		protected TQueue Queue { get; }
		#endregion

		#region 取消订阅
		public ValueTask UnsubscribeAsync(CancellationToken cancellation = default)
		{
			var unsubscribed = Interlocked.CompareExchange(ref _unsubscribed, 1, 0);
			if(unsubscribed != 0)
				return ValueTask.CompletedTask;

			//执行取消订阅
			var task = this.OnUnsubscribeAsync(cancellation);

			//更新订阅标记(未订阅)
			if(task.IsCompletedSuccessfully)
				InvokeUnsubscribed();
			else
				task.AsTask().ContinueWith(t =>
				{
					if(t.IsCompletedSuccessfully)
						InvokeUnsubscribed();
				}, cancellation);

			return task;

			void InvokeUnsubscribed()
			{
				this.Unsubscribed?.Invoke(this, EventArgs.Empty);
			}
		}

		protected abstract ValueTask OnUnsubscribeAsync(CancellationToken cancellation);
		#endregion

		#region 私有方法
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private static string[] Slice(string text) => string.IsNullOrEmpty(text) ? [] :
			text.Split([',', ';'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
		#endregion

		#region 资源释放
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			var handler = Interlocked.Exchange(ref _handler, null);
			if(handler == null)
				return;

			if(disposing)
				this.UnsubscribeAsync().AsTask().ConfigureAwait(false);
		}
		#endregion
	}
}
