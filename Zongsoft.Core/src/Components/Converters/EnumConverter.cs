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
using System.Globalization;
using System.ComponentModel;

namespace Zongsoft.Components.Converters;

public class EnumConverter : System.ComponentModel.EnumConverter
{
	#region 成员变量
	private Common.EnumEntry[] _entries;
	#endregion

	#region 构造函数
	public EnumConverter(Type enumType) : base(enumType) { }
	#endregion

	#region 重写方法
	public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string) || sourceType == typeof(DBNull) ||
		sourceType == typeof(byte) || sourceType == typeof(sbyte) ||
		sourceType == typeof(short) || sourceType == typeof(ushort) ||
		sourceType == typeof(int) || sourceType == typeof(uint) ||
		sourceType == typeof(long) || sourceType == typeof(ulong) ||
		sourceType == typeof(double) || sourceType == typeof(float) ||
		sourceType == typeof(decimal) || base.CanConvertFrom(context, sourceType);

	public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType == typeof(string) ||
		destinationType == typeof(byte) || destinationType == typeof(sbyte) ||
		destinationType == typeof(short) || destinationType == typeof(ushort) ||
		destinationType == typeof(int) || destinationType == typeof(uint) ||
		destinationType == typeof(long) || destinationType == typeof(ulong) ||
		destinationType == typeof(double) || destinationType == typeof(float) ||
		destinationType == typeof(decimal) || base.CanConvertTo(context, destinationType);

	public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
	{
		if(value == null || Convert.IsDBNull(value))
			return Common.TypeExtension.GetDefaultValue(this.EnumType);

		if(value is string valueString)
		{
			if(valueString.IndexOf(',') > 1)
			{
				long convertedValue = 0;
				var parts = valueString.Split(',');

				foreach(var part in parts)
				{
					if(!string.IsNullOrWhiteSpace(part))
						convertedValue |= GetEnumValue(part, true);
				}

				return Enum.ToObject(this.EnumType, convertedValue);
			}
			else
			{
				return Enum.ToObject(this.EnumType, GetEnumValue(valueString, true));
			}
		}
		else if(value.GetType().IsPrimitive || value.GetType() == typeof(decimal))
		{
			switch(Type.GetTypeCode(value.GetType()))
			{
				case TypeCode.Double:
				case TypeCode.Single:
				case TypeCode.Decimal:
					value = Convert.ToInt64(value);
					break;
			}

			return Enum.ToObject(this.EnumType, value);
		}
		else if(value is Enum[] enumerates)
		{
			var enumValue = 0L;

			foreach(var item in enumerates)
				enumValue |= Convert.ToInt64(item, culture);

			return Enum.ToObject(this.EnumType, enumValue);
		}

		return base.ConvertFrom(context, culture, value);
	}

	public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
	{
		if(destinationType == typeof(string))
			return value?.ToString();

		var underlyingType = Enum.GetUnderlyingType(value.GetType());
		var underlyingValue = Convert.ChangeType(value, underlyingType);
		return Convert.ChangeType(underlyingValue, destinationType);
	}

	public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
	{
		if(this.Values != null)
			return this.Values;

		var baseIndex = 0;
		var enumType = this.EnumType;
		var underlyingTypeOfNullable = Nullable.GetUnderlyingType(typeof(Enum));

		if(underlyingTypeOfNullable != null)
		{
			enumType = underlyingTypeOfNullable;
			baseIndex = 1;
		}

		var array = Enum.GetValues(enumType);
		var values = new Enum[array.Length + baseIndex];
		array.CopyTo(values, baseIndex);

		this.Values = new StandardValuesCollection(values);
		return this.Values;
	}
	#endregion

	#region 私有方法
	private long GetEnumValue(string valueText, bool throwExceptions)
	{
		if(this.TryGetEnumValue(valueText, out var result))
			return result;

		if(throwExceptions)
			throw new FormatException(string.Format("Can not from this '{0}' string convert to '{1}' enum.", valueText, this.EnumType.AssemblyQualifiedName));

		return 0;
	}

	private bool TryGetEnumValue(string valueText, out long underlyingValue)
	{
		underlyingValue = 0;

		if(string.IsNullOrWhiteSpace(valueText))
			return false;

		valueText = valueText.Trim();

		if(long.TryParse(valueText, out underlyingValue))
			return true;

		if(_entries == null)
			_entries = Common.EnumUtility.GetEnumEntries(this.EnumType, true);

		foreach(var entry in _entries)
		{
			if(entry.Value == null)
				continue;

			if(string.Equals(valueText, entry.Name, StringComparison.OrdinalIgnoreCase))
			{
				underlyingValue = Convert.ToInt64(entry.Value);
				return true;
			}

			if(string.Equals(valueText, entry.Value.ToString(), StringComparison.OrdinalIgnoreCase))
			{
				underlyingValue = Convert.ToInt64(entry.Value);
				return true;
			}

			if(entry.HasAlias(valueText))
			{
				underlyingValue = Convert.ToInt64(entry.Value);
				return true;
			}
		}

		return false;
	}
	#endregion
}
