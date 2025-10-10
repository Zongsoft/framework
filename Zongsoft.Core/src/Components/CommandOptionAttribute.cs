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
	public CommandOptionAttribute(string name, Type type = null, string description = null) : this(name, '\0', type, null, false, description) { }
	public CommandOptionAttribute(string name, char symbol = '\0', Type type = null, string description = null) : this(name, symbol, type, null, false, description) { }

	public CommandOptionAttribute(string name, Type type, object defaultValue, string description = null) : this(name, '\0', type, defaultValue, false, description) { }
	public CommandOptionAttribute(string name, Type type, object defaultValue, bool required, string description = null) : this(name, '\0', type, defaultValue, required, description) { }

	public CommandOptionAttribute(string name, char symbol, Type type, object defaultValue, string description = null) : this(name, symbol, type, defaultValue, false, description) { }
	public CommandOptionAttribute(string name, char symbol, Type type, object defaultValue, bool required, string description = null)
	{
		if(string.IsNullOrWhiteSpace(name))
			throw new ArgumentNullException(nameof(name));

		if(type == null)
			_defaultValue = defaultValue;
		else
			_defaultValue = Zongsoft.Common.Convert.ConvertValue(defaultValue, type);

		_name = name;
		_type = type;
		_symbol = symbol;
		_required = required;
		_description = description ?? string.Empty;
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
				throw new ArgumentException($"The '{value.FullName}' type is not TypeConverter.");

			_converterType = value;
			_converter = null;
		}
	}

	/// <summary>获取或设置命令选项的默认值。</summary>
	public object DefaultValue
	{
		get => _defaultValue;
		set
		{
			if(_type != null)
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
