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
	#region 成员字段
	private object _defaultValue;
	#endregion

	#region 构造函数
	/// <summary>初始化没有声明默认值的命令选项描述。</summary>
	/// <param name="name">选项名称。</param>
	/// <param name="required">是否必需。</param>
	/// <param name="type">值类型，<see langword="null"/> 表示无值选项。</param>
	/// <param name="converterType">值转换器的类型。</param>
	public CommandOptionDescriptor(string name, bool required, Type type = null, Type converterType = null) : this(name, '\0', required, type, converterType) { }

	/// <summary>初始化没有声明默认值的命令选项描述。</summary>
	/// <param name="name">选项名称。</param>
	/// <param name="symbol">缩写字符。</param>
	/// <param name="required">是否必需。</param>
	/// <param name="type">值类型，<see langword="null"/> 表示无值选项。</param>
	/// <param name="converterType">值转换器的类型。</param>
	public CommandOptionDescriptor(string name, char symbol, bool required, Type type = null, Type converterType = null)
	{
		this.Name = name;
		this.Symbol = symbol;
		this.Required = required;
		this.Type = type;
		this.ConverterType = converterType;
	}

	/// <summary>初始化具有已声明默认值的命令选项描述。</summary>
	/// <param name="name">选项名称。</param>
	/// <param name="required">是否必需。</param>
	/// <param name="type">值类型，<see langword="null"/> 表示无值选项。</param>
	/// <param name="converterType">值转换器的类型。</param>
	/// <param name="defaultValue">声明的默认值，显式传入 <see langword="null"/> 也属于已声明。</param>
	/// <param name="description">文本描述。</param>
	public CommandOptionDescriptor(string name, bool required, Type type, Type converterType, object defaultValue, string description = null) : this(name, '\0', required, type, converterType, defaultValue, description) { }

	/// <summary>初始化具有已声明默认值的命令选项描述。</summary>
	/// <param name="name">选项名称。</param>
	/// <param name="symbol">缩写字符。</param>
	/// <param name="required">是否必需。</param>
	/// <param name="type">值类型，<see langword="null"/> 表示无值选项。</param>
	/// <param name="converterType">值转换器的类型。</param>
	/// <param name="defaultValue">声明的默认值，显式传入 <see langword="null"/> 也属于已声明。</param>
	/// <param name="description">文本描述。</param>
	public CommandOptionDescriptor(string name, char symbol, bool required, Type type, Type converterType, object defaultValue, string description = null) : this(name, symbol, required, type, converterType)
	{
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

	/// <summary>获取一个值，指示是否明确声明了默认值。</summary>
	/// <remarks>显式赋值 <see langword="null"/> 也表示已声明；未声明时不根据选项类型生成默认值。</remarks>
	public bool HasDefaultValue { get; private set; }

	/// <summary>获取或设置命令选项声明的默认值。</summary>
	/// <remarks>赋值后 <see cref="HasDefaultValue"/> 为 <see langword="true"/>，包括赋值 <see langword="null"/>；读取不会生成类型的零值。</remarks>
	public object DefaultValue
	{
		get => _defaultValue;
		set
		{
			_defaultValue = value;
			this.HasDefaultValue = true;
		}
	}

	/// <summary>获取或设置命令选项的文本描述。</summary>
	public string Description { get; set; }
	#endregion

	#region 公共方法
	public TypeConverter GetConverter() => this.ConverterType == null ? null : TypeDescriptor.GetConverter(this.ConverterType);
	#endregion

	#region 重写方法
	public override string ToString()
	{
		var defaultValue = this.DefaultValue;

		if(this.Symbol == '\0')
			return defaultValue == null ?
				$"{this.Name}:{Common.TypeAlias.GetAlias(this.Type)}{(this.Required ? "(required)" : null)}" :
				$"{this.Name}:{Common.TypeAlias.GetAlias(this.Type)}={defaultValue}{(this.Required ? "(required)" : null)}";
		else
			return defaultValue == null ?
				$"[{this.Symbol}]{this.Name}:{Common.TypeAlias.GetAlias(this.Type)}{(this.Required ? "(required)" : null)}" :
				$"[{this.Symbol}]{this.Name}:{Common.TypeAlias.GetAlias(this.Type)}={defaultValue}{(this.Required ? "(required)" : null)}";
	}
	#endregion
}
