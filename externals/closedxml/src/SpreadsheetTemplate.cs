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
 * Copyright (C) 2010-2023 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.ClosedXml library.
 *
 * The Zongsoft.Externals.ClosedXml is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.ClosedXml is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.ClosedXml library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;

using ClosedXML;
using ClosedXML.Excel;

using Zongsoft.Data;
using Zongsoft.Data.Templates;

namespace Zongsoft.Externals.ClosedXml
{
	public class SpreadsheetTemplate : IDataTemplate, IEquatable<SpreadsheetTemplate>
	{
		#region 构造函数
		private SpreadsheetTemplate(string filePath, string title = null, string description = null)
		{
			if(string.IsNullOrEmpty(filePath))
				throw new ArgumentNullException(nameof(filePath));

			this.Name = Path.GetFileNameWithoutExtension(filePath);
			this.FilePath = filePath;
			this.Title = string.IsNullOrEmpty(title) ? this.Name : title;
			this.Description = description;
		}
		#endregion

		#region 公共属性
		public string Name { get; }
		public string Type => SpreadsheetFormat.Name;
		public string FilePath { get; }
		public string Title { get; set; }
		public string Description { get; set; }
		#endregion

		#region 公共方法
		public Stream Open() => File.OpenRead(this.FilePath);
		#endregion

		#region 重写方法
		public bool Equals(SpreadsheetTemplate other) => other != null && string.Equals(this.FilePath, other.FilePath, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
		public override bool Equals(object obj) => obj is SpreadsheetTemplate template && this.Equals(template);
		public override int GetHashCode() => OperatingSystem.IsWindows() ? HashCode.Combine(this.FilePath.ToUpperInvariant()) : HashCode.Combine(this.FilePath);
		public override string ToString() => $"{this.Name}@{this.FilePath}";
		#endregion

		#region 静态方法
		public static SpreadsheetTemplate Create(string filePath)
		{
			if(string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
				return null;

			using var workbook = new XLWorkbook(filePath);
			return new(filePath, workbook.Properties.Title, workbook.Properties.Comments);
		}
		#endregion
	}
}