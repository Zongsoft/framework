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
