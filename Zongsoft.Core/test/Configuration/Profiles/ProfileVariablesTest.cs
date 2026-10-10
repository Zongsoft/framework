using System;
using System.IO;

using Zongsoft.Configuration.Profiles;
using Zongsoft.Common;
using Zongsoft.Text.Templating;

using Xunit;

namespace Zongsoft.Configuration.Tests;

public class ProfileVariablesTest
{
	[Theory]
	[InlineData("io rustfs")]
	[InlineData("io.rustfs")]
	public void ProfileView_MapsRootAndAbsoluteNamespaces(string sectionName)
	{
		using var reader = new StringReader($"root=default\n[io]\nparentOnly=parent\n[{sectionName}]\nAccess-Key=42\nflag\nreference=${{root}}");
		var profile = Profile.Load(reader);
		var variables = profile.ToVariables();

		Assert.True(variables.TryGetValue("ROOT", out var root));
		Assert.Equal("default", root);
		AssertValue(variables, null, "root", "default");
		AssertValue(variables, string.Empty, "root", "default");
		AssertValue(variables, "IO.RUSTFS", "ACCESS_KEY", "42");
		AssertValue(variables, "io.rustfs", "flag", null);
		AssertValue(variables, "io.rustfs", "reference", "${root}");
		Assert.False(variables.TryGetValue("Access_Key", out _));
		Assert.False(variables.TryGetValue("io.rustfs", "root", out _));
		Assert.False(variables.TryGetValue("io.rustfs", "parentOnly", out _));
		Assert.False(variables.TryGetValue("rustfs", "Access_Key", out _));
	}

	[Fact]
	public void SectionView_ContainsOnlyItsAbsoluteSubtree()
	{
		using var reader = new StringReader("root=default\n[io]\nname=parent\n[io rustfs]\nname=child\n[other]\nname=sibling");
		var profile = Profile.Load(reader);
		var section = profile.Sections["io"];
		var variables = section.ToVariables();

		AssertValue(variables, "IO", "NAME", "parent");
		AssertValue(variables, "io.rustfs", "name", "child");
		Assert.False(variables.TryGetValue("name", out _));
		Assert.False(variables.TryGetValue(string.Empty, "name", out _));
		Assert.False(variables.TryGetValue("root", out _));
		Assert.False(variables.TryGetValue("rustfs", "name", out _));
		Assert.False(variables.TryGetValue("other", "name", out _));
		var child = section.Sections["rustfs"].ToVariables();

		AssertValue(child, "io.rustfs", "name", "child");
		Assert.False(child.TryGetValue("io", "name", out _));
	}

	[Theory]
	[InlineData(null)]
	[InlineData("io.rustfs")]
	[InlineData("_io._rustfs9")]
	public void EntryView_RetainsOneEntryIdentityAfterReplacement(string scope)
	{
		var profile = new Profile();
		var section = scope == null ? null : profile.Sections.Add(scope);
		var entries = section?.Entries ?? profile.Entries;
		var original = entries.Add("access-key", "original");
		entries.Add("other", "neighbor");
		var variables = original.ToVariables();

		AssertValue(variables, scope, "ACCESS_KEY", "original");
		Assert.False(variables.TryGetValue(scope, "other", out _));
		Assert.False(variables.TryGetValue(scope == null ? "other" : null, "access_key", out _));
		entries[0] = section == null ? new ProfileEntry(profile, "access-key", "replacement") : new ProfileEntry(section, "access-key", "replacement");
		original.Value = "changed original";

		AssertValue(profile.ToVariables(), scope, "access_key", "replacement");
		AssertValue(variables, scope, "access_key", "changed original");
		if(scope == null)
			AssertValue(variables, string.Empty, "access_key", "changed original");
	}

	[Fact]
	public void Views_ReflectCurrentEntriesAndSubsections()
	{
		var profile = new Profile();
		var root = profile.Entries.Add("name", "root");
		var section = profile.Sections.Add("app");
		section.Entries.Add("name", "initial");
		var variables = profile.ToVariables();
		var sectionVariables = section.ToVariables();

		root.Value = " root updated ";
		section.Entries[0] = new ProfileEntry(section, "name", "replaced");
		var child = section.Sections.Add("worker");
		child.Entries.Add("port", "9000");
		AssertValue(variables, null, "name", " root updated ");
		AssertValue(variables, "app", "name", "replaced");
		AssertValue(sectionVariables, "app", "name", "replaced");
		AssertValue(variables, "app.worker", "port", "9000");
		AssertValue(sectionVariables, "app.worker", "port", "9000");

		section.Entries.Clear();
		section.Sections.Remove(child);
		profile.Entries.Remove(root);
		Assert.False(variables.TryGetValue("name", out _));
		Assert.False(variables.TryGetValue("app", "name", out _));
		Assert.False(sectionVariables.TryGetValue("app", "name", out _));
		Assert.False(variables.TryGetValue("app.worker", "port", out _));
		Assert.False(sectionVariables.TryGetValue("app.worker", "port", out _));

		var replacement = new ProfileSection(profile, "app");
		replacement.Entries.Add("name", "new section");
		profile.Sections[0] = replacement;
		AssertValue(variables, "app", "name", "new section");
		Assert.False(sectionVariables.TryGetValue("app", "name", out _));
	}

	[Theory]
	[InlineData("db-name", "db_name")]
	[InlineData("db.name", "db_name")]
	[InlineData(".db-name", "_db_name")]
	[InlineData("-db.name", "_db_name")]
	[InlineData("._-A9", "___A9")]
	[InlineData("_A9", "_A9")]
	public void EntryNames_NormalizeDotsAndDashesWithoutChangingSource(string sourceName, string variableName)
	{
		var profile = new Profile();
		var section = profile.Sections.Add("app");
		var entry = section.Entries.Add(sourceName, "value");

		AssertValue(profile.ToVariables(), "app", variableName, "value");
		AssertValue(section.ToVariables(), "app", variableName, "value");
		AssertValue(entry.ToVariables(), "app", variableName, "value");
		Assert.Equal(sourceName, entry.Name);
		using var writer = new StringWriter { NewLine = "\n" };

		profile.Save(writer);
		Assert.Contains(sourceName + "=value", writer.ToString());
	}

	[Theory]
	[InlineData("9name")]
	[InlineData("naïve")]
	[InlineData("db:key")]
	[InlineData("db/key")]
	[InlineData("two names")]
	[InlineData("$name")]
	public void InvalidEntryNames_AreIgnoredByEveryView(string name)
	{
		var profile = new Profile();
		var section = profile.Sections.Add("app");
		var entry = section.Entries.Add(name, "invalid");
		section.Entries.Add("valid", "kept");

		Assert.False(profile.ToVariables().TryGetValue("app", name, out _));
		Assert.False(section.ToVariables().TryGetValue("app", name, out _));
		Assert.False(entry.ToVariables().TryGetValue("app", name, out _));
		Assert.False(entry.ToVariables().TryGetValue("app", "valid", out _));
		AssertValue(profile.ToVariables(), "app", "valid", "kept");
	}

	[Theory]
	[InlineData("9app", "_9app")]
	[InlineData("app-name", "app_name")]
	[InlineData("app..worker", "app.worker")]
	[InlineData("app.9worker", "app._9worker")]
	[InlineData("app.naïve", "app.naive")]
	[InlineData(".app", "app")]
	[InlineData("app.", "app")]
	[InlineData("app worker", "app.worker")]
	public void InvalidNamespace_ExcludesWholeBranch(string name, string normalizedScope)
	{
		var profile = new Profile();
		profile.Entries.Add("valid", "root");
		var section = profile.Sections.Add(name);
		section.Entries.Add("valid", "invalid branch");
		var child = section.Sections.Add("child");
		var entry = child.Entries.Add("valid", "invalid descendant");

		AssertValue(profile.ToVariables(), null, "valid", "root");
		Assert.False(profile.ToVariables().TryGetValue(name, "valid", out _));
		Assert.False(profile.ToVariables().TryGetValue(name + ".child", "valid", out _));
		Assert.False(section.ToVariables().TryGetValue(name + ".child", "valid", out _));
		Assert.False(profile.ToVariables().TryGetValue(normalizedScope, "valid", out _));
		Assert.False(profile.ToVariables().TryGetValue(normalizedScope + ".child", "valid", out _));
		Assert.False(section.ToVariables().TryGetValue(normalizedScope + ".child", "valid", out _));
		Assert.False(child.ToVariables().TryGetValue("child", "valid", out _));
		Assert.False(entry.ToVariables().TryGetValue("valid", out _));
	}

	[Theory]
	[InlineData("app", "")]
	[InlineData("app", "key-name")]
	[InlineData("app", "key.name")]
	[InlineData("app", " key_name ")]
	[InlineData("app", "app:key_name")]
	[InlineData(" app ", "key_name")]
	[InlineData(" \t ", "key_name")]
	[InlineData("app.", "key_name")]
	[InlineData("app..worker", "key_name")]
	public void Queries_RequireCanonicalNamesWithoutTrimmingOrNormalization(string scope, string name)
	{
		var profile = new Profile();
		var section = profile.Sections.Add("app");
		var entry = section.Entries.Add("key-name", "value");

		foreach(var variables in new[] { profile.ToVariables(), section.ToVariables(), entry.ToVariables() })
		{
			Assert.False(variables.TryGetValue(scope, name, out _));
			AssertValue(variables, "app", "key_name", "value");
		}
	}

	[Fact]
	public void NullSourcesAndNames_ThrowArgumentNullException()
	{
		Assert.Throws<ArgumentNullException>(() => ((Profile)null).ToVariables());
		Assert.Throws<ArgumentNullException>(() => ((ProfileSection)null).ToVariables());
		Assert.Throws<ArgumentNullException>(() => ((ProfileEntry)null).ToVariables());
		var profile = new Profile();
		var section = profile.Sections.Add("app");
		var entry = section.Entries.Add("name", "value");

		foreach(var variables in new[] { profile.ToVariables(), section.ToVariables(), entry.ToVariables() })
		{
			Assert.Throws<ArgumentNullException>("name", () => variables.TryGetValue(null, out _));
			Assert.Throws<ArgumentNullException>("name", () => variables.TryGetValue(null, null, out _));
			Assert.Throws<ArgumentNullException>("name", () => variables.TryGetValue(string.Empty, null, out _));
			Assert.Throws<ArgumentNullException>("name", () => variables.TryGetValue("app", null, out _));
		}
	}

	[Fact]
	public void ImportView_UsesEffectiveOverridesAndMergedSectionEntries()
	{
		using var files = new ProfileImportTest.ProfileFiles();
		files.Write("base.ini", "edition=base\n[app]\ndb-name=base\nbaseOnly=imported\n[app worker]\nport=9000");
		var path = files.Write("root.ini", "#@import base.ini\nedition=customer\n[app]\ndb-name=customer\nlocalOnly=local");
		var profile = Profile.Load(path);
		var variables = profile.ToVariables();
		var sectionVariables = profile.Sections["app"].ToVariables();

		AssertValue(variables, null, "edition", "customer");
		AssertValue(variables, "app", "db_name", "customer");
		AssertValue(sectionVariables, "app", "db_name", "customer");
		AssertValue(sectionVariables, "app", "baseOnly", "imported");
		AssertValue(sectionVariables, "app", "localOnly", "local");
		AssertValue(sectionVariables, "app.worker", "port", "9000");
		profile.Sections["app"].Entries["baseOnly"].Value = "changed imported";
		AssertValue(variables, "app", "baseOnly", "changed imported");
		AssertValue(sectionVariables, "app", "baseOnly", "changed imported");
	}

	[Fact]
	public void NormalizedEntryCollision_OnlyFailsMatchingLookupAndClearsWhenRemoved()
	{
		var profile = new Profile();
		var section = profile.Sections.Add("app");
		var dashed = section.Entries.Add("db-name", "dash");
		var dotted = section.Entries.Add("db.name", "dot");
		section.Entries.Add("db_name", "underscore");
		section.Entries.Add("other", "unaffected");
		var variables = profile.ToVariables();
		var sectionVariables = section.ToVariables();

		AssertValue(variables, "app", "other", "unaffected");
		Assert.False(variables.TryGetValue("other", "db_name", out _));
		Assert.Throws<ProfileException>(() => variables.TryGetValue("APP", "DB_NAME", out _));
		Assert.Throws<ProfileException>(() => sectionVariables.TryGetValue("app", "db_name", out _));
		AssertValue(dashed.ToVariables(), "app", "db_name", "dash");
		section.Entries.Remove(dashed);
		Assert.Throws<ProfileException>(() => variables.TryGetValue("app", "db_name", out _));
		section.Entries.Remove(dotted);
		AssertValue(variables, "app", "db_name", "underscore");
		AssertValue(sectionVariables, "app", "db_name", "underscore");
	}

	[Fact]
	public void EquivalentNamespaces_MergeDistinctNamesAndLimitConflictsToSelectedView()
	{
		using var reader = new StringReader("[io rustfs]\nshared=nested\nnestedOnly=first\n[io.rustfs]\nshared=flat\nflatOnly=second");
		var profile = Profile.Load(reader);
		var variables = profile.ToVariables();
		var nested = profile.Sections["io"].ToVariables();
		var flat = profile.Sections["io.rustfs"].ToVariables();

		AssertValue(variables, "io.rustfs", "nestedOnly", "first");
		AssertValue(variables, "io.rustfs", "flatOnly", "second");
		Assert.Throws<ProfileException>(() => variables.TryGetValue("IO.RUSTFS", "SHARED", out _));
		AssertValue(nested, "io.rustfs", "shared", "nested");
		AssertValue(flat, "io.rustfs", "shared", "flat");
		Assert.False(nested.TryGetValue("io.rustfs", "flatOnly", out _));
		Assert.False(flat.TryGetValue("io.rustfs", "nestedOnly", out _));
	}

	[Fact]
	public void TemplateEvaluation_UsesRawStringsAndNullValuesWithoutRecursiveExpansion()
	{
		using var reader = new StringReader("root=default\nreference=${root}\n[io rustfs]\naccess-key=42\nflag");
		var profile = Profile.Load(reader);
		var fallback = new Variables { ["io.rustfs:flag"] = "fallback", ["other"] = "last" };
		var evaluator = new TemplateEvaluator { Providers = { profile.ToVariables(), fallback } };

		Assert.Equal("default/42//${root}/last", evaluator.Evaluate("${root}/${IO.RUSTFS:ACCESS_KEY}/${io.rustfs:flag}/${reference}/${other}"));
		profile.Sections.Find("io rustfs").Entries["access-key"].Value = "updated";
		Assert.Equal("updated", evaluator.Evaluate("${io.rustfs:access_key}"));
	}

	[Fact]
	public void TemplateEvaluation_WrapsQueriedCollisionAsProviderFailed()
	{
		var profile = new Profile();
		profile.Entries.Add("db-name", "dash");
		profile.Entries.Add("db.name", "dot");
		profile.Entries.Add("other", "safe");
		var evaluator = new TemplateEvaluator { Providers = { profile.ToVariables() } };

		Assert.Equal("safe", evaluator.Evaluate("${other}"));
		var error = Assert.Throws<TemplateEvaluationException>(() => evaluator.Evaluate("${db_name}"));

		Assert.Equal("ProviderFailed", error.Code);
		Assert.IsType<ProfileException>(error.InnerException);
	}

	private static void AssertValue(IVariables variables, string scope, string name, string expected)
	{
		Assert.True(variables.TryGetValue(scope, name, out var value));
		Assert.Equal(expected, value);
	}
}
