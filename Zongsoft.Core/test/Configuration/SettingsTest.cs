using System;
using System.Linq;

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

	[Fact]
	public void TestParse()
	{
		var TEXT = @" key1=value1; key2 = value2; key3 = ; key 4 = value 4 ; key5; key 6 = ' value\t;\nEnd'; ";

		Assert.True(Settings.TryParse(TEXT, out var settings));
		Assert.NotNull(settings);
		Assert.False(settings.IsEmpty);

		int index = 0;
		foreach(var entry in settings)
		{
			switch(index++)
			{
				case 0:
					Assert.Equal("key1", entry.Key);
					Assert.Equal("value1", entry.Value);
					break;
				case 1:
					Assert.Equal("key2", entry.Key);
					Assert.Equal("value2", entry.Value);
					break;
				case 2:
					Assert.Equal("key3", entry.Key);
					Assert.True(string.IsNullOrEmpty(entry.Value));
					break;
				case 3:
					Assert.Equal("key 4", entry.Key);
					Assert.Equal("value 4", entry.Value);
					break;
				case 4:
					Assert.Equal("key5", entry.Key);
					Assert.True(string.IsNullOrEmpty(entry.Value));
					break;
				case 5:
					Assert.Equal("key 6", entry.Key);
					Assert.Equal(" value\t;\nEnd", entry.Value);
					break;
			}
		}
	}
}
