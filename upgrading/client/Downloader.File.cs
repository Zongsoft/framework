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
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.IO;

namespace Zongsoft.Upgrading;

partial class Downloader
{
	internal sealed class FileDownloader(Fetcher.FileFetcher fetcher) : Downloader
	{
		#region 私有字段
		private Func<string, Release, CancellationToken, ValueTask<Stream>> _downloader;
		private readonly Func<string, Release, CancellationToken, ValueTask<Stream>>[] _downloaders =
		[
			Download1Async,
			Download2Async,
			Download3Async,
			Download4Async,
		];

		private readonly Fetcher.FileFetcher _fetcher = fetcher ?? throw new ArgumentNullException(nameof(fetcher));
		#endregion

		#region 重写方法
		protected override async ValueTask<Stream> DownloadAsync(Release release, CancellationToken cancellation)
		{
			if(string.IsNullOrEmpty(_fetcher.Url))
				return null;

			if(release.Properties.TryGetValue("path", out var path) && path != null)
			{
				var stream = await DownloadAsync(_fetcher.Url, path.ToString(), cancellation);

				if(stream != null)
					return stream;
			}

			if(release.Properties.TryGetValue("download", out path) && path != null)
			{
				var stream = await DownloadAsync(_fetcher.Url, path.ToString(), cancellation);

				if(stream != null)
					return stream;
			}

			if(_downloader != null)
				return await _downloader(_fetcher.Url, release, cancellation);

			for(int i = 0; i < _downloaders.Length; i++)
			{
				var stream = await _downloaders[i](_fetcher.Url, release, cancellation);

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
		static ValueTask<Stream> Download1Async(string url, Release release, CancellationToken cancellation) => DownloadAsync(url, $"packages/{release.Name}/{release.Version}/{release.GetRuntimeIdentifier()}/{System.IO.Path.GetFileName(release.Path)}", cancellation);
		static ValueTask<Stream> Download2Async(string url, Release release, CancellationToken cancellation) => DownloadAsync(url, $"packages/{release.Name}/{release.Version}/{System.IO.Path.GetFileName(release.Path)}", cancellation);
		static ValueTask<Stream> Download3Async(string url, Release release, CancellationToken cancellation) => DownloadAsync(url, $"packages/{System.IO.Path.GetFileName(release.Path)}", cancellation);
		static ValueTask<Stream> Download4Async(string url, Release release, CancellationToken cancellation) => DownloadAsync(url, $"packages/{release.Name}@{release.Version}_{release.GetRuntimeIdentifier()}{System.IO.Path.GetExtension(release.Path)}", cancellation);

		static async ValueTask<Stream> DownloadAsync(string url, string path, CancellationToken cancellation)
		{
			try
			{
				url = Zongsoft.IO.Path.Combine(url, path);

				if(string.IsNullOrEmpty(url))
					return null;

				if(await FileSystem.File.ExistsAsync(url, cancellation))
					return await FileSystem.File.OpenAsync(url, FileMode.Open, FileAccess.Read, cancellation);

				return null;
			}
			catch(Exception ex)
			{
				Zongsoft.Diagnostics.Logging.GetLogging<FileDownloader>().Error(ex);
				return null;
			}
		}
		#endregion
	}
}
