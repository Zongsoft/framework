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
 * Copyright (C) 2020-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Upgrading library.
 *
 * The Zongsoft.Upgrading is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Upgrading is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Upgrading library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Zongsoft.Upgrading;

partial class Fetcher
{
	internal sealed class WebFetcher : FetcherBase
	{
		#region 构造函数
		public WebFetcher() : base("Web") => this.Downloader = new Downloader.WebDownloader(this);
		#endregion

		#region 公共属性
		public HttpClient Client
		{
			get
			{
				if(field == null)
				{
					var settings = this.GetSettings();

					if(settings != null && settings.TryGetValue("server", out var url) && !string.IsNullOrEmpty(url))
						return field = HttpUtility.CreateHttpClient(url);
				}

				return field;
			}
		}
		#endregion

		#region 重写方法
		protected override async IAsyncEnumerable<Package> OnFetchAsync(Version version, [System.Runtime.CompilerServices.EnumeratorCancellation]CancellationToken cancellation)
		{
			var client = this.Client;
			if(client == null)
				yield break;

			using var response = await client.GetAsync($"{Utility.ApplicationName}/{Utility.RuntimeIdentifier}?{GetParameters(version)}", cancellation);
			if(!response.IsSuccessStatusCode)
				yield break;

			await foreach(var package in response.Content.ReadFromJsonAsAsyncEnumerable<Package>(cancellation))
				yield return package;

			static string GetParameters(Version version)
			{
				var parameters = version == null ?
					new KeyValuePair<string, string>[]
					{
						new(nameof(Package.Name), Utility.ApplicationName),
						new(nameof(Package.Platform), Utility.Platform.ToString()),
						new(nameof(Package.Architecture), Utility.Architecture.ToString()),
						new("CurrentlyVersion", Utility.ApplicationVersion.ToString()),
					}:
					new KeyValuePair<string, string>[]
					{
						new(nameof(Package.Name), Utility.ApplicationName),
						new(nameof(Package.Platform), Utility.Platform.ToString()),
						new(nameof(Package.Architecture), Utility.Architecture.ToString()),
						new("CurrentlyVersion", Utility.ApplicationVersion.ToString()),
						new("UpgradingVersion", version.ToString()),
					};

				return string.Join('&', parameters.Select(parameter => $"{parameter.Key}={parameter.Value}"));
			}
		}
		#endregion
	}
}
