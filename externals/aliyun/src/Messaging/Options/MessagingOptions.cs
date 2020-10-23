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
 * This file is part of Zongsoft.Externals.Aliyun library.
 *
 * The Zongsoft.Externals.Aliyun is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Aliyun is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Aliyun library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;

namespace Zongsoft.Externals.Aliyun.Messaging.Options
{
	/// <summary>
	/// 表示阿里云消息服务的配置选项。
	/// </summary>
	public class MessagingOptions
	{
		#region 构造函数
		public MessagingOptions()
		{
			this.Queues = new QueueOptionCollection();
			this.Topics = new TopicOptionCollection();
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取或设置消息服务的访问标识。
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// 获取消息队列提供程序的配置项。
		/// </summary>
		public QueueOptionCollection Queues { get; }

		/// <summary>
		/// 获取消息主题提供程序的配置项。
		/// </summary>
		public TopicOptionCollection Topics { get; }
		#endregion
	}
}
