using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Zongsoft.Configuration.Profiles;

using Xunit;

namespace Zongsoft.Configuration.Tests;

public class ProfileDirectiveRegistryTest
{
	[Fact]
	public void DirectiveContexts_InheritLoadingContextAndDescribeImmediateReferer()
	{
		using var files = new ProfileImportTest.ProfileFiles();
		var leaf = files.Write("c.ini", "value=C");
		var child = files.Write("b.ini", "owner=B\n[nested]\n#@import c.ini");
		var root = files.Write("a.ini", "owner=A\n#@import b.ini");
		var contexts = new List<ProfileDirectiveContext>();
		var options = new ProfileOptions();
		options.Directives.Processing = context =>
		{
			contexts.Add(context);
			if(context.Depth == 2)
			{
				Assert.Equal("B", context.Profile.Entries["owner"].Value);
				Assert.Equal("A", context.Referer.Entries["owner"].Value);
			}
		};

		var profile = Profile.Load(root, options);

		Assert.Equal(2, contexts.Count);
		Assert.IsAssignableFrom<ProfileContext>(contexts[0]);
		Assert.IsAssignableFrom<ProfileContext>(contexts[1]);
		Assert.Same(profile, contexts[0].Profile);
		Assert.Null(contexts[0].Referer);
		Assert.Equal(root, contexts[0].FilePath);
		Assert.Equal(1, contexts[0].Depth);
		Assert.Equal(child, contexts[1].FilePath);
		Assert.Equal(2, contexts[1].Depth);
		Assert.Equal(3, contexts[1].LineNumber);
		Assert.Same(profile, contexts[1].Referer);
		Assert.Same(profile.Entries["owner"].Profile, contexts[1].Profile);
		Assert.Same(contexts[1].Profile.Sections["nested"], contexts[1].Section);
		Assert.Equal(leaf, profile.Entries["value"].Profile.FilePath);
		Assert.Equal("C", contexts[1].Profile.Entries["value"].Value);
	}

	[Fact]
	public void DirectiveCallbacks_OwnerIsClonedAndMutationsApplyNextRoot()
	{
		using var files = new ProfileImportTest.ProfileFiles();
		var root = files.Write("root.ini", "\n#@custom first\n#@custom second\nvalue=kept");
		var events = new List<string>();
		var options = new ProfileOptions();
		var source = options.Directives;
		var failure = new InvalidOperationException("Changed callback");
		source.Processing = context =>
		{
			events.Add("processing:" + context.Argument);
			source.Options.PreserveBlanks = false;
			source.Processing = _ => throw failure;
			source.Processed = _ => throw failure;
		};
		source.Processed = context => events.Add("processed:" + context.Argument);
		var session = ProfileImportTest.CreateSession(options);
		var snapshot = (ProfileOptions)session.GetType().GetProperty("Options").GetValue(session);

		var profile = ProfileImportTest.ReadProfile(session, root);

		Assert.NotSame(options, snapshot);
		Assert.NotSame(source, snapshot.Directives);
		Assert.Same(options, source.Options);
		Assert.Same(snapshot, snapshot.Directives.Options);
		Assert.True(snapshot.PreserveBlanks);
		Assert.False(options.PreserveBlanks);
		Assert.Equal([0], profile.Blanks);
		Assert.Equal("kept", profile.Entries["value"].Value);
		Assert.Equal(["processing:first", "processed:first", "processing:second", "processed:second"], events);
		Assert.Same(failure, Assert.Throws<InvalidOperationException>(() => Profile.Load(root, options)));
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData(" ")]
	[InlineData("1invalid")]
	[InlineData("invalid/name")]
	public void DirectiveBase_RejectsInvalidNames(string name)
	{
		var error = Assert.ThrowsAny<ArgumentException>(() => new EchoDirective(name));
		Assert.Equal("name", error.ParamName);
	}

	[Fact]
	public void Registry_UsesCaseInsensitiveUniqueNames()
	{
		var registry = new ProfileDirectiveCollection();
		var directive = new EchoDirective("Custom.Name-2");
		registry.Add(directive);

		Assert.True(registry.Contains("custom.NAME-2"));
		Assert.True(registry.TryGetValue("CUSTOM.name-2", out var found));
		Assert.Same(directive, found);
		Assert.Same(directive, registry["custom.name-2"]);
		Assert.Equal("Custom.Name-2", directive.Name);
		Assert.False(registry.Contains("absent"));
		Assert.False(registry.TryGetValue("absent", out var absent));
		Assert.Null(absent);
		Assert.Throws<KeyNotFoundException>(() => registry["absent"]);
		Assert.Throws<ArgumentNullException>(() => registry.Add(null));
		Assert.Throws<ArgumentException>(() => registry.Add(new EchoDirective("CUSTOM.NAME-2")));
		Assert.Same(directive, Assert.Single(registry));
	}

	[Fact]
	public void Registry_ConcurrentAddsAndEnumerationsRemainConsistent()
	{
		var registry = new ProfileDirectiveCollection();
		var directives = Enumerable.Range(0, 32).Select(index => new EchoDirective("custom_" + index)).ToArray();

		Parallel.ForEach(directives, directive =>
		{
			registry.Add(directive);
			Assert.Same(directive, registry[directive.Name.ToUpperInvariant()]);
			Assert.True(registry.TryGetValue(directive.Name, out var found));
			Assert.Same(directive, found);
			Assert.True(registry.Contains(directive.Name));
			Assert.InRange(registry.Count, 1, directives.Length);
			var snapshot = registry.Select(item => item.Name).ToArray();
			Assert.Equal(snapshot.Length, snapshot.Distinct(StringComparer.OrdinalIgnoreCase).Count());
		});

		Assert.Equal(directives.Length, registry.Count);
		Assert.Equal(directives.Select(item => item.Name).OrderBy(name => name), registry.Select(item => item.Name).OrderBy(name => name));
		using var enumerator = registry.GetEnumerator();
		registry.Add(new EchoDirective("after_snapshot"));
		var names = new List<string>();

		while(enumerator.MoveNext())
			names.Add(enumerator.Current.Name);

		Assert.Equal(directives.Length, names.Count);
		Assert.DoesNotContain("after_snapshot", names);
		Assert.Equal(directives.Length + 1, registry.Count);
	}

	[Fact]
	public void Registry_BuiltinImportIsOneSharedInstance()
	{
		Assert.Same(ImportDirective.Instance, Profile.Directives["IMPORT"]);
		Assert.Same(ImportDirective.Instance, Assert.Single(Profile.Directives, directive => directive.Name.Equals("import", StringComparison.OrdinalIgnoreCase)));
		Assert.Throws<ArgumentException>(() => Profile.Directives.Add(new ImportDirective()));
		Assert.Same(ImportDirective.Instance, Profile.Directives["import"]);
	}

	[Theory]
	[InlineData(ProfileDirectiveBehavior.None)]
	[InlineData(ProfileDirectiveBehavior.Strict)]
	[InlineData(ProfileDirectiveBehavior.Ignore)]
	[InlineData(ProfileDirectiveBehavior.Suppress)]
	public void RegisteredDirective_IsGlobalWithPerLoadBehavior(ProfileDirectiveBehavior behavior)
	{
		var directive = new CallbackDirective(UniqueName());
		Profile.Directives.Add(directive);
		var text = "#@" + directive.Name.ToUpperInvariant() + " configured\nlocal=kept";
		using var input = new StringReader(text);
		var invocations = 0;
		var configuration = new CallbackOptions(directive.Name, (instance, context) =>
		{
			Assert.Same(directive, instance);
			invocations++;
			context.Profile.Entries.Add("executed", context.Argument);
		})
		{
			Behavior = behavior,
		};
		var options = new ProfileOptions { Directives = { configuration } };
		var processed = new List<ProfileDirectiveContext>();
		options.Directives.Processed = processed.Add;

		if(behavior == ProfileDirectiveBehavior.Suppress)
		{
			Assert.Throws<ProfileException>(() => Profile.Load(input, options));
			Assert.Empty(processed);
		}
		else
		{
			var profile = Profile.Load(input, options);
			Assert.Equal("kept", profile.Entries["local"].Value);
			Assert.Equal(behavior == ProfileDirectiveBehavior.Ignore ? null : "configured", profile.Entries["executed"]?.Value);

			if(behavior == ProfileDirectiveBehavior.Ignore)
				Assert.Empty(processed);
			else
				Assert.True(Assert.Single(processed).Handled);
		}

		Assert.Equal(behavior is ProfileDirectiveBehavior.Ignore or ProfileDirectiveBehavior.Suppress ? 0 : 1, invocations);
		using var next = new StringReader(text);
		Assert.Equal("configured", Profile.Load(next).Entries["executed"].Value);
		Assert.Same(directive, Profile.Directives[directive.Name]);
	}

	[Fact]
	public void Registry_RegistrationsDuringRootLoadingWaitForNextRoot()
	{
		using var files = new ProfileImportTest.ProfileFiles();
		var directive = new EchoDirective(UniqueName());
		files.Write("child.ini", "#@" + directive.Name + " child\nchild=present");
		var root = files.Write("root.ini", "#@" + directive.Name + " root\n#@import child.ini");
		var handled = new List<bool>();
		var registered = false;
		var options = new ProfileOptions
		{
			Loading = context =>
			{
				if(context.Depth == 1 && !registered)
				{
					Profile.Directives.Add(directive);
					registered = true;
				}
			},
		};
		options.Directives.Processed = context =>
		{
			if(context.Name == directive.Name)
				handled.Add(context.Handled);
		};

		var first = Profile.Load(root, options);

		Assert.Equal([false, false], handled);
		Assert.Null(first.Entries["executed"]);
		Assert.Equal("present", first.Entries["child"].Value);
		Assert.Same(directive, Profile.Directives[directive.Name]);
		handled.Clear();
		var second = Profile.Load(root, options);
		Assert.Equal([true, true], handled);
		Assert.Equal("child", second.Entries["executed"].Value);
		Assert.Equal(files.PathFor("child.ini"), second.Entries["executed"].Profile.FilePath);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void RegisteredDirective_ImplementationFailurePropagatesUnlessProcessingHandlesIt(bool handled)
	{
		var failure = new FileNotFoundException("Registered directive failure");
		var directive = new ThrowingDirective(UniqueName(), failure);
		Profile.Directives.Add(directive);
		var events = new List<string>();
		var options = new ProfileOptions
		{
			Loading = _ => events.Add("loading"),
			Loaded = _ => events.Add("loaded"),
		};
		options.Directives.Processing = context =>
		{
			events.Add("processing");
			context.Handled = handled;
		};
		options.Directives.Processed = context =>
		{
			Assert.True(context.Handled);
			events.Add("processed");
		};
		using var input = new MemoryStream(Encoding.UTF8.GetBytes("#@" + directive.Name + " argument\nvalue=kept"));

		if(handled)
		{
			Assert.Equal("kept", Profile.Load(input, options).Entries["value"].Value);
			Assert.Equal(["loading", "processing", "processed", "loaded"], events);
		}
		else
		{
			Assert.Same(failure, Assert.Throws<FileNotFoundException>(() => Profile.Load(input, options)));
			Assert.Equal(["loading", "processing"], events);
		}

		Assert.False(input.CanRead);
	}

	[Fact]
	public async Task RegisteredDirective_ConcurrentLoadsShareInstanceWithIndependentContexts()
	{
		using var files = new ProfileImportTest.ProfileFiles();
		var directive = new CallbackDirective(UniqueName());
		Profile.Directives.Add(directive);
		var child = files.Write("shared.ini", "#@" + directive.Name + " value\nchild=present");
		var first = files.Write("first.ini", "owner=first\n#@import shared.ini");
		var second = files.Write("second.ini", "owner=second\n#@import shared.ini");
		using var barrier = new Barrier(2);
		var contexts = new ConcurrentBag<ProfileDirectiveContext>();
		var instances = new ConcurrentBag<ProfileDirectiveBase>();
		var options = new[] { "first", "second" }.Select(owner => new ProfileOptions
		{
			Directives =
			{
				new CallbackOptions(directive.Name, (instance, context) =>
				{
					instances.Add(instance);
					contexts.Add(context);
					Assert.Equal(2, context.Depth);
					Assert.Equal(child, context.FilePath);
					Assert.Equal(owner, context.Referer.Entries["owner"].Value);
					Assert.True(barrier.SignalAndWait(TimeSpan.FromSeconds(30)));
					context.Profile.Entries.Add("executed", owner);
				}),
			},
		}).ToArray();
		var roots = new[] { first, second };
		var tasks = roots.Select((root, index) => Task.Factory.StartNew(() => Profile.Load(root, options[index]),
			CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray();

		var profiles = await Task.WhenAll(tasks);

		Assert.Equal(["first", "second"], profiles.Select(profile => profile.Entries["executed"].Value));
		Assert.Equal(2, instances.Count);
		Assert.All(instances, instance => Assert.Same(directive, instance));
		Assert.Equal(2, contexts.Select(context => context.Profile).Distinct().Count());
		Assert.Equal(2, contexts.Select(context => context.Options).Distinct().Count());
		Assert.Equal(2, contexts.Select(context => context.Referer).Distinct().Count());
		Assert.All(contexts, context =>
		{
			Assert.Contains(context.Referer, profiles);
			Assert.Same(context.Profile, context.Referer.Entries["executed"].Profile);
			Assert.Equal("present", context.Profile.Entries["child"].Value);
			Assert.True(context.Handled);
		});
	}

	private static string UniqueName() => "test_" + Guid.NewGuid().ToString("N");

	public sealed class EchoDirective(string name) : ProfileDirectiveBase(name)
	{
		public override void Process(ProfileDirectiveContext context) => context.Profile.Entries.Add("executed", context.Argument);
	}

	public sealed class CallbackDirective(string name) : ProfileDirectiveBase(name)
	{
		public override void Process(ProfileDirectiveContext context)
		{
			if(context.Options is CallbackOptions options)
				options.Callback(this, context);
			else
				context.Profile.Entries.Add("executed", context.Argument);
		}
	}

	public sealed class ThrowingDirective(string name, Exception failure) : ProfileDirectiveBase(name)
	{
		public override void Process(ProfileDirectiveContext context) => throw failure;
	}

	private sealed class CallbackOptions(string name, Action<ProfileDirectiveBase, ProfileDirectiveContext> callback) : ProfileDirectiveOptions(name)
	{
		public Action<ProfileDirectiveBase, ProfileDirectiveContext> Callback { get; } = callback;
	}
}
