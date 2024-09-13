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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.Redis library.
 *
 * The Zongsoft.Externals.Redis is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Redis is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Redis library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;

using Zongsoft.Configuration;

namespace Zongsoft.Externals.Redis.Configuration
{
	public sealed class RedisConnectionSettingsModeler : IConnectionSettingsModeler<StackExchange.Redis.ConfigurationOptions>
	{
		object IConnectionSettingsModeler.Model(IConnectionSettings settings) => this.Model(settings);
		public StackExchange.Redis.ConfigurationOptions Model(IConnectionSettings settings)
		{
			if(!settings.IsDriver(RedisConnectionSettingsDriver.NAME))
				throw new ConfigurationException($"The specified '{settings}' connection settings is not a Redis configuration.");

			var host = settings.Server;
			if(settings.Port != 0)
				host += $":{settings.Port}";

			var entries = Enumerable.Empty<string>();

			if(settings.Driver.Mapper != null && settings.Driver.Mapper.Mapping != null)
			{
				entries = settings.Driver.Mapper.Mapping
					.Where(entry =>
						!string.IsNullOrEmpty(entry.Key) &&
						!entry.Key.Equals(nameof(ConnectionSettings.Server), StringComparison.OrdinalIgnoreCase) &&
						!entry.Key.Equals(nameof(ConnectionSettings.Port), StringComparison.OrdinalIgnoreCase))
					.Select(entry => $"{entry.Key}={entry.Value}");
			}

			return StackExchange.Redis.ConfigurationOptions.Parse(entries.Any() ? host + ',' + string.Join(',', entries) : host, true);
		}
	}
}
