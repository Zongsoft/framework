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
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Upgrading;

/// <summary>表示升级获取器的接口。</summary>
public interface IFetcher
{
	/// <summary>获取获取器名称。</summary>
	string Name { get; }
	/// <summary>获取下载器对象。</summary>
	IDownloader Downloader { get; }

	/// <summary>获取指定版本的升级信息。</summary>
	/// <param name="version">指定要升级到的版本号，如果为空(<c>null</c>)表示升级到最新版本。</param>
	/// <param name="cancellation">异步操作的取消标记。</param>
	/// <returns>返回的升级信息结果。</returns>
	ValueTask<Manifest> FetchAsync(Version version, CancellationToken cancellation = default);
}
