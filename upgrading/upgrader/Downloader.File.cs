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
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.IO;

namespace Zongsoft.Upgrading;

partial class Downloader
{
	internal sealed class FileDownloader(Fetcher.FileFetcher fetcher) : Downloader
	{
		#region 私有字段
		private readonly Fetcher.FileFetcher _fetcher = fetcher ?? throw new ArgumentNullException(nameof(fetcher));
		#endregion

		#region 重写方法
		protected override async ValueTask<Stream> DownloadAsync(Release release, CancellationToken cancellation)
		{
			if(string.IsNullOrEmpty(_fetcher.Url))
				return null;

			if(release.Properties.TryGetValue(Downloader.DOWNLOAD_PATH, out var path) && path != null)
			{
				var stream = await DownloadAsync(_fetcher.Url, path.ToString(), cancellation);

				if(stream != null)
					return stream;
			}

			return await DownloadAsync(_fetcher.Url, release.Path, cancellation);
		}
		#endregion

		#region 私有方法
		static async ValueTask<Stream> DownloadAsync(string url, string path, CancellationToken cancellation)
		{
			if(string.IsNullOrEmpty(url) && string.IsNullOrEmpty(path))
				return null;

			try
			{
				if(path.Contains(':'))
					url = path;
				else
					url = IO.Path.Combine(url, path);

				if(string.IsNullOrEmpty(url))
					return null;

				if(await FileSystem.File.ExistsAsync(url, cancellation))
					return await FileSystem.File.OpenAsync(url, FileMode.Open, FileAccess.Read, cancellation);

				//记录下载文件不存在的日志
				await Diagnostics.Logging.GetLogging<FileDownloader>().WarnAsync($"The file '{url}' does not exist.", cancellation);

				//返回空对象
				return null;
			}
			catch(Exception ex)
			{
				Diagnostics.Logging.GetLogging<FileDownloader>().Error(ex);
				return null;
			}
		}
		#endregion
	}
}
