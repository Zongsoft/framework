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
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Upgrading;

/// <summary>提供升级包下载功能的接口。</summary>
public interface IDownloader
{
	/// <summary>表示下载完成的事件。</summary>
	event EventHandler<Downloader.DownloadEventArgs> Downloaded;
	/// <summary>表示开始下载的事件。</summary>
	event EventHandler<Downloader.DownloadEventArgs> Downloading;

	/// <summary>下载升级包文件到指定的目录中。</summary>
	/// <param name="directory">指定的下载目录。</param>
	/// <param name="release">要下载的升级发布信息。</param>
	/// <param name="cancellation">异步操作的取消标记。</param>
	/// <returns>如果下载成功则返回下载文件的完整路径，否则返回空(<c>null</c>)。</returns>
	ValueTask<string> DownloadAsync(string directory, Release release, CancellationToken cancellation = default);
}
