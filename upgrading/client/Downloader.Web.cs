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
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Upgrading;

partial class Downloader
{
	internal sealed class WebDownloader(Fetcher.WebFetcher fetcher) : Downloader
	{
		#region 私有字段
		private Func<HttpClient, Package, CancellationToken, ValueTask<Stream>> _downloader;
		private readonly Func<HttpClient, Package, CancellationToken, ValueTask<Stream>>[] _downloaders =
		[
			Download1Async,
			Download2Async,
			Download3Async,
			Download4Async,
			Download5Async,
			Download6Async,
		];

		private readonly Fetcher.WebFetcher _fetcher = fetcher ?? throw new ArgumentNullException(nameof(fetcher));
		#endregion

		#region 重写方法
		protected override async ValueTask<Stream> DownloadAsync(Package package, CancellationToken cancellation)
		{
			var client = _fetcher.Client;
			if(client == null)
				return null;

			if(package.Properties.TryGetValue("url", out var url) && url != null)
			{
				var stream = await DownloadAsync(client, url, cancellation);

				if(stream != null)
					return stream;
			}

			if(package.Properties.TryGetValue("download", out url) && url != null)
			{
				var stream = await DownloadAsync(client, url, cancellation);

				if(stream != null)
					return stream;
			}

			if(_downloader != null)
				return await _downloader(client, package, cancellation);

			for(int i = 0; i < _downloaders.Length; i++)
			{
				var stream = await _downloaders[i](client, package, cancellation);

				if(stream != null)
				{
					_downloader = _downloaders[i];
					return stream;
				}
			}

			return null;
		}
		#endregion

		#region 私有方法
		static ValueTask<Stream> Download1Async(HttpClient client, Package package, CancellationToken cancellation) => DownloadAsync(client, $"Download/{package.Name}/{package.GetRuntimeIdentifier()}", cancellation);
		static ValueTask<Stream> Download2Async(HttpClient client, Package package, CancellationToken cancellation) => DownloadAsync(client, $"packages/{package.Name}/{package.Version}/{package.GetRuntimeIdentifier()}/{Path.GetFileName(package.Path)}", cancellation);
		static ValueTask<Stream> Download3Async(HttpClient client, Package package, CancellationToken cancellation) => DownloadAsync(client, $"packages/{package.Name}/{package.Version}/{Path.GetFileName(package.Path)}", cancellation);
		static ValueTask<Stream> Download4Async(HttpClient client, Package package, CancellationToken cancellation) => DownloadAsync(client, $"packages/{Path.GetFileName(package.Path)}", cancellation);
		static ValueTask<Stream> Download5Async(HttpClient client, Package package, CancellationToken cancellation) => DownloadAsync(client, $"packages/{package.Name}@{package.Version}_{package.GetRuntimeIdentifier()}{Path.GetExtension(package.Path)}", cancellation);
		static ValueTask<Stream> Download6Async(HttpClient client, Package package, CancellationToken cancellation)
		{
			var path = Path.IsPathFullyQualified(package.Path) ?
				Path.GetRelativePath(Path.GetPathRoot(package.Path), package.Path): package.Path;
			return DownloadAsync(client, path, cancellation);
		}

		static async ValueTask<Stream> DownloadAsync(HttpClient client, string url, CancellationToken cancellation)
		{
			var response = await client.GetAsync(url, cancellation);

			return response != null && response.IsSuccessStatusCode ?
				await response.Content.ReadAsStreamAsync(cancellation) : null;
		}
		#endregion
	}
}
