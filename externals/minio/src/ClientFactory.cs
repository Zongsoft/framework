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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.MinIO library.
 *
 * The Zongsoft.Externals.MinIO is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.MinIO is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.MinIO library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

using Minio;
using Microsoft.Extensions.Configuration;

using Zongsoft.Caching;
using Zongsoft.Services;
using Zongsoft.Configuration;

namespace Zongsoft.Externals.MinIO;

public static class ClientFactory
{
	private static readonly MemoryCache _cache = new();

	public static IMinioClient GetClient(string region) => GetClient(null, region);
	public static IMinioClient GetClient(this IConfiguration configuration, string region)
	{
		configuration ??= ApplicationContext.Current?.Configuration ?? throw new InvalidOperationException($"Missing required configuration.");
		return _cache.GetOrCreate(region ?? string.Empty, key => (CreateClient(configuration, (string)key), configuration.GetReloadToken()));
	}

	private static IMinioClient CreateClient(IConfiguration configuration, string region)
	{
		var settings = GetSettings(configuration, region) ??
			throw (string.IsNullOrEmpty(region) ?
				new InvalidOperationException("No default region configuration is provided.") :
				new InvalidOperationException($"The specified ‘{region}’ region is not configured."));

		var client = new MinioClient()
			.WithEndpoint(settings.Server)
			.WithRegion(string.IsNullOrEmpty(settings.Region) ? settings.Name : settings.Region)
			.WithSSL(settings.Secured)
			.WithSessionToken(settings.Token)
			.WithTimeout((int)settings.Timeout.TotalMilliseconds)
			.WithCredentials(settings.UserName, settings.Password);

		return client.Build();
	}

	private static Configuration.MinIOConnectionSettings GetSettings(IConfiguration configuration, string region)
	{
		var collection = configuration.GetOption<ConnectionSettingsCollection>("/Externals/MinIO/ConnectionSettings");
		if(collection == null || collection.Count == 0)
			return null;

		if(string.IsNullOrEmpty(region))
			return collection.GetDefault() as Configuration.MinIOConnectionSettings;

		foreach(var entry in collection)
		{
			if(entry.IsDriver(Configuration.MinIOConnectionSettingsDriver.NAME) &&
			   entry is Configuration.MinIOConnectionSettings setting &&
			   string.Equals(setting.Region, region, StringComparison.OrdinalIgnoreCase))
				return setting;
		}

		return null;
	}
}
