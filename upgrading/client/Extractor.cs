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
	/// <param name="filePath">指定的升级清单文件路径。</param>
	/// <param name="cancellation">异步操作的取消标记。</param>
	/// <returns>如果部署成功则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	public static async ValueTask<bool> ExtractAsync(string filePath, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(filePath))
			return false;

		//如果升级清单文件不存在则返回失败
		if(!File.Exists(filePath))
		{
			Zongsoft.Diagnostics.Logging.GetLogging<Extractor>().Error($"The manifest file '{filePath}' does not exist.");
			return false;
		}

		//反序列化升级清单文件
		var manifest = await Serialization.Serializer.Json.DeserializeAsync<Upgrader.Manifest>(File.OpenRead(filePath), cancellation);
		if(manifest.IsEmpty)
			return false;

		//获取升级包元数据文件所在目录
		var directory = Path.GetDirectoryName(filePath);
		//在升级包元数据文件所在目录下创建一个临时目录用于解压升级包
		var destination = EnsureDirectory(Path.Combine(directory, ".app"));

		if(manifest.Baseline != null)
		{
			//如果全量包存在则删除临时解压目录并重新创建一个空目录用于解压全量包
			if(destination.Exists)
			{
				destination.Delete(true);
				destination.Create();
			}

			//获取全量包文件路径
			var source = Downloader.GetFilePath(directory, manifest.Baseline);
			if(!File.Exists(source))
				return false;

			//将安装包读取为Zip压缩文件
			using var zip = ZipFile.OpenRead(source);
			//解压安装包到临时目录
			zip.ExtractToDirectory(destination.FullName, true);
		}

		for(int i = 0; i < manifest.Deltas.Length; i++)
		{
			var delta = manifest.Deltas[i];

			//获取增量包文件路径
			var source = Downloader.GetFilePath(directory, delta);
			if(!File.Exists(source))
				return false;

			//将安装包读取为Zip压缩文件
			using var zip = ZipFile.OpenRead(source);

			//解压安装包到临时目录
			zip.ExtractToDirectory(destination.FullName, true);
		}

		//在目标目录下创建一个版本文件并将版本号写入到该文件中
		using var writer = File.CreateText(Path.Combine(destination.FullName, ".version"));
		writer.WriteLine($"{manifest.Name}@{manifest.Version}");

		//返回部署成功
		return true;
	}

	static DirectoryInfo EnsureDirectory(string path) => Directory.Exists(path) ? new DirectoryInfo(path) : Directory.CreateDirectory(path);
}
