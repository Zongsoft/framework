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
 * This file is part of Zongsoft.Reporting library.
 *
 * The Zongsoft.Reporting is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Reporting is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Reporting library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Collections.Generic;

using Zongsoft.IO;
using Zongsoft.Services;

namespace Zongsoft.Reporting
{
	//[Service(typeof(IReportLocator))]
	public class FileReportLocator : IReportLocator
	{
		#region 常量定义
		private const string REPORTS_DIRECTORY = "reports";
		private static readonly string[] DEFAULT_EXTENSIONS = new[] { ".rdlx" };
		#endregion

		#region 成员字段
		private readonly IDictionary<string, FileReportDescriptor> _cache;
		#endregion

		#region 构造函数
		public FileReportLocator()
		{
			this.Priority = 100;
			_cache = new Dictionary<string, FileReportDescriptor>(StringComparer.OrdinalIgnoreCase);
		}
		#endregion

		#region 公共属性
		public int Priority { get; set; }
		public string Path { get; set; }
		public string Extension { get; set; }
		#endregion

		#region 公共方法
		public IReportDescriptor GetReport(string name)
		{
			if(string.IsNullOrEmpty(name))
				return null;

			if(_cache.TryGetValue(name, out var descriptor))
				return descriptor;

			var directories = GetReportDirectories(this.Path);
			var extensions = string.IsNullOrWhiteSpace(this.Extension) ? DEFAULT_EXTENSIONS : this.Extension.Split(new char[] { '|', ';', ',' }, StringSplitOptions.RemoveEmptyEntries);

			foreach(var directory in directories)
			{
				for(int i = 0; i < extensions.Length; i++)
				{
					var filePath = Zongsoft.IO.Path.Combine(directory, name.EndsWith(extensions[i], StringComparison.OrdinalIgnoreCase) ? name : name + extensions[i]);

					if(FileSystem.File.Exists(filePath))
						return _cache[name] = new FileReportDescriptor(filePath);
				}
			}

			return null;
		}

		public IEnumerable<IReportDescriptor> GetReports()
		{
			var directories = GetReportDirectories(this.Path);
			var extensions = string.IsNullOrWhiteSpace(this.Extension) ? DEFAULT_EXTENSIONS : this.Extension.Split(new char[] { '|', ';', ',' }, StringSplitOptions.RemoveEmptyEntries);

			foreach(var directory in directories)
			{
				for(int i = 0; i < extensions.Length; i++)
				{
					var files = FileSystem.Directory.GetFiles(directory, "*" + extensions[i]);

					foreach(var file in files)
						yield return new FileReportDescriptor(file);
				}
			}
		}
		#endregion

		#region 私有方法
		private static ICollection<string> GetReportDirectories(string path)
		{
			var paths = string.IsNullOrWhiteSpace(path) ? new[] { ApplicationContext.Current.ApplicationPath } : path.Split(new char[] { '|', ';' }, StringSplitOptions.RemoveEmptyEntries);

			if(paths.Length > 0)
			{
				var directories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

				for(int i = 0; i < paths.Length; i++)
				{
					FillReportDirectory(
						directories,
						paths[i].StartsWith('~') ? ApplicationContext.Current.ApplicationPath + (paths[i].Length > 1 ? paths[i].Substring(1) : null) : paths[i]);
				}

				return directories;
			}

			return Array.Empty<string>();
		}

		private static void FillReportDirectory(ISet<string> hashset, string path)
		{
			if(FileSystem.Directory.Exists(path) && hashset.Add(path))
			{
				var directories = FileSystem.Directory.GetDirectories(path);

				foreach(var directory in directories)
				{
					if(string.Equals(directory.Name, REPORTS_DIRECTORY, StringComparison.OrdinalIgnoreCase))
						hashset.Add(directory.Path.FullPath);
					else
						FillReportDirectory(hashset, directory.Path.FullPath);
				}
			}
		}
		#endregion
	}

	public class FileReportDescriptor : IReportDescriptor
	{
		#region 构造函数
		public FileReportDescriptor(string filePath)
		{
			if(string.IsNullOrEmpty(filePath))
				throw new ArgumentNullException(nameof(filePath));

			var path = Zongsoft.IO.Path.Parse(filePath);

			this.Name = path.FileName;
			this.Type = System.IO.Path.GetExtension(path.FileName).Trim('.');
			this.FilePath = filePath;
			this.Url = FileSystem.GetUrl(path.Url);
		}

		public FileReportDescriptor(string name, string type, string filePath)
		{
			if(string.IsNullOrEmpty(filePath))
				throw new ArgumentNullException(nameof(filePath));

			var path = Zongsoft.IO.Path.Parse(filePath);

			this.Name = string.IsNullOrEmpty(name) ? path.FileName : name;
			this.Type = string.IsNullOrEmpty(type) ? System.IO.Path.GetExtension(path.FileName).Trim('.') : type;
			this.FilePath = filePath;
			this.Url = FileSystem.GetUrl(path.Url);
		}

		public FileReportDescriptor(Zongsoft.IO.FileInfo file)
		{
			if(file == null)
				throw new ArgumentNullException(nameof(file));

			this.Name = file.Name;
			this.Type = System.IO.Path.GetExtension(file.Name).Trim('.');
			this.FilePath = file.Path.FullPath;
			this.Url = FileSystem.GetUrl(file.Url);
		}

		public FileReportDescriptor(string name, string type, Zongsoft.IO.FileInfo file)
		{
			if(file == null)
				throw new ArgumentNullException(nameof(file));

			this.Name = string.IsNullOrEmpty(name) ? file.Name : name;
			this.Type = string.IsNullOrEmpty(type) ? System.IO.Path.GetExtension(file.Name).Trim('.') : type;
			this.FilePath = file.Path.FullPath;
			this.Url = FileSystem.GetUrl(file.Url);
		}
		#endregion

		#region 公共属性
		public string Name { get; }
		public string Type { get; }
		public string FilePath { get; }
		public string Url { get; }
		#endregion

		#region 公共方法
		public Stream Open()
		{
			return FileSystem.File.Open(this.FilePath, FileMode.Open, FileAccess.Read);
		}
		#endregion
	}
}
