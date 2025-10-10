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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
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
using System.ComponentModel;

namespace Zongsoft.Components;

public class CommandOptionDescriptor
{
	#region 构造函数
	public CommandOptionDescriptor(string name, bool required, Type type = null, Type converterType = null, object defaultValue = null, string description = null) :
		this(name, '\0', required, type, converterType, defaultValue, description) { }
	public CommandOptionDescriptor(string name, char symbol, bool required, Type type = null, Type converterType = null, object defaultValue = null, string description = null)
	{
		this.Name = name;
		this.Symbol = symbol;
		this.Required = required;
		this.Type = type;
		this.ConverterType = converterType;
		this.DefaultValue = defaultValue;
		this.Description = description;
	}
	#endregion

	#region 公共属性
	/// <summary>获取命令选项的名称。</summary>
	public string Name { get; }

	/// <summary>获取命令选项的缩写字符。</summary>
	public char Symbol { get; }

	/// <summary>获取或设置命令选项是否必需的，默认值为假(<c>False</c>)。</summary>
	public bool Required { get; set; }

	/// <summary>获取或设置命令选项的值类型，如果返回空(<c>null</c>)则表示当前选项没有值。</summary>
	public Type Type { get; set; }

	/// <summary>获取或设置命令选项值的类型转换器的类型。</summary>
	public Type ConverterType { get; set; }

	/// <summary>获取或设置命令选项的默认值。</summary>
	public object DefaultValue { get; set; }

	/// <summary>获取或设置命令选项的文本描述。</summary>
	public string Description { get; set; }
	#endregion

	#region 公共方法
	public TypeConverter GetConverter() => this.ConverterType == null ? null : TypeDescriptor.GetConverter(this.ConverterType);
	#endregion
}
