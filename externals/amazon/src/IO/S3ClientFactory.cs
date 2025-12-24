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
 * This file is part of Zongsoft.Externals.Amazon library.
 *
 * The Zongsoft.Externals.Amazon is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Amazon is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Amazon library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

using Microsoft.Extensions.Configuration;

using Amazon;
using Amazon.S3;

using Zongsoft.Caching;
using Zongsoft.Services;
using Zongsoft.Configuration;

namespace Zongsoft.Externals.Amazon.IO;

internal static class S3ClientFactory
{
	private static readonly MemoryCache _cache = new();

	public static AmazonS3Client GetClient(string region) => GetClient(null, region);
	public static AmazonS3Client GetClient(this IConfiguration configuration, string region)
	{
		configuration ??= ApplicationContext.Current?.Configuration ?? throw new InvalidOperationException($"Missing required configuration.");
		return _cache.GetOrCreate(region ?? string.Empty, key => (CreateClient(configuration, (string)key), configuration.GetReloadToken()));
	}

	private static AmazonS3Client CreateClient(IConfiguration configuration, string region)
	{
		var settings = GetSettings(configuration, region) ??
			throw (string.IsNullOrEmpty(region) ?
				new InvalidOperationException("No default region configuration is provided.") :
				new InvalidOperationException($"The specified '{region}' region is not configured."));

		return new AmazonS3Client(settings.GetOptions());
	}

	private static Configuration.S3ConnectionSettings GetSettings(IConfiguration configuration, string region)
	{
		var collection = configuration.GetOption<ConnectionSettingsCollection>("/Externals/Amazon/ConnectionSettings");
		if(collection == null || collection.Count == 0)
			return null;

		if(string.IsNullOrEmpty(region))
			return collection.GetDefault() as Configuration.S3ConnectionSettings;

		foreach(var entry in collection)
		{
			if(entry.IsDriver(Configuration.S3ConnectionSettingsDriver.NAME) &&
			   entry is Configuration.S3ConnectionSettings setting &&
			   string.Equals(setting.Region.SystemName, region, StringComparison.OrdinalIgnoreCase))
				return setting;
		}

		return null;
	}
}
