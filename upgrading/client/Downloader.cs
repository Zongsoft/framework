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
	#region 构造函数
	protected Downloader() { }
	#endregion

	#region 公共方法
	public async ValueTask<bool> DownloadAsync(string directory, Package package, CancellationToken cancellation = default)
	{
		ArgumentNullException.ThrowIfNull(package);
		ArgumentNullException.ThrowIfNullOrEmpty(directory);

		//如果指定的目标目录不存在则直接返回
		if(!Directory.Exists(directory))
			return false;

		//下载升级包文件
		using var source = await this.DownloadAsync(package, cancellation);
		if(source == null || !source.CanRead)
			return false;

		//将下载的升级包文件保存到指定目录
		using var stream = File.OpenWrite(GetFilePath(directory, package));
		await source.CopyToAsync(stream, cancellation);

		stream.Close(); //关闭文件流
		source.Close(); //关闭下载流

		//如果升级包文件包含校验码则需进行校验
		if(!package.Checksum.IsEmpty)
		{
			//计算下载的升级包文件的校验码
			var checksum = await Common.Checksum.ComputeAsync(package.Checksum.Name, File.OpenRead(stream.Name), cancellation);

			//如果下载的升级包校验码与包元数据中声明的校验码不一致则删除下载的升级包文件
			if(package.Checksum != checksum)
			{
				File.Delete(stream.Name);
				return false;
			}
		}

		//返回下载成功
		return true;
	}
	#endregion

	#region 抽象方法
	protected abstract ValueTask<Stream> DownloadAsync(Package package, CancellationToken cancellation);
	#endregion

	#region 静态方法
	public static string GetFilePath(string directory, Package package)
	{
		var fileName = Path.GetFileNameWithoutExtension(package.Path);

		if(string.Equals(package.Name, fileName, StringComparison.OrdinalIgnoreCase))
			fileName = $"{package.Name}@{package.Version}{Path.GetExtension(package.Path)}";

		return Path.Combine(directory, "packages", fileName);
	}
	#endregion
}
