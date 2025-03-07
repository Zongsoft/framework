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
using System.Text;
using System.Globalization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Zongsoft.Components.Converters;

public class CollectionConverter : TypeConverter
{
	#region 单例字段
	public static readonly CollectionConverter Default = new();
	#endregion

	#region 构造函数
	public CollectionConverter() : this(null) { }
	public CollectionConverter(char[] separators)
	{
		this.Separators = separators == null || separators.Length == 0 ? ['|', ';', ',', '\n'] : separators;
	}
	#endregion

	#region 公共属性
	public char[] Separators { get; }
	#endregion

	#region 重写方法
	public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
	{
		if(destinationType == typeof(string))
			return true;

		var elementType = Common.TypeExtension.GetElementType(destinationType);
		return elementType != null && elementType != typeof(object);
	}

	public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
	{
		if(value == null)
			return null;

		if(destinationType == typeof(string))
			return this.Convert(value);

		if(value is string text)
			return this.Convert(text, destinationType);

		return null;
	}
	#endregion

	#region 私有方法
	private string Convert(object value)
	{
		if(value is System.Collections.IEnumerable enumerable)
		{
			var text = new StringBuilder();

			foreach(var item in enumerable)
			{
				if(text.Length > 0)
					text.Append(this.Separators[0]);

				text.Append(Common.Convert.ConvertValue<string>(item));
			}

			return text.ToString();
		}

		return value?.ToString();
	}

	private object Convert(string text, Type conversionType)
	{
		var elementType = Common.TypeExtension.GetCollectionElementType(conversionType);

		var parts = text.Split(this.Separators, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
		if(parts.Length == 0)
			return null;

		if(conversionType.IsArray)
		{
			var array = Array.CreateInstance(elementType, parts.Length);

			for(var i = 0; i < parts.Length; i++)
				array.SetValue(Common.Convert.ConvertValue(parts[i], elementType), i);

			return array;
		}

		var result = conversionType.IsAbstract ?
			Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType)) :
			Activator.CreateInstance(conversionType);

		for(var i = 0; i < parts.Length; i++)
			Collections.CollectionUtility.TryAdd(result, Common.Convert.ConvertValue(parts[i], elementType));

		return result;
	}
	#endregion
}
