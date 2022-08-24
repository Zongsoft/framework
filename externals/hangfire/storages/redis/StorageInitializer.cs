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
 * Copyright (C) 2010-2022 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.Hangfire library.
 *
 * The Zongsoft.Externals.Hangfire is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Hangfire is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Hangfire library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;

using Hangfire;
using Hangfire.Redis;

using Zongsoft.Services;
using Zongsoft.Configuration;

namespace Zongsoft.Externals.Hangfire.Storages.Redis
{
	[Service(typeof(IApplicationInitializer))]
	public class StorageInitializer : IApplicationInitializer
	{
		public void Initialize(IApplicationContext context)
		{
			var setting = GetConnectionSetting(context.Configuration?.GetOption<ConnectionSettingCollection>("/Externals/Hangfire/ConnectionSettings"));

			if(setting != null)
			{
				var storage = new RedisStorage(setting.Value, new RedisStorageOptions()
				{
					FetchTimeout = TimeSpan.FromSeconds(5),
				});

				GlobalConfiguration.Configuration.UseStorage(storage);
			}
		}

		private static ConnectionSetting GetConnectionSetting(ConnectionSettingCollection settings)
		{
			if(settings == null || settings.Count == 0)
				return null;

			var setting = settings.GetDefault();
			if(setting != null && string.Equals(setting.Driver, "redis", StringComparison.OrdinalIgnoreCase))
				return setting;

			return settings.FirstOrDefault(setting => setting != null && string.Equals(setting.Driver, "redis", StringComparison.OrdinalIgnoreCase));
		}
	}
}
