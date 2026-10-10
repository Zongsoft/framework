using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Zongsoft.Configuration.Profiles;

using Xunit;

namespace Zongsoft.Configuration.Tests;

public class ProfileDirectiveTest
{
	[Theory]
	[InlineData("#@import", "import", true)]
	[InlineData(";@ImPoRt", "ImPoRt", true)]
	[InlineData("#@imported", "imported", false)]
	[InlineData("#@import.child", "import.child", false)]
	[InlineData("#@_custom-2", "_custom-2", false)]
	[InlineData("# @import", null, false)]
	[InlineData("#@1import", null, false)]
	[InlineData("#@import/child", null, false)]
	public void Directive_RecognizesValidNamesAndExactBuiltinTokens(string prefix, string name, bool imports)
	{
		using var files = new ProfileImportTest.ProfileFiles();
		files.Write("child.ini", "imported=child");
		var root = files.Write("root.ini", prefix + " child.ini\nlocal=root");
		var processing = new List<ProfileDirectiveContext>();
		var profile = Profile.Load(root, new ProfileOptions { Directives = { Processing = processing.Add } });

		Assert.Equal(imports ? "child" : null, profile.Entries["imported"]?.Value);
		Assert.Equal("root", profile.Entries["local"].Value);
		Assert.Equal(prefix[1..] + " child.ini", Assert.Single(profile.Comments).Text);

		if(name == null)
			Assert.Empty(processing);
		else
		{
			var context = Assert.Single(processing);
			Assert.Equal(name, context.Name);
			Assert.Equal(imports, context.Handled);
		}
	}

	[Fact]
	public void Callbacks_MultipleAndNestedImportsHaveIndependentLifecycles()
	{
		using var files = new ProfileImportTest.ProfileFiles();
		files.Write("leaf.ini", "leaf=present");
		files.Write("first.ini", "#@import leaf.ini\nfirst=present");
		files.Write("second.ini", "second=present");

		var root = files.Write("root.ini", "local=present\n[section]\n;@ImPoRt first.ini\n#@import second.ini\nlast=present");
		var events = new List<string>();
		var starting = new List<ProfileContext>();
		var completed = new List<ProfileContext>();
		var processing = new List<ProfileDirectiveContext>();
		var processed = new List<ProfileDirectiveContext>();

		var options = new ProfileOptions
		{
			Loading = context =>
			{
				starting.Add(context);
				Assert.Null(context.Profile);
				events.Add("load:" + Path.GetFileName(context.FilePath));
			},
			Loaded = context =>
			{
				completed.Add(context);
				Assert.Equal(context.FilePath, context.Profile.FilePath);
				if(context.Referer != null && context.Profile.Entries["leaf"] != null)
					Assert.Same(context.Profile.Entries["leaf"], context.Referer.Entries["leaf"]);
				events.Add("loaded:" + Path.GetFileName(context.FilePath));
			},
		};
		options.Directives.Processing = context =>
		{
			processing.Add(context);
			Assert.False(context.Handled);
			events.Add("directive:" + Path.GetFileName(context.FilePath));
		};
		options.Directives.Processed = context =>
		{
			processed.Add(context);
			Assert.True(context.Handled);
			Assert.Equal("present", context.Profile.Entries["leaf"].Value);
			events.Add("processed:" + Path.GetFileName(context.FilePath));
		};

		var profile = Profile.Load(root, options);

		Assert.Equal(new[]
		{
			"load:root.ini", "directive:root.ini", "load:first.ini", "directive:first.ini",
			"load:leaf.ini", "loaded:leaf.ini", "processed:first.ini", "loaded:first.ini",
			"processed:root.ini", "directive:root.ini", "load:second.ini", "loaded:second.ini",
			"processed:root.ini", "loaded:root.ini",
		}, events);

		Assert.Equal([1, 2, 3, 2], starting.Select(context => context.Depth));
		Assert.Equal([3, 2, 2, 1], completed.Select(context => context.Depth));
		Assert.Null(starting[0].Referer);
		Assert.Null(completed[3].Referer);
		Assert.Same(profile, completed[3].Profile);
		Assert.Same(profile, starting[1].Referer);
		Assert.NotSame(starting[0], completed[3]);
		Assert.All(starting, context => Assert.Null(context.Profile));
		Assert.Same(processing[0], processed[1]);
		Assert.Same(processing[1], processed[0]);
		Assert.Same(processing[2], processed[2]);
		Assert.Equal("ImPoRt", processing[0].Name);
		Assert.Equal("first.ini", processing[0].Argument);
		Assert.Equal(3, processing[0].LineNumber);
		Assert.Equal(1, processing[0].Depth);
		Assert.Same(profile, processing[0].Profile);
		Assert.Same(profile.Sections["section"], processing[0].Section);
		Assert.Equal(2, processing[1].Depth);
		Assert.Equal(1, processing[1].LineNumber);
		Assert.Null(processing[1].Section);
		Assert.Equal("second.ini", processing[2].Argument);
		Assert.Equal(4, processing[2].LineNumber);
		Assert.Equal(1, processing[2].Depth);
		Assert.Same(profile, processing[2].Profile);
		Assert.Same(profile.Sections["section"], processing[2].Section);
		Assert.Equal("present", profile.Entries["second"].Value);
		Assert.Equal("present", profile.Sections["section"].Entries["last"].Value);
	}

	[Theory]
	[InlineData("path")]
	[InlineData("stream")]
	[InlineData("text")]
	public void RootCallbacks_DescribeAllInputKinds(string kind)
	{
		using var files = new ProfileImportTest.ProfileFiles();
		var path = files.Write("root.ini", "value=complete");
		var notifications = new List<ProfileContext>();
		var options = new ProfileOptions { Loading = notifications.Add, Loaded = notifications.Add };
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes("value=complete"));
		using var reader = new StringReader("value=complete");

		var profile = kind switch
		{
			"path" => Profile.Load(path, options),
			"stream" => Profile.Load(stream, options),
			_ => Profile.Load(reader, options),
		};

		Assert.Equal(2, notifications.Count);
		Assert.Null(notifications[0].Profile);
		Assert.Same(profile, notifications[1].Profile);
		Assert.NotSame(notifications[0], notifications[1]);
		Assert.All(notifications, context =>
		{
			Assert.Equal(1, context.Depth);
			Assert.Null(context.Referer);
			Assert.Equal(kind == "path" ? path : string.Empty, context.FilePath);
		});
		Assert.Equal("complete", profile.Entries["value"].Value);

		if(kind == "stream")
			Assert.False(stream.CanRead);
		if(kind == "text")
			Assert.Equal(-1, reader.Peek());
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void RootCallbackFailure_ReleasesOwnedInputAndCanRetry(bool after)
	{
		var failure = new FileNotFoundException("Callback failure");
		var events = new List<string>();
		var failing = true;
		var options = new ProfileOptions
		{
			Loading = _ =>
			{
				events.Add("loading");
				if(failing && !after)
					throw failure;
			},
			Loaded = _ =>
			{
				events.Add("loaded");
				if(failing && after)
					throw failure;
			},
		};

		using var stream = new MemoryStream(Encoding.UTF8.GetBytes("value=complete"));
		Assert.Same(failure, Assert.Throws<FileNotFoundException>(() => Profile.Load(stream, options)));
		Assert.False(stream.CanRead);
		Assert.Equal(after ? ["loading", "loaded"] : ["loading"], events);

		using var text = new StringReader("value=complete");
		Assert.Same(failure, Assert.Throws<FileNotFoundException>(() => Profile.Load(text, options)));
		Assert.Equal(after ? -1 : 'v', text.Peek());

		failing = false;
		events.Clear();
		using var retry = new StringReader("value=complete");
		Assert.Equal("complete", Profile.Load(retry, options).Entries["value"].Value);
		Assert.Equal(["loading", "loaded"], events);
	}

	[Fact]
	public void Directive_RewrittenArgumentsPreserveOriginalDeclarations()
	{
		using var files = new ProfileImportTest.ProfileFiles();
		files.Write("actual settings.ini", "value=rewritten");
		var root = files.Write("root.ini", "#@import absent.ini");
		var options = new ProfileOptions
		{
			Directives = { ProfileDirectiveOptions.Import(ProfileDirectiveBehavior.Strict) },
		};
		options.Directives.Processing = context =>
		{
			Assert.Equal("absent.ini", context.Argument);
			context.Argument = " \tactual settings.ini\t ";
		};
		options.Directives.Processed = context =>
		{
			Assert.Equal("actual settings.ini", context.Argument);
			Assert.Equal("rewritten", context.Profile.Entries["value"].Value);
			context.Argument = "another-missing.ini";
		};

		var profile = Profile.Load(root, options);

		Assert.Equal("rewritten", profile.Entries["value"].Value);
		using var output = new StringWriter { NewLine = "\n" };
		profile.Save(output);
		Assert.Equal("#@import absent.ini\n", output.ToString());
	}

	[Theory]
	[InlineData("import")]
	[InlineData("custom")]
	public void Directive_HandledSkipsBuiltinImportAndSupportsCustomCommands(string name)
	{
		using var input = new StringReader("#@" + name + " nonexistent.ini\nlocal=kept");
		var completed = new List<ProfileDirectiveContext>();
		var options = new ProfileOptions
		{
			Directives = { new ProfileDirectiveOptions(name, ProfileDirectiveBehavior.Strict) },
		};
		options.Directives.Processing = context =>
		{
			context.Profile.Entries.Add("handled", "externally");
			context.Handled = true;
		};
		options.Directives.Processed = completed.Add;

		var profile = Profile.Load(input, options);

		Assert.Equal("externally", profile.Entries["handled"].Value);
		Assert.Equal("kept", profile.Entries["local"].Value);
		Assert.True(Assert.Single(completed).Handled);
		Assert.Equal("@" + name + " nonexistent.ini", Assert.Single(profile.Comments).Text);
	}

	[Theory]
	[InlineData("import", ProfileDirectiveBehavior.Ignore)]
	[InlineData("import", ProfileDirectiveBehavior.Suppress)]
	[InlineData("custom", ProfileDirectiveBehavior.Ignore)]
	[InlineData("custom", ProfileDirectiveBehavior.Suppress)]
	public void Directive_BehaviorPrecedesCallbacksAndInput(string name, ProfileDirectiveBehavior behavior)
	{
		using var files = new ProfileImportTest.ProfileFiles();
		var child = files.Write("child.ini", "child=value");
		var root = files.Write("root.ini", "#@" + name + " child.ini\nlocal=kept");
		var events = new List<string>();
		var options = new ProfileOptions
		{
			Directives = { new ProfileDirectiveOptions(name.ToUpperInvariant(), behavior) },
			Loading = context => events.Add("loading:" + context.Depth),
			Loaded = context => events.Add("loaded:" + context.Depth),
		};
		options.Directives.Processing = _ => Assert.Fail("Ignored or prohibited directives must not be processed.");
		options.Directives.Processed = _ => Assert.Fail("Ignored or prohibited directives must not complete.");
		using var locked = new FileStream(child, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

		if(behavior == ProfileDirectiveBehavior.Suppress)
		{
			Assert.Throws<ProfileException>(() => Profile.Load(root, options));
			Assert.Equal(["loading:1"], events);
		}
		else
		{
			var profile = Profile.Load(root, options);
			Assert.Equal("kept", Assert.Single(profile.Entries).Value);
			Assert.Equal("@" + name + " child.ini", Assert.Single(profile.Comments).Text);
			Assert.Equal(["loading:1", "loaded:1"], events);
		}
	}

	[Theory]
	[InlineData(ProfileDirectiveBehavior.None, false)]
	[InlineData(ProfileDirectiveBehavior.Strict, false)]
	[InlineData(ProfileDirectiveBehavior.Strict, true)]
	public void Directive_UnknownStrictRequiresHandling(ProfileDirectiveBehavior behavior, bool handled)
	{
		using var input = new StringReader("#@unknown argument\nvalue=kept");
		var events = new List<string>();
		var options = new ProfileOptions
		{
			Directives = { new ProfileDirectiveOptions("unknown", behavior) },
		};
		options.Directives.Processing = context => { events.Add("processing"); context.Handled = handled; };
		options.Directives.Processed = context => { events.Add("processed"); Assert.Equal(handled, context.Handled); };

		if(behavior == ProfileDirectiveBehavior.Strict && !handled)
		{
			Assert.Throws<ProfileException>(() => Profile.Load(input, options));
			Assert.Equal(["processing"], events);
			Assert.Equal('v', input.Peek());
		}
		else
		{
			var profile = Profile.Load(input, options);
			Assert.Equal("kept", profile.Entries["value"].Value);
			Assert.Equal("@unknown argument", Assert.Single(profile.Comments).Text);
			Assert.Equal(["processing", "processed"], events);
		}
	}

	[Theory]
	[InlineData("")]
	[InlineData("missing.ini")]
	public void Directive_EmptyAndOptionalMissingImportsComplete(string argument)
	{
		using var files = new ProfileImportTest.ProfileFiles();
		var root = files.Write("root.ini", "#@import " + argument + "\nvalue=kept");
		var events = new List<string>();
		var profile = Profile.Load(root, new ProfileOptions
		{
			Loading = context => events.Add("loading:" + context.Depth),
			Loaded = context => events.Add("loaded:" + context.Depth),
			Directives =
			{
				Processing = _ => events.Add("processing"),
				Processed = context => { Assert.True(context.Handled); events.Add("processed"); },
			},
		});

		Assert.Equal("kept", profile.Entries["value"].Value);
		Assert.Equal(["loading:1", "processing", "processed", "loaded:1"], events);
	}

	[Theory]
	[InlineData(null, null)]
	[InlineData("", "")]
	[InlineData(" \t ", "")]
	public void Directive_EmptyArgumentOverrideSkipsImportAndPreservesDeclarations(string argument, string expected)
	{
		using var files = new ProfileImportTest.ProfileFiles();
		var child = files.Write("child.ini", "imported=unexpected");
		var root = files.Write("root.ini", "#@import child.ini\nvalue=kept");
		var events = new List<string>();
		var options = new ProfileOptions
		{
			Directives = { ProfileDirectiveOptions.Import(ProfileDirectiveBehavior.Strict) },
			Loading = context => events.Add("loading:" + context.Depth),
			Loaded = context => events.Add("loaded:" + context.Depth),
		};
		options.Directives.Processing = context =>
		{
			Assert.Equal("child.ini", context.Argument);
			context.Argument = argument;
			events.Add("processing");
		};
		options.Directives.Processed = context =>
		{
			Assert.Equal(expected, context.Argument);
			Assert.True(context.Handled);
			events.Add("processed");
		};

		using var locked = new FileStream(child, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
		var profile = Profile.Load(root, options);

		Assert.Equal("kept", Assert.Single(profile.Entries).Value);
		Assert.Null(profile.Entries["imported"]);
		Assert.Equal(["loading:1", "processing", "processed", "loaded:1"], events);

		using var output = new StringWriter { NewLine = "\n" };
		profile.Save(output);
		Assert.Equal("#@import child.ini\nvalue=kept\n", output.ToString());
	}

	[Fact]
	public void Directive_OptionsAreClonedAtRootAndForEachContext()
	{
		var custom = new ProfileOptionsTest.ExtendedOptions("custom") { Behavior = ProfileDirectiveBehavior.Strict, Values = ["original"] };
		var import = ProfileDirectiveOptions.Import(ProfileDirectiveBehavior.Strict, 4);
		var seen = new List<ProfileDirectiveOptions>();
		var options = new ProfileOptions { Directives = { custom, import } };

		options.Loading = _ =>
		{
			custom.Values.Add("source changed");
			options.Directives.Clear();
		};

		options.Directives.Processing = context =>
		{
			Assert.Equal(ProfileDirectiveBehavior.Strict, context.Behavior);
			seen.Add(context.Options);
			Assert.Equal(ProfileDirectiveBehavior.Strict, context.Options.Behavior);
			context.Options.Behavior = ProfileDirectiveBehavior.Suppress;

			if(context.Name == "custom")
			{
				var current = Assert.IsType<ProfileOptionsTest.ExtendedOptions>(context.Options);
				Assert.Equal(["original"], current.Values);
				current.Values.Add("context changed");
			}
			else
			{
				var current = Assert.IsType<ProfileDirectiveOptions.ImportOptions>(context.Options);
				Assert.Equal(4, current.MaximumDepth);
				current.MaximumDepth = 1;
			}

			context.Handled = true;
		};

		using var input = new StringReader("#@custom first\n#@import missing.ini\n#@custom second\n#@import absent.ini\nvalue=kept");
		var profile = Profile.Load(input, options);

		Assert.Equal("kept", profile.Entries["value"].Value);
		Assert.Equal(4, seen.Count);
		Assert.NotSame(seen[0], seen[2]);
		Assert.NotSame(seen[1], seen[3]);
		Assert.Equal(["original", "source changed"], custom.Values);
		Assert.Equal(4, import.MaximumDepth);
		Assert.Equal(ProfileDirectiveBehavior.Strict, import.Behavior);
		Assert.Empty(options.Directives);
	}

	[Fact]
	public void Directive_VariableExpressionsRemainLiteralUntilCallbackRewrites()
	{
		using var files = new ProfileImportTest.ProfileFiles();
		files.Write("$(product).ini", "value=literal");
		files.Write("actual.ini", "value=rewritten");
		var root = files.Write("root.ini", "#@import $(product).ini\nexpression=$(product)");
		var options = new ProfileOptions { Directives = { ProfileDirectiveOptions.Import(ProfileDirectiveBehavior.Strict) } };

		var literal = Profile.Load(root, options);
		Assert.Equal("literal", literal.Entries["value"].Value);
		Assert.Equal("$(product)", literal.Entries["expression"].Value);
		options.Directives.Processing = context => context.Argument = context.Argument.Replace("$(product)", "actual");
		var rewritten = Profile.Load(root, options);
		Assert.Equal("rewritten", rewritten.Entries["value"].Value);
		Assert.Equal("$(product)", rewritten.Entries["expression"].Value);
		Assert.Equal("@import $(product).ini", Assert.Single(rewritten.Comments).Text);
	}

	[Fact]
	public void Directive_CallbackChangesApplyOnlyToNextRoot()
	{
		var events = new List<string>();
		var failure = new InvalidOperationException("Replacement callback");
		var options = new ProfileOptions();
		options.Directives.Processing = context =>
		{
			events.Add("processing:" + context.Argument);
			options.Directives.Processing = _ => throw failure;
			options.Directives.Processed = _ => throw failure;
		};
		options.Directives.Processed = context => events.Add("processed:" + context.Argument);
		using var first = new StringReader("#@custom first\n#@custom second\nvalue=kept");

		Assert.Equal("kept", Profile.Load(first, options).Entries["value"].Value);
		Assert.Equal(["processing:first", "processed:first", "processing:second", "processed:second"], events);
		using var second = new StringReader("#@custom third\nvalue=kept");
		Assert.Same(failure, Assert.Throws<InvalidOperationException>(() => Profile.Load(second, options)));
		Assert.Equal('v', second.Peek());
	}

	[Fact]
	public void Directive_FailedImportSkipsDirectiveAndRootCompletion()
	{
		using var files = new ProfileImportTest.ProfileFiles();
		var root = files.Write("root.ini", "#@import missing.ini\nvalue=unread");
		var events = new List<string>();
		var options = new ProfileOptions
		{
			Directives = { ProfileDirectiveOptions.Import(ProfileDirectiveBehavior.Strict) },
			Loading = context => events.Add("loading:" + context.Depth),
			Loaded = context => events.Add("loaded:" + context.Depth),
		};
		options.Directives.Processing = _ => events.Add("processing");
		options.Directives.Processed = _ => events.Add("processed");

		var error = Assert.Throws<ProfileException>(() => Profile.Load(root, options));
		Assert.IsType<FileNotFoundException>(error.InnerException);
		Assert.Equal(["loading:1", "processing"], events);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void Directive_CallbackFailurePropagatesAndReleasesFiles(bool after)
	{
		using var files = new ProfileImportTest.ProfileFiles();
		var child = files.Write("child.ini", "child=present");
		var root = files.Write("root.ini", "#@import child.ini\nlocal=present");
		var failure = new FileNotFoundException("Directive callback failure");
		var events = new List<string>();
		var failing = true;

		ProfileDirectiveContext seen = null;
		var options = new ProfileOptions
		{
			Loaded = context => events.Add("loaded:" + context.Depth),
		};
		options.Directives.Processing = context =>
		{
			seen = context;
			events.Add("processing");
			if(failing && !after)
				throw failure;
		};
		options.Directives.Processed = _ =>
		{
			events.Add("processed");
			if(failing && after)
				throw failure;
		};

		Assert.Same(failure, Assert.Throws<FileNotFoundException>(() => Profile.Load(root, options)));
		Assert.Equal(after ? ["processing", "loaded:2", "processed"] : ["processing"], events);
		Assert.Equal(after ? "present" : null, seen.Profile.Entries["child"]?.Value);
		Assert.Null(seen.Profile.Entries["local"]);

		using(var exclusive = new FileStream(child, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
			Assert.True(exclusive.Length > 0);

		using(var exclusive = new FileStream(root, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
			Assert.True(exclusive.Length > 0);

		failing = false;
		events.Clear();
		Assert.Equal("present", Profile.Load(root, options).Entries["local"].Value);
		Assert.Equal(["processing", "loaded:2", "processed", "loaded:1"], events);
	}
}
