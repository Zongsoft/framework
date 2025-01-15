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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Messaging.RabbitMQ library.
 *
 * The Zongsoft.Messaging.RabbitMQ is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Messaging.RabbitMQ is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Messaging.RabbitMQ library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections;
using System.Collections.Generic;

using Zongsoft.Services;
using Zongsoft.Configuration;

namespace Zongsoft.Messaging.RabbitMQ;

[Service(typeof(IMessageQueueProvider))]
public class RabbitQueueProvider() : MessageQueueProviderBase(RabbitQueue.NAME)
{
	#region 重写方法
	public override bool Exists(string name)
	{
		var connectionSettings = ApplicationContext.Current?.Configuration.GetOption<ConnectionSettingsCollection>("/Messaging/ConnectionSettings");
		return connectionSettings != null && connectionSettings.Contains(name, this.Name);
	}

	protected override IMessageQueue OnCreate(string name, IEnumerable<KeyValuePair<string, string>> settings)
	{
		var connectionSettings = ApplicationContext.Current?.Configuration.GetConnectionSettings("/Messaging/ConnectionSettings", name, this.Name);
		if(connectionSettings == null)
			throw new ConfigurationException($"The specified {this.Name} message queue connection setting named '{name}' was not found.");

		if(settings != null)
		{
			foreach(var setting in settings)
				connectionSettings.Properties[setting.Key] = setting.Value;
		}

		return new RabbitQueue(name, connectionSettings);
	}
	#endregion
}
