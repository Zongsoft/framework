using System;
using System.Linq;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Configuration.Tests;

public class SettingsDictionaryTest
{
	[Fact]
	public void SettingsContractExposesMutableDictionaryWithConsistentQueries()
	{
		ISettings settings = new Settings("profile", "Enabled=true;Retries=3");
		Assert.True(typeof(IDictionary<string, string>).IsAssignableFrom(typeof(ISettings)));
		var dictionary = Assert.IsAssignableFrom<IDictionary<string, string>>(settings);
		var collection = Assert.IsAssignableFrom<ICollection<KeyValuePair<string, string>>>(settings);

		Assert.False(collection.IsReadOnly);
		Assert.Equal(2, dictionary.Count);
		Assert.Equal(2, collection.Count);
		Assert.True(dictionary.ContainsKey("enabled"));
		Assert.True(dictionary.TryGetValue("RETRIES", out var retries));
		Assert.Equal("3", retries);
		Assert.True(collection.Contains(new("ENABLED", "true")));
		Assert.False(collection.Contains(new("Enabled", "TRUE")));
		Assert.False(collection.Contains(new("Missing", "true")));
		Assert.Equal(2, dictionary.Keys.Count);
		Assert.Contains("Enabled", dictionary.Keys, StringComparer.OrdinalIgnoreCase);
		Assert.Contains("Retries", dictionary.Keys, StringComparer.OrdinalIgnoreCase);
		Assert.Equal(2, dictionary.Values.Count);
		Assert.Contains("true", dictionary.Values);
		Assert.Contains("3", dictionary.Values);
		Assert.False(settings.IsEmpty);
		Assert.True(settings.HasValue);

		KeyValuePair<string, string>[] copied = [new("Before", "left"), default, default, new("After", "right")];
		collection.CopyTo(copied, 1);

		Assert.Equal(new KeyValuePair<string, string>("Before", "left"), copied[0]);
		Assert.Equal(new KeyValuePair<string, string>("After", "right"), copied[3]);
		var contents = copied.Skip(1).Take(2).ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase);
		Assert.Equal(2, contents.Count);
		Assert.Equal("true", contents["Enabled"]);
		Assert.Equal("3", contents["Retries"]);
	}

	[Theory]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public void AddThroughEitherViewUpdatesEveryViewAndValue(bool throughCollection, bool readBeforeMutation)
	{
		var settings = new Settings("Retries=3");
		var dictionary = Assert.IsAssignableFrom<IDictionary<string, string>>(settings);
		var collection = Assert.IsAssignableFrom<ICollection<KeyValuePair<string, string>>>(settings);

		if(readBeforeMutation)
			Assert.Equal("3", Settings.Parse(settings.Value)["Retries"]);

		Add(dictionary, collection, throughCollection, "Enabled", "false");

		Assert.Equal(2, dictionary.Count);
		Assert.Equal(2, collection.Count);
		Assert.Equal("false", settings["enabled"]);
		Assert.Equal("false", dictionary["ENABLED"]);
		Assert.True(collection.Contains(new("Enabled", "false")));
		var value = Settings.Parse(settings.Value);
		Assert.Equal(2, value.Count());
		Assert.Equal("3", value["Retries"]);
		Assert.Equal("false", value["Enabled"]);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void DuplicateAddLeavesBothViewsAndValueUnchanged(bool throughCollection)
	{
		var settings = new Settings("Name=original;Retries=3");
		var dictionary = Assert.IsAssignableFrom<IDictionary<string, string>>(settings);
		var collection = Assert.IsAssignableFrom<ICollection<KeyValuePair<string, string>>>(settings);
		var value = settings.Value;

		Assert.Throws<ArgumentException>(() => Add(dictionary, collection, throughCollection, "NAME", "replacement"));

		Assert.Equal(2, dictionary.Count);
		Assert.Equal(2, collection.Count);
		Assert.Equal("original", settings["Name"]);
		Assert.Equal("original", dictionary["name"]);
		Assert.Equal("3", dictionary["Retries"]);
		Assert.True(collection.Contains(new("Name", "original")));
		Assert.False(collection.Contains(new("Name", "replacement")));
		Assert.Equal(value, settings.Value);
	}

	[Theory]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public void RemoveThroughEitherViewUpdatesEveryViewAndValue(bool throughCollection, bool readBeforeMutation)
	{
		var settings = new Settings("Retries=3;Enabled=false");
		var dictionary = Assert.IsAssignableFrom<IDictionary<string, string>>(settings);
		var collection = Assert.IsAssignableFrom<ICollection<KeyValuePair<string, string>>>(settings);

		if(readBeforeMutation)
			Assert.Equal(2, Settings.Parse(settings.Value).Count());

		Assert.False(dictionary.Remove("Missing"));
		Assert.False(collection.Remove(new("Enabled", "true")));
		Assert.Equal("false", settings["Enabled"]);
		var removed = throughCollection ? collection.Remove(new("enabled", "false")) : dictionary.Remove("ENABLED");

		Assert.True(removed);
		Assert.Single(dictionary);
		Assert.Single(collection);
		Assert.False(dictionary.ContainsKey("Enabled"));
		Assert.False(dictionary.TryGetValue("Enabled", out _));
		Assert.False(collection.Contains(new("Enabled", "false")));
		Assert.Null(settings["Enabled"]);
		Assert.Equal("3", settings["Retries"]);
		var value = Settings.Parse(settings.Value);
		Assert.Single(value);
		Assert.Equal("3", value["Retries"]);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void ClearEmptiesEveryViewAndValue(bool readBeforeMutation)
	{
		var settings = new Settings("profile", "Retries=3;Enabled=false");
		var dictionary = Assert.IsAssignableFrom<IDictionary<string, string>>(settings);
		var collection = Assert.IsAssignableFrom<ICollection<KeyValuePair<string, string>>>(settings);

		if(readBeforeMutation)
			Assert.Equal(2, Settings.Parse(settings.Value).Count());

		collection.Clear();

		Assert.Empty(dictionary);
		Assert.Empty(collection);
		Assert.Empty(dictionary.Keys);
		Assert.Empty(dictionary.Values);
		Assert.True(settings.IsEmpty);
		Assert.False(settings.HasValue);
		Assert.Equal("profile", settings.Name);
		Assert.Empty(Settings.Parse(settings.Value));
	}

	[Theory]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public void TypedConnectionAddAcceptsDefinedNamesAndRejectsUnknownNames(bool throughCollection, bool readBeforeMutation)
	{
		var settings = ConnectionSettingsTest.MyDriver.Instance.GetSettings("Text=original;ABC=extension");
		var dictionary = Assert.IsAssignableFrom<IDictionary<string, string>>(settings);
		var collection = Assert.IsAssignableFrom<ICollection<KeyValuePair<string, string>>>(settings);
		Assert.False(dictionary.ContainsKey("Port"));
		Assert.Equal((ushort)7969, settings.Port);

		if(readBeforeMutation)
			Assert.Equal(2, Settings.Parse(settings.Value).Count());

		Add(dictionary, collection, throughCollection, "Port", "3308");

		Assert.Equal(2, dictionary.Count);
		Assert.Equal(2, collection.Count);
		Assert.Equal("3308", dictionary["Port"]);
		Assert.Equal((ushort)3308, settings.Port);
		Assert.Single(settings.Properties);
		Assert.Equal("extension", settings.Properties["ABC"]);
		var value = Settings.Parse(settings.Value);
		Assert.Equal(3, value.Count());
		Assert.Equal("original", value["Text"]);
		Assert.Equal("3308", value["Port"]);
		Assert.Equal("extension", value["ABC"]);
		var serialized = settings.Value;

		Assert.Throws<ArgumentException>(() => Add(dictionary, collection, throughCollection, "ABC", "replacement"));
		Assert.Throws<ArgumentException>(() => Add(dictionary, collection, throughCollection, "UnknownParameter", "added"));
		Assert.Throws<ArgumentException>(() => Add(dictionary, collection, throughCollection, "PORT", "3309"));

		Assert.Equal(2, dictionary.Count);
		Assert.Equal(2, collection.Count);
		Assert.Equal("original", dictionary["Text"]);
		Assert.Equal("3308", dictionary["Port"]);
		Assert.Equal((ushort)3308, settings.Port);
		Assert.False(dictionary.ContainsKey("ABC"));
		Assert.False(dictionary.ContainsKey("UnknownParameter"));
		Assert.Single(settings.Properties);
		Assert.Equal("extension", settings.Properties["ABC"]);
		Assert.Equal(serialized, settings.Value);
	}

	[Theory]
	[InlineData("Remove", false)]
	[InlineData("Remove", true)]
	[InlineData("Pair", false)]
	[InlineData("Pair", true)]
	[InlineData("Clear", false)]
	[InlineData("Clear", true)]
	public void TypedConnectionRemovalAndClearPreserveExtensionProperties(string operation, bool readBeforeMutation)
	{
		var settings = ConnectionSettingsTest.MyDriver.Instance.GetSettings("Port=3308;Text=original;ABC=extension;CircuitBreaker.Duration=60s");
		var dictionary = Assert.IsAssignableFrom<IDictionary<string, string>>(settings);
		var collection = Assert.IsAssignableFrom<ICollection<KeyValuePair<string, string>>>(settings);

		if(readBeforeMutation)
			Assert.Equal(4, Settings.Parse(settings.Value).Count());

		Assert.False(dictionary.Remove("ABC"));
		Assert.False(collection.Remove(new("ABC", "extension")));
		Assert.Equal("extension", settings.Properties["ABC"]);

		switch(operation)
		{
			case "Remove":
				Assert.True(dictionary.Remove("PORT"));
				break;
			case "Pair":
				Assert.False(collection.Remove(new("Port", "3309")));
				Assert.True(collection.Remove(new("port", "3308")));
				break;
			case "Clear":
				collection.Clear();
				break;
			default:
				throw new NotSupportedException(operation);
		}

		var cleared = operation == "Clear";
		Assert.Equal(cleared ? 0 : 1, dictionary.Count);
		Assert.Equal(cleared ? 0 : 1, collection.Count);
		Assert.False(dictionary.ContainsKey("Port"));
		Assert.False(dictionary.ContainsKey("ABC"));
		Assert.Equal((ushort)7969, settings.Port);
		Assert.Equal(cleared ? null : "original", settings.Text);
		Assert.Equal(2, settings.Properties.Count);
		Assert.Equal("extension", settings.Properties["ABC"]);
		Assert.Equal("60s", settings.Properties["CircuitBreaker.Duration"]);
		var value = Settings.Parse(settings.Value);
		Assert.Equal(cleared ? 2 : 3, value.Count());
		Assert.False(value.ContainsKey("Port"));
		Assert.Equal(cleared ? null : "original", value["Text"]);
		Assert.Equal("extension", value["ABC"]);
		Assert.Equal("60s", value["CircuitBreaker.Duration"]);
	}

	[Theory]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public void ConnectionWithoutOptionsAllowsArbitraryDictionaryAdds(bool throughCollection, bool readBeforeMutation)
	{
		var settings = new ConnectionSettings("state", "Database=archive");
		var dictionary = Assert.IsAssignableFrom<IDictionary<string, string>>(settings);
		var collection = Assert.IsAssignableFrom<ICollection<KeyValuePair<string, string>>>(settings);

		if(readBeforeMutation)
			Assert.Equal("archive", Settings.Parse(settings.Value)["Database"]);

		Add(dictionary, collection, throughCollection, "ABC", "original");
		Add(dictionary, collection, throughCollection, "CircuitBreaker.Duration", "60s");

		Assert.Equal(3, dictionary.Count);
		Assert.Equal(3, collection.Count);
		Assert.Equal("archive", settings.Database);
		Assert.Equal("original", settings["ABC"]);
		Assert.Equal("60s", settings["CircuitBreaker.Duration"]);
		Assert.Empty(settings.Properties);
		var value = Settings.Parse(settings.Value);
		Assert.Equal(3, value.Count());
		Assert.Equal("archive", value["Database"]);
		Assert.Equal("original", value["ABC"]);
		Assert.Equal("60s", value["CircuitBreaker.Duration"]);
	}

	private static void Add(IDictionary<string, string> dictionary, ICollection<KeyValuePair<string, string>> collection, bool throughCollection, string name, string value)
	{
		if(throughCollection)
			collection.Add(new(name, value));
		else
			dictionary.Add(name, value);
	}
}
