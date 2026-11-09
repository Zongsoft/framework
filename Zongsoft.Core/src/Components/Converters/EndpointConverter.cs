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
using System.Net;
using System.Globalization;
using System.ComponentModel;

namespace Zongsoft.Components.Converters;

public class EndpointConverter : TypeConverter
{
	public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string);
	public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
	{
		if(value is string text)
		{
			if(IPEndPoint.TryParse(text, out var ip))
				return ip;

			var index = text.LastIndexOf(':');

			if(index > 0 && index < text.Length)
			{
				if(int.TryParse(text.AsSpan()[(index + 1)..], out var port))
					return new DnsEndPoint(text[..index], port);

				throw new InvalidOperationException(string.Format(Properties.Resources.Conversion_ValueType_Message, text, typeof(global::System.Net.EndPoint)));
			}

			return new DnsEndPoint(text, 0);
		}

		return null;
	}
	public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType == typeof(string);
	public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
	{
		if(value is DnsEndPoint dns)
			return dns.Port > 0 ? $"{dns.Host}:{dns.Port}" : dns.Host;
		else
			return value?.ToString();
	}
}
