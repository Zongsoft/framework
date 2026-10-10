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

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public class CommandOptionAttribute : Attribute
{
	#region 成员变量
	private string _name;
	private char _symbol;
	private object _defaultValue;
	private Type _type;
	private Type _converterType;
	private TypeConverter _converter;
	private bool _required;
	private string _description;
	#endregion

	#region 构造函数
	/// <summary>初始化没有声明默认值的命令选项特性。</summary>
	/// <param name="name">选项名称，不能为 <see langword="null"/>、空字符串或纯空白。</param>
	/// <param name="type">值类型，<see langword="null"/> 表示无值选项。</param>
	/// <exception cref="ArgumentNullException"><paramref name="name"/> 为空或纯空白。</exception>
	public CommandOptionAttribute(string name, Type type = null) : this(name, '\0', type) { }
	/// <summary>初始化没有声明默认值的命令选项特性。</summary>
	/// <param name="name">选项名称，不能为 <see langword="null"/>、空字符串或纯空白。</param>
	/// <param name="symbol">缩写字符。</param>
	/// <param name="type">值类型，<see langword="null"/> 表示无值选项。</param>
	/// <exception cref="ArgumentNullException"><paramref name="name"/> 为空或纯空白。</exception>
	public CommandOptionAttribute(string name, char symbol, Type type = null)
	{
		if(string.IsNullOrWhiteSpace(name))
			throw new ArgumentNullException(nameof(name));

		_name = name;
		_type = type;
		_symbol = symbol;
		_description = string.Empty;
	}

	/// <summary>初始化具有已声明默认值的命令选项特性。</summary>
	/// <param name="name">选项名称，不能为 <see langword="null"/>、空字符串或纯空白。</param>
	/// <param name="type">值类型，<see langword="null"/> 表示无值选项。</param>
	/// <param name="defaultValue">声明的默认值；显式 <see langword="null"/> 也属于已声明且保持为空。</param>
	/// <param name="description">文本描述。</param>
	/// <exception cref="ArgumentNullException"><paramref name="name"/> 为空或纯空白。</exception>
	public CommandOptionAttribute(string name, Type type, object defaultValue, string description = null) : this(name, '\0', type, defaultValue, false, description) { }
	/// <summary>初始化具有已声明默认值的命令选项特性。</summary>
	/// <param name="name">选项名称，不能为 <see langword="null"/>、空字符串或纯空白。</param>
	/// <param name="type">值类型，<see langword="null"/> 表示无值选项。</param>
	/// <param name="defaultValue">声明的默认值；显式 <see langword="null"/> 也属于已声明且保持为空。</param>
	/// <param name="required">是否必需。</param>
	/// <param name="description">文本描述。</param>
	/// <exception cref="ArgumentNullException"><paramref name="name"/> 为空或纯空白。</exception>
	public CommandOptionAttribute(string name, Type type, object defaultValue, bool required, string description = null) : this(name, '\0', type, defaultValue, required, description) { }
	/// <summary>初始化具有已声明默认值的命令选项特性。</summary>
	/// <param name="name">选项名称，不能为 <see langword="null"/>、空字符串或纯空白。</param>
	/// <param name="symbol">缩写字符。</param>
	/// <param name="type">值类型，<see langword="null"/> 表示无值选项。</param>
	/// <param name="defaultValue">声明的默认值；显式 <see langword="null"/> 也属于已声明且保持为空。</param>
	/// <param name="description">文本描述。</param>
	/// <exception cref="ArgumentNullException"><paramref name="name"/> 为空或纯空白。</exception>
	public CommandOptionAttribute(string name, char symbol, Type type, object defaultValue, string description = null) : this(name, symbol, type, defaultValue, false, description) { }
	/// <summary>初始化具有已声明默认值的命令选项特性。</summary>
	/// <param name="name">选项名称，不能为 <see langword="null"/>、空字符串或纯空白。</param>
	/// <param name="symbol">缩写字符。</param>
	/// <param name="type">值类型，<see langword="null"/> 表示无值选项。</param>
	/// <param name="defaultValue">声明的默认值；显式 <see langword="null"/> 也属于已声明且保持为空。</param>
	/// <param name="required">是否必需。</param>
	/// <param name="description">文本描述。</param>
	/// <exception cref="ArgumentNullException"><paramref name="name"/> 为空或纯空白。</exception>
	public CommandOptionAttribute(string name, char symbol, Type type, object defaultValue, bool required, string description = null) : this(name, symbol, type)
	{
		this.Required = required;
		this.Description = description;
		this.DefaultValue = defaultValue;
	}
	#endregion

	#region 公共属性
	/// <summary>获取命令选项的名称。</summary>
	public string Name => _name;

	/// <summary>获取命令选项的缩写字符。</summary>
	public char Symbol => _symbol;

	/// <summary>获取或设置命令选项是否必需的，默认值为假(<c>False</c>)。</summary>
	public bool Required
	{
		get => _required;
		set => _required = value;
	}

	/// <summary>获取或设置命令选项的值类型，如果返回空则表示当前选项没有值。</summary>
	public Type Type
	{
		get => _type;
		set
		{
			if(_type == value)
				return;

			if(_type != null)
				throw new InvalidOperationException();

			_type = value;
		}
	}

	/// <summary>获取命令选项的值类型转换器。</summary>
	public TypeConverter Converter
	{
		get
		{
			if(_converter == null)
			{
				var converterType = _converterType;

				if(converterType != null)
					System.Threading.Interlocked.CompareExchange(ref _converter, (TypeConverter)Activator.CreateInstance(converterType), null);
			}

			return _converter;
		}
	}

	/// <summary>获取或设置命令选项值的类型转换器的类型。</summary>
	public Type ConverterType
	{
		get => _converterType;
		set
		{
			if(_converterType == value)
				return;

			if(value != null && !typeof(TypeConverter).IsAssignableFrom(value))
				throw new ArgumentException(string.Format(Properties.Resources.CommandOption_InvalidConverter_Message, value.FullName));

			_converterType = value;
			_converter = null;
		}
	}

	/// <summary>获取一个值，指示是否明确声明了默认值。</summary>
	/// <remarks>包含显式声明的空(<c>null</c>)；没有默认值参数的构造函数不会声明默认值。</remarks>
	public bool HasDefaultValue { get; private set; }

	/// <summary>获取或设置命令选项声明的默认值。</summary>
	/// <remarks>赋值后 <see cref="HasDefaultValue" /> 为 <c>true</c>；显式 <c>null</c> 保留为 <c>null</c>，不转换成类型的零值。</remarks>
	public object DefaultValue
	{
		get => _defaultValue;
		set
		{
			if(value != null && _type != null)
				_defaultValue = Zongsoft.Common.Convert.ConvertValue(value, _type, () =>
				{
					var converter = this.Converter;

					if(converter != null)
						return converter.ConvertFrom(value);
					else
						return Zongsoft.Common.TypeExtension.GetDefaultValue(_type);
				});
			else
				_defaultValue = value;

			this.HasDefaultValue = true;
		}
	}

	/// <summary>获取或设置命令选项的文本描述。</summary>
	public string Description
	{
		get => _description;
		set => _description = value ?? string.Empty;
	}
	#endregion
}
