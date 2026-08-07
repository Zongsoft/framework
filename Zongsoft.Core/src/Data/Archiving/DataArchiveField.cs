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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

using Zongsoft.Components;

namespace Zongsoft.Data.Archiving;

/// <summary>表示归档字段内容的水平对齐方式。</summary>
public enum DataArchiveFieldAlignment : byte
{
	/// <summary>左对齐。</summary>
	Left,
	/// <summary>居中对齐。</summary>
	Center,
	/// <summary>右对齐。</summary>
	Right,
}

/// <summary>表示归档字段内容的字体样式。</summary>
[Flags]
public enum DataArchiveFontStyle : byte
{
	/// <summary>常规字体。</summary>
	Regular = 0,
	/// <summary>粗体。</summary>
	Bold = 1,
	/// <summary>斜体。</summary>
	Italic = 2,
	/// <summary>下划线。</summary>
	Underline = 4,
	/// <summary>删除线。</summary>
	Strikeout = 8,
}

/// <summary>表示归档字段内容的文本显示模式。</summary>
public enum DataArchiveFieldTextMode : byte
{
	/// <summary>不换行。</summary>
	None,
	/// <summary>自动换行。</summary>
	Wrap,
	/// <summary>缩小字体以适应可用宽度。</summary>
	Shrink,
}

public class DataArchiveField
{
	#region 构造函数
	public DataArchiveField(string name, string label = null, string description = null) : this(name, 0, label, description) { }
	public DataArchiveField(string name, double width, string label = null, string description = null)
	{
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		this.Name = name;
		this.Width = width;
		this.Label = label;
		this.Description = description;
	}
	#endregion

	#region 公共属性
	/// <summary>获取字段名称。</summary>
	public string Name { get; }
	/// <summary>获取或设置字段标签。</summary>
	public string Label { get; set; }
	/// <summary>获取或设置字段描述。</summary>
	public string Description { get; set; }
	/// <summary>获取或设置以排版点（1/72 英寸）为单位的字段宽度，零表示未指定。</summary>
	public double Width { get; set; }
	/// <summary>获取或设置字段内容的水平对齐方式。</summary>
	public DataArchiveFieldAlignment? Alignment { get; set; }
	/// <summary>获取或设置字段内容的字体名称。</summary>
	public string FontName { get; set; }
	/// <summary>获取或设置以排版点（1/72 英寸）为单位的字段内容字体大小，零表示未指定。</summary>
	public double FontSize { get; set; }
	/// <summary>获取或设置字段内容的字体样式。</summary>
	public DataArchiveFontStyle? FontStyle { get; set; }
	/// <summary>获取或设置字段内容的前景色，空值表示未指定。</summary>
	public Color? ForegroundColor { get; set; }
	/// <summary>获取或设置字段内容的背景色，空值表示未指定。</summary>
	public Color? BackgroundColor { get; set; }
	/// <summary>获取或设置字段内容的文本显示模式。</summary>
	public DataArchiveFieldTextMode? TextMode { get; set; }
	/// <summary>获取或设置字段值的 .NET 格式字符串。</summary>
	public string Format { get; set; }
	#endregion

	#region 重写方法
	public override string ToString() => string.IsNullOrEmpty(this.Label) || string.Equals(this.Name, this.Label) ? this.Name : $"{this.Name}({this.Label})";
	#endregion
}
