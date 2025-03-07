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
using System.ComponentModel.Design.Serialization;

namespace Zongsoft.Components.Converters;

public class GuidConverter : System.ComponentModel.GuidConverter
{
	public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
	{
		if(sourceType == typeof(byte[]))
			return true;

		return base.CanConvertFrom(context, sourceType);
	}

	public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
	{
		if(destinationType == typeof(InstanceDescriptor) || destinationType == typeof(byte[]))
			return true;

		return base.CanConvertTo(context, destinationType);
	}

	public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
	{
		if(value is byte[])
		{
			var array = value as byte[];

			if(array.Length == 16)
				return new Guid(array);

			if(array.Length > 16)
			{
				return new Guid(BitConverter.ToUInt32(array, 0), BitConverter.ToUInt16(array, 4), BitConverter.ToUInt16(array, 6),
					array[8], array[9], array[10], array[11], array[12], array[13], array[14], array[15]);
			}
		}

		return base.ConvertFrom(context, culture, value);
	}

	public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
	{
		if(destinationType == typeof(InstanceDescriptor) && value is Guid)
		{
			var constructor = typeof(Guid).GetConstructor([typeof(string)]);

			if(constructor != null)
				return new InstanceDescriptor(constructor, new object[] { value.ToString() });
		}
		else if(destinationType == typeof(byte[]) && value is Guid guid)
			return guid.ToByteArray();

		return base.ConvertTo(context, culture, value, destinationType);
	}
}
