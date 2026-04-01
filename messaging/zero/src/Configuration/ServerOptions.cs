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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Messaging.ZeroMQ library.
 *
 * The Zongsoft.Messaging.ZeroMQ is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Messaging.ZeroMQ is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Messaging.ZeroMQ library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Globalization;
using System.ComponentModel;

namespace Zongsoft.Messaging.ZeroMQ.Configuration;

public sealed class ServerOptions
{
	public string Name { get; set; }
	public ServerPort Port { get; set; }

	[TypeConverter(typeof(ServcerPortConverter))]
	public readonly struct ServerPort(int incoming, int outgoing)
	{
		public readonly int Incoming = incoming;
		public readonly int Outgoing = outgoing;
		public override string ToString() => this.Incoming == 0 && this.Outgoing == 0 ? "*" : $"{this.Incoming}|{this.Outgoing}";
	}

	private sealed class ServcerPortConverter : TypeConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

		public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
		{
			if (value is string text)
			{
				if(string.IsNullOrWhiteSpace(text) || text.Trim() == "*")
					return default(ServerPort);

				var index = text.IndexOfAny([',', ';', '|']);
				if(index > 0 && index < text.Length - 1 &&
				   TryGetPort(text.AsSpan()[..index], out var incoming) &&
				   TryGetPort(text.AsSpan()[(index + 1)..], out var outgoing))
					return new ServerPort(incoming, outgoing);

				throw new FormatException($"Invalid server port format: {text}");
			}

			return base.ConvertFrom(context, culture, value);

			static bool TryGetPort(ReadOnlySpan<char> text, out int port)
			{
				if(text.IsEmpty || text.Trim() == "*")
				{
					port = 0;
					return true;
				}

				return int.TryParse(text, out port);
			}
		}

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			if(value is ServerPort port)
				return port.ToString();

			return base.ConvertTo(context, culture, value, destinationType);
		}
	}
}