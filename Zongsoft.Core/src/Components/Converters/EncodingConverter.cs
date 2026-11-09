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
using System.Text;
using System.ComponentModel;

namespace Zongsoft.Components.Converters;

public class EncodingConverter : TypeConverter
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
				"utf8" or "utf-8" or "" => Encoding.UTF8,
				"utf7" or "utf-7" => Encoding.UTF7,
				"utf32" or "utf-32" => Encoding.UTF32,
				"ascii" => Encoding.ASCII,
				"latin1" => Encoding.Latin1,
				"unicode" => Encoding.Unicode,
				"bigend" or "bigendian" => Encoding.BigEndianUnicode,
				_ => Encoding.GetEncoding((string)value),
			};
		}
		else if(value.GetType().IsPrimitive || value.GetType() == typeof(decimal))
		{
			return Encoding.GetEncoding((int)Convert.ChangeType(value, typeof(int)));
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
