using System;
using System.Linq;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Configuration.Tests;

public class SettingsContractTest
{
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
