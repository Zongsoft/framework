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
		private Func<HttpClient, Release, CancellationToken, ValueTask<Stream>> _downloader;
		private readonly Func<HttpClient, Release, CancellationToken, ValueTask<Stream>>[] _downloaders =
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
		protected override async ValueTask<Stream> DownloadAsync(Release release, CancellationToken cancellation)
		{
			var client = _fetcher.Client;
			if(client == null)
				return null;

			if(release.Properties.TryGetValue(DOWNLOAD_URL, out var url) && url != null)
			{
				var stream = await DownloadAsync(client, url.ToString(), cancellation);

				if(stream != null)
					return stream;
			}

			if(_downloader != null)
				return await _downloader(client, release, cancellation);

			for(int i = 0; i < _downloaders.Length; i++)
			{
				var stream = await _downloaders[i](client, release, cancellation);

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
		static ValueTask<Stream> Download1Async(HttpClient client, Release release, CancellationToken cancellation) => DownloadAsync(client, $"Download/{release.Name}/{release.GetRuntimeIdentifier()}", cancellation);
		static ValueTask<Stream> Download2Async(HttpClient client, Release release, CancellationToken cancellation) => DownloadAsync(client, $"packages/{release.Name}/{release.Version}/{release.GetRuntimeIdentifier()}/{Path.GetFileName(release.Path)}", cancellation);
		static ValueTask<Stream> Download3Async(HttpClient client, Release release, CancellationToken cancellation) => DownloadAsync(client, $"packages/{release.Name}/{release.Version}/{Path.GetFileName(release.Path)}", cancellation);
		static ValueTask<Stream> Download4Async(HttpClient client, Release release, CancellationToken cancellation) => DownloadAsync(client, $"packages/{Path.GetFileName(release.Path)}", cancellation);
		static ValueTask<Stream> Download5Async(HttpClient client, Release release, CancellationToken cancellation) => DownloadAsync(client, $"packages/{release.Name}@{release.Version}_{release.GetRuntimeIdentifier()}{Path.GetExtension(release.Path)}", cancellation);
		static ValueTask<Stream> Download6Async(HttpClient client, Release release, CancellationToken cancellation)
		{
			var path = Path.IsPathFullyQualified(release.Path) ?
				Path.GetRelativePath(Path.GetPathRoot(release.Path), release.Path): release.Path;
			return DownloadAsync(client, path, cancellation);
		}

		static async ValueTask<Stream> DownloadAsync(HttpClient client, string url, CancellationToken cancellation)
		{
			try
			{
				var response = await client.GetAsync(url, cancellation);

				return response != null && response.IsSuccessStatusCode ?
					await response.Content.ReadAsStreamAsync(cancellation) : null;
			}
			catch(Exception ex)
			{
				await Zongsoft.Diagnostics.Logging.GetLogging<WebDownloader>().ErrorAsync(ex, cancellation);
				return null;
			}
		}
		#endregion
	}
}
