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
 * Copyright (C) 2025-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Learning library.
 *
 * The Zongsoft.Learning is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Learning is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Learning library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Globalization;
using System.ComponentModel;

using Microsoft.ML;

namespace Zongsoft.Learning;

internal class InputOutputColumnPairConverter : TypeConverter
{
	public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string) && base.CanConvertFrom(context, sourceType);
	public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType == typeof(string) && base.CanConvertTo(context, destinationType);

	public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
	{
		if(value is string text)
		{
			if(string.IsNullOrEmpty(text))
				return null;

			var index = text.IndexOfAny([':', '=']);
			if(index <= 0)
			{
				index = text.IndexOf("->");

				if(index <= 0)
					return new InputOutputColumnPair(text);

				return new InputOutputColumnPair(text[(index + 2)..], text[..index]);
			}
			else
			{
				return new InputOutputColumnPair(text[..index], text[(index + 1)..]);
			}
		}

		return base.ConvertFrom(context, culture, value);
	}

	public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
	{
		if(destinationType == typeof(string))
		{
			if(value is InputOutputColumnPair pair)
				return $"{pair.InputColumnName}->{pair.OutputColumnName}";
		}

		return base.ConvertTo(context, culture, value, destinationType);
	}
}
