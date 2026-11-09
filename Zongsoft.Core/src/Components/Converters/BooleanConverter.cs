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
using System.Globalization;
using System.ComponentModel;

using Zongsoft.Common;

namespace Zongsoft.Components.Converters;

public class BooleanConverter : TypeConverter
{
	public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string) || sourceType.IsInteger();
	public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
	public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
	{
		if(value == null)
			return false;

		if(value is string text)
		{
			if(string.Equals(text, "1", StringComparison.OrdinalIgnoreCase) ||
			   string.Equals(text, "on", StringComparison.OrdinalIgnoreCase) ||
			   string.Equals(text, "yes", StringComparison.OrdinalIgnoreCase) ||
			   string.Equals(text, "enable", StringComparison.OrdinalIgnoreCase) ||
			   string.Equals(text, "enabled", StringComparison.OrdinalIgnoreCase))
				return true;

			if(string.Equals(text, "0", StringComparison.OrdinalIgnoreCase) ||
			   string.Equals(text, "no", StringComparison.OrdinalIgnoreCase) ||
			   string.Equals(text, "off", StringComparison.OrdinalIgnoreCase) ||
			   string.Equals(text, "disable", StringComparison.OrdinalIgnoreCase) ||
			   string.Equals(text, "disabled", StringComparison.OrdinalIgnoreCase))
				return false;

			return bool.Parse(text);
		}

		return value switch
		{
			char chr => chr == 'T' || chr == 't' || chr == '1',
			byte number => number != 0,
			sbyte number => number != 0,
			short number => number != 0,
			ushort number => number != 0,
			int number => number != 0,
			uint number => number != 0,
			long number => number != 0,
			ulong number => number != 0,
			float number => number != 0,
			double number => number != 0,
			decimal number => number != 0,
			_ => false,
		};
	}

	public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
	{
		if(destinationType == typeof(string))
			return value.ToString();

		return base.ConvertTo(context, culture, value, destinationType);
	}
}
