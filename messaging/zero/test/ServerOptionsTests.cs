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
using System.ComponentModel;
using System.Globalization;

using Xunit;

using Zongsoft.Messaging.ZeroMQ.Configuration;

namespace Zongsoft.Messaging.ZeroMQ.Tests;

public class ServerOptionsTests
{
	[Fact]
	public void ServerPortConstructorUsesControlIncomingOutgoingOrder()
	{
		var full = new ServerOptions.ServerPort(32100, 32101, 32102);
		Assert.Equal(32100, full.Control);
		Assert.Equal(32101, full.Incoming);
		Assert.Equal(32102, full.Outgoing);

		var broadcast = new ServerOptions.ServerPort(32101, 32102);
		Assert.Equal(0, broadcast.Control);
		Assert.Equal(32101, broadcast.Incoming);
		Assert.Equal(32102, broadcast.Outgoing);
	}

	[Theory]
	[InlineData("*", 0, 0, 0)]
	[InlineData("32101|32102", 0, 32101, 32102)]
	[InlineData("32100,32101,32102", 32100, 32101, 32102)]
	[InlineData("32100; 32101; 32102", 32100, 32101, 32102)]
	public void ServerPortConverterParsesSupportedForms(string text, int control, int incoming, int outgoing)
	{
		var converter = TypeDescriptor.GetConverter(typeof(ServerOptions.ServerPort));
		var port = Assert.IsType<ServerOptions.ServerPort>(converter.ConvertFrom(null, CultureInfo.InvariantCulture, text));

		Assert.Equal(control, port.Control);
		Assert.Equal(incoming, port.Incoming);
		Assert.Equal(outgoing, port.Outgoing);
	}

	[Theory]
	[InlineData(0, 0, 0, "*")]
	[InlineData(0, 32101, 32102, "32101|32102")]
	[InlineData(32100, 32101, 32102, "32100|32101|32102")]
	public void ServerPortConverterFormatsCanonicalOrder(int control, int incoming, int outgoing, string expected)
	{
		var converter = TypeDescriptor.GetConverter(typeof(ServerOptions.ServerPort));
		var port = new ServerOptions.ServerPort(control, incoming, outgoing);

		Assert.Equal(expected, port.ToString());
		Assert.Equal(expected, converter.ConvertTo(null, CultureInfo.InvariantCulture, port, typeof(string)));
	}

	[Theory]
	[InlineData("32100")]
	[InlineData("32100|32101|32102|32103")]
	[InlineData("control|32101|32102")]
	public void ServerPortConverterRejectsMalformedText(string text)
	{
		var converter = TypeDescriptor.GetConverter(typeof(ServerOptions.ServerPort));
		Assert.Throws<FormatException>(() => converter.ConvertFrom(null, CultureInfo.InvariantCulture, text));
	}
}
