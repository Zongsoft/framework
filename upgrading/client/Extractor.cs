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
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Upgrading;

public sealed class Extractor
{
	/// <summary>执行升级包解压提取操作。</summary>
	/// <param name="manifest">指定的升级清单对象。</param>
	/// <param name="filePath">指定的升级清单文件路径。</param>
	/// <param name="cancellation">异步操作的取消标记。</param>
	/// <returns>如果部署成功则返回版本文件的完整路径，否则返回空(<c>null</c>)。</returns>
	public static async ValueTask<string> ExtractAsync(Manifest manifest, string filePath, CancellationToken cancellation = default)
	{
		if(manifest == null || string.IsNullOrEmpty(filePath))
			return null;

		//获取升级包元数据文件所在目录
		var directory = Path.GetDirectoryName(filePath);
		//在升级包元数据文件所在目录下创建一个临时目录用于解压升级包
		var destination = CreateDirectory(Path.Combine(directory, ".app"));

		if(manifest.Trunk != null)
		{
			//获取全量包文件路径，如果不存在则返回失败
			if(!manifest.Trunk.TryGetFilePath(out var source) || !File.Exists(source))
				return null;

			//将安装包读取为Zip压缩文件
			using var zip = ZipFile.OpenRead(source);
			//解压安装包到临时目录
			zip.ExtractToDirectory(destination.FullName, true);
		}

		for(int i = 0; i < manifest.Deltas.Length; i++)
		{
			var delta = manifest.Deltas[i];

			//获取增量包文件路径，如果不存在则返回失败
			if(!delta.TryGetFilePath(out var source) || !File.Exists(source))
				return null;

			//将安装包读取为Zip压缩文件
			using var zip = ZipFile.OpenRead(source);

			//解压安装包到临时目录
			zip.ExtractToDirectory(destination.FullName, true);
		}

		//在目标目录下创建一个版本文件并将版本号写入到该文件中
		var version = new FileInfo(Path.Combine(destination.FullName, ".version"));
		using var writer = new StreamWriter(version.OpenWrite());
		writer.WriteLine($"{manifest.Name}@{manifest.Version}");
		await writer.DisposeAsync();

		//返回部署成功
		return version.FullName;

		static DirectoryInfo CreateDirectory(string path)
		{
			if(Directory.Exists(path))
				Directory.Delete(path, true);

			return Directory.CreateDirectory(path);
		}
	}
}
