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
	/// <summary>
	/// 表示主题消息的结构。
	/// </summary>
	public struct TopicMessage
	{
		#region 构造函数
		public TopicMessage(IMessageTopic topic, byte[] data, string tags = null)
		{
			this.Topic = topic;
			this.Data = data;
			this.Tags = tags;
			this.Identifier = null;
			this.Identity = null;
			this.Timestamp = DateTime.UtcNow;
			this.Description = null;
		}
		#endregion

		#region 公共属性
		/// <summary>获取或设置消息主题。</summary>
		public IMessageTopic Topic { get; }

		/// <summary>获取或设置消息内容。</summary>
		public byte[] Data { get; set; }

		/// <summary>获取或设置主题标签。</summary>
		public string Tags { get; set; }

		/// <summary>获取或设置消息的身份标识。</summary>
		public string Identity { get; set; }

		/// <summary>获取或设置消息的标识符。</summary>
		public string Identifier { get; set; }

		/// <summary>获取或设置消息时间戳。</summary>
		public DateTime Timestamp { get; set; }

		/// <summary>获取或设置消息描述信息。</summary>
		public string Description { get; set; }
		#endregion
	}
}
