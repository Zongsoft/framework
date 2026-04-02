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
using System.Collections.Generic;

using Zongsoft.Services;
using Zongsoft.Configuration;

namespace Zongsoft.Upgrading;

public sealed partial class Fetcher
{
	#region 私有字段
	private static readonly Dictionary<string, IFetcher> _fetchers;
	#endregion

	#region 静态构造
	static Fetcher()
	{
		_fetchers = new(StringComparer.OrdinalIgnoreCase);

		foreach(var type in typeof(Fetcher).GetNestedTypes())
		{
			if(typeof(IFetcher).IsAssignableFrom(type))
			{
				var fetcher = (IFetcher)Activator.CreateInstance(type);
				_fetchers.Add(fetcher.Name, fetcher);
			}
		}
	}
	#endregion

	#region 静态方法
	/// <summary>通过默认通道获取最新版本的升级信息。</summary>
	/// <param name="cancellation">异步操作的取消标记。</param>
	/// <returns>如果获取成功则返回升级清单文件(<seealso cref="Upgrader.Manifest"/>)的完整路径，否则返回空(<c>null</c>)。</returns>
	public static ValueTask<string> FetchAsync(CancellationToken cancellation = default) => FetchAsync((Version)null, cancellation);

	/// <summary>通过默认通道获取指定版本的升级信息。</summary>
	/// <param name="version">指定要升级到的版本号，如果为空(<c>null</c>)表示升级到最新版本。</param>
	/// <param name="cancellation">异步操作的取消标记。</param>
	/// <returns>如果获取成功则返回升级清单文件(<seealso cref="Upgrader.Manifest"/>)的完整路径，否则返回空(<c>null</c>)。</returns>
	public static ValueTask<string> FetchAsync(Version version, CancellationToken cancellation = default)
	{
		var settings = ApplicationContext.Current.Configuration.GetConnectionSettings(nameof(Upgrading), null);
		return settings == null && string.IsNullOrEmpty(settings.Name) ? default : FetchAsync(settings.Name, version, cancellation);
	}

	/// <summary>通过指定通道获取最新版本的升级信息。</summary>
	/// <param name="name">指定的通道，即获取器名称（譬如：<c>Web</c>、<c>File</c>）。</param>
	/// <param name="cancellation">异步操作的取消标记。</param>
	/// <returns>如果获取成功则返回升级清单文件(<seealso cref="Upgrader.Manifest"/>)的完整路径，否则返回空(<c>null</c>)。</returns>
	public static ValueTask<string> FetchAsync(string name, CancellationToken cancellation = default) => FetchAsync(name, null, cancellation);

	/// <summary>通过指定通道获取指定版本的升级信息。</summary>
	/// <param name="name">指定的通道，即获取器名称（譬如：<c>Web</c>、<c>File</c>）。</param>
	/// <param name="version">指定要升级到的版本号，如果为空(<c>null</c>)表示升级到最新版本。</param>
	/// <param name="cancellation">异步操作的取消标记。</param>
	/// <returns>如果获取成功则返回升级清单文件(<seealso cref="Upgrader.Manifest"/>)的完整路径，否则返回空(<c>null</c>)。</returns>
	public static async ValueTask<string> FetchAsync(string name, Version version, CancellationToken cancellation = default)
	{
		ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));

		//获取指定名称的获取器
		if(!_fetchers.TryGetValue(name, out var fetcher))
			return null;

		//获取升级清单信息
		var manifest = await fetcher.FetchAsync(version, cancellation);
		if(manifest == null || manifest.IsEmpty)
			return null;

		//获取升级清单文件所在目录
		var directory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Utility.ApplicationName, manifest.Version.ToString()));
		if(!directory.Exists)
			directory.Create();

		//在升级目录下创建升级清单文件并将升级清单信息写入该文件
		using var stream = File.OpenWrite(Path.Combine(directory.FullName, Upgrader.Manifest.FileName));
		await Serialization.Serializer.Json.SerializeAsync(stream, manifest, cancellation);
		stream.Close();

		//下载全量包和增量包到升级目录
		if(manifest.Baseline != null)
			await fetcher.Downloader.DownloadAsync(directory.FullName, manifest.Baseline, cancellation);

		//依次下载增量包到升级目录
		for(int i = 0; i < manifest.Deltas.Length; i++)
			await fetcher.Downloader.DownloadAsync(directory.FullName, manifest.Deltas[i], cancellation);

		//返回升级清单文件的完整路径
		return stream.Name;
	}
	#endregion
}
