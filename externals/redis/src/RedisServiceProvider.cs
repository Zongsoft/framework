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
using Zongsoft.Distributing;
using Zongsoft.Configuration;

namespace Zongsoft.Externals.Redis
{
	[Service(typeof(IServiceProvider<IDistributedCache>), typeof(IServiceProvider<ISequence>), typeof(IServiceProvider<IDistributedLockManager>))]
	public class RedisServiceProvider : IServiceProvider<IDistributedCache>, IServiceProvider<ISequence>, IServiceProvider<IDistributedLockManager>
	{
		#region 静态字段
		private static readonly ConcurrentDictionary<string, RedisService> _services = new ConcurrentDictionary<string, RedisService>(StringComparer.OrdinalIgnoreCase);
		#endregion

		#region 公共方法
		public RedisService GetRedis(string name)
		{
			return _services.GetOrAdd(name ?? string.Empty, key =>
			{
				var settings = ApplicationContext.Current.Configuration.GetOption<ConnectionSettingCollection>("/Externals/Redis/ConnectionSettings");

				if(settings != null)
				{
					if(string.IsNullOrEmpty(key))
						key = settings.Default ?? string.Empty;

					if(settings.TryGet(key, out var setting))
						return new RedisService(key, RedisServiceSettings.Parse(setting.Value));
				}

				return null;
			});
		}
		#endregion

		#region 显式实现
		ISequence IServiceProvider<ISequence>.GetService(string name) => this.GetRedis(name);
		IDistributedCache IServiceProvider<IDistributedCache>.GetService(string name) => this.GetRedis(name);
		IDistributedLockManager IServiceProvider<IDistributedLockManager>.GetService(string name) => this.GetRedis(name);
		#endregion
	}
}
