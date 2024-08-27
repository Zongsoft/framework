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
using System.Collections;
using System.Collections.Generic;

using Zongsoft.Services;
using Zongsoft.Messaging;

namespace Zongsoft.Externals.Aliyun.Messaging
{
	[Service(typeof(IMessageQueueProvider))]
	public class MessageQueueProvider : MessageQueueProviderBase
	{
		#region 构造函数
		public MessageQueueProvider() : base("Aliyun.MNS") { }
		#endregion

		#region 公共方法
		public override bool Exists(string name)
		{
			var options = MessageUtility.GetOptions();
			return options != null && (options.Queues.Contains(name) || options.Topics.Contains(name));
		}

		protected override IMessageQueue OnCreate(string name, IEnumerable<KeyValuePair<string, string>> settings)
		{
			var options = MessageUtility.GetOptions();

			if(options.Queues.TryGetValue(name, out var queue))
				return new MessageQueue(queue.Name);
			if(options.Topics.TryGetValue(name, out var topic))
				return new MessageTopic(topic.Name);

			return null;
		}
		#endregion
	}
}
