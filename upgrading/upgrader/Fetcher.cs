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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Services;
using Zongsoft.Configuration;

namespace Zongsoft.Upgrading;

public abstract partial class Fetcher
{
	#region 私有字段
	private static readonly Dictionary<string, IFetcher> _fetchers;
	#endregion

	#region 静态构造
	static Fetcher()
	{
		_fetchers = new(StringComparer.OrdinalIgnoreCase);

		foreach(var type in typeof(Fetcher).GetNestedTypes(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
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
	/// <returns>如果获取成功则返回包含结果升级清单和文件信息的结果对象，否则返回空(<c>null</c>)。</returns>
	public static ValueTask<Result> FetchAsync(CancellationToken cancellation = default) => FetchAsync(null, null, null, cancellation);

	/// <summary>通过默认通道获取指定版本的升级信息。</summary>
	/// <param name="version">指定要升级到的版本号，如果为空(<c>null</c>)表示升级到最新版本。</param>
	/// <param name="cancellation">异步操作的取消标记。</param>
	/// <returns>如果获取成功则返回包含结果升级清单和文件信息的结果对象，否则返回空(<c>null</c>)。</returns>
	public static ValueTask<Result> FetchAsync(Version version, CancellationToken cancellation = default) => FetchAsync(null, null, version, cancellation);

	/// <summary>通过指定通道获取最新版本的升级信息。</summary>
	/// <param name="name">指定的通道，即获取器名称（譬如：<c>Web</c>、<c>File</c>）；如果为空(<c>null</c>)或空字符串(<c>""</c>)，则表示默认通道。</param>
	/// <param name="cancellation">异步操作的取消标记。</param>
	/// <returns>如果获取成功则返回包含结果升级清单和文件信息的结果对象，否则返回空(<c>null</c>)。</returns>
	public static ValueTask<Result> FetchAsync(string name, CancellationToken cancellation = default) => FetchAsync(name, null, null, cancellation);

	/// <summary>通过指定通道获取指定版本的升级信息。</summary>
	/// <param name="name">指定的通道，即获取器名称（譬如：<c>Web</c>、<c>File</c>）；如果为空(<c>null</c>)或空字符串(<c>""</c>)，则表示默认通道。</param>
	/// <param name="edition">指定要升级的版本名，如果为空(<c>null</c>)或空字符串则表示不限定版本名。</param>
	/// <param name="version">指定要升级到的版本号，如果为空(<c>null</c>)表示升级到最新版本。</param>
	/// <param name="cancellation">异步操作的取消标记。</param>
	/// <returns>如果获取成功则返回包含结果升级清单和文件信息的结果对象，否则返回空(<c>null</c>)。</returns>
	public static async ValueTask<Result> FetchAsync(string name, string edition, Version version, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(name))
		{
			name = ApplicationContext.Current?.Configuration.GetConnectionSettings(nameof(Upgrading), null)?.Name;

			if(string.IsNullOrEmpty(name))
			{
				Diagnostics.Logging.GetLogging<Fetcher>().Warn("No fetcher name specified, and no default fetcher name found in configuration.");
				return null;
			}
		}

		//获取指定名称的获取器
		if(!_fetchers.TryGetValue(name, out var fetcher))
		{
			await Diagnostics.Logging.GetLogging<Fetcher>().WarnAsync($"No fetcher found with the name '{name}'.", cancellation);
			return default;
		}

		//获取升级清单信息
		var manifest = await fetcher.FetchAsync(edition, version, cancellation);
		if(manifest == null || manifest.IsEmpty)
		{
			var app = new ApplicationIdentifier(Application.ApplicationName, string.IsNullOrEmpty(edition) ? Application.ApplicationEdition : edition, Application.ApplicationVersion);
			await Diagnostics.Logging.GetLogging<Fetcher>().InfoAsync($"The release for the '{app}' application was not found in the {fetcher.Name} channel.", cancellation);
			return default;
		}

		//获取升级清单文件所在目录
		var directory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Application.ApplicationName));
		if(!directory.Exists)
			directory.Create();

		//在升级目录下创建升级清单文件并将升级清单信息写入该文件
		var result = await manifest.SaveAsync(directory.FullName, cancellation);

		//下载全量包和增量包到升级目录
		if(manifest.Trunk != null)
		{
			//下载全量包文件，如果失败则直接返回
			var filePath = await fetcher.Downloader.DownloadAsync(directory.FullName, manifest.Trunk, cancellation);

			if(string.IsNullOrEmpty(filePath))
				return default;

			//设置下载的安装包文件路径到指定的扩展属性
			manifest.Trunk.SetFilePath(filePath);
		}

		//依次下载增量包到升级目录
		for(int i = 0; i < manifest.Deltas.Length; i++)
		{
			//下载增量包文件，如果失败则直接返回
			var filePath = await fetcher.Downloader.DownloadAsync(directory.FullName, manifest.Deltas[i], cancellation);

			if(string.IsNullOrEmpty(filePath))
				return default;

			//设置下载的安装包文件路径到指定的扩展属性
			manifest.Trunk.SetFilePath(filePath);
		}

		//返回升级清单文件的完整路径
		return new(fetcher, manifest, result);
	}
	#endregion

	#region 嵌套结构
	public sealed class Result(IFetcher fetcher, Manifest manifest, string filePath)
	{
		/// <summary>获取器。</summary>
		public readonly IFetcher Fetcher = fetcher;
		/// <summary>升级清单。</summary>
		public readonly Manifest Manifest = manifest;
		/// <summary>升级清单文件路径。</summary>
		public readonly string FilePath = filePath;
	}
	#endregion
}

partial class Fetcher : IFetcher
{
	#region 构造函数
	protected Fetcher(string name, IDownloader downloader = null)
	{
		this.Name = name ?? throw new ArgumentNullException(nameof(name));
		this.Downloader = downloader;
	}
	#endregion

	#region 保护属性
	protected string Name { get; }
	protected IDownloader Downloader { get; set; }
	protected IConnectionSettings Settings => field ??= ApplicationContext.Current?.Configuration.GetConnectionSettings(nameof(Upgrading), this.Name);
	#endregion

	#region 显式实现
	string IFetcher.Name => this.Name;
	IDownloader IFetcher.Downloader => this.Downloader;
	async ValueTask<Manifest> IFetcher.FetchAsync(string edition, Version version, CancellationToken cancellation)
	{
		var trunk = default(Release);
		var deltas = new List<Release>();
		var upgradingVersion = version;
		var currentlyVersion = Application.ApplicationVersion;

		//如果指定的版本名为空，则取当前应用版本名
		if(string.IsNullOrEmpty(edition))
			edition = Application.ApplicationEdition;

		//获取升级发布集合
		var releases = this.OnFetchAsync(edition, version, cancellation);

		await foreach(var release in releases)
		{
			//跳过废弃和无效版本号的升级发布
			if(release == null || release.Deprecated || release.Version.IsZero())
				continue;

			//跳过版本名不匹配的升级发布
			if(!string.IsNullOrEmpty(edition) && !string.IsNullOrEmpty(release.Edition) && !string.Equals(edition, release.Edition, StringComparison.OrdinalIgnoreCase))
				continue;

			//筛选出满足要求的升级发布：
			//应用名、平台和架构匹配的，版本号大于当前版本且小于等于升级版本
			if(Application.Platform == release.Platform &&
			   Application.Architecture == release.Architecture &&
			   string.Equals(Application.ApplicationName, release.Name, StringComparison.OrdinalIgnoreCase) &&
			   release.Version > currentlyVersion &&
			   (upgradingVersion.IsZero() || release.Version <= upgradingVersion))
			{
				if(release.Kind == ReleaseKind.Delta)
					deltas.Add(release);
				else if(trunk == null || release.Version > trunk.Version)
					trunk = release;
			}
		}

		return trunk == null ?
			new(deltas.OrderBy(delta => delta.Version).ToArray()) :
			new(trunk, deltas.Where(delta => delta.Version > trunk.Version).OrderBy(delta => delta.Version).ToArray());
	}
	#endregion

	#region 抽象方法
	protected abstract IAsyncEnumerable<Release> OnFetchAsync(string edition, Version version, CancellationToken cancellation);
	#endregion
}