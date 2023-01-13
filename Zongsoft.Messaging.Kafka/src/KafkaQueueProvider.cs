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
 * This file is part of Zongsoft.Messaging.Kafka library.
 *
 * The Zongsoft.Messaging.Kafka is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Messaging.Kafka is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Messaging.Kafka library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections;
using System.Collections.Generic;

using Zongsoft.Services;
using Zongsoft.Configuration;

namespace Zongsoft.Messaging.Kafka
{
	[Service(typeof(IMessageQueueProvider))]
	public class KafkaQueueProvider : MessageQueueProviderBase
	{
		#region 构造函数
		public KafkaQueueProvider() : base("Kafka") { }
		#endregion

		#region 重写方法
		public override bool Exists(string name)
		{
			var connectionSettings = ApplicationContext.Current?.Configuration.GetOption<ConnectionSettingCollection>("/Messaging/ConnectionSettings");
			return connectionSettings != null && (string.IsNullOrEmpty(name) ? connectionSettings.Contains(connectionSettings.Default) : connectionSettings.Contains(name));
		}

		protected override IMessageQueue OnCreate(string name, IEnumerable<KeyValuePair<string, string>> settings)
		{
			var connectionSettings = ApplicationContext.Current?.Configuration.GetOption<ConnectionSettingCollection>("/Messaging/ConnectionSettings");
			if(connectionSettings == null || !connectionSettings.TryGet(name, out var connectionSetting))
				return null;

			//如果指定的连接设置的driver属性有值，但却不匹配当前消息队列提供程序名则抛出异常
			if(!string.IsNullOrEmpty(connectionSetting.Driver) &&
			   !connectionSetting.Driver.Equals(this.Name, StringComparison.OrdinalIgnoreCase) &&
			   !connectionSetting.Driver.EndsWith($".{this.Name}", StringComparison.OrdinalIgnoreCase))
				throw new ConfigurationException($"The specified name '{name}' is not the connection setting for the '{this.Name}' message queue provider because its driver is '{connectionSetting.Driver}'.");

			if(settings != null)
			{
				foreach(var setting in settings)
					connectionSetting.Properties[setting.Key] = setting.Value;
			}

			return new KafkaQueue(name, connectionSetting);
		}
		#endregion
	}
}
