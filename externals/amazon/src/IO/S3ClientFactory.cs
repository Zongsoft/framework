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
		configuration ??= ApplicationContext.Current?.Configuration ?? throw new InvalidOperationException(Properties.Resources.Configuration_Required_Message);
		return _cache.GetOrCreate(region ?? string.Empty, key => (CreateClient(configuration, (string)key), configuration.GetReloadToken()));
	}

	private static AmazonS3Client CreateClient(IConfiguration configuration, string region)
	{
		var settings = GetSettings(configuration, region) ??
			throw (string.IsNullOrEmpty(region) ?
				new InvalidOperationException(Properties.Resources.Storage_DefaultRegionRequired_Message) :
				new InvalidOperationException(string.Format(Properties.Resources.Storage_RegionNotConfigured_Message, region)));

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
