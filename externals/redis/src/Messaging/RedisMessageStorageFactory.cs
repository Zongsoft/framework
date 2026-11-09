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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
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
			throw new ConfigurationException(string.Format(Properties.Resources.Redis_ConnectionSettingInvalid_Message, name));

		return new RedisMessageStorage(name, redis, this.GetPartition(redis.Name));
	}
	#endregion
}
