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

using Zongsoft.Services;
using Zongsoft.Configuration;

namespace Zongsoft.Externals.Amazon;

internal static class Utility
{
	public static bool IsSucceed(this System.Net.HttpStatusCode status) => status >= System.Net.HttpStatusCode.OK && status < (System.Net.HttpStatusCode)300;

	public static global::Amazon.RegionEndpoint GetRegion(this IConfiguration configuration)
	{
		configuration ??= ApplicationContext.Current?.Configuration;
		if(configuration == null)
			return null;

		var general = configuration.GetOption<Configuration.GeneralOptions>("/Externals/Amazon/General");
		if(general == null)
			return null;

		return string.IsNullOrEmpty(general.Region) ? null : global::Amazon.RegionEndpoint.GetBySystemName(general.Region);
	}

	public static global::Amazon.Runtime.AWSCredentials GetCredentials(global::Amazon.RegionEndpoint region = null) => GetCredentials(null, region);
	public static global::Amazon.Runtime.AWSCredentials GetCredentials(this IConfiguration configuration, global::Amazon.RegionEndpoint region = null)
	{
		configuration ??= ApplicationContext.Current?.Configuration;
		if(configuration == null)
			return null;

		var storages = configuration.GetOption<Storages.Options.StorageOptionsCollection>("/Externals/Amazon/Storages");
		if(storages == null || storages.Count == 0)
			return configuration.GetCredentials(string.Empty);

		if(region == null)
			return configuration.GetCredentials(storages.GetDefault()?.Credential);

		if(storages.TryGetValue(region.SystemName, out var storage))
			return configuration.GetCredentials(storage.Credential);

		return null;
	}

	public static global::Amazon.Runtime.AWSCredentials GetCredentials(string name = null) => GetCredentials(null, name);
	public static global::Amazon.Runtime.AWSCredentials GetCredentials(this IConfiguration configuration, string name = null)
	{
		configuration ??= ApplicationContext.Current?.Configuration;
		if(configuration == null)
			return null;

		var general = configuration.GetOption<Configuration.GeneralOptions>("/Externals/Amazon/General");
		if(general == null || general.Credentials == null)
			return null;

		if(string.IsNullOrEmpty(name))
			return general.Credentials.TryGetDefault(out var options) && !string.IsNullOrEmpty(options.Code) ? new ConfiguredCredentials(options) : null;

		if(general.Credentials.TryGetValue(name, out var result) && !string.IsNullOrEmpty(result.Code))
			return new ConfiguredCredentials(result);

		return null;
	}

	private sealed class ConfiguredCredentials(Configuration.CredentialOptions options) : global::Amazon.Runtime.AWSCredentials
	{
		private readonly Configuration.CredentialOptions _options = options ?? throw new ArgumentNullException(nameof(options));
		public override global::Amazon.Runtime.ImmutableCredentials GetCredentials() => new(_options.Code, _options.Secret, _options.Token);
	}
}
