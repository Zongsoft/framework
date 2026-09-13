using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Zongsoft.Configuration.Profiles;
using Zongsoft.Configuration.Profiles.Directives;

using Xunit;

namespace Zongsoft.Configuration.Tests;

public class ProfileImportTest
{
	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public void Import_InvalidDepthRejects(int depth)
	{
		var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new ImportDirective(depth));
		Assert.Equal("maximumDepth", exception.ParamName);
	}

	[Theory]
	[InlineData(64, true)]
	[InlineData(65, false)]
	public void Import_DefaultDepthHonorsBoundary(int length, bool succeeds)
	{
		using var files = new ProfileFiles();
		var root = files.Chain(length);

		if(succeeds)
			Assert.Equal("complete", Profile.Load(root).Entries["result"].Value);
		else
		{
			var exception = Assert.Throws<ProfileException>(() => Profile.Load(root));
			Assert.Contains("63.ini", exception.Message);
			Assert.Contains("64.ini", exception.Message);
		}
	}

	[Fact]
	public void Import_CustomDepthCountsRootAndRejectsBeforeNotification()
	{
		using var files = new ProfileFiles();
		var root = files.Chain(4);
		var notifications = new List<string>();
		var options = Options(new ImportDirective(3, path => notifications.Add(Path.GetFileName(path))));

		var exception = Assert.Throws<ProfileException>(() => Profile.Load(root, options));

		Assert.Equal(["1.ini", "2.ini"], notifications);
		Assert.Contains("3.ini", exception.Message);
		files.Write("2.ini", "result=retry");
		Assert.Equal("retry", Profile.Load(root, options).Entries["result"].Value);
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
		var options = Options(new ImportDirective(importing: notifications.Add));

		var exception = Assert.Throws<ProfileException>(() => Profile.Load(root, options));

		Assert.Contains(root, exception.Message);
		Assert.Equal(indirect ? [child] : Array.Empty<string>(), notifications);
		files.Write(indirect ? "child.ini" : "root.ini", "result=recovered");
		Assert.Equal("recovered", Profile.Load(root, options).Entries["result"].Value);
	}

	[Fact]
	public void Import_CycleReportsOrderedChainAndDirectiveLine()
	{
		using var files = new ProfileFiles();
		var root = files.Write("root.ini", "#@import child.ini");
		var child = files.Write("child.ini", "# comment\n#@import root.ini");
		var culture = CultureInfo.CurrentUICulture;

		try
		{
			CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
			var exception = Assert.Throws<ProfileException>(() => Profile.Load(root));
			Assert.Contains(string.Join(" -> ", root, child, root), exception.Message);
			Assert.EndsWith("Referenced from " + child + ", line 2.", exception.Message);
		}
		finally
		{
			CultureInfo.CurrentUICulture = culture;
		}
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
		var options = Options(new ImportDirective(
			importing: path => events.Add("before:" + Path.GetFileName(path)),
			imported: profile => events.Add("after:" + profile.FileName)));

		var result = Profile.Load(root, options);

		Assert.Equal(["before:left.ini", "before:leaf.ini", "after:leaf.ini", "after:left.ini", "before:right.ini", "before:leaf.ini", "after:leaf.ini", "after:right.ini", "before:leaf.ini", "after:leaf.ini"], events);
		Assert.Equal(3, result.Entries.Count);
		Assert.Equal("shared", result.Entries["leaf"].Value);
		Assert.Equal("present", result.Entries["left"].Value);
		Assert.Equal("present", result.Entries["right"].Value);
	}

	[Fact]
	public void Import_CallbacksSurroundParsingAndMerge()
	{
		using var files = new ProfileFiles();
		var child = files.Write("child.ini", "#@observe child\nvalue=child");
		var root = files.Write("root.ini", "#@observe root\n#@import sub/../child.ini");
		Directory.CreateDirectory(files.PathFor("sub"));
		var events = new List<string>();
		Profile parent = null;
		var observer = new ObserverDirective((context, argument) =>
		{
			events.Add("parse:" + argument);
			if(argument == "root")
				parent = context.Profile;
		});
		var options = Options(new ImportDirective(
			importing: path =>
			{
				Assert.Equal(child, path);
				Assert.Null(parent.Entries["value"]);
				events.Add("before");
			},
			imported: profile =>
			{
				Assert.Equal("child", parent.Entries["value"].Value);
				Assert.Same(profile, parent.Entries["value"].Profile);
				events.Add("after");
			}), observer);

		var result = Profile.Load(root, options);

		Assert.Same(parent, result);
		Assert.Equal(["parse:root", "before", "parse:child", "after"], events);
	}

	[Fact]
	public void Import_OptionalMissingHasNoCallbacks()
	{
		using var files = new ProfileFiles();
		var root = files.Write("root.ini", "#@import absent.ini absent/child.ini\nvalue=root");
		var events = new List<string>();
		var result = Profile.Load(root, Options(new ImportDirective(
			importing: path => events.Add(path),
			imported: profile => events.Add(profile.FilePath))));

		Assert.Empty(events);
		Assert.Equal("root", result.Entries["value"].Value);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void Import_CallbackFailurePropagatesAndReleasesFiles(bool after)
	{
		using var files = new ProfileFiles();
		var child = files.Write("child.ini", "#@observe child\nvalue=child");
		var root = files.Write("root.ini", "#@import child.ini");
		var failure = new FileNotFoundException("callback failure must not be treated as an optional missing import");
		var events = new List<string>();
		var shouldFail = true;
		var options = Options(new ImportDirective(
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
			}), new ObserverDirective((context, argument) => events.Add("parse")));

		Assert.Same(failure, Assert.Throws<FileNotFoundException>(() => Profile.Load(root, options)));
		Assert.Equal(after ? ["before", "parse", "after"] : ["before"], events);
		using(var exclusive = new FileStream(child, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
			Assert.True(exclusive.Length > 0);

		shouldFail = false;
		events.Clear();
		Assert.Equal("child", Profile.Load(root, options).Entries["value"].Value);
		Assert.Equal(["before", "parse", "after"], events);
	}

	[Fact]
	public void Import_OpenFailureDoesNotNotifyOrDisappearAsOptional()
	{
		using var files = new ProfileFiles();
		var child = files.Write("child.ini", "value=child");
		var root = files.Write("root.ini", "#@import child.ini");
		var events = new List<string>();
		var options = Options(new ImportDirective(importing: _ => events.Add("before"), imported: _ => events.Add("after")));

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
		var options = Options(new ImportDirective(importing: _ => events.Add("before"), imported: _ => events.Add("after")));

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
	public void Import_OptionsAndCollectionFlowIntoChildren(bool reservedBlanks)
	{
		using var files = new ProfileFiles();
		files.Write("child.ini", "\n#@observe child\nvalue=child");
		var root = files.Write("root.ini", "\n#@import child.ini");
		var observed = new List<string>();
		Profile child = null;
		var options = new ProfileOptions(reservedBlanks, (IEnumerable<IProfileDirective>)[
			new ImportDirective(imported: profile => child = profile),
			new ObserverDirective((context, argument) => observed.Add(argument))]);

		var result = Profile.Load(root, options);

		Assert.Equal(["child"], observed);
		Assert.Equal(reservedBlanks ? [0] : Array.Empty<int>(), result.Blanks);
		Assert.Equal(reservedBlanks ? [0] : Array.Empty<int>(), child.Blanks);
		Assert.Equal("child", result.Entries["value"].Value);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(2)]
	[InlineData(3)]
	public void Import_DefaultFallbackAndExplicitDirectiveImport(int mode)
	{
		using var files = new ProfileFiles();
		var child = files.Write("child.ini", "imported=child");
		var root = files.Write("root.ini", "#@import child.ini\nlocal=root");

		var profile = mode switch
		{
			0 => Profile.Load(root),
			1 => Profile.Load(root, options: null),
			2 => Profile.Load(root, new ProfileOptions()),
			3 => Profile.Load(root, new ProfileOptions(ImportDirective.Default)),
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
		files.Write("child.ini", "\n#@observe child\nvalue=child");
		var root = files.Write("root.ini", "#@observe root\n#@import child.ini\n\n");
		ProfileOptions options = null;
		Profile child = null;
		var observations = new List<string>();
		options = Options(new ImportDirective(imported: profile => child = profile), new ObserverDirective((context, argument) =>
		{
			observations.Add(argument);
			var reader = typeof(ProfileReadingContext).GetProperty("Reader", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(context);
			var snapshot = (ProfileOptions)reader.GetType().GetProperty("Options", BindingFlags.Instance | BindingFlags.Public).GetValue(reader);
			Assert.NotSame(options, snapshot);
			Assert.Same(options.Directives, snapshot.Directives);
			Assert.True(snapshot.ReservedBlanks);
			options.ReservedBlanks = false;
		}));

		var result = Profile.Load(root, options);

		Assert.Equal(["root", "child"], observations);
		Assert.Equal([0], child.Blanks);
		Assert.Equal([2], result.Blanks);
	}

	[Fact]
	public async Task Import_SharedOptionsAreIsolated()
	{
		using var files = new ProfileFiles();
		files.Write("child.ini", "value=child");
		var root = files.Write("root.ini", "#@import child.ini");
		var notifications = new ConcurrentBag<Profile>();
		var options = Options(new ImportDirective(imported: notifications.Add));

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
			var exception = Assert.Throws<ProfileException>(() => Profile.Load(root, Options(new ImportDirective(importing: notifications.Add))));
			Assert.Contains(root, exception.Message);
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
		var options = Options(new ImportDirective(importing: notifications.Add));

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
		Assert.Same(profile, profile.Entries["name"].Profile);
		Assert.Equal(child, profile.Entries["only"].Profile.FilePath);
		Assert.Equal(child, profile.Sections.Find("plugins/nested").Entries["value"].Profile.FilePath);
		Assert.Equal("root", profile.Sections["plugins"].Entries["local"].Value);
		using var output = new StringWriter();
		profile.Save(output);
		Assert.Contains("#@import child.ini", output.ToString());
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
	public void Reader_RootLoadsOwnDistinctReadersAndImportsShareTheirReader()
	{
		using var files = new ProfileFiles();
		files.Write("child.ini", "#@observe child\nvalue=imported");
		var root = files.Write("root.ini", "#@observe root\n#@import child.ini");
		var observations = new List<(string Name, object Reader)>();
		var options = Options(ImportDirective.Default, new ObserverDirective((context, argument) =>
		{
			var reader = typeof(ProfileReadingContext).GetProperty("Reader", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(context);
			observations.Add((argument, reader));
		}));

		var first = Profile.Load(root, options);
		var second = Profile.Load(root, options);

		Assert.Equal(["root", "child", "root", "child"], observations.Select(item => item.Name));
		Assert.Same(observations[0].Reader, observations[1].Reader);
		Assert.Same(observations[2].Reader, observations[3].Reader);
		Assert.NotSame(observations[0].Reader, observations[2].Reader);
		Assert.NotSame(first, second);
		Assert.Equal("imported", first.Entries["value"].Value);
		Assert.Equal("imported", second.Entries["value"].Value);
		Assert.False(observations[0].Reader.GetType().IsVisible);
		Assert.True(observations[0].Reader.GetType().IsSealed);
		Assert.True(typeof(Profile).GetProperty(nameof(Profile.Blanks)).GetSetMethod(nonPublic: true).IsAssembly);
	}

	[Fact]
	public void Reader_StreamCallbacksObserveParsingAndCompletedProfile()
	{
		var events = new List<string>();
		var options = Options(new ObserverDirective((context, argument) => events.Add("parse:" + argument)));
		var reader = CreateReader(options);
		using var stream = new MemoryStream(Encoding.Latin1.GetBytes("#@observe child\nname=café\n\n"));
		Profile completed = null;

		var profile = ReadProfile(reader, stream, Encoding.Latin1,
			loading: path =>
			{
				Assert.Equal(string.Empty, path);
				Assert.Equal(0, stream.Position);
				events.Add("before");
			},
			loaded: result =>
			{
				Assert.Equal("café", result.Entries["name"].Value);
				Assert.Equal([2], result.Blanks);
				completed = result;
				events.Add("after");
			});

		Assert.Same(completed, profile);
		Assert.Equal(["before", "parse:child", "after"], events);
		Assert.False(stream.CanRead);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void Reader_StreamCallbackFailureReleasesInputAndAllowsRetry(bool after)
	{
		var events = new List<string>();
		var reader = CreateReader(Options(new ObserverDirective((context, argument) => events.Add("parse"))));
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes("#@observe child\nvalue=child"));
		var failure = new InvalidOperationException("stream callback failed");

		var exception = Assert.Throws<InvalidOperationException>(() => ReadProfile(reader, stream, Encoding.UTF8,
			loading: _ =>
			{
				events.Add("before");
				if(!after)
					throw failure;
			},
			loaded: _ =>
			{
				events.Add("after");
				throw failure;
			}));

		Assert.Same(failure, exception);
		Assert.Equal(after ? ["before", "parse", "after"] : ["before"], events);
		Assert.False(stream.CanRead);
		using var retry = new MemoryStream(Encoding.UTF8.GetBytes("value=recovered"));
		var completed = new List<Profile>();
		var profile = ReadProfile(reader, retry, Encoding.UTF8, loaded: completed.Add);
		Assert.Same(profile, Assert.Single(completed));
		Assert.Equal("recovered", profile.Entries["value"].Value);
		Assert.False(retry.CanRead);
	}

	[Fact]
	public void Reader_LoadedOnlySkipsMissingAndFailedFiles()
	{
		using var files = new ProfileFiles();
		var path = files.PathFor("source.ini");
		var reader = CreateReader(null);
		var completed = new List<Profile>();

		Assert.Null(ReadProfile(reader, path, optional: true, loaded: completed.Add));
		Assert.Empty(completed);
		files.Write("source.ini", "value=first\nvalue=duplicate");
		Assert.Throws<ArgumentException>(() => ReadProfile(reader, path, optional: true, loaded: completed.Add));
		Assert.Empty(completed);

		files.Write("source.ini", "value=recovered");
		var profile = ReadProfile(reader, path, optional: true, loaded: completed.Add);
		Assert.Same(profile, Assert.Single(completed));
		Assert.Equal("recovered", profile.Entries["value"].Value);
	}

	[Fact]
	public void Reader_LoadedFailureRetainsActiveGuardThenCleansReader()
	{
		using var files = new ProfileFiles();
		var path = files.Write("source.ini", "value=complete");
		var reader = CreateReader(null);
		var failure = new FileNotFoundException("loaded callback failure");
		var count = 0;

		var exception = Assert.Throws<FileNotFoundException>(() => ReadProfile(reader, path, optional: true,
			loaded: profile =>
			{
				count++;
				Assert.Equal("complete", profile.Entries["value"].Value);
				Assert.Throws<ProfileException>(() => ReadProfile(reader, path));
				throw failure;
			}));

		Assert.Same(failure, exception);
		Assert.Equal(1, count);
		using(var exclusive = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
			Assert.True(exclusive.Length > 0);
		var recovered = ReadProfile(reader, path, loaded: _ => count++, maximumDepth: 1);
		Assert.Equal("complete", recovered.Entries["value"].Value);
		Assert.Equal(2, count);
	}

	private static object CreateReader(ProfileOptions options)
	{
		var type = typeof(Profile).Assembly.GetType("Zongsoft.Configuration.Profiles.ProfileReader", throwOnError: true);
		return Activator.CreateInstance(type, [options]);
	}

	private static Profile ReadProfile(object reader, Stream stream, Encoding encoding, Action<string> loading = null, Action<Profile> loaded = null)
	{
		var method = reader.GetType().GetMethod("Read", BindingFlags.Instance | BindingFlags.Public, null,
			[typeof(Stream), typeof(Encoding), typeof(Action<string>), typeof(Action<Profile>)], null);
		var read = method.CreateDelegate<Func<Stream, Encoding, Action<string>, Action<Profile>, Profile>>(reader);
		return read(stream, encoding, loading, loaded);
	}

	private static Profile ReadProfile(object reader, string path, bool optional = false, Action<Profile> loaded = null, int maximumDepth = int.MaxValue)
	{
		var method = reader.GetType().GetMethod("Read", BindingFlags.Instance | BindingFlags.Public, null,
			[typeof(string), typeof(int), typeof(bool), typeof(Action<string>), typeof(Action<Profile>), typeof(ProfileReadingContext)], null);
		var read = method.CreateDelegate<Func<string, int, bool, Action<string>, Action<Profile>, ProfileReadingContext, Profile>>(reader);
		return read(path, maximumDepth, optional, null, loaded, null);
	}

	private static ProfileOptions Options(params IProfileDirective[] directives) => new((IEnumerable<IProfileDirective>)directives);

	private sealed class ObserverDirective(Action<ProfileReadingContext, string> observe) : IProfileDirective
	{
		public string Name => "observe";
		public void OnRead(ProfileReadingContext context, string argument) => observe(context, argument);
		public void OnWrite(ProfileWritingContext context, string argument) { }
	}

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