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

using System;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Zongsoft.Services;
using Zongsoft.Configuration;

namespace Zongsoft.Externals.Redis;

public sealed class RedisServiceRegistration : IServiceRegistration
{
	public void Register(IServiceCollection services, IConfiguration configuration)
	{
		var settings = GetConnectionSettings(configuration);
		if(settings == null)
			return;

		services.AddStackExchangeRedisCache(options =>
		{
			var configuration = settings.GetOptions<StackExchange.Redis.ConfigurationOptions>();
			options.ConfigurationOptions = configuration;
			options.ConnectionMultiplexerFactory = async () =>
			{
				var lease = await RedisConnectionPool.AcquireAsync(configuration);
				return lease.CreateProxy();
			};
		});

		static IConnectionSettings GetConnectionSettings(IConfiguration configuration)
		{
			var settings = configuration.GetOption<ConnectionSettingsCollection>("/Externals/Redis/ConnectionSettings");
			if(settings == null || settings.Count == 0)
				return null;

			return settings.GetDefault();
		}
	}
}
