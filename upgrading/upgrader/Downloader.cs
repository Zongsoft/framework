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
	#region 事件声明
	public event EventHandler<DownloadEventArgs> Downloaded;
	public event EventHandler<DownloadEventArgs> Downloading;
	#endregion

	#region 构造函数
	protected Downloader() { }
	#endregion

	#region 公共方法
	public async ValueTask<string> DownloadAsync(string directory, Release release, CancellationToken cancellation = default)
	{
		ArgumentNullException.ThrowIfNull(release);
		ArgumentNullException.ThrowIfNullOrEmpty(directory);

		//如果指定的目标目录不存在则直接返回
		if(!Directory.Exists(directory))
			return null;

		//获取下载升级包的保存位置
		var destination = new FileInfo(this.GetFilePath(directory, release));

		//如果待保存的目标文件已存在，且当前发布的校验码不为空并且两者的文件大小必须相同
		if(destination.Exists && !release.Checksum.IsEmpty && release.Size == destination.Length)
		{
			//如果目标文件与当前发布的校验码一致，则说明该文件已下载过，因此可以跳过重新下载
			if(await ChecksumAsync(release, destination.OpenRead(), cancellation))
				return destination.FullName;
		}

		//触发“Downloading”事件
		await this.OnDownloadingAsync(release, destination.FullName, cancellation);

		//下载升级包文件
		using var source = await this.DownloadAsync(release, cancellation);
		if(source == null || !source.CanRead)
			return null;

		//将下载的升级包文件保存到指定目录
		using var stream = new FileStream(destination.FullName, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024);
		await source.CopyToAsync(stream, cancellation);

		stream.Close(); //关闭文件流
		source.Close(); //关闭下载流

		//如果升级包文件包含校验码则需进行校验
		if(!release.Checksum.IsEmpty)
		{
			//刷新目标文件的状态
			destination.Refresh();

			//如果下载的升级包校验码与包元数据中声明的校验码不一致则删除下载的升级包文件
			if(destination.Exists && !await ChecksumAsync(release, destination.OpenRead(), cancellation))
			{
				//删除已下载的目标文件
				destination.Delete();

				//记录下载文件校验失败的日志
				await Diagnostics.Logging.GetLogging<Downloader>().WarnAsync(
					$"The downloaded file '{destination.FullName}' does not match the checksum declared in the release, and has been deleted.",
					new
					{
						release.Name,
						release.Edition,
						release.Version,
						release.Checksum,
						release.Kind,
						release.Size,
						release.Path,
					}, cancellation);

				//返回下载失败
				return null;
			}
		}

		//触发“Downloaded”事件
		await this.OnDownloadedAsync(release, destination.FullName, cancellation);

		//返回下载成功
		return destination.FullName;

		static Task<bool> ChecksumAsync(Release release, FileStream stream, CancellationToken cancellation)
		{
			if(release == null || release.Checksum.IsEmpty)
				return Task.FromResult(true);

			var task = release.Checksum.VerifyAsync(stream, cancellation);
			if(task.IsCompletedSuccessfully)
				return Task.FromResult(task.Result);

			return task.AsTask()
				.ContinueWith((task, state) =>
				{
					((Stream)state).Dispose();
					return task.Result;
				}, stream, cancellation);
		}
	}
	#endregion

	#region 抽象方法
	protected abstract ValueTask<Stream> DownloadAsync(Release release, CancellationToken cancellation);
	#endregion

	#region 虚拟方法
	protected virtual string GetFilePath(string directory, Release release)
	{
		var fileName = Path.GetFileNameWithoutExtension(release.Path);

		if(string.Equals(release.Name, fileName, StringComparison.OrdinalIgnoreCase))
			fileName = $"{release.Name}@{release.Version}{Path.GetExtension(release.Path)}";
		else
			fileName = Path.GetFileName(release.Path);

		return Path.Combine(directory, fileName);
	}
	#endregion

	#region 触发事件
	private async ValueTask OnDownloadedAsync(Release release, string directory, CancellationToken cancellation)
	{
		var args = new DownloadEventArgs(release, directory);
		this.OnDownloaded(args);
		await Executor.ExecuteAsync(nameof(Downloaded), this, args, cancellation);
	}

	private async ValueTask OnDownloadingAsync(Release release, string directory, CancellationToken cancellation)
	{
		var args = new DownloadEventArgs(release, directory);
		this.OnDownloading(args);
		await Executor.ExecuteAsync(nameof(Downloading), this, args, cancellation);
	}

	protected virtual void OnDownloaded(DownloadEventArgs args) => this.Downloaded?.Invoke(this, args);
	protected virtual void OnDownloading(DownloadEventArgs args) => this.Downloading?.Invoke(this, args);
	#endregion

	#region 嵌套子类
	/// <summary>表示下载事件的参数类。</summary>
	public sealed class DownloadEventArgs : ReleaseEventArgs
	{
		public DownloadEventArgs(Release release, string destination) : base(release)
		{
			this.Destination = destination;
		}

		/// <summary>获取下载的目标文件路径。</summary>
		public string Destination { get; }
	}
	#endregion
}
