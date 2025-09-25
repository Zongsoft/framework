﻿/*
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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Collections.Generic;
using System.Collections.Concurrent;

using Zongsoft.Common;
using Zongsoft.Caching;
using Zongsoft.Services;
using Zongsoft.Configuration;

namespace Zongsoft.Externals.Redis;

[Service(
	typeof(IServiceProvider<ISequence>),
	typeof(IServiceProvider<ISequenceBase>),
	typeof(IServiceProvider<IDistributedCache>),
	typeof(IServiceProvider<Services.Distributing.IDistributedLockManager>))]
public class RedisServiceProvider :
	IServiceProvider<ISequence>,
	IServiceProvider<ISequenceBase>,
	IServiceProvider<IDistributedCache>,
	IServiceProvider<Services.Distributing.IDistributedLockManager>
{
	#region 静态字段
	private static readonly ConcurrentDictionary<string, RedisService> _services = new ConcurrentDictionary<string, RedisService>(StringComparer.OrdinalIgnoreCase);
	#endregion

	#region 公共方法
	public static RedisService GetRedis(string name)
	{
		if(ApplicationContext.Current?.Configuration == null)
			return null;

		return _services.GetOrAdd(name ?? string.Empty, key => new RedisService(key, GetConnectionSettings(key)));

		static IConnectionSettings GetConnectionSettings(string name)
		{
			var settings = ApplicationContext.Current.Configuration.GetOption<ConnectionSettingsCollection>("/Externals/Redis/ConnectionSettings");
			if(settings == null || settings.Count == 0)
				throw new ConfigurationException($"Missing redis connection settings.");

			if(!string.IsNullOrEmpty(name) && settings.TryGetValue(name, Configuration.RedisConnectionSettingsDriver.NAME, out var setting))
				return setting;

			setting = settings.GetDefault();
			if(setting == null)
				throw new ConfigurationException(string.IsNullOrEmpty(name) ?
					$"Missing the default redis connection setting." :
					$"The specified '{name}' redis connection setting does not exist and the default redis connection setting is not defined.");

			return setting;
		}
	}
	#endregion

	#region 显式实现
	ISequence IServiceProvider<ISequence>.GetService(string name) => GetRedis(name);
	ISequenceBase IServiceProvider<ISequenceBase>.GetService(string name) => GetRedis(name);
	IDistributedCache IServiceProvider<IDistributedCache>.GetService(string name) => GetRedis(name);
	Services.Distributing.IDistributedLockManager IServiceProvider<Services.Distributing.IDistributedLockManager>.GetService(string name) => GetRedis(name);
	#endregion
}
