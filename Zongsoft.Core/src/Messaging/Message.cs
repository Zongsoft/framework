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

namespace Zongsoft.Messaging
{
	/// <summary>
	/// 表示消息的结构。
	/// </summary>
	public struct Message
	{
		#region 静态字段
		public static readonly Message Empty = new Message();
		#endregion

		#region 成员字段
		private readonly Delegate _acknowledger;
		#endregion

		#region 构造函数
		public Message(string topic, byte[] data) : this(null, topic, data, null, (Delegate)null) { }

		public Message(string topic, byte[] data, Action acknowledger) : this(null, topic, data, null, (Delegate)acknowledger) { }
		public Message(string topic, byte[] data, Action<TimeSpan> acknowledger) : this(null, topic, data, null, (Delegate)acknowledger) { }
		public Message(string topic, byte[] data, string tags, Action acknowledger) : this(null, topic, data, tags, (Delegate)acknowledger) { }
		public Message(string topic, byte[] data, string tags, Action<TimeSpan> acknowledger) : this(null, topic, data, tags, (Delegate)acknowledger) { }

		public Message(string topic, byte[] data, Func<CancellationToken, ValueTask> acknowledger) : this(null, topic, data, null, (Delegate)acknowledger) { }
		public Message(string topic, byte[] data, Func<TimeSpan, CancellationToken, ValueTask> acknowledger) : this(null, topic, data, null, (Delegate)acknowledger) { }
		public Message(string topic, byte[] data, string tags, Func<CancellationToken, ValueTask> acknowledger) : this(null, topic, data, tags, (Delegate)acknowledger) { }
		public Message(string topic, byte[] data, string tags, Func<TimeSpan, CancellationToken, ValueTask> acknowledger) : this(null, topic, data, tags, (Delegate)acknowledger) { }

		public Message(string identifier, string topic, byte[] data) : this(identifier, topic, data, null, (Delegate)null) { }

		public Message(string identifier, string topic, byte[] data, Action acknowledger) : this(identifier, topic, data, null, (Delegate)acknowledger) { }
		public Message(string identifier, string topic, byte[] data, Action<TimeSpan> acknowledger) : this(identifier, topic, data, null, (Delegate)acknowledger) { }
		public Message(string identifier, string topic, byte[] data, string tags, Action acknowledger) : this(identifier, topic, data, tags, (Delegate)acknowledger) { }
		public Message(string identifier, string topic, byte[] data, string tags, Action<TimeSpan> acknowledger) : this(identifier, topic, data, tags, (Delegate)acknowledger) { }

		public Message(string identifier, string topic, byte[] data, Func<CancellationToken, ValueTask> acknowledger) : this(identifier, topic, data, null, (Delegate)acknowledger) { }
		public Message(string identifier, string topic, byte[] data, Func<TimeSpan, CancellationToken, ValueTask> acknowledger) : this(identifier, topic, data, null, (Delegate)acknowledger) { }
		public Message(string identifier, string topic, byte[] data, string tags, Func<CancellationToken, ValueTask> acknowledger) : this(identifier, topic, data, tags, (Delegate)acknowledger) { }
		public Message(string identifier, string topic, byte[] data, string tags, Func<TimeSpan, CancellationToken, ValueTask> acknowledger) : this(identifier, topic, data, tags, (Delegate)acknowledger) { }

		private Message(string identifier, string topic, byte[] data, string tags, Delegate acknowledger)
		{
			this.Topic = topic;
			this.Data = data;
			this.Tags = tags;
			this.Identifier = identifier;
			this.Identity = null;
			this.Timestamp = DateTime.UtcNow;
			_acknowledger = acknowledger;
		}
		#endregion

		#region 公共属性
		/// <summary>获取或设置消息主题。</summary>
		public string Topic { get; }

		/// <summary>获取或设置消息内容。</summary>
		public byte[] Data { get; set; }

		/// <summary>获取或设置消息标签。</summary>
		public string Tags { get; set; }

		/// <summary>获取或设置消息的身份标识。</summary>
		public string Identity { get; set; }

		/// <summary>获取或设置消息的标识符。</summary>
		public string Identifier { get; set; }

		/// <summary>获取或设置消息时间戳。</summary>
		public DateTime Timestamp { get; set; }

		/// <summary>获取一个值，指示消息包是否为空包。</summary>
		[Serialization.SerializationMember(Ignored = true)]
		[System.Text.Json.Serialization.JsonIgnore]
		public bool IsEmpty { get => this.Data == null; }
		#endregion

		#region 公共方法
		/// <summary>应答消息。</summary>
		public readonly void Acknowledge() => this.Acknowledge(TimeSpan.Zero);

		/// <summary>应答消息。</summary>
		/// <param name="delay">指定的应答延迟。</param>
		public readonly void Acknowledge(TimeSpan delay)
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
					((ValueTask)acknowledger.DynamicInvoke(CancellationToken.None)).GetAwaiter().GetResult();
				else
					((ValueTask)acknowledger.DynamicInvoke(delay, CancellationToken.None)).GetAwaiter().GetResult();
			}
		}

		/// <summary>应答消息。</summary>
		/// <param name="cancellation">指定的异步操作取消标记。</param>
		/// <returns>返回的异步任务。</returns>
		public readonly ValueTask AcknowledgeAsync(CancellationToken cancellation = default) => this.AcknowledgeAsync(TimeSpan.Zero, cancellation);

		/// <summary>应答消息。</summary>
		/// <param name="delay">指定的应答延迟。</param>
		/// <param name="cancellation">指定的异步操作取消标记。</param>
		/// <returns>返回的异步任务。</returns>
		public readonly ValueTask AcknowledgeAsync(TimeSpan delay, CancellationToken cancellation = default)
		{
			var acknowledger = _acknowledger;

			if(acknowledger == null)
				return ValueTask.CompletedTask;

			if(_acknowledger.Method.ReturnType == null || _acknowledger.Method.ReturnType == typeof(void))
			{
				if(acknowledger.Method.GetParameters().Length == 0)
					acknowledger.DynamicInvoke();
				else
					acknowledger.DynamicInvoke(delay);

				return ValueTask.CompletedTask;
			}

			return acknowledger.Method.GetParameters().Length == 1 ? (ValueTask)_acknowledger.DynamicInvoke(cancellation) : (ValueTask)_acknowledger.DynamicInvoke(delay, cancellation);
		}
		#endregion
	}
}
