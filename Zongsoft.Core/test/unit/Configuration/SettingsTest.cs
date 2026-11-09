using System;
using System.Linq;
using System.Collections.Generic;

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

public class SettingsBehaviorTest
{
	[Fact]
	public void LayeredSettings_PipelineTrainingModelAndOverride_PreserveEffectiveValues()
	{
		var settings = new Settings("training", null);
		ApplyLayer(settings, "Epochs=10;Enabled=true;Optimizer=adam;BatchSize=16;Workers=2");
		ApplyLayer(settings, "epochs=20;BatchSize=32;Seed=7");
		ApplyLayer(settings, "EPOCHS=30;batchsize=64;Architecture=classifier");
		ApplyLayer(settings, "Epochs=0;ENABLED=false;Workers=4");

		Assert.Equal(0, settings.GetValue("epochs", 99));
		Assert.False(settings.GetValue("enabled", true));
		Assert.Equal("adam", settings.GetValue("optimizer", "sgd"));
		Assert.Equal(64, settings.GetValue("BATCHSIZE", 128));
		Assert.Equal(7, settings.GetValue("seed", 42));
		Assert.Equal(4, settings.GetValue("workers", 1));
		Assert.Equal("classifier", settings.GetValue("architecture", "unknown"));
		Assert.Equal(7, settings.Count());

		var restored = Settings.Parse(settings.Name, settings.Value);

		Assert.Equal("training", restored.Name);
		Assert.Equal(7, restored.Count());
		Assert.Equal("0", restored["EPOCHS"]);
		Assert.Equal("false", restored["Enabled"]);
		Assert.Equal("adam", restored["Optimizer"]);
		Assert.Equal("64", restored["BatchSize"]);
		Assert.Equal("7", restored["Seed"]);
		Assert.Equal("4", restored["Workers"]);
		Assert.Equal("classifier", restored["Architecture"]);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData(" \t\r\n ")]
	public void LayeredSettings_UnspecifiedTrainingAndOverride_KeepPipelineAndModel(string unspecified)
	{
		var settings = new Settings("training", null);
		ApplyLayer(settings, "Epochs=10;Enabled=true;Optimizer=adam");
		ApplyLayer(settings, unspecified);
		ApplyLayer(settings, "epochs=30;BatchSize=64");
		ApplyLayer(settings, unspecified);

		Assert.Equal(30, settings.GetValue("Epochs", 99));
		Assert.True(settings.GetValue("Enabled", false));
		Assert.Equal("adam", settings.GetValue("Optimizer", "sgd"));
		Assert.Equal(64, settings.GetValue("BatchSize", 128));
		Assert.Equal(4, settings.Count());
	}

	[Fact]
	public void GetValue_MissingKeys_UsesCallerDefaultsWithoutAddingEntries()
	{
		var settings = new Settings("training", "Epochs=0;Enabled=false");
		var defaultCalls = 0;

		Assert.Equal(64, settings.GetValue("BatchSize", 64));
		Assert.Equal("adam", settings.GetValue("Optimizer", "adam"));
		Assert.Equal(7, settings.GetValue("Seed", () => { defaultCalls++; return 7; }));
		Assert.Equal(1, defaultCalls);

		Assert.Equal(0, settings.GetValue("EPOCHS", () => { defaultCalls++; return 99; }));
		Assert.False(settings.GetValue("ENABLED", true));
		Assert.Equal(1, defaultCalls);
		Assert.False(settings.ContainsKey("Seed"));
		Assert.False(settings.ContainsKey("BatchSize"));
		Assert.Equal(2, settings.Count());
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	public void Indexer_UpdatesAndRemovals_KeepValueAndEntriesSynchronized(string removed)
	{
		var settings = new Settings("training", "Epochs=20;Enabled=true");
		settings["EPOCHS"] = "0";

		Assert.Equal(0, settings.GetValue("epochs", 99));
		Assert.Equal("true", settings["Enabled"]);
		Assert.Equal(2, settings.Count());
		Assert.Equal("0", Settings.Parse(settings.Value)["Epochs"]);

		settings["enabled"] = removed;

		Assert.False(settings.ContainsKey("Enabled"));
		Assert.True(settings.HasValue);
		Assert.Single(settings);
		Assert.Equal("0", Settings.Parse(settings.Value)["Epochs"]);
		Assert.False(Settings.Parse(settings.Value).ContainsKey("Enabled"));

		settings["epochs"] = removed;

		Assert.Empty(settings);
		Assert.True(settings.IsEmpty);
		Assert.False(settings.HasValue);
		Assert.Empty(settings.Value);
	}

	[Fact]
	public void Value_ReplacementAfterIndexerChanges_DropsEveryPreviousEntry()
	{
		var settings = new Settings("training", "Epochs=20;Enabled=true");
		settings["BatchSize"] = "32";
		Assert.Equal(3, settings.Count());

		settings.Value = "Optimizer=sgd;Workers=4";

		Assert.Equal("training", settings.Name);
		Assert.Equal(2, settings.Count());
		Assert.Equal("sgd", settings["Optimizer"]);
		Assert.Equal("4", settings["Workers"]);
		Assert.False(settings.ContainsKey("Epochs"));
		Assert.False(settings.ContainsKey("Enabled"));
		Assert.False(settings.ContainsKey("BatchSize"));

		var restored = Settings.Parse(settings.Value);

		Assert.Equal(2, restored.Count());
		Assert.Equal("sgd", restored["Optimizer"]);
		Assert.Equal("4", restored["Workers"]);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void Value_RestoringOriginalText_ReplacesIndexerChangesRegardlessOfPriorReads(bool readChangedValue)
	{
		const string ORIGINAL = "Epochs=20;Enabled=true";
		var settings = new Settings("training", ORIGINAL);
		settings["Epochs"] = "30";
		settings["Workers"] = "4";

		if(readChangedValue)
			Assert.Contains("Workers=4", settings.Value);

		settings.Value = ORIGINAL;

		Assert.Equal(2, settings.Count());
		Assert.Equal("20", settings["Epochs"]);
		Assert.Equal("true", settings["Enabled"]);
		Assert.False(settings.ContainsKey("Workers"));
		Assert.Equal("20", Settings.Parse(settings.Value)["Epochs"]);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData(" \t\r\n ")]
	public void Value_UnspecifiedReplacement_ClearsParsedAndAssignedEntries(string replacement)
	{
		var settings = new Settings("training", "Epochs=20;Enabled=true");
		settings["Workers"] = "4";

		settings.Value = replacement;

		Assert.Equal("training", settings.Name);
		Assert.Empty(settings);
		Assert.True(settings.IsEmpty);
		Assert.False(settings.HasValue);
		Assert.Empty(Settings.Parse(settings.Value));
	}

	[Theory]
	[InlineData("constructor", false)]
	[InlineData("constructor", true)]
	[InlineData("value", false)]
	[InlineData("value", true)]
	[InlineData("parse", false)]
	[InlineData("parse", true)]
	[InlineData("tryparse", false)]
	[InlineData("tryparse", true)]
	public void DuplicateParameters_AllTextEntrypoints_LastValueWins(string entrypoint, bool mixedCase)
	{
		var text = mixedCase ?
			"Epochs=10;Enabled=true;ePoChS=20;Workers=4;EPOCHS=30" :
			"Epochs=10;Enabled=true;Epochs=20;Workers=4;Epochs=30";

		Settings settings;

		switch(entrypoint)
		{
			case "constructor":
				settings = new Settings("training", text);
				break;
			case "value":
				settings = new Settings("training", "OldOnly=removed;Epochs=99");
				settings["AssignedOnly"] = "removed";
				settings.Value = text;
				break;
			case "parse":
				settings = Settings.Parse("training", text);
				break;
			case "tryparse":
				Assert.True(Settings.TryParse("training", text, out settings));
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(entrypoint));
		}

		Assert.NotNull(settings);
		Assert.Equal("training", settings.Name);
		Assert.Equal(3, settings.Count());
		Assert.Equal("30", settings["Epochs"]);
		Assert.Equal("30", settings["EPOCHS"]);
		Assert.Equal("true", settings["Enabled"]);
		Assert.Equal("4", settings["Workers"]);
		Assert.False(settings.ContainsKey("OldOnly"));
		Assert.False(settings.ContainsKey("AssignedOnly"));
	}

	[Theory]
	[InlineData("Text='first;second';Epochs=4", "first;second")]
	[InlineData("Text='loss=value;phase=training';Epochs=4", "loss=value;phase=training")]
	[InlineData("Text='trainer\\'s model';Epochs=4", "trainer's model")]
	[InlineData("Text=\"say \\\"go\\\"\";Epochs=4", "say \"go\"")]
	public void Parse_QuotedText_SurvivesUnrelatedMutationAndValueReparse(string text, string expected)
	{
		var settings = Settings.Parse("training", text);

		Assert.Equal(expected, settings["Text"]);
		Assert.Equal("4", settings["Epochs"]);
		Assert.Equal(2, settings.Count());

		settings["Epochs"] = "8";
		var restored = Settings.Parse(settings.Name, settings.Value);

		Assert.Equal(expected, restored["Text"]);
		Assert.Equal("8", restored["Epochs"]);
		Assert.Equal("training", restored.Name);
		Assert.Equal(2, restored.Count());
	}

	[Theory]
	[InlineData("first;second")]
	[InlineData("loss=value")]
	[InlineData("'quoted'")]
	[InlineData("\"quoted\"")]
	[InlineData("trainer's model;loss=value")]
	[InlineData("folder\\model")]
	[InlineData("first\tsecond")]
	[InlineData("first\nsecond")]
	[InlineData("first\rsecond")]
	public void Indexer_SpecialCharacters_SurviveValueReparse(string text)
	{
		var settings = new Settings("training", "Epochs=4");
		settings["Text"] = text;
		var restored = Settings.Parse(settings.Name, settings.Value);

		Assert.Equal(text, restored["Text"]);
		Assert.Equal("4", restored["Epochs"]);
		Assert.Equal("training", restored.Name);
		Assert.Equal(2, restored.Count());
	}

	[Theory]
	[InlineData("TRAINING", " Enabled=true ; EPOCHS=20 ;", true)]
	[InlineData("other", "Epochs=20;Enabled=true", false)]
	[InlineData("training", "Epochs=21;Enabled=true", false)]
	[InlineData("training", "Epochs=20;Enabled=True", false)]
	[InlineData("training", "Epochs=20", false)]
	[InlineData("training", "Epochs=20;Enabled=true;Workers=2", false)]
	public void SettingsEquality_NameAndEntryContent_AgreeAcrossAllPublicEntrypoints(string name, string value, bool expected)
	{
		var first = new Settings("training", "Epochs=20;Enabled=true");
		var second = new Settings(name, value);

		AssertSettingsEquality(first, second, expected);
	}

	[Fact]
	public void SettingsEquality_EntryMutationAndRestoration_ChangesContentEquality()
	{
		var first = new Settings("training", "Epochs=20;Enabled=true");
		var second = new Settings("TRAINING", "ENABLED=true;EPOCHS=20");
		AssertSettingsEquality(first, second, true);

		second["Epochs"] = "21";

		AssertSettingsEquality(first, second, false);
		Assert.Equal("20", first["Epochs"]);
		Assert.Equal("true", second["Enabled"]);

		second["epochs"] = "20";

		AssertSettingsEquality(first, second, true);
	}

	[Theory]
	[InlineData("TRAINING", "trainer", "ENABLED=true;EPOCHS=20", true)]
	[InlineData("other", "Trainer", "Epochs=20;Enabled=true", false)]
	[InlineData("training", "AnotherTrainer", "Epochs=20;Enabled=true", false)]
	[InlineData("training", ".Trainer", "Epochs=20;Enabled=true", false)]
	[InlineData("training", "Trainer", "Epochs=21;Enabled=true", false)]
	[InlineData("training", "Trainer", "Epochs=20;Enabled=True", false)]
	[InlineData("training", "Trainer", "Epochs=20", false)]
	public void ConnectionEquality_NameDriverNameAndEntries_AgreeAcrossAllPublicEntrypoints(string name, string driverName, string value, bool expected)
	{
		var first = new ScenarioConnectionSettings(new ScenarioDriver("Trainer"), "training", "Epochs=20;Enabled=true");
		var second = new ScenarioConnectionSettings(new ScenarioDriver(driverName), name, value);

		Assert.NotSame(first.Driver, second.Driver);
		AssertConnectionEquality(first, second, expected);
	}

	[Theory]
	[InlineData("OWNER", "team", false, true)]
	[InlineData("Owner", "Team", false, false)]
	[InlineData("Other", "team", false, false)]
	[InlineData("Owner", "team", true, false)]
	public void ConnectionEquality_PropertiesParticipateInContentComparison(string propertyName, string propertyValue, bool extra, bool expected)
	{
		var first = new ScenarioConnectionSettings(new ScenarioDriver("Trainer"), "training", "Epochs=20;Enabled=true");
		first.Properties["Owner"] = "team";

		var second = new ScenarioConnectionSettings(new ScenarioDriver("trainer"), "TRAINING", "ENABLED=true;EPOCHS=20");
		second.Properties[propertyName] = propertyValue;
		if(extra)
			second.Properties["Region"] = "laboratory";

		AssertConnectionEquality(first, second, expected);
		Assert.Equal(2, first.Count());
		Assert.Equal(2, second.Count());
		Assert.Equal("team", first.Properties["Owner"]);
	}

	[Fact]
	public void ConnectionEquality_PropertyInsertionOrderAndAllocation_DoNotAffectEqualityOrHashCode()
	{
		var first = new ScenarioConnectionSettings(new ScenarioDriver("Trainer"), "training", "Epochs=20;Enabled=true");
		var second = new ScenarioConnectionSettings(new ScenarioDriver("trainer"), "TRAINING", "ENABLED=true;EPOCHS=20");
		Assert.Empty(second.Properties);

		AssertConnectionEquality(first, second, true);

		first.Properties["Owner"] = "team";
		first.Properties["Region"] = "laboratory";

		second.Properties["REGION"] = "laboratory";
		second.Properties["OWNER"] = "team";

		AssertConnectionEquality(first, second, true);
	}

	[Fact]
	public void SettingsAndConnectionSettings_IdenticalNamesAndEntries_AreNeverEqual()
	{
		var settings = new Settings("training", "Epochs=20;Enabled=true");
		var connection = new ScenarioConnectionSettings(new ScenarioDriver("Trainer"), "training", "Epochs=20;Enabled=true");

		Assert.False(settings.Equals((object)connection));
		Assert.False(((ISettings)settings).Equals(connection));
		Assert.False(connection.Equals((object)settings));
		Assert.False(((IConnectionSettings)connection).Equals(settings));
		Assert.False(((Settings)connection).Equals((object)settings));
		Assert.False(object.Equals(settings, connection));
		Assert.False(object.Equals(connection, settings));
		Assert.Equal(2, new HashSet<object> { settings, connection }.Count);
	}

	private static void ApplyLayer(Settings target, string layer)
	{
		if(string.IsNullOrWhiteSpace(layer))
			return;

		foreach(var entry in Settings.Parse(layer))
			target[entry.Key] = entry.Value;
	}

	private static void AssertSettingsEquality(Settings first, Settings second, bool expected)
	{
		foreach(var (left, right) in new[] { (first, second), (second, first) })
		{
			Assert.Equal(expected, left.Equals(right));
			Assert.Equal(expected, left.Equals((ISettings)right));
			Assert.Equal(expected, left.Equals((object)right));
			Assert.Equal(expected, ((IEquatable<Settings>)left).Equals(right));
			Assert.Equal(expected, ((ISettings)left).Equals(right));
			Assert.Equal(expected, object.Equals(left, right));
			Assert.Equal(expected, EqualityComparer<Settings>.Default.Equals(left, right));
			Assert.Equal(expected, new HashSet<Settings> { left }.Contains(right));
		}

		if(expected)
			Assert.Equal(first.GetHashCode(), second.GetHashCode());
	}

	private static void AssertConnectionEquality(ScenarioConnectionSettings first, ScenarioConnectionSettings second, bool expected)
	{
		foreach(var (left, right) in new[] { (first, second), (second, first) })
		{
			Assert.Equal(expected, left.Equals(right));
			Assert.Equal(expected, left.Equals((ConnectionSettingsBase<ScenarioDriver>)right));
			Assert.Equal(expected, left.Equals((ConnectionSettingsBase)right));
			Assert.Equal(expected, left.Equals((IConnectionSettings)right));
			Assert.Equal(expected, left.Equals((Settings)right));
			Assert.Equal(expected, left.Equals((ISettings)right));
			Assert.Equal(expected, left.Equals((object)right));
			Assert.Equal(expected, ((IEquatable<ConnectionSettingsBase<ScenarioDriver>>)left).Equals(right));
			Assert.Equal(expected, ((IEquatable<ConnectionSettingsBase>)left).Equals(right));
			Assert.Equal(expected, ((IEquatable<IConnectionSettings>)left).Equals(right));
			Assert.Equal(expected, ((IEquatable<Settings>)left).Equals(right));
			Assert.Equal(expected, ((IEquatable<ISettings>)left).Equals(right));
			Assert.Equal(expected, object.Equals(left, right));
			Assert.Equal(expected, EqualityComparer<IConnectionSettings>.Default.Equals(left, right));
			Assert.Equal(expected, new HashSet<IConnectionSettings> { left }.Contains(right));
		}

		if(expected)
			Assert.Equal(first.GetHashCode(), second.GetHashCode());
	}

	private sealed class ScenarioDriver(string name) : ConnectionSettingsDriver<ScenarioConnectionSettings>(name) { }

	private sealed class ScenarioConnectionSettings(ScenarioDriver driver, string name, string value) : ConnectionSettingsBase<ScenarioDriver>(driver, name, value)
	{
		public int Epochs
		{
			get => this.GetValue<int>();
			set => this.SetValue(value);
		}

		public bool Enabled
		{
			get => this.GetValue<bool>();
			set => this.SetValue(value);
		}
	}
}
