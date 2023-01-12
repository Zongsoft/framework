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
	[Service(typeof(IMessageQueueProvider))]
	public class MqttQueueProvider : MessageQueueProviderBase
	{
		#region 构造函数
		public MqttQueueProvider() : base("Mqtt") { }
		#endregion

		#region 重写方法
		protected override IMessageQueue OnCreate(string name, IEnumerable<KeyValuePair<string, string>> settings)
		{
			var connectionSetting = ApplicationContext.Current?.Configuration.GetOption<ConnectionSetting>("/Messaging/Mqtt/ConnectionSettings/" + name);
			if(connectionSetting == null)
				return null;

			if(settings != null)
			{
				foreach(var setting in settings)
					connectionSetting.Properties[setting.Key] = setting.Value;
			}

			return new MqttQueue(name, connectionSetting);
		}
		#endregion
	}
}
