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
using System.Text;

namespace Zongsoft.ComponentModel
{
	public class EncodingConverter : System.ComponentModel.TypeConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			if(sourceType.IsPrimitive || sourceType == typeof(string) || sourceType == typeof(decimal))
				return true;

			return base.CanConvertFrom(context, sourceType);
		}

		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
		{
			if(destinationType.IsPrimitive || destinationType == typeof(string) || destinationType == typeof(decimal))
				return true;

			return base.CanConvertTo(context, destinationType);
		}

		public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
		{
			if(value == null)
				return Encoding.UTF8;

			if(value.GetType() == typeof(string))
			{
				return ((string)value).ToLowerInvariant() switch
				{
					"utf8" or "utf-8" => Encoding.UTF8,
					"utf7" or "utf-7" => Encoding.UTF7,
					"utf32" => Encoding.UTF32,
					"unicode" => Encoding.Unicode,
					"ascii" => Encoding.ASCII,
					"bigend" or "bigendian" => Encoding.BigEndianUnicode,
					_ => Encoding.GetEncoding((string)value),
				};
			}
			else if(value.GetType().IsPrimitive || value.GetType() == typeof(decimal))
			{
				return Encoding.GetEncoding((int)System.Convert.ChangeType(value, typeof(int)));
			}

			return base.ConvertFrom(context, culture, value);
		}

		public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
		{
			var encoding = value as Encoding;

			if(destinationType == typeof(string))
				return encoding == null ? "utf-8" : encoding.EncodingName;

			if(value.GetType().IsPrimitive || value.GetType() == typeof(decimal))
				return encoding == null ? Encoding.UTF8.CodePage : encoding.CodePage;

			return base.ConvertTo(context, culture, value, destinationType);
		}
	}
}
