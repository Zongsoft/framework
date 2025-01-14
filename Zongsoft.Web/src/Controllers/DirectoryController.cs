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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc;

using Zongsoft.Services;

namespace Zongsoft.Web.Controllers;

[ApiController]
[Route("Directories")]
public class DirectoryController : ControllerBase
{
	#region 公共方法
	[HttpDelete("{**path}")]
	public ValueTask<IActionResult> DeleteAsync(string path, [FromQuery]bool recursive = false, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(path))
			return ValueTask.FromResult<IActionResult>(this.NotFound());

		var physicalPath = Utility.GetPhysicalPath(path);

		try
		{
			if(!System.IO.Directory.Exists(physicalPath))
				return ValueTask.FromResult<IActionResult>(this.NotFound());

			System.IO.Directory.Delete(physicalPath, recursive);
			return ValueTask.FromResult<IActionResult>(this.NoContent());
		}
		catch
		{
			return ValueTask.FromResult<IActionResult>(this.NotFound());
		}
	}

	[HttpGet("{**path}")]
	[HttpHead("{**path}")]
	public ValueTask<IActionResult> GetAsync(string path, [FromQuery]string mode, [FromQuery]string pattern = null, [FromQuery]bool recursive = false, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(path))
			return ValueTask.FromResult<IActionResult>(this.NotFound());

		var physicalPath = Utility.GetPhysicalPath(path);

		try
		{
			var info = new System.IO.DirectoryInfo(physicalPath);
			if(!info.Exists)
				return ValueTask.FromResult<IActionResult>(this.NotFound());

			this.Response.Headers.TryAdd("X-Directory-Name", info.Name);
			this.Response.Headers.TryAdd("X-Directory-Creation", info.CreationTimeUtc.ToString("s"));
			this.Response.Headers.TryAdd("X-Directory-Modification", info.LastWriteTimeUtc.ToString("s"));
			this.Response.Headers.TryAdd("X-Directory-Attributes", info.Attributes.ToString());

			if(string.Equals(this.Request.Method, "HEAD", StringComparison.OrdinalIgnoreCase))
				return ValueTask.FromResult<IActionResult>(this.NoContent());

			if(string.IsNullOrEmpty(pattern))
				pattern = "*";

			var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

			if(string.Equals(mode, "directory", StringComparison.OrdinalIgnoreCase) ||
			   string.Equals(mode, "directories", StringComparison.OrdinalIgnoreCase))
				return ValueTask.FromResult<IActionResult>(this.Ok(info.EnumerateDirectories(pattern, option).Select(info => new DirectoryInfo(info))));

			if(string.Equals(mode, "file", StringComparison.OrdinalIgnoreCase) ||
			   string.Equals(mode, "files", StringComparison.OrdinalIgnoreCase))
				return ValueTask.FromResult<IActionResult>(this.Ok(info.EnumerateFiles(pattern, option).Select(info => new FileInfo(info))));

			return ValueTask.FromResult<IActionResult>(this.Ok(new
			{
				Directories = info.EnumerateDirectories(pattern, option).Select(info => new DirectoryInfo(info)),
				Files = info.EnumerateFiles(pattern, option).Select(info => new FileInfo(info)),
			}));
		}
		catch
		{
			return ValueTask.FromResult<IActionResult>(this.NotFound());
		}
	}

	[HttpPost("{**path}")]
	public ValueTask<IActionResult> CreateAsync(string path, CancellationToken cancellation = default)
	{
		var physicalPath = Utility.GetPhysicalPath(path);
		Directory.CreateDirectory(physicalPath);
		return ValueTask.FromResult<IActionResult>(this.NoContent());
	}
	#endregion

	#region 嵌套结构
	private struct FileInfo
	{
		public FileInfo(System.IO.FileInfo info)
		{
			Name = info.FullName.StartsWith(ApplicationContext.Current.ApplicationPath) ? info.FullName[ApplicationContext.Current.ApplicationPath.Length..] : info.FullName;
			Size = info.Length;
			Type = IO.Mime.GetMimeType(info.Name);
			Creation = info.CreationTimeUtc;
			Modification = info.LastWriteTimeUtc;

			if(System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
				Name = Name.Replace('\\', '/');
		}

		public string Name;
		public long Size;
		public string Type;
		public DateTimeOffset Creation;
		public DateTimeOffset Modification;
	}

	private struct DirectoryInfo
	{
		public DirectoryInfo(System.IO.DirectoryInfo info)
		{
			Name = info.FullName.StartsWith(ApplicationContext.Current.ApplicationPath) ? info.FullName[ApplicationContext.Current.ApplicationPath.Length..] : info.FullName;
			Creation = info.CreationTimeUtc;
			Modification = info.LastWriteTimeUtc;

			if(System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
				Name = Name.Replace('\\', '/');
		}

		public string Name;
		public DateTimeOffset Creation;
		public DateTimeOffset Modification;
	}
	#endregion
}
