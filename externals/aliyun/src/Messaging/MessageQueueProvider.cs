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
using System.Collections;
using System.Collections.Generic;

using Zongsoft.Services;
using Zongsoft.Messaging;

namespace Zongsoft.Externals.Aliyun.Messaging
{
	[Service(typeof(IMessageQueueProvider))]
	public class MessageQueueProvider : IMessageQueueProvider, IEnumerable<MessageQueue>
	{
		#region 成员字段
		private readonly Dictionary<string, MessageQueue> _queues = new Dictionary<string, MessageQueue>(StringComparer.OrdinalIgnoreCase);
		#endregion

		#region 公共属性
		public string Name => "Aliyun.Queue";
		public int Count => _queues.Count;
		#endregion

		#region 公共方法
		public IMessageQueue GetQueue(string name)
		{
			if(string.IsNullOrEmpty(name))
				return null;

			if(_queues.TryGetValue(name, out var queue) && queue != null)
				return queue;

			lock(_queues)
			{
				if(_queues.TryGetValue(name, out queue) && queue != null)
					return queue;

				var options = MessageUtility.GetOptions();

				if(options != null && options.Queues.TryGet(name, out var option))
					_queues.Add(option.Name, queue = new MessageQueue(option.Name));

				return queue;
			}
		}
		#endregion

		#region 遍历枚举
		public IEnumerator<MessageQueue> GetEnumerator() => _queues.Values.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => _queues.Values.GetEnumerator();
		#endregion
	}
}
