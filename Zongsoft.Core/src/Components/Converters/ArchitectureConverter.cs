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
using System.Runtime.InteropServices;

namespace Zongsoft.Components.Converters;

public sealed class ArchitectureConverter : TypeConverter
{
	public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
	public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

	public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
	{
		if(value == null)
			return Architecture.X64;

		if(value is string text)
		{
			if(string.IsNullOrWhiteSpace(text))
				return Architecture.X64;

			return text.Trim().ToLowerInvariant() switch
			{
				"x64" or "amd64" => Architecture.X64,
				"x32" or "x86" or "i386" => Architecture.X86,
				"arm64" or "aarch64" => Architecture.Arm64,
				"arm32" or "arm" or "armhf" => Architecture.Arm,
				"loong" or "loongArch64" => Architecture.LoongArch64,
				"ppc64" or "ppc64le" => Architecture.Ppc64le,
				_ => Enum.Parse<Architecture>(text, true),
			};
		}

		return base.ConvertFrom(context, culture, value);
	}

	public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
	{
		if(destinationType == typeof(string))
			return value.ToString();

		return base.ConvertTo(context, culture, value, destinationType);
	}
}
