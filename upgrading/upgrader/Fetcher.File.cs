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
using System.Collections.Generic;

using Zongsoft.IO;

namespace Zongsoft.Upgrading;

partial class Fetcher
{
	internal sealed class FileFetcher : Fetcher
	{
		#region 常量定义
		private const string URL_SETTING = "url";
		#endregion

		#region 构造函数
		public FileFetcher() : base("File") => this.Downloader = new Downloader.FileDownloader(this);
		#endregion

		#region 公共属性
		public string Url
		{
			get
			{
				if(field == null)
				{
					var settings = this.Settings;

					if(settings != null)
						field = settings.TryGetValue(URL_SETTING, out var url) ? url : null;
				}

				return field;
			}
		}
		#endregion

		#region 重写方法
		protected override async IAsyncEnumerable<Release> OnFetchAsync(string edition, Version version, [System.Runtime.CompilerServices.EnumeratorCancellation]CancellationToken cancellation)
		{
			if(string.IsNullOrEmpty(this.Url))
				yield break;

			var name = Application.ApplicationName;
			var type = Application.ApplicationType?.ToLowerInvariant();
			var extension = System.IO.Path.GetExtension(Manifest.FILE_NAME);

			//在“应用名”的目录下查找所有清单文件，譬如：zfs.s3:/upgrading/releases/zongsoft.daemon/*.manifest
			var path = Path.Combine(this.Url, name);
			if(await FileSystem.Directory.ExistsAsync(path, cancellation))
			{
				await foreach(var release in EnumerableAsync(GetFilesAsync(path, $"*{extension}", cancellation), extension, cancellation))
					yield return release;
			}

			//如果应用类型不为空，则继续尝试应用类型的目录下查找清单文件
			if(!string.IsNullOrEmpty(type))
			{
				//在“应用类型/应用名”的目录下查找所有清单文件，譬如：zfs.s3:/upgrading/releases/daemon/zongsoft.daemon/*.manifest
				path = Path.Combine(this.Url, type, name);
				if(await FileSystem.Directory.ExistsAsync(path, cancellation))
				{
					await foreach(var release in EnumerableAsync(GetFilesAsync(path, $"*{extension}", cancellation), extension, cancellation))
						yield return release;
				}

				//在“应用类型”的目录下查找以应用名打头的清单文件，譬如：zfs.s3:/upgrading/releases/daemon/zongsoft.daemon*.manifest
				path = Path.Combine(this.Url, type);
				if(await FileSystem.Directory.ExistsAsync(path, cancellation))
				{
					await foreach(var release in EnumerableAsync(GetFilesAsync(path, $"{name}*{extension}", cancellation), extension, cancellation))
						yield return release;
				}
			}

			//在根目录下查找以应用名打头的清单文件，譬如：zfs.s3:/upgrading/releases/zongsoft.daemon*.manifest
			await foreach(var release in EnumerableAsync(GetFilesAsync(this.Url, $"{name}*{extension}", cancellation), extension, cancellation))
				yield return release;

			static IAsyncEnumerable<FileInfo> GetFilesAsync(string path, string pattern, CancellationToken cancellation) => FileSystem.Directory.GetFilesAsync(path, pattern, false, cancellation);
			static async IAsyncEnumerable<Release> EnumerableAsync(IAsyncEnumerable<FileInfo> files, string extension, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellation)
			{
				await foreach(var file in files)
				{
					//确保当前文件是清单文件
					if(!string.Equals(System.IO.Path.GetExtension(file.Name), extension, StringComparison.OrdinalIgnoreCase))
						continue;

					var stream = await FileSystem.File.OpenAsync(file.Path.Url, System.IO.FileMode.Open, System.IO.FileAccess.Read, cancellation);
					var release = await Release.ReadAsync(stream, cancellation);

					if(file.HasProperties)
					{
						foreach(var property in file.Properties)
							release.Properties[property.Key] = property.Value;
					}

					//设置当前发布的包文件的下载地址
					release.Properties[Upgrading.Downloader.DOWNLOAD_PATH] = Path.Combine(file.Path.GetDirectoryUrl(), release.Path);
					release.Properties[Upgrading.Downloader.DOWNLOAD_URL] = FileSystem.GetUrl(Path.Combine(file.Path.GetDirectoryUrl(), release.Path));

					yield return release;
				}
			}
		}
		#endregion
	}
}
