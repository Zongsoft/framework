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

/// <summary>表示升级获取器的接口。</summary>
public interface IFetcher
{
	/// <summary>获取获取器名称。</summary>
	string Name { get; }
	/// <summary>获取追踪器。</summary>
	ITracer Tracer { get; }
	/// <summary>获取下载器对象。</summary>
	IDownloader Downloader { get; }

	/// <summary>获取指定版本的升级信息。</summary>
	/// <param name="edition">指定要升级的版本名。</param>
	/// <param name="version">指定要升级到的版本号，如果为空(<c>null</c>)表示升级到最新版本。</param>
	/// <param name="cancellation">异步操作的取消标记。</param>
	/// <returns>返回的升级信息结果。</returns>
	ValueTask<Manifest> FetchAsync(string edition, Version version, CancellationToken cancellation = default);
}
