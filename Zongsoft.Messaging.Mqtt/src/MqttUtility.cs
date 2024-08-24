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
using System.Threading;
using System.Threading.Tasks;

using MQTTnet.Client;
using MQTTnet.Protocol;
using MQTTnet.Extensions.ManagedClient;

namespace Zongsoft.Messaging.Mqtt
{
	internal static class MqttUtility
	{
		public static MqttQualityOfServiceLevel ToQoS(this MessageReliability reliability) => reliability switch
		{
			MessageReliability.MostOnce => MqttQualityOfServiceLevel.AtMostOnce,
			MessageReliability.LeastOnce => MqttQualityOfServiceLevel.AtLeastOnce,
			MessageReliability.ExactlyOnce => MqttQualityOfServiceLevel.ExactlyOnce,
			_ => MqttQualityOfServiceLevel.AtMostOnce,
		};

		public static MqttClientOptions GetOptions(Zongsoft.Configuration.IConnectionSetting setting)
		{
			if(setting == null)
				throw new ArgumentNullException(nameof(setting));

			var clientId = setting.Values.Client;

			//确保ClientId不为空
			if(string.IsNullOrWhiteSpace(clientId))
				clientId = 'C' + Zongsoft.Common.Randomizer.GenerateString();

			return new MqttClientOptionsBuilder()
				.WithClientId(clientId)
				.WithTcpServer(setting.Values.Server)
				.WithCredentials(setting.Values.UserName, setting.Values.Password)
				.Build();
		}

		public async static Task EnsureStart(this IManagedMqttClient client, Zongsoft.Configuration.IConnectionSetting setting = null)
		{
			if(client == null)
				throw new ArgumentNullException(nameof(client));

			if(client.IsStarted)
				return;

			var mqttOptions = new ManagedMqttClientOptionsBuilder()
				.WithAutoReconnectDelay(TimeSpan.FromSeconds(10))
				.WithClientOptions(GetOptions(setting))
				.Build();

			//开启客户端
			await client.StartAsync(mqttOptions);

			//确保客户端已连接成功
			if(!client.IsConnected)
			{
				int round = 0;

				//如果没有连接成功，则尝试进行等待一小会
				while(!client.IsConnected && round++ < 10)
				{
					Thread.SpinWait(100 * Environment.ProcessorCount);
				}
			}
		}
	}
}
