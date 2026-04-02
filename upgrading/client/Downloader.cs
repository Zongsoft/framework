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

namespace Zongsoft.Upgrading;

public abstract partial class Downloader : IDownloader
{
	public async ValueTask DownloadAsync(string directory, Package package, CancellationToken cancellation = default)
	{
		ArgumentNullException.ThrowIfNull(package);
		ArgumentNullException.ThrowIfNullOrEmpty(directory);

		if(!Directory.Exists(directory))
			return;

		using var source = await this.DownloadAsync(package, cancellation);
		if(source == null || !source.CanRead)
			return;

		using var stream = File.OpenWrite(this.GetFilePath(directory, package));
		await source.CopyToAsync(stream, cancellation);
	}

	protected abstract ValueTask<Stream> DownloadAsync(Package package, CancellationToken cancellation);
	protected virtual string GetFilePath(string directory, Package package) => Path.Combine(directory, Path.GetFileName(package.Path));
}
