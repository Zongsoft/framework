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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Confluent.Kafka;

using Zongsoft.Common;
using Zongsoft.Components;
using Zongsoft.Configuration;

namespace Zongsoft.Messaging.Kafka
{
	internal static class KafkaUtility
	{
		public static ProducerConfig GetProducerOptions(IConnectionSettings settings)
		{
			if(settings == null)
				return null;

			var config = new ProducerConfig
			{
				ClientId = settings.Client,
				BootstrapServers = settings.Server
			};

			foreach(var setting in settings)
				config.Set(setting.Key, setting.Value);

			return config;
		}

		public static ConsumerConfig GetConsumerOptions(IConnectionSettings settings)
		{
			if(settings == null)
				return null;

			var config = new ConsumerConfig
			{
				GroupId = settings.Group,
				ClientId = settings.Client,
				BootstrapServers = settings.Server
			};

			foreach(var setting in settings)
				config.Set(setting.Key, setting.Value);

			return config;
		}
	}
}
