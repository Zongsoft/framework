using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Zongsoft.Configuration.Profiles;

using Xunit;

namespace Zongsoft.Configuration.Tests;

public class ProfileImportTest
{
	[Theory]
	[InlineData(64, true)]
	[InlineData(65, false)]
	public void Import_DefaultDepthHonorsBoundary(int length, bool succeeds)
	{
		using var files = new ProfileFiles();
		var root = files.Chain(length);
		var starting = new List<ProfileContext>();
		var completed = new List<ProfileContext>();
		var options = new ProfileOptions { Importing = starting.Add, Imported = completed.Add };

		if(succeeds)
		{
			Assert.Equal("complete", Profile.Load(root, options).Entries["result"].Value);
			Assert.Equal(Enumerable.Range(2, 63).Reverse(), completed.Select(context => context.Depth));
		}
		else
		{
			Assert.Throws<ProfileException>(() => Profile.Load(root, options));
			Assert.Empty(completed);
		}

		Assert.Equal(Enumerable.Range(2, 63), starting.Select(context => context.Depth));
		Assert.DoesNotContain(starting, context => context.FilePath == root || context.FilePath == files.PathFor("64.ini"));
	}

	[Theory]
	[InlineData(1, 1, true)]
	[InlineData(1, 2, false)]
	[InlineData(3, 3, true)]
	[InlineData(3, 4, false)]
	[InlineData(65, 65, true)]
	public void Import_CustomMaximumDepthCountsRootAndRejectsBeforeCallbacks(int maximumDepth, int length, bool succeeds)
	{
		using var files = new ProfileFiles();
		var root = files.Chain(length);
		var starting = new List<int>();
		var completed = new List<int>();
		var options = new ProfileOptions
		{
			MaximumDepth = maximumDepth,
			Importing = context => starting.Add(context.Depth),
			Imported = context => completed.Add(context.Depth),
		};
		var reader = CreateReader(options);

		if(succeeds)
		{
			Assert.Equal("complete", ReadProfile(reader, root).Entries["result"].Value);
			Assert.Equal(Enumerable.Range(2, length - 1), starting);
			Assert.Equal(Enumerable.Range(2, length - 1).Reverse(), completed);
		}
		else
		{
			Assert.Throws<ProfileException>(() => ReadProfile(reader, root));
			Assert.Equal(Enumerable.Range(2, maximumDepth - 1), starting);
			Assert.Empty(completed);
			using(var input = new FileStream(files.PathFor(maximumDepth + ".ini"), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
				Assert.True(input.Length > 0);

			files.Write((maximumDepth - 1) + ".ini", "result=recovered");
			starting.Clear();
			Assert.Equal("recovered", ReadProfile(reader, root).Entries["result"].Value);
			Assert.Equal(Enumerable.Range(2, maximumDepth - 1), starting);
			Assert.Equal(Enumerable.Range(2, maximumDepth - 1).Reverse(), completed);
		}
	}

	[Fact]
	public void Reader_MaximumDepthSnapshotIsFixedBeforeReading()
	{
		using var files = new ProfileFiles();
		var root = files.Chain(3);
		var starting = new List<int>();
		var options = new ProfileOptions
		{
			MaximumDepth = 3,
			Importing = context => starting.Add(context.Depth),
		};
		var reader = CreateReader(options);
		options.MaximumDepth = 1;

		Assert.Equal("complete", ReadProfile(reader, root).Entries["result"].Value);
		Assert.Equal([2, 3], starting);
		starting.Clear();

		Assert.Throws<ProfileException>(() => Profile.Load(root, options));
		Assert.Empty(starting);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void Import_MaximumDepthChangesInCallbacksApplyOnlyToNextRoot(bool increase)
	{
		using var files = new ProfileFiles();
		var root = files.Chain(3);
		var starting = new List<int>();
		var completed = new List<int>();
		var options = new ProfileOptions { MaximumDepth = increase ? 2 : 3 };
		options.Importing = context =>
		{
			starting.Add(context.Depth);
			options.MaximumDepth = increase ? 3 : 1;
		};
		options.Imported = context => completed.Add(context.Depth);

		if(increase)
		{
			Assert.Throws<ProfileException>(() => Profile.Load(root, options));
			Assert.Equal([2], starting);
			Assert.Empty(completed);
		}
		else
		{
			Assert.Equal("complete", Profile.Load(root, options).Entries["result"].Value);
			Assert.Equal([2, 3], starting);
			Assert.Equal([3, 2], completed);
		}

		starting.Clear();
		completed.Clear();

		if(increase)
		{
			Assert.Equal("complete", Profile.Load(root, options).Entries["result"].Value);
			Assert.Equal([2, 3], starting);
			Assert.Equal([3, 2], completed);
		}
		else
		{
			Assert.Throws<ProfileException>(() => Profile.Load(root, options));
			Assert.Empty(starting);
			Assert.Empty(completed);
		}
	}

	[Fact]
	public void Import_CallbackCanEnforceBusinessDepthAndAllowsRetry()
	{
		using var files = new ProfileFiles();
		var root = files.Chain(4);
		var depths = new List<int>();
		var completed = new List<ProfileContext>();
		var failure = new InvalidOperationException("Business depth exceeded.");
		var options = new ProfileOptions
		{
			Importing = context =>
			{
				depths.Add(context.Depth);

				if(context.Depth > 3)
					throw failure;
			},
			Imported = completed.Add,
		};
		var reader = CreateReader(options);

		Assert.Same(failure, Assert.Throws<InvalidOperationException>(() => ReadProfile(reader, root)));
		Assert.Equal([2, 3, 4], depths);
		Assert.Empty(completed);
		files.Write("2.ini", "result=retry");
		depths.Clear();

		Assert.Equal("retry", ReadProfile(reader, root).Entries["result"].Value);
		Assert.Equal([2, 3], depths);
		Assert.Equal([3, 2], completed.Select(context => context.Depth));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void Import_CycleRejectsBeforeNotificationAndCanRetry(bool indirect)
	{
		using var files = new ProfileFiles();
		var root = files.Write("root.ini", indirect ? "#@import child.ini" : "#@import root.ini");
		var child = files.Write("child.ini", "#@import root.ini");
		var notifications = new List<string>();
		var options = Options(importing: notifications.Add);

		Assert.Throws<ProfileException>(() => Profile.Load(root, options));

		Assert.Equal(indirect ? [child] : Array.Empty<string>(), notifications);
		files.Write(indirect ? "child.ini" : "root.ini", "result=recovered");
		Assert.Equal("recovered", Profile.Load(root, options).Entries["result"].Value);
	}

	[Fact]
	public void Import_DiamondAndRepeatedFilesReload()
	{
		using var files = new ProfileFiles();
		files.Write("left.ini", "#@import leaf.ini\nleft=present");
		files.Write("right.ini", "#@import leaf.ini\nright=present");
		files.Write("leaf.ini", "leaf=shared");
		var root = files.Write("root.ini", "#@import left.ini | right.ini\tleaf.ini");
		var events = new List<string>();
		var options = Options(
			importing: path => events.Add("before:" + Path.GetFileName(path)),
			imported: profile => events.Add("after:" + profile.FileName));

		var result = Profile.Load(root, options);

		Assert.Equal(["before:left.ini", "before:leaf.ini", "after:leaf.ini", "after:left.ini", "before:right.ini", "before:leaf.ini", "after:leaf.ini", "after:right.ini", "before:leaf.ini", "after:leaf.ini"], events);
		Assert.Equal(3, result.Entries.Count);
		Assert.Equal("shared", result.Entries["leaf"].Value);
		Assert.Equal("present", result.Entries["left"].Value);
		Assert.Equal("present", result.Entries["right"].Value);
	}

	[Fact]
	public void Import_NestedRelativePathsUseDeclaringFileDirectory()
	{
		using var files = new ProfileFiles();
		var leaf = files.Write("shared/leaf.ini", "value=leaf");
		var child = files.Write("nested/child.ini", "#@import ../shared/leaf.ini\nchild=present");
		var root = files.Write("root.ini", "#@import nested/child.ini");
		var events = new List<string>();
		var options = Options(
			importing: path => events.Add("before:" + path),
			imported: profile => events.Add("after:" + profile.FilePath));

		var profile = Profile.Load(root, options);

		Assert.Equal(["before:" + child, "before:" + leaf, "after:" + leaf, "after:" + child], events);
		Assert.Equal("leaf", profile.Entries["value"].Value);
		Assert.Equal(leaf, profile.Entries["value"].Profile.FilePath);
		Assert.Equal("present", profile.Entries["child"].Value);
	}

	[Fact]
	public void Import_CallbacksSurroundParsingAndPreserveMergedReferences()
	{
		using var files = new ProfileFiles();
		var child = files.Write("child.ini", "value=child\n[section]\ncomplete=yes");
		var root = files.Write("root.ini", "#@import sub/../child.ini");
		Directory.CreateDirectory(files.PathFor("sub"));
		var events = new List<string>();
		Profile completed = null;
		var options = Options(
			importing: path =>
			{
				Assert.Equal(child, path);
				Assert.Null(completed);
				events.Add("before");
			},
			imported: profile =>
			{
				Assert.Equal("child", profile.Entries["value"].Value);
				Assert.Equal("yes", profile.Sections["section"].Entries["complete"].Value);
				profile.Entries["value"].Value = "updated";
				completed = profile;
				events.Add("after");
			});

		var result = Profile.Load(root, options);

		Assert.Equal(["before", "after"], events);
		Assert.Same(completed, result.Entries["value"].Profile);
		Assert.Equal("updated", result.Entries["value"].Value);
	}

	[Fact]
	public void Import_ContextsDescribeNestedImportsAndRemainStable()
	{
		using var files = new ProfileFiles();
		var leaf = files.Write("leaf.ini", "leaf=value");
		var child = files.Write("child.ini", "#@import leaf.ini\nchild=value");
		var root = files.Write("root.ini", "#@import child.ini");
		var starting = new List<ProfileContext>();
		var completed = new List<ProfileContext>();
		var events = new List<string>();
		var options = new ProfileOptions
		{
			Importing = context =>
			{
				Assert.Null(context.Profile);
				Assert.Null(context.Referer.Entries["leaf"]);
				starting.Add(context);
				events.Add("before:" + Path.GetFileName(context.FilePath));
			},
			Imported = context =>
			{
				Assert.Equal(context.FilePath, context.Profile.FilePath);
				Assert.Same(context.Profile.Entries["leaf"], context.Referer.Entries["leaf"]);
				Assert.Equal("value", context.Referer.Entries["leaf"].Value);
				completed.Add(context);
				events.Add("after:" + Path.GetFileName(context.FilePath));
			},
		};

		var profile = Profile.Load(root, options);

		Assert.Equal(["before:child.ini", "before:leaf.ini", "after:leaf.ini", "after:child.ini"], events);
		Assert.Equal([child, leaf], starting.Select(context => context.FilePath));
		Assert.Equal([2, 3], starting.Select(context => context.Depth));
		Assert.Equal([3, 2], completed.Select(context => context.Depth));
		Assert.Same(profile, starting[0].Referer);
		Assert.Same(profile, completed[1].Referer);
		Assert.Same(completed[1].Profile, starting[1].Referer);
		Assert.Same(completed[1].Profile, completed[0].Referer);
		Assert.Same(completed[0].Profile, profile.Entries["leaf"].Profile);
		Assert.NotSame(starting[0], completed[1]);
		Assert.NotSame(starting[1], completed[0]);
		Assert.All(starting, context => Assert.Null(context.Profile));
		Assert.Equal("value", profile.Entries["child"].Value);
	}

	[Fact]
	public void Import_ImportingRunsBeforeAnyParsing()
	{
		using var files = new ProfileFiles();
		files.Write("invalid.ini", "value=first\nvalue=duplicate");
		var root = files.Write("root.ini", "#@import invalid.ini");
		var failure = new InvalidOperationException("Before parsing");
		var completed = new List<Profile>();
		var options = Options(importing: _ => throw failure, imported: completed.Add);

		Assert.Same(failure, Assert.Throws<InvalidOperationException>(() => Profile.Load(root, options)));
		Assert.Empty(completed);
	}

	[Fact]
	public void Import_OptionalMissingHasNoCallbacks()
	{
		using var files = new ProfileFiles();
		var root = files.Write("root.ini", "#@import absent.ini absent/child.ini\nvalue=root");
		var events = new List<string>();
		var result = Profile.Load(root, Options(
			importing: path => events.Add(path),
			imported: profile => events.Add(profile.FilePath)));

		Assert.Empty(events);
		Assert.Equal("root", result.Entries["value"].Value);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void Import_CallbackFailurePropagatesAndReleasesFiles(bool after)
	{
		using var files = new ProfileFiles();
		var child = files.Write("child.ini", "value=child");
		var root = files.Write("root.ini", "#@import child.ini");
		var failure = new FileNotFoundException("callback failure must not be treated as an optional missing import");
		var events = new List<string>();
		var shouldFail = true;
		var options = Options(
			importing: path =>
			{
				events.Add("before");
				if(shouldFail && !after)
					throw failure;
			},
			imported: profile =>
			{
				events.Add("after");
				if(shouldFail && after)
					throw failure;
			});

		Assert.Same(failure, Assert.Throws<FileNotFoundException>(() => Profile.Load(root, options)));
		Assert.Equal(after ? ["before", "after"] : ["before"], events);
		using(var exclusive = new FileStream(child, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
			Assert.True(exclusive.Length > 0);

		shouldFail = false;
		events.Clear();
		Assert.Equal("child", Profile.Load(root, options).Entries["value"].Value);
		Assert.Equal(["before", "after"], events);
	}

	[Fact]
	public void Import_OpenFailureDoesNotNotifyOrDisappearAsOptional()
	{
		using var files = new ProfileFiles();
		var child = files.Write("child.ini", "value=child");
		var root = files.Write("root.ini", "#@import child.ini");
		var events = new List<string>();
		var options = Options(importing: _ => events.Add("before"), imported: _ => events.Add("after"));

		using(var exclusive = new FileStream(child, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
		{
			Assert.Throws<IOException>(() => Profile.Load(root, options));
			Assert.Empty(events);
		}

		Assert.Equal("child", Profile.Load(root, options).Entries["value"].Value);
		Assert.Equal(["before", "after"], events);
	}
	[Fact]
	public void Import_ParseFailureSkipsImportedAndCanRetry()
	{
		using var files = new ProfileFiles();
		var child = files.Write("child.ini", "value=first\nvalue=duplicate");
		var root = files.Write("root.ini", "#@import child.ini");
		var events = new List<string>();
		var options = Options(importing: _ => events.Add("before"), imported: _ => events.Add("after"));

		Assert.Throws<ArgumentException>(() => Profile.Load(root, options));
		Assert.Equal(["before"], events);
		using(var exclusive = new FileStream(child, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
			Assert.True(exclusive.Length > 0);

		files.Write("child.ini", "value=recovered");
		events.Clear();
		Assert.Equal("recovered", Profile.Load(root, options).Entries["value"].Value);
		Assert.Equal(["before", "after"], events);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void Import_OptionsFlowIntoChildren(bool preserveBlanks)
	{
		using var files = new ProfileFiles();
		files.Write("child.ini", "\nvalue=child");
		var root = files.Write("root.ini", "\n#@import child.ini");
		Profile child = null;
		var options = new ProfileOptions(preserveBlanks) { Imported = context => child = context.Profile };

		var result = Profile.Load(root, options);

		Assert.Equal(preserveBlanks ? [0] : Array.Empty<int>(), result.Blanks);
		Assert.Equal(preserveBlanks ? [0] : Array.Empty<int>(), child.Blanks);
		Assert.Equal("child", result.Entries["value"].Value);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(2)]
	public void Import_DefaultFallbackAndExplicitOptions(int mode)
	{
		using var files = new ProfileFiles();
		var child = files.Write("child.ini", "imported=child");
		var root = files.Write("root.ini", "#@import child.ini\nlocal=root");

		var profile = mode switch
		{
			0 => Profile.Load(root),
			1 => Profile.Load(root, options: null),
			2 => Profile.Load(root, new ProfileOptions()),
			_ => throw new ArgumentOutOfRangeException(nameof(mode)),
		};

		Assert.Equal("root", profile.Entries["local"].Value);
		Assert.Equal(2, profile.Entries.Count);
		Assert.Equal("child", profile.Entries["imported"].Value);
		Assert.Equal(child, profile.Entries["imported"].Profile.FilePath);
	}

	[Fact]
	public void Import_OptionsAreFixedAtRootEntry()
	{
		using var files = new ProfileFiles();
		files.Write("leaf.ini", "\nleaf=value");
		files.Write("child.ini", "\n#@import leaf.ini\nvalue=child");
		var root = files.Write("root.ini", "#@import child.ini\n\n");
		var events = new List<string>();
		var options = new ProfileOptions();
		options.Importing = context =>
		{
			events.Add("before:" + Path.GetFileName(context.FilePath));
			options.PreserveBlanks = false;
			options.Importing = _ => throw new InvalidOperationException("Changed callback must wait for next load.");
			options.Imported = options.Importing;
		};
		options.Imported = context =>
		{
			Assert.Equal([0], context.Profile.Blanks);
			events.Add("after:" + Path.GetFileName(context.FilePath));
		};

		var result = Profile.Load(root, options);

		Assert.False(options.PreserveBlanks);
		Assert.Equal([1], result.Blanks);
		Assert.Equal("value", result.Entries["leaf"].Value);
		Assert.Equal(["before:child.ini", "before:leaf.ini", "after:leaf.ini", "after:child.ini"], events);
		Assert.Throws<InvalidOperationException>(() => Profile.Load(root, options));
	}

	[Fact]
	public async Task Import_SharedOptionsAreIsolated()
	{
		using var files = new ProfileFiles();
		files.Write("child.ini", "value=child");
		var root = files.Write("root.ini", "#@import child.ini");
		var notifications = new ConcurrentBag<Profile>();
		var options = Options(imported: notifications.Add);

		var results = await Task.WhenAll(Enumerable.Range(0, 12).Select(_ => Task.Run(() => Profile.Load(root, options))));

		Assert.Equal(12, notifications.Count);
		Assert.Equal(12, results.Distinct().Count());
		Assert.Equal(12, notifications.Distinct().Count());
		Assert.All(results, profile => Assert.Equal("child", profile.Entries["value"].Value));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void Import_LinkAliasDetectsCycle(bool directoryLink)
	{
		using var files = new ProfileFiles();
		var root = files.Write("root.ini", directoryLink ? "#@import alias/root.ini" : "#@import alias.ini");
		var alias = files.PathFor(directoryLink ? "alias" : "alias.ini");
		try
		{
			if(directoryLink)
				Directory.CreateSymbolicLink(alias, files.Root);
			else
				File.CreateSymbolicLink(alias, root);
		}
		catch(Exception exception) when(exception is UnauthorizedAccessException or PlatformNotSupportedException or IOException)
		{
			Assert.Skip("Symbolic links unavailable: " + exception.Message);
		}

		var notifications = new List<string>();
		try
		{
			var exception = Assert.Throws<ProfileException>(() => Profile.Load(root, Options(importing: notifications.Add)));
			Assert.Empty(notifications);
		}
		finally
		{
			if(directoryLink)
				Directory.Delete(alias);
			else
				File.Delete(alias);
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void Import_AnonymousStreamHandlesAbsoluteAndRejectsRelative(bool absolute)
	{
		using var files = new ProfileFiles();
		var child = files.Write("child.ini", "value=child");
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes("#@import " + (absolute ? child : "child.ini")));
		var notifications = new List<string>();
		var options = Options(importing: notifications.Add);

		if(absolute)
		{
			var profile = Profile.Load(stream, options);
			Assert.Equal(string.Empty, profile.FilePath);
			Assert.Equal("child", profile.Entries["value"].Value);
			Assert.Equal([child], notifications);
		}
		else
		{
			Assert.Throws<ProfileException>(() => Profile.Load(stream, options));
			Assert.Empty(notifications);
		}

		Assert.False(stream.CanRead);
	}

	[Fact]
	public void Import_FileStreamRootParticipatesInCycle()
	{
		using var files = new ProfileFiles();
		var root = files.Write("root.ini", "#@import root.ini");
		using var stream = File.OpenRead(root);

		Assert.Throws<ProfileException>(() => Profile.Load(stream));
		Assert.False(stream.CanRead);
	}

	[Fact]
	public void Import_MergePreservesSourceAndSave()
	{
		using var files = new ProfileFiles();
		var child = files.Write("child.ini", "name=child\nonly=imported\n[plugins nested]\nvalue=child");
		var root = files.Write("root.ini", "name=root\n#@import child.ini\n[plugins]\nlocal=root");

		var profile = Profile.Load(root);

		Assert.Equal("child", profile.Entries["name"].Value);
		Assert.Equal(child, profile.Entries["name"].Profile.FilePath);
		Assert.Equal(child, profile.Entries["only"].Profile.FilePath);
		Assert.Equal(child, profile.Sections.Find("plugins/nested").Entries["value"].Profile.FilePath);
		Assert.Equal("root", profile.Sections["plugins"].Entries["local"].Value);
		using var output = new StringWriter();
		profile.Save(output);
		Assert.Contains("#@import child.ini", output.ToString());
		Assert.Contains("name=root", output.ToString());
		Assert.DoesNotContain("name=child", output.ToString());
		Assert.DoesNotContain("only=imported", output.ToString().Replace(" ", ""));
		Assert.DoesNotContain("value=child", output.ToString().Replace(" ", ""));
	}

	[Fact]
	public void Import_EncodingAndStreamOwnership()
	{
		using var files = new ProfileFiles();
		files.Write("child.ini", "child=中文");
		var root = files.PathFor("root.ini");
		File.WriteAllText(root, "name=café\n#@import child.ini", Encoding.Latin1);
		using var stream = File.OpenRead(root);

		var profile = Profile.Load(stream, Encoding.Latin1);

		Assert.Equal("café", profile.Entries["name"].Value);
		Assert.Equal("中文", profile.Entries["child"].Value);
		Assert.False(stream.CanRead);
		Assert.Empty(profile.Blanks);
	}

	[Fact]
	public void Reader_SnapshotCopiesOptionsAndCallbackReferences()
	{
		using var files = new ProfileFiles();
		var child = files.Write("child.ini", "\nvalue=child");
		var notifications = new List<ProfileContext>();
		var options = new ProfileOptions { Importing = notifications.Add, Imported = notifications.Add };
		var reader = CreateReader(options);
		var snapshot = (ProfileOptions)reader.GetType().GetProperty("Options").GetValue(reader);
		options.PreserveBlanks = false;
		options.Importing = _ => throw new InvalidOperationException("Snapshot must retain original callback.");
		options.Imported = options.Importing;
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes("\n#@import " + child));

		var profile = ReadProfile(reader, stream, Encoding.UTF8);

		Assert.NotSame(options, snapshot);
		Assert.NotSame(options.Importing, snapshot.Importing);
		Assert.NotSame(options.Imported, snapshot.Imported);
		Assert.True(snapshot.PreserveBlanks);
		Assert.Equal([0], profile.Blanks);
		Assert.Equal("child", Assert.Single(profile.Entries).Value);
		Assert.Equal(2, notifications.Count);
		Assert.Null(notifications[0].Profile);
		Assert.Equal([0], notifications[1].Profile.Blanks);
		Assert.Same(profile, notifications[0].Referer);
		Assert.Same(profile, notifications[1].Referer);
		Assert.False(stream.CanRead);
		Assert.False(reader.GetType().IsVisible);
		Assert.True(reader.GetType().IsSealed);
	}

	[Fact]
	public void Reader_StreamEncodingAndRootNotifications()
	{
		var events = new List<string>();
		var reader = CreateReader(Options(
			importing: path => events.Add(path), imported: profile => events.Add(profile.FilePath)));
		using var stream = new MemoryStream(Encoding.Latin1.GetBytes("name=café\n\n"));

		var profile = ReadProfile(reader, stream, Encoding.Latin1);

		Assert.Equal("café", profile.Entries["name"].Value);
		Assert.Equal([1], profile.Blanks);
		Assert.Empty(events);
		Assert.False(stream.CanRead);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void Reader_ImportCallbackFailureReleasesInputAndAllowsRetry(bool after)
	{
		using var files = new ProfileFiles();
		var child = files.Write("child.ini", "value=child");
		var events = new List<string>();
		var failure = new InvalidOperationException("import callback failed");
		var shouldFail = true;
		var reader = CreateReader(Options(
			importing: _ =>
			{
				events.Add("before");
				if(shouldFail && !after)
					throw failure;
			},
			imported: _ =>
			{
				events.Add("after");
				if(shouldFail && after)
					throw failure;
			}));
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes("#@import " + child));

		Assert.Same(failure, Assert.Throws<InvalidOperationException>(() => ReadProfile(reader, stream, Encoding.UTF8)));
		Assert.Equal(after ? ["before", "after"] : ["before"], events);
		Assert.False(stream.CanRead);
		using(var exclusive = new FileStream(child, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
			Assert.True(exclusive.Length > 0);

		shouldFail = false;
		events.Clear();
		using var retry = new MemoryStream(Encoding.UTF8.GetBytes("#@import " + child));
		var profile = ReadProfile(reader, retry, Encoding.UTF8);
		Assert.Equal("child", profile.Entries["value"].Value);
		Assert.Equal(["before", "after"], events);
		Assert.False(retry.CanRead);
	}

	[Fact]
	public void Reader_MissingAndFailedFilesAllowRetry()
	{
		using var files = new ProfileFiles();
		var path = files.PathFor("source.ini");
		var reader = CreateReader(null);

		Assert.Throws<FileNotFoundException>(() => ReadProfile(reader, path));
		files.Write("source.ini", "value=first\nvalue=duplicate");
		Assert.Throws<ArgumentException>(() => ReadProfile(reader, path));

		files.Write("source.ini", "value=recovered");
		Assert.Equal("recovered", ReadProfile(reader, path).Entries["value"].Value);
	}

	[Fact]
	public void Reader_ImportedFailureRetainsActiveGuardThenCleansReader()
	{
		using var files = new ProfileFiles();
		var child = files.Write("child.ini", "value=complete");
		var root = files.Write("root.ini", "#@import child.ini");
		object reader = null;
		var failure = new FileNotFoundException("imported callback failure");
		var shouldFail = true;
		var count = 0;
		reader = CreateReader(Options(imported: profile =>
		{
			count++;
			Assert.Equal("complete", profile.Entries["value"].Value);
			Assert.Throws<ProfileException>(() => ReadProfile(reader, child));
			if(shouldFail)
				throw failure;
		}));

		Assert.Same(failure, Assert.Throws<FileNotFoundException>(() => ReadProfile(reader, root)));
		Assert.Equal(1, count);
		using(var exclusive = new FileStream(child, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
			Assert.True(exclusive.Length > 0);

		shouldFail = false;
		Assert.Equal("complete", ReadProfile(reader, root).Entries["value"].Value);
		Assert.Equal(2, count);
	}

	private static object CreateReader(ProfileOptions options)
	{
		var type = typeof(Profile).Assembly.GetType("Zongsoft.Configuration.Profiles.ProfileReader", throwOnError: true);
		return Activator.CreateInstance(type, [options]);
	}

	private static Profile ReadProfile(object reader, Stream stream, Encoding encoding)
	{
		var method = reader.GetType().GetMethod("Read", BindingFlags.Instance | BindingFlags.Public, null,
			[typeof(Stream), typeof(Encoding)], null);
		return method.CreateDelegate<Func<Stream, Encoding, Profile>>(reader)(stream, encoding);
	}

	private static Profile ReadProfile(object reader, string path)
	{
		var method = reader.GetType().GetMethod("Read", BindingFlags.Instance | BindingFlags.Public, null, [typeof(string)], null);
		return method.CreateDelegate<Func<string, Profile>>(reader)(path);
	}

	private static ProfileOptions Options(Action<string> importing = null, Action<Profile> imported = null) => new()
	{
		Importing = importing == null ? null : context => importing(context.FilePath),
		Imported = imported == null ? null : context => imported(context.Profile),
	};

	internal sealed class ProfileFiles : IDisposable
	{
		public ProfileFiles() => Directory.CreateDirectory(this.Root);
		public string Root { get; } = Path.Combine(Path.GetTempPath(), "zongsoft-profile-tests", Guid.NewGuid().ToString("N"));
		public string PathFor(string relative) => Path.Combine(this.Root, relative);
		public string Write(string relative, string content)
		{
			var path = Path.GetFullPath(this.PathFor(relative));
			Assert.StartsWith(this.Root + Path.DirectorySeparatorChar, path);
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			File.WriteAllText(path, content.Replace("\n", "\r\n"));
			return path;
		}
		public string Chain(int length)
		{
			for(var index = 0; index < length; index++)
				this.Write(index + ".ini", index == length - 1 ? "result=complete" : "#@import " + (index + 1) + ".ini");
			return this.PathFor("0.ini");
		}
		public void Dispose()
		{
			var parent = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "zongsoft-profile-tests")) + Path.DirectorySeparatorChar;
			Assert.StartsWith(parent, Path.GetFullPath(this.Root));
			Directory.Delete(this.Root, true);
		}
	}
}
