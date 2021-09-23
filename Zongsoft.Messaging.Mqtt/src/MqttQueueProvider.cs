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
 * Copyright (C) 2010-2021 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Messaging.Mqtt library.
 *
 * The Zongsoft.Messaging.Mqtt is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Messaging.Mqtt is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Messaging.Mqtt library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections;
using System.Collections.Generic;

using Zongsoft.Services;
using Zongsoft.Configuration;

namespace Zongsoft.Messaging.Mqtt
{
	[Service(typeof(IMessageTopicProvider))]
	public class MqttQueueProvider : IMessageTopicProvider, IEnumerable<MqttQueue>
	{
		#region 成员字段
		private readonly Dictionary<string, MqttQueue> _queues = new Dictionary<string, MqttQueue>(StringComparer.OrdinalIgnoreCase);
		#endregion

		#region 公共属性
		public int Count => _queues.Count;
		#endregion

		#region 公共方法
		public IMessageTopic GetTopic(string name)
		{
			if(string.IsNullOrEmpty(name))
				return null;

			if(_queues.TryGetValue(name, out var queue) && queue != null)
				return queue;

			lock(_queues)
			{
				if(_queues.TryGetValue(name, out queue) && queue != null)
					return queue;

				var setting = ApplicationContext.Current?.Configuration.GetOption<ConnectionSetting>("/Messaging/Mqtt/ConnectionSettings/" + name);

				if(setting == null)
					return null;

				_queues.Add(name, queue = new MqttQueue(name, setting));
				return queue;
			}
		}
		#endregion

		#region 遍历枚举
		public IEnumerator<MqttQueue> GetEnumerator() => _queues.Values.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => _queues.Values.GetEnumerator();
		#endregion
	}
}
