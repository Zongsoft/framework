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
 * This file is part of Zongsoft.Externals.Velopack library.
 *
 * The Zongsoft.Externals.Velopack is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Velopack is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Velopack library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Velopack;

using Zongsoft.IO;
using Zongsoft.Services;

namespace Zongsoft.Externals.Velopack.Web;

public class VelopackFileScanner
{
	const string RELEASES_DIRECTORY = "releases";

	public static IAsyncEnumerable<VelopackAsset> ScanAsync(CancellationToken cancellation = default) => ScanAsync(string.Empty, null, cancellation);
	public static IAsyncEnumerable<VelopackAsset> ScanAsync(Predicate<VelopackAsset> predicate, CancellationToken cancellation = default) => ScanAsync(string.Empty, predicate, cancellation);
	public static IAsyncEnumerable<VelopackAsset> ScanAsync(Predicate<VelopackAsset> predicate, string path, CancellationToken cancellation = default) => ScanAsync(path, predicate, cancellation);
	public static IAsyncEnumerable<VelopackAsset> ScanAsync(string path, CancellationToken cancellation = default) => ScanAsync(path, null, cancellation);
	public static async IAsyncEnumerable<VelopackAsset> ScanAsync(string path, Predicate<VelopackAsset> predicate, [System.Runtime.CompilerServices.EnumeratorCancellation]CancellationToken cancellation = default)
	{
		var direcotory = GetDirectory(path);
		if(!await FileSystem.Directory.ExistsAsync(direcotory, cancellation))
			yield break;

		var files = FileSystem.Directory.GetFilesAsync(direcotory, "releases.*.json", true, cancellation);

		await foreach(var file in files)
		{
			//确保文件扩展名为“.json”，以防某些虚拟文件系统不支持上述的查询模式
			if(!string.Equals(System.IO.Path.GetExtension(file.Url), ".json", StringComparison.OrdinalIgnoreCase))
				continue;

			using var stream = await file.OpenAsync(System.IO.FileMode.Open, System.IO.FileAccess.Read, cancellation);
			using var reader = new System.IO.StreamReader(stream);
			var json = reader.ReadToEnd();
			var feed = VelopackAssetFeed.FromJson(json);

			if(predicate == null)
			{
				for(int i = 0; i < feed.Assets.Length; i++)
					yield return feed.Assets[i];
			}
			else
			{
				for(int i = 0; i < feed.Assets.Length; i++)
				{
					if(predicate(feed.Assets[i]))
						yield return feed.Assets[i];
				}
			}
		}

		static string GetDirectory(string path)
		{
			if(string.IsNullOrEmpty(path))
				return ApplicationContext.Current == null ?
					Path.Combine(AppContext.BaseDirectory, RELEASES_DIRECTORY) :
					Path.Combine(ApplicationContext.Current.ApplicationPath, RELEASES_DIRECTORY);

			return Path.Parse(path).Url;
		}
	}
}
