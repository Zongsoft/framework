using System;
using System.Linq;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Configuration.Tests;

public class SettingsTest
{
	[Fact]
	public void Test()
	{
		const string SETTINGS = " a=1; b =2;; c= 3; d =		4  ; ";

		var settings = new Settings(string.Empty);
		Assert.NotNull(settings);
		Assert.Empty(settings);
		Assert.Empty(settings.Value);
		Assert.True(settings.IsEmpty);

		settings.Value = SETTINGS;
		Assert.NotEmpty(settings);
		Assert.NotEmpty(settings.Value);
		Assert.False(settings.IsEmpty);
		Assert.Equal(4, settings.Count());

		Assert.Equal("1", settings["A"]);
		Assert.Equal("2", settings["b"]);
		Assert.Equal("3", settings["C"]);
		Assert.Equal("4", settings["d"]);
		Assert.Null(settings["z"]);

		settings["z"] = "26";
		Assert.Equal("26", settings["Z"]);
		Assert.Equal(5, settings.Count());
		Assert.Equal("a=1;b=2;c=3;d=4;z=26", settings.Value);

		settings["D"] = string.Empty;
		Assert.Null(settings["d"]);
		Assert.Equal("1", settings["A"]);
		Assert.Equal("2", settings["B"]);
		Assert.Equal("3", settings["C"]);
		Assert.Equal("26", settings["Z"]);
		Assert.Equal(4, settings.Count());
		Assert.Equal("a=1;b=2;c=3;z=26", settings.Value);

		settings["c"] = "0";
		Assert.Equal("1", settings["A"]);
		Assert.Equal("2", settings["B"]);
		Assert.Equal("0", settings["C"]);
		Assert.Equal("26", settings["Z"]);
		Assert.Equal(4, settings.Count());
		Assert.Equal("a=1;b=2;c=0;z=26", settings.Value);

		settings.Value = null;
		Assert.Empty(settings);
		Assert.Empty(settings.Value);
		Assert.True(settings.IsEmpty);
	}
}
