using System;
using System.Linq;
using System.ComponentModel;
using System.Collections.Generic;

using Xunit;

using Zongsoft.Components;

namespace Zongsoft.Configuration.Tests;

public class ConnectionSettingsStateTest
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void PropertiesIndexerAssignmentUpdatesValueBeforeAndAfterReading(bool readBeforeMutation)
	{
		var settings = CreateStateSettings(readBeforeMutation);

		settings.Properties["ABC"] = "updated";

		Assert.Equal("updated", settings.Properties["ABC"]);
		Assert.Equal(2, settings.Properties.Count);
		Assert.Single(settings);
		var value = Settings.Parse(settings.Value);
		Assert.Equal(3, ((IReadOnlyDictionary<string, string>)value).Count);
		Assert.Equal("archive", value["Database"]);
		Assert.Equal("updated", value["ABC"]);
		Assert.Equal("60s", value["CircuitBreaker.Duration"]);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void PropertiesAddUpdatesValueBeforeAndAfterReading(bool readBeforeMutation)
	{
		var settings = CreateStateSettings(readBeforeMutation);

		settings.Properties.Add("XYZ", "added");

		Assert.Equal("added", settings.Properties["XYZ"]);
		Assert.Equal(3, settings.Properties.Count);
		Assert.Single(settings);
		var value = Settings.Parse(settings.Value);
		Assert.Equal(4, ((IReadOnlyDictionary<string, string>)value).Count);
		Assert.Equal("archive", value["Database"]);
		Assert.Equal("original", value["ABC"]);
		Assert.Equal("60s", value["CircuitBreaker.Duration"]);
		Assert.Equal("added", value["XYZ"]);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void PropertiesRemoveUpdatesValueBeforeAndAfterReading(bool readBeforeMutation)
	{
		var settings = CreateStateSettings(readBeforeMutation);

		Assert.True(settings.Properties.Remove("ABC"));

		Assert.False(settings.Properties.ContainsKey("ABC"));
		Assert.Single(settings.Properties);
		Assert.Single(settings);
		var value = Settings.Parse(settings.Value);
		Assert.Equal(2, ((IReadOnlyDictionary<string, string>)value).Count);
		Assert.Equal("archive", value["Database"]);
		Assert.Equal("60s", value["CircuitBreaker.Duration"]);
		Assert.False(value.ContainsKey("ABC"));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void PropertiesClearUpdatesValueBeforeAndAfterReading(bool readBeforeMutation)
	{
		var settings = CreateStateSettings(readBeforeMutation);

		settings.Properties.Clear();

		Assert.Empty(settings.Properties);
		Assert.Single(settings);
		Assert.Equal("archive", settings.Database);
		var value = Settings.Parse(settings.Value);
		Assert.Single(value);
		Assert.Equal("archive", value["Database"]);
		Assert.False(value.ContainsKey("ABC"));
		Assert.False(value.ContainsKey("CircuitBreaker.Duration"));
	}

	[Fact]
	public void DatabaseUpdatePreservesOriginalAndAddedUnknownProperties()
	{
		var driver = new StateDriver();
		var settings = new StateSettings(driver, "Database=archive;CircuitBreaker.Duration=60s");
		settings.Properties.Add("ABC", "added");

		settings.Database = "reports";

		Assert.Equal("reports", settings.Database);
		Assert.Single(settings);
		Assert.Equal(2, settings.Properties.Count);
		Assert.Equal("60s", settings.Properties["CircuitBreaker.Duration"]);
		Assert.Equal("added", settings.Properties["ABC"]);
		var value = Settings.Parse(settings.Value);
		Assert.Equal(3, ((IReadOnlyDictionary<string, string>)value).Count);
		Assert.Equal("reports", value["Database"]);
		Assert.Equal("60s", value["CircuitBreaker.Duration"]);
		Assert.Equal("added", value["ABC"]);

		var restored = new StateSettings(driver, settings.Value);
		Assert.Single(restored);
		Assert.Equal("reports", restored.Database);
		Assert.Equal(2, restored.Properties.Count);
		Assert.Equal("60s", restored.Properties["CircuitBreaker.Duration"]);
		Assert.Equal("added", restored.Properties["ABC"]);
	}

	[Theory]
	[InlineData("'first;second'", "first;second")]
	[InlineData("\"left=right;phase='training'\"", "left=right;phase='training'")]
	[InlineData("'say \"hello\";path=C:\\\\models'", "say \"hello\";path=C:\\models")]
	public void UnknownPropertyTextRoundTripsThroughValue(string encoded, string expected)
	{
		var driver = new StateDriver();
		var settings = new StateSettings(driver, $"Database=archive;Template={encoded}");
		Assert.Single(settings);
		Assert.Equal(expected, settings.Properties["Template"]);

		settings.Properties.Add("Copy", expected);
		settings.Database = "reports";

		var value = Settings.Parse(settings.Value);
		Assert.Equal(3, ((IReadOnlyDictionary<string, string>)value).Count);
		Assert.Equal("reports", value["Database"]);
		Assert.Equal(expected, value["Template"]);
		Assert.Equal(expected, value["Copy"]);

		var restored = new StateSettings(driver, settings.Value);
		Assert.Single(restored);
		Assert.Equal("reports", restored.Database);
		Assert.Equal(2, restored.Properties.Count);
		Assert.Equal(expected, restored.Properties["Template"]);
		Assert.Equal(expected, restored.Properties["Copy"]);
	}

	[Theory]
	[InlineData("Base")]
	[InlineData("Driver")]
	[InlineData("Default")]
	[InlineData("Options")]
	public void EnumerationsStaySeparateWhileValueContainsBothDictionaries(string kind)
	{
		var settings = CreateSettings(kind, "Database=archive");
		settings.Properties.Add("CircuitBreaker.Duration", "60s");
		settings.Properties.Add("ABC", "added");

		var entry = Assert.Single(settings);
		Assert.Equal("Database", entry.Key, ignoreCase: true);
		Assert.Equal("archive", entry.Value);
		Assert.False(settings.ContainsKey("ABC"));
		Assert.False(settings.ContainsKey("CircuitBreaker.Duration"));
		Assert.Equal(2, settings.Properties.Count);
		Assert.False(settings.Properties.ContainsKey("Database"));
		Assert.Contains(settings.Properties, property => string.Equals(property.Key, "ABC", StringComparison.OrdinalIgnoreCase) && property.Value == "added");
		Assert.Contains(settings.Properties, property => string.Equals(property.Key, "CircuitBreaker.Duration", StringComparison.OrdinalIgnoreCase) && property.Value == "60s");

		var value = Settings.Parse(settings.Value);
		Assert.Equal(3, ((IReadOnlyDictionary<string, string>)value).Count);
		Assert.Equal("archive", value["Database"]);
		Assert.Equal("added", value["ABC"]);
		Assert.Equal("60s", value["CircuitBreaker.Duration"]);

		var restored = CreateSettings(kind, settings.Value);
		var restoredValue = Settings.Parse(restored.Value);
		Assert.Equal(3, ((IReadOnlyDictionary<string, string>)restoredValue).Count);
		Assert.Equal("archive", restoredValue["Database"]);
		Assert.Equal("added", restoredValue["ABC"]);
		Assert.Equal("60s", restoredValue["CircuitBreaker.Duration"]);
	}

	[Theory]
	[InlineData("Base")]
	[InlineData("Driver")]
	[InlineData("Default")]
	public void IndexerWithoutOptionsStoresArbitraryNamesInEntries(string kind)
	{
		var settings = CreateSettings(kind, "Database=archive;ABC=original");
		Assert.True(settings.TryGetValue("ABC", out var original));
		Assert.Equal("original", original);
		Assert.Empty(settings.Properties);

		settings["ABC"] = "updated";
		settings["CircuitBreaker.Duration"] = "60s";

		Assert.Equal(3, settings.Count);
		Assert.Equal("archive", settings["Database"]);
		Assert.Equal("updated", settings["ABC"]);
		Assert.Equal("60s", settings["CircuitBreaker.Duration"]);
		Assert.True(settings.TryGetValue("CircuitBreaker.Duration", out var duration));
		Assert.Equal("60s", duration);
		Assert.Empty(settings.Properties);
		var value = Settings.Parse(settings.Value);
		Assert.Equal(3, ((IReadOnlyDictionary<string, string>)value).Count);
		Assert.Equal("archive", value["Database"]);
		Assert.Equal("updated", value["ABC"]);
		Assert.Equal("60s", value["CircuitBreaker.Duration"]);
	}

	[Fact]
	public void IndexerWithOptionsAcceptsKnownAndCompositeNamesAndPropertiesAcceptUnknownNames()
	{
		var settings = new StateSettings(new StateDriver(), "Database=archive");

		settings["Database"] = "reports";
		settings["Cluster.Address"] = "node-a";
		settings["Cluster.Heartbeat"] = "30s";
		settings.Properties["ABC"] = "added";

		var entries = (IReadOnlyDictionary<string, string>)settings;
		Assert.Equal(3, entries.Count);
		Assert.Equal("reports", entries["Database"]);
		Assert.Equal("node-a", entries["Cluster.Address"]);
		Assert.Equal("30s", entries["Cluster.Heartbeat"]);
		Assert.False(entries.ContainsKey("ABC"));
		Assert.Single(settings.Properties);
		Assert.Equal("added", settings.Properties["ABC"]);
		Assert.Equal("node-a", settings.Cluster.Address);
		Assert.Equal(TimeSpan.FromSeconds(30), settings.Cluster.Heartbeat);
		var options = settings.GetOptions();
		Assert.Equal("reports", options.Database);
		Assert.Equal("node-a", options.Cluster.Address);
		Assert.Equal(TimeSpan.FromSeconds(30), options.Cluster.Heartbeat);

		var value = Settings.Parse(settings.Value);
		Assert.Equal(4, ((IReadOnlyDictionary<string, string>)value).Count);
		Assert.Equal("reports", value["Database"]);
		Assert.Equal("node-a", value["Cluster.Address"]);
		Assert.Equal("30s", value["Cluster.Heartbeat"]);
		Assert.Equal("added", value["ABC"]);
	}

	[Theory]
	[InlineData(null, "Port")]
	[InlineData(null, "pOrT")]
	[InlineData(null, "ServicePort")]
	[InlineData("3306", "Port")]
	[InlineData("3306", "pOrT")]
	[InlineData("3306", "SERVICEPORT")]
	public void MainIndexerWritesKnownParametersAndAliases(string original, string name)
	{
		var settings = new StateSettings(new StateDriver(), "Database=archive;ABC=retained" + (original == null ? string.Empty : $";Port={original}"));
		var entries = (IReadOnlyDictionary<string, string>)settings;
		Assert.Equal(original != null, entries.ContainsKey("Port"));
		Assert.Equal((ushort)3306, settings.Port);

		settings[name] = "3308";

		Assert.Equal(2, entries.Count);
		Assert.True(entries.TryGetValue("Port", out var port));
		Assert.Equal("3308", port);
		Assert.Equal((ushort)3308, settings.Port);
		Assert.Equal((ushort)3308, settings.ReadPort());
		Assert.Equal((ushort)3308, ((IConnectionSettings)settings).GetValue<ushort>("Port"));
		Assert.Equal((ushort)3308, settings.GetOptions().Port);
		Assert.Single(settings.Properties);
		Assert.Equal("retained", settings.Properties["ABC"]);
		Assert.False(settings.Properties.ContainsKey(name));

		var value = Settings.Parse(settings.Value);
		Assert.Equal(3, ((IReadOnlyDictionary<string, string>)value).Count);
		Assert.Equal("archive", value["Database"]);
		Assert.Equal("3308", value["Port"]);
		Assert.Equal("retained", value["ABC"]);
		AssertSingleCanonicalParameter(settings.Value, "Port", "ServicePort");
	}

	[Theory]
	[InlineData("Host")]
	[InlineData("host")]
	public void MainIndexerAliasWritesCanonicalServer(string name)
	{
		var settings = new StateSettings(new StateDriver(), "Database=archive;Server=node-a;ABC=retained");

		settings[name] = "node-b";

		var entries = (IReadOnlyDictionary<string, string>)settings;
		Assert.Equal(2, entries.Count);
		Assert.True(entries.TryGetValue("Server", out var server));
		Assert.Equal("node-b", server);
		Assert.Equal("node-b", settings.Server);
		Assert.Equal("node-b", settings.GetOptions().Server);
		Assert.Single(settings.Properties);
		Assert.Equal("retained", settings.Properties["ABC"]);
		Assert.False(settings.Properties.ContainsKey(name));

		var value = Settings.Parse(settings.Value);
		Assert.Equal(3, ((IReadOnlyDictionary<string, string>)value).Count);
		Assert.Equal("archive", value["Database"]);
		Assert.Equal("node-b", value["Server"]);
		Assert.Equal("retained", value["ABC"]);
		AssertSingleCanonicalParameter(settings.Value, "Server", "Host");
	}

	[Fact]
	public void PropertiesQueriesExposeOnlyExtensionEntries()
	{
		var settings = new StateSettings(new StateDriver(), "Database=archive;ABC=retained;CircuitBreaker.Duration=60s");
		settings["Port"] = "3308";
		settings["Host"] = "node-b";
		settings["Cluster.Address"] = "node-a";
		settings["Cluster.Heartbeat"] = "30s";

		var properties = settings.Properties;
		var collection = (ICollection<KeyValuePair<string, string>>)properties;
		Assert.Equal(2, properties.Count);
		Assert.Equal(2, properties.Keys.Count);
		Assert.Contains("ABC", properties.Keys, StringComparer.OrdinalIgnoreCase);
		Assert.Contains("CircuitBreaker.Duration", properties.Keys, StringComparer.OrdinalIgnoreCase);
		Assert.Equal(2, properties.Values.Count);
		Assert.Contains("retained", properties.Values);
		Assert.Contains("60s", properties.Values);
		Assert.DoesNotContain("3308", properties.Values);
		Assert.DoesNotContain("node-b", properties.Values);
		Assert.DoesNotContain("node-a", properties.Values);
		Assert.DoesNotContain("30s", properties.Values);
		Assert.Equal(2, properties.ToArray().Length);
		Assert.Contains(properties, property => string.Equals(property.Key, "ABC", StringComparison.OrdinalIgnoreCase) && property.Value == "retained");
		Assert.Contains(properties, property => string.Equals(property.Key, "CircuitBreaker.Duration", StringComparison.OrdinalIgnoreCase) && property.Value == "60s");
		Assert.True(properties.TryGetValue("ABC", out var extension));
		Assert.Equal("retained", extension);
		Assert.True(collection.Contains(new("ABC", "retained")));
		Assert.False(collection.Contains(new("ABC", "different")));

		foreach(var (name, value) in new[]
		{
			("Port", "3308"), ("PORT", "3308"), ("ServicePort", "3308"), ("serviceport", "3308"),
			("Server", "node-b"), ("Host", "node-b"), ("HOST", "node-b"),
			("Cluster.Address", "node-a"), ("cluster.heartbeat", "30s"),
		})
		{
			Assert.False(properties.ContainsKey(name));
			Assert.False(properties.TryGetValue(name, out _));
			Assert.False(collection.Contains(new(name, value)));
			Assert.DoesNotContain(properties.Keys, key => string.Equals(key, name, StringComparison.OrdinalIgnoreCase));
		}

		Assert.Equal((ushort)3308, settings.Port);
		Assert.Equal("node-b", settings.Server);
		Assert.Equal("node-a", settings.Cluster.Address);
		Assert.Equal(TimeSpan.FromSeconds(30), settings.Cluster.Heartbeat);
	}

	[Theory]
	[InlineData("ABC")]
	[InlineData("XYZ")]
	[InlineData("CircuitBreaker.Duration")]
	public void MainIndexerRejectsUnknownNamesWithoutChanges(string name)
	{
		var settings = new StateSettings(new StateDriver(), "Database=archive;Port=3308;ABC=retained;CircuitBreaker.Duration=60s");

		AssertRejectedWriteKeepsState(settings, () => settings[name] = "replacement");

		Assert.Equal((ushort)3308, settings.Port);
		Assert.Equal("archive", settings.Database);
		Assert.Equal("retained", settings.Properties["ABC"]);
		Assert.Equal("60s", settings.Properties["CircuitBreaker.Duration"]);
		Assert.False(((IReadOnlyDictionary<string, string>)settings).ContainsKey(name));
	}

	[Theory]
	[InlineData("Indexer", "Port", false)]
	[InlineData("Indexer", "Port", true)]
	[InlineData("Indexer", "pOrT", true)]
	[InlineData("Indexer", "ServicePort", false)]
	[InlineData("Indexer", "hOsT", true)]
	[InlineData("Indexer", "Cluster.Address", false)]
	[InlineData("Indexer", "Cluster.Address", true)]
	[InlineData("Indexer", "cLuStEr.hEaRtBeAt", true)]
	[InlineData("Add", "Port", false)]
	[InlineData("Add", "Port", true)]
	[InlineData("Add", "pOrT", true)]
	[InlineData("Add", "ServicePort", false)]
	[InlineData("Add", "hOsT", true)]
	[InlineData("Add", "Cluster.Address", false)]
	[InlineData("Add", "Cluster.Address", true)]
	[InlineData("Add", "cLuStEr.hEaRtBeAt", true)]
	[InlineData("CollectionAdd", "Port", false)]
	[InlineData("CollectionAdd", "Port", true)]
	[InlineData("CollectionAdd", "pOrT", true)]
	[InlineData("CollectionAdd", "ServicePort", false)]
	[InlineData("CollectionAdd", "hOsT", true)]
	[InlineData("CollectionAdd", "Cluster.Address", false)]
	[InlineData("CollectionAdd", "Cluster.Address", true)]
	[InlineData("CollectionAdd", "cLuStEr.hEaRtBeAt", true)]
	public void PropertiesWritesRejectReservedNamesWithoutChanges(string operation, string name, bool populated)
	{
		var value = "Database=archive;Server=node-a;ABC=retained;CircuitBreaker.Duration=60s";
		if(populated)
			value += ";Port=3307;Cluster.Address=node-a;Cluster.Heartbeat=30s";

		var settings = new StateSettings(new StateDriver(), value);

		AssertRejectedWriteKeepsState(settings, () => WriteProperty(settings, operation, name, "3308"));

		Assert.Equal(populated, ((IReadOnlyDictionary<string, string>)settings).ContainsKey("Port"));
		Assert.Equal(populated ? (ushort)3307 : (ushort)3306, settings.Port);
		Assert.Equal("node-a", settings.Server);
		Assert.Equal("retained", settings.Properties["ABC"]);
		Assert.Equal("60s", settings.Properties["CircuitBreaker.Duration"]);
		Assert.False(settings.Properties.ContainsKey(name));
	}

	[Theory]
	[InlineData("Add")]
	[InlineData("CollectionAdd")]
	public void PropertiesAddAcceptsExtensionNames(string operation)
	{
		var settings = new StateSettings(new StateDriver(), "Database=archive;Port=3308;ABC=retained");

		WriteProperty(settings, operation, "XYZ", "added");

		Assert.Equal(2, settings.Properties.Count);
		Assert.Equal("retained", settings.Properties["ABC"]);
		Assert.Equal("added", settings.Properties["XYZ"]);
		Assert.Equal(2, ((IReadOnlyDictionary<string, string>)settings).Count);
		Assert.False(((IReadOnlyDictionary<string, string>)settings).ContainsKey("XYZ"));
		Assert.Equal((ushort)3308, settings.Port);
		var value = Settings.Parse(settings.Value);
		Assert.Equal(4, ((IReadOnlyDictionary<string, string>)value).Count);
		Assert.Equal("archive", value["Database"]);
		Assert.Equal("3308", value["Port"]);
		Assert.Equal("retained", value["ABC"]);
		Assert.Equal("added", value["XYZ"]);
	}

	[Theory]
	[InlineData("Add")]
	[InlineData("CollectionAdd")]
	public void PropertiesAddRejectsDuplicateExtensionNamesWithoutChanges(string operation)
	{
		var settings = new StateSettings(new StateDriver(), "Database=archive;Port=3308;ABC=retained;CircuitBreaker.Duration=60s");

		AssertRejectedWriteKeepsState(settings, () => WriteProperty(settings, operation, "ABC", "replacement"));

		Assert.Equal((ushort)3308, settings.Port);
		Assert.Equal("retained", settings.Properties["ABC"]);
		Assert.Equal("60s", settings.Properties["CircuitBreaker.Duration"]);
	}

	[Fact]
	public void PropertiesRemoveAndClearKeepMainParameterUntilMainIndexerRemovesIt()
	{
		var settings = new StateSettings(new StateDriver(), "Database=archive;Port=3306;ABC=retained;CircuitBreaker.Duration=60s");
		settings["Port"] = "3308";

		Assert.False(settings.Properties.Remove("Port"));
		Assert.False(settings.Properties.Remove("ServicePort"));
		Assert.Equal((ushort)3308, settings.Port);
		Assert.Equal(2, settings.Properties.Count);

		settings.Properties.Clear();

		Assert.Empty(settings.Properties);
		var entries = (IReadOnlyDictionary<string, string>)settings;
		Assert.Equal(2, entries.Count);
		Assert.Equal("3308", entries["Port"]);
		Assert.Equal((ushort)3308, settings.Port);
		Assert.Equal((ushort)3308, settings.GetOptions().Port);
		var cleared = Settings.Parse(settings.Value);
		Assert.Equal(2, ((IReadOnlyDictionary<string, string>)cleared).Count);
		Assert.Equal("archive", cleared["Database"]);
		Assert.Equal("3308", cleared["Port"]);
		Assert.False(cleared.ContainsKey("ABC"));
		Assert.False(cleared.ContainsKey("CircuitBreaker.Duration"));

		settings["Port"] = null;

		Assert.Single(settings);
		Assert.False(entries.ContainsKey("Port"));
		Assert.Empty(settings.Properties);
		Assert.Equal((ushort)3306, settings.Port);
		Assert.Equal((ushort)3306, settings.GetOptions().Port);
		var removed = Settings.Parse(settings.Value);
		Assert.Single(removed);
		Assert.Equal("archive", removed["Database"]);
		Assert.False(removed.ContainsKey("Port"));
	}

	[Fact]
	public void PropertiesCollectionRemoveMatchesOnlyVisibleExtensionEntries()
	{
		var settings = new StateSettings(new StateDriver(), "Database=archive;Port=3308;ABC=retained;CircuitBreaker.Duration=60s");
		var collection = (ICollection<KeyValuePair<string, string>>)settings.Properties;

		Assert.False(collection.Remove(new("Port", "3308")));
		Assert.False(collection.Remove(new("ServicePort", "3308")));
		Assert.Equal((ushort)3308, settings.Port);
		Assert.False(collection.Remove(new("ABC", "different")));
		Assert.Equal("retained", settings.Properties["ABC"]);
		Assert.Equal(2, settings.Properties.Count);

		Assert.True(collection.Remove(new("ABC", "retained")));

		Assert.False(settings.Properties.ContainsKey("ABC"));
		Assert.Single(settings.Properties);
		Assert.Equal("60s", settings.Properties["CircuitBreaker.Duration"]);
		Assert.Equal((ushort)3308, settings.Port);
		Assert.Equal(2, ((IReadOnlyDictionary<string, string>)settings).Count);
		var value = Settings.Parse(settings.Value);
		Assert.Equal(3, ((IReadOnlyDictionary<string, string>)value).Count);
		Assert.Equal("archive", value["Database"]);
		Assert.Equal("3308", value["Port"]);
		Assert.Equal("60s", value["CircuitBreaker.Duration"]);
		Assert.False(value.ContainsKey("ABC"));
	}

	[Fact]
	public void ValueReplacementClearsPreviousEntriesAndReclassifiesNewParameters()
	{
		var settings = new StateSettings(new StateDriver(), "Database=archive;Server=node-a;Port=3308;Cluster.Address=old-node;Cluster.Heartbeat=30s;ABC=original;CircuitBreaker.Duration=60s");
		settings.Properties["CodeOnly"] = "added";

		settings.Value = "Server=node-b;Port=3310;Cluster.Heartbeat=45s;XYZ=replacement";

		var entries = (IReadOnlyDictionary<string, string>)settings;
		Assert.Equal(3, entries.Count);
		Assert.False(entries.ContainsKey("Database"));
		Assert.False(entries.ContainsKey("Cluster.Address"));
		Assert.Equal("node-b", entries["Server"]);
		Assert.Equal("3310", entries["Port"]);
		Assert.Equal("45s", entries["Cluster.Heartbeat"]);
		Assert.Single(settings.Properties);
		Assert.Equal("replacement", settings.Properties["XYZ"]);
		Assert.False(settings.Properties.ContainsKey("ABC"));
		Assert.False(settings.Properties.ContainsKey("CircuitBreaker.Duration"));
		Assert.False(settings.Properties.ContainsKey("CodeOnly"));
		Assert.Null(settings.Database);
		Assert.Equal("node-b", settings.Server);
		Assert.Equal((ushort)3310, settings.Port);
		var options = settings.GetOptions();
		Assert.Null(options.Database);
		Assert.Equal("node-b", options.Server);
		Assert.Equal((ushort)3310, options.Port);
		Assert.Null(options.Cluster.Address);
		Assert.Equal(TimeSpan.FromSeconds(45), options.Cluster.Heartbeat);
		var value = Settings.Parse(settings.Value);
		Assert.Equal(4, ((IReadOnlyDictionary<string, string>)value).Count);
		Assert.Equal("node-b", value["Server"]);
		Assert.Equal("3310", value["Port"]);
		Assert.Equal("45s", value["Cluster.Heartbeat"]);
		Assert.Equal("replacement", value["XYZ"]);

		settings.Database = "reports";

		var updated = Settings.Parse(settings.Value);
		Assert.Equal(5, ((IReadOnlyDictionary<string, string>)updated).Count);
		Assert.Equal("reports", updated["Database"]);
		Assert.Equal("replacement", updated["XYZ"]);
		Assert.False(updated.ContainsKey("ABC"));
		Assert.False(updated.ContainsKey("CircuitBreaker.Duration"));
		Assert.False(updated.ContainsKey("CodeOnly"));
		Assert.False(updated.ContainsKey("Cluster.Address"));
	}

	[Fact]
	public void EmptyValueClearsBothDictionariesAndLaterWritesStartFresh()
	{
		var settings = new StateSettings(new StateDriver(), "Database=archive;Server=node-a;Port=3308;Cluster.Address=old-node;Cluster.Heartbeat=30s;ABC=original;CircuitBreaker.Duration=60s");
		settings.Properties["CodeOnly"] = "added";

		settings.Value = string.Empty;

		Assert.Empty(settings);
		Assert.Empty(settings.Properties);
		Assert.Empty(Settings.Parse(settings.Value));
		Assert.Null(settings.Database);
		Assert.Null(settings.Server);
		Assert.Equal((ushort)3306, settings.Port);
		var options = settings.GetOptions();
		Assert.Null(options.Database);
		Assert.Null(options.Server);
		Assert.Equal((ushort)3306, options.Port);
		Assert.Null(options.Cluster.Address);
		Assert.Equal(TimeSpan.Zero, options.Cluster.Heartbeat);

		settings["Database"] = "reports";
		settings.Properties["XYZ"] = "added";

		Assert.Single(settings);
		Assert.Single(settings.Properties);
		Assert.Equal("reports", settings.Database);
		Assert.Equal("added", settings.Properties["XYZ"]);
		var value = Settings.Parse(settings.Value);
		Assert.Equal(2, ((IReadOnlyDictionary<string, string>)value).Count);
		Assert.Equal("reports", value["Database"]);
		Assert.Equal("added", value["XYZ"]);
	}

	private static void WriteProperty(StateSettings settings, string operation, string name, string value)
	{
		switch(operation)
		{
			case "Indexer":
				settings.Properties[name] = value;
				break;
			case "Add":
				settings.Properties.Add(name, value);
				break;
			case "CollectionAdd":
				((ICollection<KeyValuePair<string, string>>)settings.Properties).Add(new(name, value));
				break;
			default:
				throw new NotSupportedException(operation);
		}
	}

	private static void AssertRejectedWriteKeepsState(StateSettings settings, Action write)
	{
		var entries = settings.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase);
		var properties = settings.Properties.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase);
		var value = settings.Value;

		Assert.Throws<ArgumentException>(write);

		var actual = (IReadOnlyDictionary<string, string>)settings;
		Assert.Equal(entries.Count, actual.Count);

		foreach(var entry in entries)
		{
			Assert.True(actual.TryGetValue(entry.Key, out var text));
			Assert.Equal(entry.Value, text);
		}

		Assert.Equal(properties.Count, settings.Properties.Count);

		foreach(var property in properties)
		{
			Assert.True(settings.Properties.TryGetValue(property.Key, out var text));
			Assert.Equal(property.Value, text);
		}

		Assert.Equal(value, settings.Value);
	}

	private static void AssertSingleCanonicalParameter(string value, string name, string alias)
	{
		var names = value.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
			.Select(parameter => parameter.Split('=', 2, StringSplitOptions.TrimEntries)[0]);
		var matching = names.Where(key => string.Equals(key, name, StringComparison.OrdinalIgnoreCase) || string.Equals(key, alias, StringComparison.OrdinalIgnoreCase));

		Assert.Equal(name, Assert.Single(matching), ignoreCase: true);
	}

	private static StateSettings CreateStateSettings(bool readBeforeMutation)
	{
		var settings = new StateSettings(new StateDriver(), "Database=archive;ABC=original;CircuitBreaker.Duration=60s");

		if(readBeforeMutation)
		{
			var value = Settings.Parse(settings.Value);
			Assert.Equal(3, ((IReadOnlyDictionary<string, string>)value).Count);
			Assert.Equal("archive", value["Database"]);
			Assert.Equal("original", value["ABC"]);
			Assert.Equal("60s", value["CircuitBreaker.Duration"]);
		}

		return settings;
	}

	private static IConnectionSettings CreateSettings(string kind, string value) => kind switch
	{
		"Base" => new PlainSettings(new OpenDriver(), value),
		"Driver" => new OpenSettings(new OpenDriver(), value),
		"Default" => new ConnectionSettings("state", value),
		"Options" => new StateSettings(new StateDriver(), value),
		_ => throw new ArgumentOutOfRangeException(nameof(kind)),
	};

	private sealed class PlainSettings : ConnectionSettingsBase
	{
		private readonly IConnectionSettingsDriver _driver;

		public PlainSettings(IConnectionSettingsDriver driver, string value) : base("state", value)
		{
			_driver = driver;
			this.OnValueChanged(value);
		}

		protected override IConnectionSettingsDriver GetDriver() => _driver;
	}

	private sealed class OpenDriver() : ConnectionSettingsDriver<OpenSettings>("Core.ConnectionSettingsState.Open") { }

	private sealed class OpenSettings(OpenDriver driver, string value) : ConnectionSettingsBase<OpenDriver>(driver, "state", value)
	{
		public string Database
		{
			get => this.GetValue<string>();
			set => this.SetValue(value);
		}
	}

	private sealed class StateDriver() : ConnectionSettingsDriver<StateSettings>("Core.ConnectionSettingsState.Options") { }

	private sealed class StateSettings(StateDriver driver, string value) : ConnectionSettingsBase<StateDriver, StateOptions>(driver, "state", value)
	{
		[Alias("Host")]
		public string Server
		{
			get => this.GetValue<string>();
			set => this.SetValue(value);
		}

		[Alias("ServicePort")]
		[DefaultValue((ushort)3306)]
		public ushort Port
		{
			get => this.GetValue<ushort>();
			set => this.SetValue(value);
		}

		public string Database
		{
			get => this.GetValue<string>();
			set => this.SetValue(value);
		}

		public ClusterOptions Cluster
		{
			get => this.GetValue<ClusterOptions>();
			set => this.SetValue(value);
		}

		public ushort ReadPort() => this.GetValue<ushort>(nameof(this.Port));

		protected override StateOptions CreateOptions() => new();
	}

	private sealed class StateOptions
	{
		public string Server { get; set; }
		public ushort Port { get; set; }
		public string Database { get; set; }
		public ClusterOptions Cluster { get; set; }
	}

	private struct ClusterOptions
	{
		public string Address { get; set; }
		public TimeSpan Heartbeat { get; set; }
	}
}
