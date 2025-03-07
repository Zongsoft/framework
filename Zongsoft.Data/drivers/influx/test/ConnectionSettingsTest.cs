﻿using System;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Data.Influx.Tests;

public class ConnectionSettingsTest
{
	[Fact]
	public void TestGetSettings()
	{
		var connectionSettings = Global.ConnectionSettings;

		Assert.NotNull(connectionSettings);
		Assert.NotEmpty(connectionSettings);
		Assert.NotEmpty(connectionSettings.Server);
		Assert.NotEmpty(connectionSettings.Database);
		Assert.NotEmpty(connectionSettings.Token);
		Assert.True(connectionSettings.Timeout > TimeSpan.Zero);
	}
}
