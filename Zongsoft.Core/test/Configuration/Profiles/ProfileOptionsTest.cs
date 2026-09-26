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
	public void RequireImports_MissingFilesAbortTheWholeLoad(string target, bool recursive)
	{
		using var files = new ProfileImportTest.ProfileFiles();
		var child = files.Write("child.ini", "#@import " + target);
		var root = files.Write("root.ini", recursive ? "#@import child.ini" : "#@import " + target);
		var completed = new List<ProfileContext>();
		var options = new ProfileOptions { RequireImports = true, Imported = completed.Add };

		var error = Assert.Throws<ProfileException>(() => Profile.Load(root, options));

		Assert.IsAssignableFrom<IOException>(error.InnerException);
		Assert.Empty(completed);
		files.Write(target, "result=complete");
		Assert.Equal("complete", Profile.Load(root, options).Entries["result"].Value);
	}

	[Fact]
	public void RequireImports_IsOptionalAndCapturedBeforeCallbacks()
	{
		using var files = new ProfileImportTest.ProfileFiles();
		files.Write("child.ini", "#@import missing.ini\nresult=complete");
		var root = files.Write("root.ini", "#@import child.ini");
		var options = new ProfileOptions();
		Assert.False(options.RequireImports);
		options.Importing = _ => options.RequireImports = true;
		Assert.Equal("complete", Profile.Load(root, options).Entries["result"].Value);

		options.Importing = _ => options.RequireImports = false;
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

		Assert.Equal(64, options.MaximumDepth);
		Assert.Null(options.Importing);
		Assert.Null(options.Imported);
		Assert.Equal(preserveBlanks ? [0] : Array.Empty<int>(), profile.Blanks);
		Assert.Equal("root", profile.Entries["local"].Value);
		Assert.Equal("child", profile.Entries["imported"].Value);
		Assert.Equal(child, profile.Entries["imported"].Profile.FilePath);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	[InlineData(int.MinValue)]
	public void Options_InvalidMaximumDepthPreservesPreviousLimit(int depth)
	{
		var options = new ProfileOptions { MaximumDepth = 3 };

		Assert.Throws<ArgumentOutOfRangeException>(() => options.MaximumDepth = depth);

		Assert.Equal(3, options.MaximumDepth);
		using var files = new ProfileImportTest.ProfileFiles();
		Assert.Equal("complete", Profile.Load(files.Chain(3), options).Entries["result"].Value);
		Assert.Throws<ProfileException>(() => Profile.Load(files.Chain(4), options));
	}

	[Fact]
	public void Options_ImportingExceptionAbortsWithoutSkippingToLaterDeclarations()
	{
		using var files = new ProfileImportTest.ProfileFiles();
		var child = files.Write("child.ini", "value=child");
		var root = files.Write("root.ini", "#@import child.ini\ninvalid=first\ninvalid=duplicate");
		var failure = new InvalidOperationException("Imports forbidden for this load.");
		var notifications = new List<ProfileContext>();
		var options = new ProfileOptions
		{
			Importing = context =>
			{
				notifications.Add(context);
				throw failure;
			},
			Imported = _ => Assert.Fail("Rejected import must not complete."),
		};

		Assert.Same(failure, Assert.Throws<InvalidOperationException>(() => Profile.Load(root, options)));
		var context = Assert.Single(notifications);
		Assert.Equal(child, context.FilePath);
		Assert.Equal(root, context.Referer.FilePath);
		Assert.Equal(2, context.Depth);
		Assert.Null(context.Profile);
		Assert.Empty(context.Referer.Entries);
		using var input = new FileStream(child, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
		Assert.True(input.Length > 0);
	}

	[Theory]
	[InlineData("import", true)]
	[InlineData("ImPoRt", true)]
	[InlineData("imported", false)]
	[InlineData("import.child", false)]
	[InlineData("other", false)]
	public void Reader_OnlyExactImportTokenExecutes(string token, bool imports)
	{
		using var files = new ProfileImportTest.ProfileFiles();
		files.Write("child.ini", "imported=child");
		var root = files.Write("root.ini", "#@" + token + " child.ini\nlocal=root");
		var notifications = new List<string>();
		var options = new ProfileOptions { Importing = context => notifications.Add(context.FilePath) };

		var profile = Profile.Load(root, options);

		Assert.Equal(imports ? 2 : 1, profile.Entries.Count);
		Assert.Equal(imports ? 1 : 0, notifications.Count);
		Assert.Equal("root", profile.Entries["local"].Value);
		using var output = new StringWriter();
		profile.Save(output);
		Assert.Contains("#@" + token + " child.ini", output.ToString());
		Assert.DoesNotContain("imported=child", output.ToString());
	}

	[Fact]
	public void Options_ContextIsReadOnlyAndImportConfigurationIsRemoved()
	{
		Assert.All(typeof(ProfileContext).GetProperties(), property => Assert.Null(property.GetSetMethod(nonPublic: true)));
		Assert.Empty(typeof(ProfileContext).GetConstructors());
		Assert.True(typeof(ProfileContext).IsSealed);
		Assert.True(typeof(ProfileContext).IsVisible);
		Assert.Equal(typeof(Action<ProfileContext>), typeof(ProfileOptions).GetProperty(nameof(ProfileOptions.Importing)).PropertyType);
		Assert.Equal(typeof(Action<ProfileContext>), typeof(ProfileOptions).GetProperty(nameof(ProfileOptions.Imported)).PropertyType);
		Assert.Equal(5, typeof(ProfileOptions).GetProperties().Length);
		Assert.Null(typeof(ProfileOptions).GetProperty("Import"));
		Assert.Null(typeof(ProfileOptions).GetProperty("Directives"));

		var removed = new[]
		{
			"IProfileDirective", "ProfileDirective", "ProfileDirectiveCollection", "ProfileDirectiveProvider",
			"IProfileDirectiveProvider", "ProfileImportOptions", "ProfileReadingContext", "ProfileWritingContext",
			"ProfileContextUtility", "Directives.ImportDirective",
		};

		Assert.All(removed, name => Assert.Null(typeof(Profile).Assembly.GetType("Zongsoft.Configuration.Profiles." + name)));
		Assert.DoesNotContain(typeof(Profile).Assembly.GetExportedTypes(), type =>
			type.Namespace?.StartsWith("Zongsoft.Configuration.Profiles", StringComparison.Ordinal) == true &&
			type.Name.Contains("Directive", StringComparison.Ordinal));
	}
}
