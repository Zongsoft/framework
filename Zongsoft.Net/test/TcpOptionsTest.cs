/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Net library.
 *
 * The Zongsoft.Net is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Net is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Net library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading.Tasks;

using Xunit;

namespace Zongsoft.Net.Tests;

public class TcpOptionsTest
{
	[Theory]
	[InlineData(nameof(TcpConnectionOptions.MaintenanceInterval), 1L)]
	[InlineData(nameof(TcpConnectionOptions.MaintenanceInterval), 9999L)]
	[InlineData(nameof(TcpConnectionOptions.MaintenanceInterval), 42949672950000L)]
	[InlineData(nameof(TcpConnectionOptions.ShutdownTimeout), 42949672950000L)]
	public void TimerSettingsRejectUnsupportedRangesAtConstruction(string property, long ticks)
	{
		var clientOptions = new TcpClientOptions();
		var serverOptions = new TcpServerOptions();
		var setting = typeof(TcpConnectionOptions).GetProperty(property);
		setting.SetValue(clientOptions, TimeSpan.FromTicks(ticks));
		setting.SetValue(serverOptions, TimeSpan.FromTicks(ticks));

		Assert.Equal(property, Assert.Throws<ArgumentOutOfRangeException>(() => TcpClient.CreateHeaded(clientOptions)).ParamName);
		Assert.Equal(property, Assert.Throws<ArgumentOutOfRangeException>(() => TcpServer.CreateHeaded(serverOptions)).ParamName);
	}

	[Fact]
	public void ConnectDeadlineRejectsUnsupportedRangeAtConstruction()
	{
		var options = new TcpClientOptions { ConnectTimeout = TimeSpan.FromDays(60) };

		Assert.Equal(nameof(options.ConnectTimeout), Assert.Throws<ArgumentOutOfRangeException>(() => TcpClient.CreateHeaded(options)).ParamName);
	}

	[Fact]
	public void BroadcastDeadlineRejectsUnsupportedRangeAtConstruction()
	{
		var options = new TcpServerOptions { SendTimeout = TimeSpan.FromDays(60) };

		Assert.Equal(nameof(options.SendTimeout), Assert.Throws<ArgumentOutOfRangeException>(() => TcpServer.CreateHeaded(options)).ParamName);
	}

	[Fact]
	public async Task TimerBoundaryAndLongElapsedTimeSettingsRemainSupported()
	{
		var maximum = TimeSpan.FromMilliseconds(uint.MaxValue - 1L);
		await using var client = TcpClient.CreateHeaded(new()
		{
			ConnectTimeout = maximum,
			ShutdownTimeout = maximum,
			SendTimeout = TimeSpan.FromDays(60),
			MaintenanceInterval = maximum,
			FrameTimeout = TimeSpan.FromDays(60),
			IdleTimeout = TimeSpan.FromDays(60),
		});
		await using var server = TcpServer.CreateHeaded(new() { MaintenanceInterval = TimeSpan.FromMilliseconds(1), SendTimeout = maximum });

		Assert.Null(client.Channel);
		Assert.Empty(server.Channels);
	}
}
