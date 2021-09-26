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
using System.Collections.Generic;

namespace Zongsoft.Messaging
{
	public class MessageEnqueueOptions
	{
		#region 单例字段
		public static readonly MessageEnqueueOptions Default = new MessageEnqueueOptions();
		#endregion

		#region 构造函数
		public MessageEnqueueOptions(byte priority = 0) : this(MessageReliability.MostOnce, priority) { }
		public MessageEnqueueOptions(MessageReliability reliability, byte priority = 0) : this(TimeSpan.Zero, reliability, priority) { }
		public MessageEnqueueOptions(TimeSpan delay, MessageReliability reliability = MessageReliability.MostOnce, byte priority = 0)
		{
			this.Delay = delay;
			this.Expiry = TimeSpan.Zero;
			this.Priority = priority;
			this.Reliability = reliability;
		}
		#endregion

		#region 公共属性
		/// <summary>获取或设置入队的延迟时长。</summary>
		public TimeSpan Delay { get; set; }

		/// <summary>获取或设置消息的有效期。</summary>
		public TimeSpan Expiry { get; set; }

		/// <summary>获取或设置消息的优先级。</summary>
		public byte Priority { get; set; }

		/// <summary>获取或设置消息的可靠性。</summary>
		public MessageReliability Reliability { get; set; }
		#endregion
	}
}
