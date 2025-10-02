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
		Assert.False(settings.HasValue);

		settings.Value = SETTINGS;
		Assert.NotEmpty(settings);
		Assert.NotEmpty(settings.Value);
		Assert.False(settings.IsEmpty);
		Assert.True(settings.HasValue);
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

		settings["Z"] = $"' {'\n'};";
		Assert.Equal("1", settings["A"]);
		Assert.Equal("2", settings["B"]);
		Assert.Equal("0", settings["C"]);
		Assert.Equal($"' {'\n'};", settings["Z"]);
		Assert.Equal(4, settings.Count());
		Assert.Equal(@"a=1;b=2;c=0;z='\' \n;'", settings.Value);

		var newer = Settings.Parse(settings.Value);
		Assert.Equal("1", newer["A"]);
		Assert.Equal("2", newer["B"]);
		Assert.Equal("0", newer["C"]);
		Assert.Equal($"' {'\n'};", newer["Z"]);
		Assert.Equal(4, newer.Count());
		Assert.Equal(settings.Value, newer.Value);

		settings.Value = null;
		Assert.Empty(settings);
		Assert.Empty(settings.Value);
		Assert.True(settings.IsEmpty);
		Assert.False(settings.HasValue);
	}

	[Fact]
	public void TestParseSimple()
	{
		Assert.Empty(Settings.Parse(""));
		Assert.Empty(Settings.Parse(" "));

		var settings = Settings.Parse("key1=value1");
		Assert.NotNull(settings);
		Assert.NotEmpty(settings);
		Assert.Single(settings);
		Assert.Equal("key1", settings.First().Key);
		Assert.Equal("value1", settings.First().Value);

		settings = Settings.Parse("key1=value1;");
		Assert.NotNull(settings);
		Assert.NotEmpty(settings);
		Assert.Single(settings);
		Assert.Equal("key1", settings.First().Key);
		Assert.Equal("value1", settings.First().Value);

		settings = Settings.Parse("key1=value1;key2=value2");
		Assert.NotNull(settings);
		Assert.NotEmpty(settings);
		Assert.Equal(2, settings.Count());
		Assert.Equal("key1", settings.First().Key);
		Assert.Equal("value1", settings.First().Value);
		Assert.Equal("key2", settings.Last().Key);
		Assert.Equal("value2", settings.Last().Value);
	}

	[Fact]
	public void TestParse()
	{
		var TEXT = @" key1=value1; key2 = value2; key3 = ; key 4 = value 4 ; key5; key 6 = ' value\t;\nEnd\\'; ";

		Assert.True(Settings.TryParse(TEXT, out var settings));
		Assert.NotNull(settings);
		Assert.True(settings.HasValue);
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
					Assert.Equal(" value\t;\nEnd\\", entry.Value);
					break;
			}
		}
	}
}
