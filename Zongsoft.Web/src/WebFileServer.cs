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
 * Copyright (C) 2020-2024 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Web library.
 *
 * The Zongsoft.Web is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Web is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Web library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc;

using Zongsoft.IO;
using Zongsoft.Services;

namespace Zongsoft.Web;

[ApiController]
[Route("Files")]
public class WebFileServer : ControllerBase
{
	#region 构造函数
	public WebFileServer() => this.Environment = ApplicationContext.Current?.Environment as IWebEnvironment;
	#endregion

	#region 公共属性
	public IWebEnvironment Environment { get; set; }
	#endregion

	#region 公共方法
	[HttpHead("{**path}")]
	public ValueTask<IActionResult> GetAsync(string path, CancellationToken cancellation)
	{
		if(string.IsNullOrEmpty(path))
			return ValueTask.FromResult<IActionResult>(this.NotFound());

		var physicalPath = GetPhysicalPath(path);

		try
		{
			var info = new System.IO.FileInfo(physicalPath);
			if(!info.Exists)
				return ValueTask.FromResult<IActionResult>(this.NotFound());

			this.Response.Headers.TryAdd("X-File-Size", info.Length.ToString());
			this.Response.Headers.TryAdd("X-File-Type", Mime.GetMimeType(physicalPath));
			this.Response.Headers.TryAdd("X-File-Creation", info.CreationTimeUtc.ToString("s"));
			this.Response.Headers.TryAdd("X-File-Modification", info.LastWriteTimeUtc.ToString("s"));
			this.Response.Headers.TryAdd("X-File-Attributes", info.Attributes.ToString());

			return ValueTask.FromResult<IActionResult>(this.NoContent());
		}
		catch
		{
			return ValueTask.FromResult<IActionResult>(this.NotFound());
		}
	}

	[HttpDelete("{**path}")]
	public ValueTask<IActionResult> DeleteAsync(string path, CancellationToken cancellation)
	{
		if(string.IsNullOrEmpty(path))
			return ValueTask.FromResult<IActionResult>(this.NotFound());

		var physicalPath = GetPhysicalPath(path);

		try
		{
			if(!System.IO.File.Exists(physicalPath))
				return ValueTask.FromResult<IActionResult>(this.NotFound());

			System.IO.File.Delete(physicalPath);
			return ValueTask.FromResult<IActionResult>(this.NoContent());
		}
		catch
		{
			return ValueTask.FromResult<IActionResult>(this.NotFound());
		}
	}

	[HttpGet("{**path}")]
	public ValueTask<IActionResult> DownloadAsync(string path, CancellationToken cancellation)
	{
		if(string.IsNullOrEmpty(path))
			return ValueTask.FromResult<IActionResult>(this.NotFound());

		var physicalPath = GetPhysicalPath(path);
		if(!System.IO.File.Exists(physicalPath))
			return ValueTask.FromResult<IActionResult>(this.NotFound());

		var contentType = Mime.GetMimeType(physicalPath) ?? "application/octet-stream";
		return ValueTask.FromResult<IActionResult>(this.PhysicalFile(physicalPath, contentType));
	}

	[HttpPost("{**path}")]
	public async ValueTask<IActionResult> UploadAsync(string path, CancellationToken cancellation)
	{
		var physicalPath = GetPhysicalPath(path);
		Directory.CreateDirectory(physicalPath);

		var files = WebFileAccessor.Default.Write(this.Request, physicalPath, options => options.Overwrite = true, cancellation);
		var result = new List<FileInfo>();

		await foreach(var file in files)
		{
			result.Add(new(file));
		}

		return result.Count switch
		{
			0 => this.NoContent(),
			1 => this.Ok(result[0]),
			_ => this.Ok(result),
		};
	}
	#endregion

	#region 私有方法
	private static string GetPhysicalPath(string path) => System.IO.Path.Combine(ApplicationContext.Current.ApplicationPath, path);
	#endregion

	#region 嵌套结构
	private struct FileInfo
	{
		public FileInfo(Zongsoft.IO.FileInfo info)
		{
			this.Size = info.Size;
			this.Type = info.Type;
			this.Creation = info.CreatedTime;
			this.Modification = info.ModifiedTime;
		}

		public long Size;
		public string Type;
		public DateTimeOffset Creation;
		public DateTimeOffset Modification;
	}
	#endregion
}
