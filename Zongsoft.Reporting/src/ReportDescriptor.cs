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

namespace Zongsoft.Reporting
{
	public abstract class ReportDescriptorBase : IReportDescriptor
	{
		#region 构造函数
		protected ReportDescriptorBase(string key, string name, string type, string url = null)
		{
			this.Key = key;
			this.Name = name;
			this.Type = type;
			this.Url = url;
		}
		#endregion

		#region 公共属性
		public string Key { get; }
		public string Name { get; }
		public string Type { get; }
		public string Url { get; set; }
		#endregion

		#region 抽象方法
		public abstract Stream Open();
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

			this.Key = HashCode.Combine(path.Url).ToString();
			this.Name = path.FileName;
			this.Type = System.IO.Path.GetExtension(path.FileName).Trim('.');
			this.FilePath = filePath;
			this.Url = Zongsoft.IO.FileSystem.GetUrl(path.Url);
		}

		public FileReportDescriptor(string key, string name, string type, string filePath)
		{
			if(string.IsNullOrEmpty(filePath))
				throw new ArgumentNullException(nameof(filePath));

			var path = Zongsoft.IO.Path.Parse(filePath);

			this.Key = string.IsNullOrEmpty(key) ? path.Url : key;
			this.Name = string.IsNullOrEmpty(name) ? path.FileName : name;
			this.Type = string.IsNullOrEmpty(type) ? System.IO.Path.GetExtension(path.FileName).Trim('.') : type;
			this.FilePath = filePath;
			this.Url = Zongsoft.IO.FileSystem.GetUrl(path.Url);
		}

		public FileReportDescriptor(Zongsoft.IO.FileInfo file)
		{
			if(file == null)
				throw new ArgumentNullException(nameof(file));

			this.Key = HashCode.Combine(file.Url).ToString();
			this.Name = file.Name;
			this.Type = System.IO.Path.GetExtension(file.Name).Trim('.');
			this.FilePath = file.Path.FullPath;
			this.Url = Zongsoft.IO.FileSystem.GetUrl(file.Url);
		}

		public FileReportDescriptor(string key, string name, string type, Zongsoft.IO.FileInfo file)
		{
			if(file == null)
				throw new ArgumentNullException(nameof(file));

			this.Key = string.IsNullOrEmpty(key) ? file.Url : key;
			this.Name = string.IsNullOrEmpty(name) ? file.Name : name;
			this.Type = string.IsNullOrEmpty(type) ? System.IO.Path.GetExtension(file.Name).Trim('.') : type;
			this.FilePath = file.Path.FullPath;
			this.Url = Zongsoft.IO.FileSystem.GetUrl(file.Url);
		}
		#endregion

		#region 公共属性
		public string Key { get; }
		public string Name { get; }
		public string Type { get; }
		public string FilePath { get; }
		public string Url { get; }
		#endregion

		#region 公共方法
		public Stream Open()
		{
			return Zongsoft.IO.FileSystem.File.Open(this.FilePath, FileMode.Open, FileAccess.Read);
		}
		#endregion
	}
}
