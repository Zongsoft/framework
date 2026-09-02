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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
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

using Zongsoft.Services;
using Zongsoft.Configuration;

namespace Zongsoft.Externals.Redis.Messaging;

/// <summary>提供基于 Redis 的消息存储器工厂。</summary>
public sealed class RedisMessageStorageFactory : Zongsoft.Messaging.MessageStorageFactoryBase<RedisMessageStorage>
{
	#region 单例字段
	public static readonly RedisMessageStorageFactory Instance = new();
	#endregion

	#region 私有构造
	private RedisMessageStorageFactory() { }
	#endregion

	#region 重写方法
	protected override RedisMessageStorage OnCreate(string name)
	{
		var settings = ApplicationContext.Current?.Configuration.GetConnectionSettings(
			"/Externals/Redis/ConnectionSettings", name, Configuration.RedisConnectionSettingsDriver.NAME);

		settings ??= ApplicationContext.Current?.Configuration.GetConnectionSettings(
			"/Messaging/Storages/ConnectionSettings", Configuration.RedisConnectionSettingsDriver.NAME);

		if(settings is not Configuration.RedisConnectionSettings redis)
			throw new ConfigurationException($"The '{name}' Redis connection setting does not exist or its driver does not match Redis.");

		return new RedisMessageStorage(name, redis, this.GetPartition(redis.Name));
	}
	#endregion
}
