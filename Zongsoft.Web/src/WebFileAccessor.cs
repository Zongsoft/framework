/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Net.Http;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.WebUtilities;

using Zongsoft.IO;
using Zongsoft.Collections;

namespace Zongsoft.Web
{
	public class WebFileAccessor
	{
		#region 常量定义
		private const string EXTENDED_PROPERTY_PREFIX = "x-zfs-";
		private const string EXTENDED_PROPERTY_FILENAME = "FileName";

		private static readonly DateTime EPOCH = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		#endregion

		#region 成员字段
		private string _basePath;
		private Http.IMimeMapper _mapping;
		#endregion

		#region 构造函数
		public WebFileAccessor()
		{
			_mapping = Http.MimeMapper.Default;
		}

		public WebFileAccessor(string basePath)
		{
			_mapping = Http.MimeMapper.Default;
			this.BasePath = basePath;
		}
		#endregion

		#region 公共属性
		public string BasePath
		{
			get
			{
				return _basePath;
			}
			set
			{
				if(string.IsNullOrWhiteSpace(value))
					_basePath = null;
				else
				{
					var text = value.Trim();
					_basePath = text + (text.EndsWith("/") ? string.Empty : "/");
				}
			}
		}

		public Http.IMimeMapper Mapping
		{
			get => _mapping;
			set => _mapping = value ?? throw new ArgumentNullException();
		}
		#endregion

		#region 公共方法
		/// <summary>
		/// 下载指定路径的文件。
		/// </summary>
		/// <param name="path">指定要下载的文件的相对路径或绝对路径（绝对路径以/斜杠打头）。</param>
		public FileStreamResult Read(string path)
		{
			if(string.IsNullOrWhiteSpace(path))
				throw new ArgumentNullException(nameof(path));

			var properties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
			var stream = FileSystem.File.Open(this.GetFilePath(path), FileMode.Open, FileAccess.Read, properties);

			if(stream == null)
				throw new FileNotFoundException();

			//创建当前文件的流内容
			var content = new FileStreamResult(stream, this.GetContentType(path));

			if(properties.Count > 0)
			{
				if(properties.TryGetValue("FileName", out string fileName))
					content.FileDownloadName = fileName;

				if(properties.TryGetValue("HTTP:LastModified", out string lastModified))
				{
					if(Zongsoft.Common.Convert.TryConvertValue(lastModified, out DateTimeOffset modifiedTime))
						content.LastModified = modifiedTime;
				}
			}

			return content;
		}

		/// <summary>
		/// 获取指定文件的外部访问路径。
		/// </summary>
		/// <param name="path">指定的文件相对路径或绝对路径（绝对路径以/斜杠打头）。</param>
		/// <returns>返回指定文件的外部访问路径。</returns>
		public string GetUrl(string path)
		{
			if(string.IsNullOrWhiteSpace(path))
				throw new ArgumentNullException(nameof(path));

			return FileSystem.GetUrl(this.GetFilePath(path));
		}

		/// <summary>
		/// 获取指定路径的文件描述信息。
		/// </summary>
		/// <param name="path">指定要获取的文件的相对路径或绝对路径（绝对路径以/斜杠打头）。</param>
		/// <returns>返回的指定的文件详细信息。</returns>
		public Task<Zongsoft.IO.FileInfo> GetInfo(string path)
		{
			if(string.IsNullOrWhiteSpace(path))
				throw new ArgumentNullException(nameof(path));

			return FileSystem.File.GetInfoAsync(this.GetFilePath(path));
		}

		/// <summary>
		/// 删除指定相对路径的文件。
		/// </summary>
		/// <param name="path">指定要删除的文件的相对路径或绝对路径（绝对路径以/斜杠打头）。</param>
		public async Task<bool> Delete(string path)
		{
			if(string.IsNullOrWhiteSpace(path))
				throw new ArgumentNullException(nameof(path));

			return await FileSystem.File.DeleteAsync(this.GetFilePath(path));
		}

		/// <summary>
		/// 修改指定路径的文件描述信息。
		/// </summary>
		/// <param name="request">网络请求消息。</param>
		/// <param name="path">指定要修改的文件相对路径或绝对路径（绝对路径以/斜杠打头）。</param>
		public async Task<bool> SetInfo(HttpRequest request, string path)
		{
			if(request == null)
				throw new ArgumentNullException(nameof(request));

			if(string.IsNullOrWhiteSpace(path))
				throw new ArgumentNullException(nameof(path));

			var properties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

			foreach(var header in request.Headers)
			{
				if(header.Key.Length > EXTENDED_PROPERTY_PREFIX.Length && header.Key.StartsWith(EXTENDED_PROPERTY_PREFIX, StringComparison.OrdinalIgnoreCase))
					properties[header.Key.Substring(EXTENDED_PROPERTY_PREFIX.Length)] = string.Join("", header.Value);
			}

			if(request.HasFormContentType)
			{
				var form = await request.ReadFormAsync(new FormOptions() {  });

				foreach(var field in form)
				{
					var key = field.Key;

					if(key.Length > EXTENDED_PROPERTY_PREFIX.Length && key.StartsWith(EXTENDED_PROPERTY_PREFIX, StringComparison.OrdinalIgnoreCase))
						properties[key.Substring(EXTENDED_PROPERTY_PREFIX.Length)] = field.Value;
				}
			}

			if(properties.Count > 0)
				return await FileSystem.File.SetInfoAsync(this.GetFilePath(path), properties);

			return false;
		}

		/// <summary>
		/// 将网络请求中的一个文件或多个文件写入到指定的目录中。
		/// </summary>
		/// <param name="request">网络请求消息。</param>
		/// <param name="directory">指定文件写入的目录路径（绝对路径以“/”斜杠符打头）；如果为空(null)或全空字符串则写入目录为<see cref="BasePath"/>属性值。</param>
		/// <param name="configure">当文件写入前激发的通知回调。</param>
		/// <returns>返回写入成功的<see cref="Zongsoft.IO.FileInfo"/>文件描述信息实体对象集。</returns>
		public async IAsyncEnumerable<Zongsoft.IO.FileInfo> Write(HttpRequest request, string directory = null, Action<WebFileAccessorOptions> configure = null)
		{
			//检测请求的内容是否为Multipart类型
			if(!request.HasFormContentType)
				throw new InvalidOperationException("Incorrect Content-Type: " + request.ContentType);

			//从当前请求内容读取多段信息并写入文件中
			var form = await request.ReadFormAsync();

			if(form.Files.Count == 0)
				yield break;

			var path = this.GetFilePath(directory);

			for(int i=0; i<form.Files.Count; i++)
			{
				var file = form.Files[i];
				var args = new WebFileAccessorOptions(path, i);

				configure(args);

				if(args.Cancel)
					break;

				var extensionName = string.IsNullOrEmpty(file.FileName) ? string.Empty : System.IO.Path.GetExtension(file.FileName);

				//如果文件名为空，则生成一个以“时间戳-随机数.ext”的默认文件名
				if(string.IsNullOrWhiteSpace(args.FileName))
					args.FileName = string.Format("X{0}-{1}{2}", ((long)(DateTime.UtcNow - EPOCH).TotalSeconds).ToString(), Zongsoft.Common.Randomizer.GenerateString(), args.ExtensionAppend ? extensionName : string.Empty);
				else if(args.ExtensionAppend && !args.FileName.EndsWith(extensionName))
					args.FileName += extensionName;

				//生成文件的完整路径
				var filePath = Zongsoft.IO.Path.Combine(path,args.FileName);

				//生成文件信息的描述实体
				var fileInfo = new Zongsoft.IO.FileInfo(filePath, file.Length, DateTime.Now, null, FileSystem.GetUrl(filePath))
				{
					Type = file.ContentType,
				};

				//将上传的原始文件名加入到文件描述实体的扩展属性中
				fileInfo.Properties.Add(EXTENDED_PROPERTY_FILENAME, file.FileName);

				var headers = file.Headers;

				if(headers != null && headers.Count > 0 && !string.IsNullOrWhiteSpace(file.Name))
				{
					//从全局头里面查找当前上传文件的自定义属性
					foreach(var header in headers)
					{
						if(header.Key.Length > file.Name.Length + 1 && header.Key.StartsWith(file.Name + "-", StringComparison.OrdinalIgnoreCase))
							fileInfo.Properties.Add(header.Key.Substring(file.Name.Length + 1), header.Value);
					}
				}

				//调用文件系统根据完整文件路径去创建一个新建文件流
				using(var stream = FileSystem.File.Open(
					filePath,
					args.Overwrite ? FileMode.Create : FileMode.CreateNew,
					FileAccess.Write,
					fileInfo.HasProperties ? fileInfo.Properties : null))
				{
					await file.CopyToAsync(stream);
				}

				//返回新增的文件信息实体集
				yield return fileInfo;
			}
		}
		#endregion

		#region 虚拟方法
		protected virtual string GetContentType(string fileName)
		{
			return _mapping.GetMimeType(fileName) ?? "application/octet-stream";
		}
		#endregion

		#region 私有方法
		private string EnsureBasePath(out string scheme)
		{
			if(Zongsoft.IO.Path.TryParse(this.BasePath, out scheme, out string path))
				return (scheme ?? Zongsoft.IO.FileSystem.Scheme) + ":" + (path ?? "/");

			scheme = Zongsoft.IO.FileSystem.Scheme;
			return scheme + ":/";
		}

		private string GetFilePath(string path)
		{
			string basePath = this.EnsureBasePath(out string scheme);

			if(string.IsNullOrWhiteSpace(path))
				return basePath;

			path = Uri.UnescapeDataString(path).Trim();

			if(path.StartsWith("/"))
				return scheme + ":" + path;
			else
				return Zongsoft.IO.Path.Combine(basePath, path);
		}
		#endregion
	}
}
