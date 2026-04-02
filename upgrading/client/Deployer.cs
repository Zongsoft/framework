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

public sealed class Deployer
{
	/// <summary>执行部署操作。</summary>
	/// <param name="filePath">指定的升级清单文件路径。</param>
	/// <param name="cancellation">异步操作的取消标记。</param>
	/// <returns>如果部署成功则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	public static async ValueTask<bool> DeployAsync(string filePath, CancellationToken cancellation = default)
	{
		//如果升级清单文件不存在则返回失败
		if(string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
			return false;

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
			var packageFile = Path.Combine(directory, Path.GetFileName(manifest.Baseline.Path));
			if(!File.Exists(packageFile))
				return false;

			//将安装包读取为Zip压缩文件
			using var zip = ZipFile.OpenRead(packageFile);
			//解压安装包到临时目录
			zip.ExtractToDirectory(destination.FullName, true);
		}

		for(int i = 0; i < manifest.Deltas.Length; i++)
		{
			var delta = manifest.Deltas[i];

			//获取增量包文件路径
			var packageFile = Path.Combine(directory, Path.GetFileName(delta.Path));
			if(!File.Exists(packageFile))
				return false;

			//将安装包读取为Zip压缩文件
			using var zip = ZipFile.OpenRead(packageFile);

			//解压安装包到临时目录
			zip.ExtractToDirectory(destination.FullName, true);
		}

		//返回部署成功
		return true;
	}

	static DirectoryInfo EnsureDirectory(string path) => Directory.Exists(path) ? new DirectoryInfo(path) : Directory.CreateDirectory(path);
}
