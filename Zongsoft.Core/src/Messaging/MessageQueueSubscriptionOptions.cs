﻿/*
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

namespace Zongsoft.Messaging
{
	public class MessageQueueSubscriptionOptions
	{
		#region 单例字段
		public static readonly MessageQueueSubscriptionOptions Default = new MessageQueueSubscriptionOptions();
		#endregion

		#region 构造函数
		public MessageQueueSubscriptionOptions(MessageReliability reliability = MessageReliability.MostOnce)
		{
			this.Reliability = reliability;
		}
		#endregion

		#region 公共属性
		/// <summary>获取或设置订阅消息回调的可靠性。</summary>
		public MessageReliability Reliability { get; set; }

		/// <summary>获取或设置订阅回调失败的重试策略。</summary>
		public MessageSubscriptionFallbackBehavior FallbackBehavior { get; set; }
		#endregion
	}
}