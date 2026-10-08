using System;
using System.IO;
using System.Collections.Generic;

using Zongsoft.Configuration.Profiles;

using Xunit;

namespace Zongsoft.Configuration.Tests;

public class ProfileOptionsTest
{
	[Theory]
	[InlineData("missing.ini", false)]
	[InlineData("missing/child.ini", false)]
	[InlineData("missing.ini", true)]
	[InlineData("missing/child.ini", true)]
	public void ImportBehavior_StrictRequiresDirectAndRecursiveFiles(string target, bool recursive)
	{
		using var files = new ProfileImportTest.ProfileFiles();
		files.Write("child.ini", "#@import " + target);
		var root = files.Write("root.ini", recursive ? "#@import child.ini" : "#@import " + target);
		var completed = new List<ProfileContext>();
		var options = new ProfileOptions
		{
			Directives = { ProfileDirectiveOptions.Import(ProfileDirectiveBehavior.Strict) },
			Loaded = completed.Add,
		};

		var error = Assert.Throws<ProfileException>(() => Profile.Load(root, options));

		Assert.IsAssignableFrom<IOException>(error.InnerException);
		Assert.Empty(completed);
		files.Write(target, "result=complete");
		Assert.Equal("complete", Profile.Load(root, options).Entries["result"].Value);
	}

	[Fact]
	public void ImportBehavior_ExistenceCheckIsCapturedBeforeCallbacks()
	{
		using var files = new ProfileImportTest.ProfileFiles();
		files.Write("child.ini", "#@import missing.ini\nresult=complete");
		var root = files.Write("root.ini", "#@import child.ini");
		var options = new ProfileOptions { Directives = { ProfileDirectiveOptions.Import() } };
		options.Loading = _ => options.Directives["import"].Behavior = ProfileDirectiveBehavior.Strict;
		Assert.Equal("complete", Profile.Load(root, options).Entries["result"].Value);

		options.Loading = _ => options.Directives["import"].Behavior = ProfileDirectiveBehavior.None;
		Assert.Throws<ProfileException>(() => Profile.Load(root, options));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void Options_DefaultCallbacksStillLoadImports(bool preserveBlanks)
	{
		using var files = new ProfileImportTest.ProfileFiles();
		var child = files.Write("child.ini", "imported=child");
		var root = files.Write("root.ini", "\n#@import child.ini\nlocal=root");
		var options = new ProfileOptions(preserveBlanks);

		var profile = Profile.Load(root, options);

		Assert.Empty(options.Directives);
		Assert.Equal(preserveBlanks ? [0] : Array.Empty<int>(), profile.Blanks);
		Assert.Equal("root", profile.Entries["local"].Value);
		Assert.Equal("child", profile.Entries["imported"].Value);
		Assert.Equal(child, profile.Entries["imported"].Profile.FilePath);
	}

	[Theory]
	[InlineData(-1)]
	[InlineData(int.MinValue)]
	public void Options_InvalidMaximumDepthPreservesPreviousLimit(int depth)
	{
		var import = ProfileDirectiveOptions.Import(maximumDepth: 3);
		var options = new ProfileOptions { Directives = { import } };

		Assert.Throws<ArgumentOutOfRangeException>(() => ProfileDirectiveOptions.Import(maximumDepth: depth));
		Assert.Throws<ArgumentOutOfRangeException>(() => new ProfileDirectiveOptions.ImportOptions(maximumDepth: depth));
		Assert.Throws<ArgumentOutOfRangeException>(() => import.MaximumDepth = depth);

		Assert.Equal(3, import.MaximumDepth);
		using var files = new ProfileImportTest.ProfileFiles();
		Assert.Equal("complete", Profile.Load(files.Chain(3), options).Entries["result"].Value);
		Assert.Throws<ProfileException>(() => Profile.Load(files.Chain(4), options));
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData(" ")]
	[InlineData("@import")]
	[InlineData("1import")]
	[InlineData("import child")]
	[InlineData("import/child")]
	[InlineData(" import")]
	public void DirectiveOptions_InvalidNamesAreRejected(string name)
	{
		var error = Assert.ThrowsAny<ArgumentException>(() => new ProfileDirectiveOptions(name));
		Assert.Equal("name", error.ParamName);
	}

	[Fact]
	public void Directives_CollectionUsesNamesAndRejectsInvalidItems()
	{
		var directives = new ProfileDirectiveOptionsCollection();
		var import = ProfileDirectiveOptions.Import(ProfileDirectiveBehavior.Strict);
		directives.Add(import);
		directives.Add(new ProfileDirectiveOptions("_custom.part-2"));

		Assert.Same(import, directives["IMPORT"]);
		Assert.True(directives.TryGetValue("ImPoRt", out var found));
		Assert.Same(import, found);
		Assert.Throws<ArgumentException>(() => directives.Add(new ProfileDirectiveOptions("IMPORT")));
		Assert.Throws<ArgumentNullException>(() => directives.Add(null));
		Assert.Throws<ArgumentNullException>(() => directives[0] = null);
		Assert.Throws<ArgumentException>(() => directives[1] = new ProfileDirectiveOptions("import"));
		Assert.Equal(2, directives.Count);
		Assert.Equal("_custom.part-2", directives[1].Name);
		Assert.True(directives.Remove("IMPORT"));
		Assert.False(directives.TryGetValue("import", out _));
		Assert.Equal("_custom.part-2", Assert.Single(directives).Name);
	}

	[Theory]
	[InlineData(-1)]
	[InlineData(4)]
	public void DirectiveOptions_InvalidBehaviorPreservesExistingValue(int behavior)
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => new ProfileDirectiveOptions("custom", (ProfileDirectiveBehavior)behavior));
		var options = ProfileDirectiveOptions.Import(ProfileDirectiveBehavior.Strict);
		Assert.Throws<ArgumentOutOfRangeException>(() => options.Behavior = (ProfileDirectiveBehavior)behavior);
		Assert.Equal(ProfileDirectiveBehavior.Strict, options.Behavior);
	}

	[Fact]
	public void Directives_RemoveRestoresBuiltinBehavior()
	{
		using var files = new ProfileImportTest.ProfileFiles();
		var root = files.Write("root.ini", "#@import missing.ini\nvalue=kept");
		var options = new ProfileOptions
		{
			Directives = { new ProfileDirectiveOptions("IMPORT", ProfileDirectiveBehavior.Strict) },
		};

		Assert.Throws<ProfileException>(() => Profile.Load(root, options));
		Assert.True(options.Directives.Remove("import"));
		Assert.Equal("kept", Profile.Load(root, options).Entries["value"].Value);
	}

	[Fact]
	public void DirectiveOptions_CloneRetainsDerivedValuesWithoutSharingMutableMembers()
	{
		var import = ProfileDirectiveOptions.Import(ProfileDirectiveBehavior.Strict, 3);
		var cloned = Assert.IsType<ProfileDirectiveOptions.ImportOptions>(import.Clone());
		cloned.Behavior = ProfileDirectiveBehavior.Suppress;
		cloned.MaximumDepth = 1;
		Assert.Equal(ProfileDirectiveBehavior.Strict, import.Behavior);
		Assert.Equal(3, import.MaximumDepth);
		Assert.Equal("import", cloned.Name);

		var original = new ExtendedOptions("custom") { Values = ["original"] };
		var copy = Assert.IsType<ExtendedOptions>(original.Clone());
		copy.Values.Add("changed");
		Assert.Equal(["original"], original.Values);
		Assert.Equal(["original", "changed"], copy.Values);
	}

	[Fact]
	public void Options_ExposeIndependentCollectionsAndTwoCallbackGroups()
	{
		var options = new ProfileOptions();
		var other = new ProfileOptions();
		options.Directives.Add(ProfileDirectiveOptions.Import(ProfileDirectiveBehavior.Suppress));
		Assert.Empty(other.Directives);
		Assert.Null(typeof(ProfileOptions).GetProperty(nameof(ProfileOptions.Directives)).SetMethod);
		Assert.Equal(typeof(Action<ProfileContext>), typeof(ProfileOptions).GetProperty(nameof(ProfileOptions.Loading)).PropertyType);
		Assert.Equal(typeof(Action<ProfileContext>), typeof(ProfileOptions).GetProperty(nameof(ProfileOptions.Loaded)).PropertyType);
		Assert.Equal(typeof(Action<ProfileDirectiveContext>), typeof(ProfileOptions).GetProperty(nameof(ProfileOptions.DirectiveProcessing)).PropertyType);
		Assert.Equal(typeof(Action<ProfileDirectiveContext>), typeof(ProfileOptions).GetProperty(nameof(ProfileOptions.DirectiveProcessed)).PropertyType);
		Assert.All(typeof(ProfileContext).GetProperties(), property => Assert.Null(property.SetMethod));
		Assert.Empty(typeof(ProfileContext).GetConstructors());
		Assert.True(typeof(ProfileContext).IsSealed);
		Assert.All(new[] { "ImportBehavior", "MaximumDepth", "Importing", "Imported", "Variables" }, name =>
			Assert.Null(typeof(ProfileOptions).GetProperty(name)));
	}

	internal sealed class ExtendedOptions(string name) : ProfileDirectiveOptions(name)
	{
		public List<string> Values { get; set; } = [];
		public override ProfileDirectiveOptions Clone()
		{
			var clone = (ExtendedOptions)base.Clone();
			clone.Values = [.. this.Values];
			return clone;
		}
	}
}
