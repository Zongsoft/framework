using System;
using System.Linq;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Configuration.Tests;

public class SettingsTest
{
	[Fact]
	public void Equality_EquivalentEntriesWithDifferentOrderAndCase_HaveCompatibleHashCode()
	{
		var first = new Settings("Profile");
		first["Alpha"] = "1";
		first["Beta"] = "2";

		var second = new Settings("profile");
		second["BETA"] = "2";
		second["ALPHA"] = "1";

		var settings = new Dictionary<Settings, string> { [first] = "matched" };

		Assert.Equal(first, second);
		Assert.Equal(first.GetHashCode(), second.GetHashCode());
		Assert.Equal("matched", settings[second]);
	}

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
		const string TEXT = @" key1=value1; key2 = value2; key3 = ; key 4 = value 4 ; key5; key 6 = ' value\t;\nEnd\\'; ";

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

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void ConnectionImplementsSettingsInterfaceWithSharedRawState(bool specialized)
	{
		var connection = CreateConnection(specialized, "Source", "Port=3306;Text=initial");
		var settings = Assert.IsAssignableFrom<ISettings>(connection);
		Assert.Same(connection, settings);
		Assert.Equal("Source", settings.Name);
		Assert.False(settings.IsEmpty);
		Assert.True(settings.HasValue);
		Assert.Equal(2, settings.Count);
		Assert.Equal("3306", settings["Port"]);
		Assert.True(settings.TryGetValue("port", out var original));
		Assert.Equal("3306", original);

		settings["port"] = "3308";

		Assert.Equal("3308", connection["Port"]);
		Assert.True(connection.TryGetValue("Port", out var updated));
		Assert.Equal("3308", updated);
		Assert.Equal("initial", connection["Text"]);
		Assert.Equal("3308", Settings.Parse(connection.Value)["Port"]);

		settings.Value = "Port=3310;Text=restored";

		Assert.Equal("3310", connection["Port"]);
		Assert.Equal("restored", connection["Text"]);
		Assert.Equal(connection.Value, settings.Value);
		Assert.Equal(2, settings.Count);
		Assert.Equal(connection.OrderBy(entry => entry.Key), settings.OrderBy(entry => entry.Key));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void ConnectionInheritsSettingsWithSharedRawState(bool specialized)
	{
		var connection = CreateConnection(specialized, "Source", "Port=3306;Text=initial");
		var settings = Assert.IsAssignableFrom<Settings>(connection);
		Assert.Same(connection, settings);
		Assert.Equal("Source", settings.Name);
		Assert.Equal("3306", settings["Port"]);
		Assert.True(settings.ContainsKey("port"));
		Assert.Equal(2, settings.Count());

		connection["Port"] = "3308";

		Assert.Equal("3308", settings["Port"]);
		Assert.True(settings.TryGetValue("PORT", out var updated));
		Assert.Equal("3308", updated);
		Assert.Equal("3308", Settings.Parse(settings.Value)["Port"]);

		settings["Port"] = null;

		Assert.False(connection.ContainsKey("Port"));
		Assert.False(connection.TryGetValue("Port", out _));
		Assert.Null(connection["Port"]);
		Assert.False(settings.ContainsKey("Port"));
		Assert.Null(settings["Port"]);
		Assert.Equal("initial", connection["Text"]);
		Assert.Single(settings);
		Assert.False(Settings.Parse(settings.Value).ContainsKey("Port"));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void ConnectionContentEqualityAgreesThroughSettingsViews(bool specialized)
	{
		var first = CreateConnection(specialized, "Source", "Port=3306;Text=initial");
		var second = CreateConnection(specialized, "SOURCE", "Text=initial;PORT=3306");
		first.Properties["Owner"] = "team";
		second.Properties["OWNER"] = "team";
		var firstSettings = Assert.IsAssignableFrom<Settings>(first);
		var secondSettings = Assert.IsAssignableFrom<Settings>(second);
		var firstInterface = Assert.IsAssignableFrom<ISettings>(first);
		var secondInterface = Assert.IsAssignableFrom<ISettings>(second);

		AssertEquality(firstSettings, secondSettings, firstInterface, secondInterface, true);

		second.Properties["Owner"] = "another-team";

		AssertEquality(firstSettings, secondSettings, firstInterface, secondInterface, false);

		second.Properties["Owner"] = "team";
		secondInterface["Port"] = "3308";

		AssertEquality(firstSettings, secondSettings, firstInterface, secondInterface, false);
		Assert.Equal("3306", first["Port"]);

		secondInterface["Port"] = "3306";

		AssertEquality(firstSettings, secondSettings, firstInterface, secondInterface, true);

		var ordinary = new Settings("Source", "Port=3306;Text=initial");
		Assert.False(firstSettings.Equals(ordinary));
		Assert.False(ordinary.Equals(firstSettings));
		Assert.False(firstInterface.Equals(ordinary));
		Assert.False(((ISettings)ordinary).Equals(firstInterface));
		Assert.False(object.Equals(firstSettings, ordinary));
		Assert.False(object.Equals(ordinary, firstSettings));
	}

	[Fact]
	public void DifferentDriversRemainUnequalThroughSettingsViews()
	{
		var first = Assert.IsAssignableFrom<Settings>(CreateConnection(false, "Source", "Port=3306;Text=initial"));
		var second = Assert.IsAssignableFrom<Settings>(CreateConnection(true, "Source", "Port=3306;Text=initial"));

		AssertEquality(first, second, first, second, false);
	}

	private static IConnectionSettings CreateConnection(bool specialized, string name, string value) => specialized ? ConnectionSettingsTest.MyDriver.Instance.GetSettings(name, value) : new ConnectionSettings(name, value);

	private static void AssertEquality(Settings first, Settings second, ISettings firstInterface, ISettings secondInterface, bool expected)
	{
		Assert.Equal(expected, first.Equals(second));
		Assert.Equal(expected, second.Equals(first));
		Assert.Equal(expected, first.Equals(secondInterface));
		Assert.Equal(expected, second.Equals(firstInterface));
		Assert.Equal(expected, firstInterface.Equals(secondInterface));
		Assert.Equal(expected, secondInterface.Equals(firstInterface));
		Assert.Equal(expected, ((IEquatable<Settings>)first).Equals(second));
		Assert.Equal(expected, ((IEquatable<Settings>)second).Equals(first));
		Assert.Equal(expected, object.Equals(first, second));
		Assert.Equal(expected, object.Equals(second, first));
		Assert.Equal(expected, EqualityComparer<Settings>.Default.Equals(first, second));
		Assert.Equal(expected, EqualityComparer<ISettings>.Default.Equals(firstInterface, secondInterface));
		Assert.Equal(expected, new HashSet<Settings> { first }.Contains(second));
		Assert.Equal(expected, new HashSet<ISettings> { firstInterface }.Contains(secondInterface));

		if(expected)
			Assert.Equal(first.GetHashCode(), second.GetHashCode());
	}
}
