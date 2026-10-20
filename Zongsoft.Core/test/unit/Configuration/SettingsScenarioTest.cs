using System;
using System.Linq;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Configuration.Tests;

public class SettingsScenarioTest
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
