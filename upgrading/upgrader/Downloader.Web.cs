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
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Zongsoft.Upgrading;

partial class Downloader
{
	internal sealed class WebDownloader(Fetcher.WebFetcher fetcher) : Downloader
	{
		#region 私有字段
		private readonly Fetcher.WebFetcher _fetcher = fetcher ?? throw new ArgumentNullException(nameof(fetcher));
		#endregion

		#region 重写方法
		protected override async ValueTask<Stream> DownloadAsync(Release release, CancellationToken cancellation)
		{
			var client = _fetcher.Client;
			if(client == null)
				return null;

			if(release.Properties.TryGetValue(DOWNLOAD_URL, out var url) && IsWebUrl(url as string))
			{
				var stream = await DownloadAsync(client, url.ToString(), cancellation);

				if(stream != null)
					return stream;
			}

			if(IsWebUrl(release.Path))
			{
				var stream = await DownloadAsync(client, release.Path, cancellation);

				if(stream != null)
					return stream;
			}

			return null;
		}

		protected override ValueTask OnDownloadingAsync(DownloadEventArgs args, CancellationToken cancellation)
		{
			var tracer = ((IFetcher)_fetcher).Tracer;
			if(tracer == null)
				return default;

			return tracer.TraceAsync("Downloading", GetProperties(args.Release), cancellation);
		}

		protected override ValueTask OnDownloadedAsync(DownloadEventArgs args, CancellationToken cancellation)
		{
			var tracer = ((IFetcher)_fetcher).Tracer;
			if(tracer == null)
				return default;

			return tracer.TraceAsync("Downloaded", GetProperties(args.Release), cancellation);
		}
		#endregion

		#region 私有方法
		static bool IsWebUrl(string url) => url != null && (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase));
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

		static IEnumerable<KeyValuePair<string, string>> GetProperties(Release release)
		{
			if(release == null || release.Properties == null)
				yield break;

			yield return new(nameof(release.Name), release.Name);
			yield return new(nameof(release.Edition), release.Edition);
			yield return new(nameof(release.Version), release.Version.ToString());
			yield return new(nameof(release.Platform), release.Platform.ToString());
			yield return new(nameof(release.Architecture), release.Architecture.ToString());

			foreach(var property in release.Properties)
				yield return new KeyValuePair<string, string>(property.Key, property.Value?.ToString());
		}
		#endregion
	}
}
