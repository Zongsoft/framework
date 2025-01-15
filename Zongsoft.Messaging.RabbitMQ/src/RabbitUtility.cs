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
using System.Collections.Generic;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

using Zongsoft.Common;
using Zongsoft.Configuration;

namespace Zongsoft.Messaging.RabbitMQ;

internal static class RabbitUtility
{
	private static readonly Dictionary<string, string> ProducerConfigurationMapping = new()
	{
		{ "Topic", null },
		{ "SecurityProtocol", "security.protocol" },
	};

	private static readonly Dictionary<string, string> ConsumerConfigurationMapping = new()
	{
		{ "Topic", null },
		{ "SecurityProtocol", "security.protocol" },
	};

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
		{
			if(ProducerConfigurationMapping.TryGetValue(setting.Key, out var key) && key != null)
				config.Set(key, setting.Value);
		}

		return config;
	}

	public static ConsumerConfig GetConsumerOptions(IConnectionSettings settings)
	{
		if(settings == null)
			return null;

		var config = new ConsumerConfig
		{
			GroupId = string.IsNullOrEmpty(settings.Group) ? $"G{Randomizer.GenerateString()}" : settings.Group,
			ClientId = settings.Client,
			BootstrapServers = settings.Server
		};

		foreach(var setting in settings)
		{
			if(ConsumerConfigurationMapping.TryGetValue(setting.Key, out var key) && key != null)
				config.Set(key, setting.Value);
		}

		return config;
	}
}
