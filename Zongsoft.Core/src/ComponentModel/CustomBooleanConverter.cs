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

namespace Zongsoft.ComponentModel
{
	public class CustomBooleanConverter : BooleanConverter
	{
		#region 成员字段
		private string _trueString;
		private string _falseString;
		#endregion

		#region 构造函数
		public CustomBooleanConverter() : this(null, null) { }
		public CustomBooleanConverter([Localizable(true)]string trueString, [Localizable(true)]string falseString)
		{
			if(string.IsNullOrEmpty(trueString))
				_trueString = bool.TrueString;
			else
				_trueString = trueString;

			if(string.IsNullOrEmpty(falseString))
				_falseString = bool.FalseString;
			else
				_falseString = falseString;
		}
		#endregion

		#region 公共属性
		[Localizable(true)]
		public string TrueString
		{
			get
			{
				return _trueString;
			}
			set
			{
				if(string.IsNullOrEmpty(value))
					value = bool.TrueString;

				if(string.Equals(_trueString, value, StringComparison.Ordinal))
					return;

				_trueString = value;
			}
		}

		[Localizable(true)]
		public string FalseString
		{
			get
			{
				return _falseString;
			}
			set
			{
				if(string.IsNullOrEmpty(value))
					value = bool.FalseString;

				if(string.Equals(_falseString, value, StringComparison.Ordinal))
					return;

				_falseString = value;
			}
		}
		#endregion

		#region 公共方法
		public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
		{
			if(value is string text)
			{
				if(bool.TryParse(text, out var result))
					return result;

				if(string.Equals(text, "yes", StringComparison.OrdinalIgnoreCase) ||
				   string.Equals(text, "on", StringComparison.OrdinalIgnoreCase))
					return true;

				if(string.Equals(_trueString, text, StringComparison.OrdinalIgnoreCase))
					return true;
				if(string.Equals(_falseString, text, StringComparison.OrdinalIgnoreCase))
					return false;

				if(string.IsNullOrWhiteSpace(text) && Common.TypeExtension.IsNullable(context.PropertyDescriptor.PropertyType))
					return null;
				else
					return false;
			}

			return base.ConvertFrom(context, culture, value);
		}

		public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
		{
			if(destinationType == typeof(string))
			{
				if(value == null)
					return null;

				return (bool)value ? _trueString : _falseString;
			}

			return base.ConvertTo(context, culture, value, destinationType);
		}

		public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
		{
			if(Common.TypeExtension.IsNullable(context.PropertyDescriptor.PropertyType))
				return new TypeConverter.StandardValuesCollection(new object[] { null, true, false });
			else
				return new TypeConverter.StandardValuesCollection(new object[] { true, false });
		}
		#endregion
	}
}
