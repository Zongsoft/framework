/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
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
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Messaging
{
	/// <summary>
	/// 表示队列消息的结构。
	/// </summary>
	public struct MessageQueueMessage
	{
		#region 成员字段
		private readonly Delegate _acknowledger;
		#endregion

		#region 构造函数
		public MessageQueueMessage(IMessageQueue queue, byte[] data) : this(queue, null, data, (Delegate)null) { }
		public MessageQueueMessage(IMessageQueue queue, byte[] data, Action acknowledger) : this(queue, null, data, acknowledger) { }
		public MessageQueueMessage(IMessageQueue queue, byte[] data, Action<TimeSpan> acknowledger) : this(queue, null, data, acknowledger) { }
		public MessageQueueMessage(IMessageQueue queue, byte[] data, Func<CancellationToken, Task> acknowledger) : this(queue, null, data, acknowledger) { }
		public MessageQueueMessage(IMessageQueue queue, byte[] data, Func<TimeSpan, CancellationToken, Task> acknowledger) : this(queue, null, data, acknowledger) { }

		public MessageQueueMessage(IMessageQueue queue, string identifier, byte[] data) : this(queue, identifier, data, (Delegate)null) { }
		public MessageQueueMessage(IMessageQueue queue, string identifier, byte[] data, Action acknowledger) : this(queue, identifier, data, (Delegate)acknowledger) { }
		public MessageQueueMessage(IMessageQueue queue, string identifier, byte[] data, Action<TimeSpan> acknowledger) : this(queue, identifier, data, (Delegate)acknowledger) { }
		public MessageQueueMessage(IMessageQueue queue, string identifier, byte[] data, Func<CancellationToken, Task> acknowledger) : this(queue, identifier, data, (Delegate)acknowledger) { }
		public MessageQueueMessage(IMessageQueue queue, string identifier, byte[] data, Func<TimeSpan, CancellationToken, Task> acknowledger) : this(queue, identifier, data, (Delegate)acknowledger) { }

		private MessageQueueMessage(IMessageQueue queue, string identifier, byte[] data, Delegate acknowledger)
		{
			this.Queue = queue;
			this.Data = data;
			this.Identifier = identifier;
			this.Identity = null;
			this.Timestamp = DateTime.UtcNow;
			_acknowledger = acknowledger;
		}
		#endregion

		#region 公共属性
		/// <summary>获取所属队列对象。</summary>
		public IMessageQueue Queue { get; }

		/// <summary>获取或设置消息内容。</summary>
		public byte[] Data { get; set; }

		/// <summary>获取或设置消息的身份标识。</summary>
		public string Identity { get; set; }

		/// <summary>获取或设置消息的标识符。</summary>
		public string Identifier { get; set; }

		/// <summary>获取或设置消息时间戳。</summary>
		public DateTime Timestamp { get; set; }
		#endregion

		#region 公共方法
		public void Acknowledge() => this.Acknowledge(TimeSpan.Zero);
		public void Acknowledge(TimeSpan delay)
		{
			var acknowledger = _acknowledger;

			if(acknowledger == null)
				return;

			if(acknowledger.Method.ReturnType == null || acknowledger.Method.ReturnType == typeof(void))
			{
				if(acknowledger.Method.GetParameters().Length == 0)
					acknowledger.DynamicInvoke();
				else
					acknowledger.DynamicInvoke(delay);
			}
			else
			{
				if(acknowledger.Method.GetParameters().Length == 1)
					((Task)acknowledger.DynamicInvoke(CancellationToken.None)).GetAwaiter().GetResult();
				else
					((Task)acknowledger.DynamicInvoke(delay, CancellationToken.None)).GetAwaiter().GetResult();
			}
		}

		public Task AcknowledgeAsync(CancellationToken cancellation = default) => this.AcknowledgeAsync(TimeSpan.Zero, cancellation);
		public Task AcknowledgeAsync(TimeSpan delay, CancellationToken cancellation = default)
		{
			var acknowledger = _acknowledger;

			if(acknowledger == null)
				return Task.CompletedTask;

			if(_acknowledger.Method.ReturnType == null || _acknowledger.Method.ReturnType == typeof(void))
			{
				if(acknowledger.Method.GetParameters().Length == 0)
					acknowledger.DynamicInvoke();
				else
					acknowledger.DynamicInvoke(delay);

				return Task.CompletedTask;
			}

			return acknowledger.Method.GetParameters().Length == 1 ? (Task)_acknowledger.DynamicInvoke(cancellation) : (Task)_acknowledger.DynamicInvoke(delay, cancellation);
		}
		#endregion
	}
}
