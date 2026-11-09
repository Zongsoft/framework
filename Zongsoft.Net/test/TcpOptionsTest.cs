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
using System.Threading.Tasks;

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
