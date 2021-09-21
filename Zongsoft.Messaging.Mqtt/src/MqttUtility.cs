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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using MQTTnet;
using MQTTnet.Protocol;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Client.Receiving;
using MQTTnet.Client.Publishing;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Extensions.ManagedClient;

namespace Zongsoft.Messaging.Mqtt
{
	public static class MqttUtility
	{
		public static MqttQualityOfServiceLevel ToQoS(this MessageReliability reliability) => reliability switch
		{
			MessageReliability.MostOnce => MqttQualityOfServiceLevel.AtMostOnce,
			MessageReliability.LeastOnce => MqttQualityOfServiceLevel.AtLeastOnce,
			MessageReliability.ExactlyOnce => MqttQualityOfServiceLevel.ExactlyOnce,
			_ => MqttQualityOfServiceLevel.AtMostOnce,
		};

		public static IMqttClientOptions GetOptions(IMessageTopicOptions options = null)
		{
			if(options == null)
				throw new ArgumentNullException(nameof(options));

			var clientId = options.ConnectionSettings.Values.Client;

			//确保ClientId不为空
			if(string.IsNullOrWhiteSpace(clientId))
				clientId = 'C' + Zongsoft.Common.Randomizer.GenerateString();

			return new MqttClientOptionsBuilder()
				.WithClientId(clientId)
				.WithTcpServer(options.ConnectionSettings.Values.Server)
				.WithCredentials(options.ConnectionSettings.Values.UserName, options.ConnectionSettings.Values.Password)
				.Build();
		}

		public async static Task EnsureStart(this IManagedMqttClient client, IMessageTopicOptions options = null)
		{
			if(client == null)
				throw new ArgumentNullException(nameof(client));

			if(client.IsStarted)
				return;

			var mqttOptions = new ManagedMqttClientOptionsBuilder()
				.WithAutoReconnectDelay(TimeSpan.FromSeconds(10))
				.WithClientOptions(GetOptions(options))
				.Build();

			await client.StartAsync(mqttOptions);
		}
	}
}
