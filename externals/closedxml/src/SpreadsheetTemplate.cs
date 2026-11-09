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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.IO;

using ClosedXML;
using ClosedXML.Excel;

using Zongsoft.Data;
using Zongsoft.Data.Archiving;

namespace Zongsoft.Externals.ClosedXml;

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
	public DataArchiveFormat Format => Spreadsheet.Format;
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