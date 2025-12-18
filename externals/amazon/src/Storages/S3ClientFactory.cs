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

namespace Zongsoft.Externals.Amazon.Storages;

internal static class S3ClientFactory
{
	private static readonly MemoryCache _cache = new();

	public static AmazonS3Client GetClient(string region) => GetClient(null, region);
	public static AmazonS3Client GetClient(this IConfiguration configuration, string region)
	{
		configuration ??= ApplicationContext.Current?.Configuration ?? throw new InvalidOperationException($"Missing required configuration.");
		var location = string.IsNullOrEmpty(region) ? configuration.GetRegion() : RegionEndpoint.GetBySystemName(region);
		return _cache.GetOrCreate(location, key => (CreateClient(configuration, (RegionEndpoint)key), configuration.GetReloadToken()));
	}

	private static AmazonS3Client CreateClient(IConfiguration configuration, RegionEndpoint region)
	{
		ArgumentNullException.ThrowIfNull(region);

		var server = configuration.GetOptionValue<string>("Externals/Amazon/General/Server");
		var credentials = configuration.GetCredentials(region) ?? throw new InvalidOperationException($"The credentials for the specified ‘{region}’ region are not configured.");

		return new AmazonS3Client(credentials, new AmazonS3Config()
		{
			ForcePathStyle = !string.IsNullOrEmpty(server),
			RegionEndpoint = region,
			ServiceURL = server,
		});
	}
}
