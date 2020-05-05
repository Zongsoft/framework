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
using Zongsoft.Services;
using Zongsoft.Configuration;
using Zongsoft.Runtime.Caching;

namespace Zongsoft.Externals.Redis
{
	[Service(typeof(IServiceProvider<ICache>), typeof(IServiceProvider<ISequence>))]
	public class RedisServiceProvider : IServiceProvider<ICache>, IServiceProvider<ISequence>
	{
		#region 静态字段
		private static readonly ConcurrentDictionary<string, RedisService> _services = new ConcurrentDictionary<string, RedisService>(StringComparer.OrdinalIgnoreCase);
		#endregion

		#region 公共方法
		public RedisService GetRedis(string name)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(name);

			return _services.GetOrAdd(name, key =>
			{
				var settings = ApplicationContext.Current.Configuration.GetOption<ConnectionSettingCollection>("/Externals/Redis/ConnectionSettings");

				if(settings != null && settings.TryGet(key, out var setting))
					return new RedisService(key, RedisServiceSettings.Parse(setting.Value));

				throw new KeyNotFoundException($"The Redis service with the specified name '{key}' is undefined.");
			});
		}
		#endregion

		#region 显式实现
		ICache IServiceProvider<ICache>.GetService(string name)
		{
			return this.GetRedis(name);
		}

		ISequence IServiceProvider<ISequence>.GetService(string name)
		{
			return this.GetRedis(name);
		}
		#endregion
	}
}
