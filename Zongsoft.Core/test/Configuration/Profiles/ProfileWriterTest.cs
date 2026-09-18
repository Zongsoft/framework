using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Zongsoft.Configuration.Profiles;

using Xunit;

namespace Zongsoft.Configuration.Tests;

public class ProfileWriterTest
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void Save_ParentOrSourceWritesChangedDeclarationOnly(bool saveSource)
	{
		using var files = new ProfileImportTest.ProfileFiles();
		var child = files.Write("defaults.ini", "[network]\ntimeout=30");
		var root = files.Write("app.ini", "#@import defaults.ini");
		var original = File.ReadAllBytes(root);
		var timestamp = File.GetLastWriteTimeUtc(root);
		var profile = Profile.Load(root);
		var entry = profile.Sections["network"].Entries["timeout"];
		Assert.Equal(child, entry.Profile.FilePath);
		entry.Value = "60";

		(saveSource ? entry.Profile : profile).Save();

		Assert.Equal(original, File.ReadAllBytes(root));
		Assert.Equal(timestamp, File.GetLastWriteTimeUtc(root));
		Assert.Equal("60", Profile.Load(child).Sections["network"].Entries["timeout"].Value);
		Assert.Equal("60", Profile.Load(root).Sections["network"].Entries["timeout"].Value);
		AssertFiles(files, "app.ini", "defaults.ini");
	}

	[Fact]
	public void Save_ChildScopeExcludesParentAndSiblingWhileParentIncludesDescendants()
	{
		using var files = new ProfileImportTest.ProfileFiles();
		var leafPath = files.Write("leaf.ini", "leaf=original");
		var childPath = files.Write("child.ini", "#@import leaf.ini\nchild=original");
		var siblingPath = files.Write("sibling.ini", "sibling=original");
		var rootPath = files.Write("root.ini", "#@import child.ini sibling.ini\nroot=original");
		var profile = Profile.Load(rootPath);
		profile.Entries["leaf"].Value = "changed";
		profile.Entries["child"].Value = "changed";
		profile.Entries["sibling"].Value = "changed";
		profile.Entries["root"].Value = "changed";

		profile.Entries["child"].Profile.Save();

		Assert.Equal("changed", Profile.Load(leafPath).Entries["leaf"].Value);
		Assert.Equal("changed", Profile.Load(childPath).Entries["child"].Value);
		Assert.Equal("original", Profile.Load(siblingPath).Entries["sibling"].Value);
		Assert.Equal("original", Profile.Load(rootPath).Entries["root"].Value);

		profile.Save();

		Assert.Equal("changed", Profile.Load(siblingPath).Entries["sibling"].Value);
		Assert.Equal("changed", Profile.Load(rootPath).Entries["root"].Value);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(2)]
	public void Save_ExplicitOutputsExcludeImportsAndDoNotClearSourceChanges(int outputKind)
	{
		using var files = new ProfileImportTest.ProfileFiles();
		var child = files.Write("child.ini", "imported=original");
		var root = files.Write("root.ini", "#@import child.ini\nlocal=original");
		var rootBytes = File.ReadAllBytes(root);
		var childBytes = File.ReadAllBytes(child);
		var profile = Profile.Load(root);
		profile.Entries["imported"].Value = "changed";
		profile.Entries["local"].Value = "changed";
		string content;

		if(outputKind == 0)
		{
			var path = files.PathFor("copy.ini");
			profile.Save(path);
			content = File.ReadAllText(path);
		}
		else if(outputKind == 1)
		{
			using var stream = new MemoryStream();
			profile.Save(stream, new UTF8Encoding(false));
			content = Encoding.UTF8.GetString(stream.ToArray());
			Assert.False(stream.CanWrite);
		}
		else
		{
			using var writer = new StringWriter();
			profile.Save(writer);
			content = writer.ToString();
			writer.Write("still open");
			Assert.EndsWith("still open", writer.ToString());
		}

		Assert.Contains("local=changed", content);
		Assert.Contains("#@import child.ini", content);
		Assert.DoesNotContain("imported=", content);
		Assert.Equal(rootBytes, File.ReadAllBytes(root));
		Assert.Equal(childBytes, File.ReadAllBytes(child));
		Assert.Equal(root, profile.FilePath);

		profile.Save();

		Assert.Equal("changed", Profile.Load(root).Entries["local"].Value);
		Assert.Equal("changed", Profile.Load(child).Entries["imported"].Value);
	}

	[Fact]
	public void Save_NullExplicitPathWritesOnlyCurrentFile()
	{
		using var files = new ProfileImportTest.ProfileFiles();
		var child = files.Write("child.ini", "imported=original");
		var root = files.Write("root.ini", "#@import child.ini\nlocal=original");
		var profile = Profile.Load(root);
		profile.Entries["imported"].Value = "changed";
		profile.Entries["local"].Value = "changed";

		profile.Save((string)null);

		Assert.Equal("changed", Profile.Load(root).Entries["local"].Value);
		Assert.Equal("original", Profile.Load(child).Entries["imported"].Value);
		profile.Save();
		Assert.Equal("changed", Profile.Load(child).Entries["imported"].Value);
	}

	[Fact]
	public void Save_UnchangedAndRevertedProfilesDoNotWrite()
	{
		using var files = new ProfileImportTest.ProfileFiles();
		var path = files.Write("root.ini", "; original comment\n#@trace root\nvalue=original");
		var original = File.ReadAllBytes(path);
		var timestamp = File.GetLastWriteTimeUtc(path);
		var profile = Profile.Load(path);

		profile.Save();
		profile.Entries["value"].Value = "changed";
		profile.Entries["value"].Value = "original";
		profile.Save();

		Assert.Equal(original, File.ReadAllBytes(path));
		Assert.Equal(timestamp, File.GetLastWriteTimeUtc(path));

		profile.Entries["value"].Value = "saved";
		profile.Save();
		var savedTimestamp = new DateTime(2001, 2, 3, 4, 5, 6, DateTimeKind.Utc);
		File.SetLastWriteTimeUtc(path, savedTimestamp);
		profile.Save();
		Assert.Equal(savedTimestamp, File.GetLastWriteTimeUtc(path));
		Assert.Equal("saved", Profile.Load(path).Entries["value"].Value);

		profile.Entries["value"].Value = "again";
		profile.Save();
		Assert.Equal("again", Profile.Load(path).Entries["value"].Value);
	}

	[Fact]
	public void Save_ImportedCallbackAndMutableCommentLinesRemainDirty()
	{
		using var files = new ProfileImportTest.ProfileFiles();
		var child = files.Write("child.ini", "value=original\n# before");
		var root = files.Write("root.ini", "#@import child.ini");
		var originalRoot = File.ReadAllBytes(root);
		var options = Options(imported: imported =>
		{
			imported.Entries["value"].Value = "callback";
			imported.Comments[0].Lines[0] = " after";
		});
		var profile = Profile.Load(root, options);

		profile.Save();

		Assert.Equal("callback", Profile.Load(child).Entries["value"].Value);
		Assert.Contains("# after", File.ReadAllText(child));
		Assert.DoesNotContain("# before", File.ReadAllText(child));
		Assert.Equal(originalRoot, File.ReadAllBytes(root));
	}

	[Fact]
	public void Save_OverriddenChildStillParticipatesAndRootDeclarationIsPreserved()
	{
		using var files = new ProfileImportTest.ProfileFiles();
		var child = files.Write("child.ini", "value=child");
		var root = files.Write("root.ini", "value=root\n#@import child.ini");
		Profile imported = null;
		var profile = Profile.Load(root, Options(imported: item => imported = item));
		Assert.Same(imported, profile.Entries["value"].Profile);
		Assert.Equal("value=root\n#@import child.ini\n", Write(profile));

		var hiddenPath = files.Write("hidden.ini", "#@import child.ini\nvalue=local");
		var hidden = Profile.Load(hiddenPath, Options(imported: item => imported = item));
		Assert.Same(hidden, hidden.Entries["value"].Profile);
		imported.Entries["value"].Value = "updated child";
		hidden.Save();

		Assert.Equal("updated child", Profile.Load(child).Entries["value"].Value);
		Assert.Equal("local", Profile.Load(hiddenPath).Entries["value"].Value);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void Save_DuplicateImportsWriteChangedInstanceOnce(bool bothChanged)
	{
		using var files = new ProfileImportTest.ProfileFiles();
		var child = files.Write("child.ini", "#@trace child\nvalue=original");
		var root = files.Write("root.ini", "#@import child.ini child.ini");
		var imported = new List<Profile>();
		var options = Options(imported: imported.Add);
		var profile = Profile.Load(root, options);
		Assert.Equal(2, imported.Count);
		Assert.NotSame(imported[0], imported[1]);
		imported[0].Entries["value"].Value = "changed";
		if(bothChanged)
			imported[1].Entries["value"].Value = "changed";

		profile.Save(options);
		var timestamp = new DateTime(2001, 2, 3, 4, 5, 6, DateTimeKind.Utc);
		File.SetLastWriteTimeUtc(child, timestamp);
		profile.Save(options);

		Assert.Equal(timestamp, File.GetLastWriteTimeUtc(child));
		Assert.Equal("changed", Profile.Load(child).Entries["value"].Value);
		AssertFiles(files, "child.ini", "root.ini");
	}

	[Fact]
	public void Save_ConflictingDuplicateImportsRejectBeforeAnyOutputAndCanRetry()
	{
		using var files = new ProfileImportTest.ProfileFiles();
		var child = files.Write("child.ini", "#@trace child\nvalue=original");
		var root = files.Write("root.ini", "#@import child.ini child.ini\nlocal=original");
		var originalChild = File.ReadAllBytes(child);
		var originalRoot = File.ReadAllBytes(root);
		var imported = new List<Profile>();
		var options = Options(imported: imported.Add);
		var profile = Profile.Load(root, options);
		profile.Entries["local"].Value = "changed";
		imported[0].Entries["value"].Value = "first";
		imported[1].Entries["value"].Value = "second";

		Assert.Throws<ProfileException>(() => profile.Save(options));


		Assert.Equal(originalRoot, File.ReadAllBytes(root));
		Assert.Equal(originalChild, File.ReadAllBytes(child));
		AssertFiles(files, "child.ini", "root.ini");
		imported[1].Entries["value"].Value = "first";
		profile.Save(options);

		Assert.Equal("first", Profile.Load(child).Entries["value"].Value);
		Assert.Equal("changed", Profile.Load(root).Entries["local"].Value);
	}

	[Fact]
	public void Save_DeclarationOrderNullEmptySectionsAndRootScopeRoundTrip()
	{
		const string CONTENT = "first=\n# between\nflag\n[empty]\n[group]\none=1\n# inside\n[group nested]\nnested=2\n[group]\ntwo=3\n[]\nlast=4\n#\n";
		var profile = Read(CONTENT);

		var output = Write(profile);
		var reloaded = Read(output);

		Assert.Equal(CONTENT, output);
		Assert.Equal(string.Empty, reloaded.Entries["first"].Value);
		Assert.Null(reloaded.Entries["flag"].Value);
		Assert.Equal("4", reloaded.Entries["last"].Value);
		Assert.Empty(reloaded.Sections["empty"].Entries);
		Assert.Equal("1", reloaded.Sections["group"].Entries["one"].Value);
		Assert.Equal("3", reloaded.Sections["group"].Entries["two"].Value);
		Assert.Equal("2", reloaded.Sections.Find("group/nested").Entries["nested"].Value);
		Assert.Null(reloaded.Sections["group"].Entries["last"]);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void Save_PreserveBlanksControlsRecordedBlankLines(bool reserved)
	{
		var profile = Read("\nfirst=1\n\n[empty]\n\n# comment\n\n", new ProfileOptions(true));

		var output = Write(profile, new ProfileOptions(reserved));

		Assert.Equal(reserved ? "\nfirst=1\n\n[empty]\n\n# comment\n\n" : "first=1\n[empty]\n# comment\n", output);
		Assert.Equal("1", Read(output).Entries["first"].Value);
	}

	[Fact]
	public void Save_ExplicitLocalOverrideAppendsAfterImportAndPreservesSource()
	{
		using var files = new ProfileImportTest.ProfileFiles();
		var child = files.Write("child.ini", "[network]\ntimeout=30");
		var root = files.Write("root.ini", "#@import child.ini\n[other]\nvalue=keep");
		var childBytes = File.ReadAllBytes(child);
		var profile = Profile.Load(root);
		profile.Sections["network"].Entries.Add("timeout", "60");
		profile.Entries.Add("root", "entry");

		profile.Save();

		var reloaded = Profile.Load(root);
		Assert.Equal("60", reloaded.Sections["network"].Entries["timeout"].Value);
		Assert.Same(reloaded, reloaded.Sections["network"].Entries["timeout"].Profile);
		Assert.Equal("entry", reloaded.Entries["root"].Value);
		Assert.Equal("keep", reloaded.Sections["other"].Entries["value"].Value);
		Assert.Equal(childBytes, File.ReadAllBytes(child));
		Assert.Contains("[]", File.ReadAllText(root));
	}

	[Theory]
	[InlineData("name=first\nname=second")]
	[InlineData("[group]\nname=first\n[group]\nNAME=second")]
	public void Read_DuplicateLocalDeclarationsStillReject(string content)
	{
		Assert.Throws<ArgumentException>(() => Read(content));
	}

	[Fact]
	public void Save_EntryReplacementRemovalAndClearKeepDeclarationsAndKeysConsistent()
	{
		var profile = Read("first=1\nsecond=2\nthird=3\n[group]\nvalue=4\n");
		profile.Entries[0] = new ProfileEntry(profile, "renamed", "new");
		Assert.False(profile.Entries.Contains("first"));
		Assert.Equal("new", profile.Entries["RENAMED"].Value);
		Assert.Throws<ArgumentException>(() => profile.Entries[0] = new ProfileEntry(profile, "second", "duplicate"));
		Assert.Equal("new", profile.Entries["renamed"].Value);
		Assert.Equal("2", profile.Entries["second"].Value);
		Assert.True(profile.Entries.Remove("second", out var removed));
		Assert.Equal("2", removed.Value);
		profile.Sections["group"].Entries.Clear();

		var reloaded = Read(Write(profile));

		Assert.Equal(2, reloaded.Entries.Count);
		Assert.Equal("new", reloaded.Entries["renamed"].Value);
		Assert.Equal("3", reloaded.Entries["third"].Value);
		Assert.Null(reloaded.Entries["first"]);
		Assert.Null(reloaded.Entries["second"]);
		Assert.Empty(reloaded.Sections["group"].Entries);
	}

	[Fact]
	public void Save_SectionReplacementRemovalAndCommentEditsKeepDeclarationsConsistent()
	{
		var profile = Read("# old\n[first]\nvalue=1\n[first nested]\nvalue=2\n[second]\nvalue=3\n[third]\nvalue=4\n");
		var replacement = new ProfileSection(profile, "renamed");
		replacement.Entries.Add("value", "new");
		profile.Sections[0] = replacement;
		Assert.False(profile.Sections.Contains("first"));
		Assert.Same(replacement, profile.Sections["RENAMED"]);
		Assert.Throws<ArgumentException>(() => profile.Sections[0] = new ProfileSection(profile, "second"));
		Assert.Same(replacement, profile.Sections["renamed"]);
		Assert.True(profile.Sections.Remove("second", out var removed));
		Assert.Equal("3", removed.Entries["value"].Value);
		profile.Comments.Clear();
		profile.Comments.Add(" new");

		var output = Write(profile);
		var reloaded = Read(output);

		Assert.Equal(2, reloaded.Sections.Count);
		Assert.Equal("new", reloaded.Sections["renamed"].Entries["value"].Value);
		Assert.Equal("4", reloaded.Sections["third"].Entries["value"].Value);
		Assert.Null(reloaded.Sections["first"]);
		Assert.Null(reloaded.Sections["second"]);
		Assert.DoesNotContain("# old", output);
		Assert.Contains("# new", output);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void Save_RemovingImportedEntryThroughParentRejectsWithoutMutation(bool byName)
	{
		using var files = new ProfileImportTest.ProfileFiles();
		files.Write("child.ini", "value=child");
		var root = files.Write("root.ini", "#@import child.ini");
		var profile = Profile.Load(root);
		var entry = profile.Entries["value"];

		Assert.Throws<InvalidOperationException>(() =>
		{
			if(byName)
				profile.Entries.Remove("value", out _);
			else
				profile.Entries.Remove(entry);
		});

		Assert.Same(entry, Assert.Single(profile.Entries));
		Assert.Same(entry, profile.Entries["value"]);
		entry.Profile.Entries.Remove("value", out _);
		profile.Save();
		Assert.Empty(Profile.Load(root).Entries);
	}

	[Fact]
	public void Save_UnknownAnnotationsRemainOrdinaryCommentsInDeclarationOrder()
	{
		const string CONTENT = "#@trace first\n[group]\n#@trace second\n[group nested]\n#@trace third\n[]\n#@trace fourth\n#@unknown retained\n";

		var profile = Read(CONTENT);
		var output = Write(profile);

		Assert.Equal(CONTENT, output);
		Assert.All(profile.Comments, comment => Assert.IsType<ProfileComment>(comment));
		Assert.IsType<ProfileComment>(Assert.Single(profile.Sections["group"].Comments));
		Assert.IsType<ProfileComment>(Assert.Single(profile.Sections.Find("group/nested").Comments));
	}

	[Fact]
	public void Save_WriterSnapshotsOptionsBeforeOutput()
	{
		var options = new ProfileOptions(true);
		var profile = Read("# ordinary\n\nvalue=1\n", options);
		using var writer = new ObservingWriter(() => options.PreserveBlanks = false) { NewLine = "\n" };

		profile.Save(writer, options);

		Assert.Equal("# ordinary\n\nvalue=1\n", writer.ToString());
		Assert.False(options.PreserveBlanks);
		Assert.Equal("# ordinary\nvalue=1\n", Write(profile, options));
	}

	[Fact]
	public void Save_PreparationValidationFailurePreservesAllFilesAndAllowsRetry()
	{
		using var files = new ProfileImportTest.ProfileFiles();
		var child = files.Write("child.ini", "child=original");
		var root = files.Write("root.ini", "#@import child.ini\nroot=original");
		var originalChild = File.ReadAllBytes(child);
		var originalRoot = File.ReadAllBytes(root);
		var profile = Profile.Load(root);
		profile.Entries["child"].Value = "changed";
		profile.Entries["root"].Value = "invalid\nvalue";

		Assert.Throws<ProfileException>(() => profile.Save());

		Assert.Equal(originalRoot, File.ReadAllBytes(root));
		Assert.Equal(originalChild, File.ReadAllBytes(child));
		AssertFiles(files, "child.ini", "root.ini");
		profile.Entries["root"].Value = "changed";
		profile.Save();
		Assert.Equal("changed", Profile.Load(child).Entries["child"].Value);
		Assert.Equal("changed", Profile.Load(root).Entries["root"].Value);
	}

	[Fact]
	public void Save_PartialCommitFailureReportsCompletedFileAndRetriesOnlyPendingFile()
	{
		if(!OperatingSystem.IsWindows())
			Assert.Skip("This test uses Windows file sharing to reject replacement after preparation.");

		using var files = new ProfileImportTest.ProfileFiles();
		var child = files.Write("child.ini", "child=original");
		var root = files.Write("root.ini", "#@import child.ini\nroot=original");
		var originalRoot = File.ReadAllBytes(root);
		var profile = Profile.Load(root);
		profile.Entries["child"].Value = "changed";
		profile.Entries["root"].Value = "changed";

		using(var locked = new FileStream(root, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
		{
			var exception = Assert.Throws<ProfileException>(() => profile.Save());
			Assert.NotNull(exception.InnerException);
			Assert.Equal(root, exception.Data["FailedPath"]);
			Assert.Equal([child], Assert.IsType<string[]>(exception.Data["CompletedPaths"]));
		}

		Assert.Equal(originalRoot, File.ReadAllBytes(root));
		Assert.Equal("changed", Profile.Load(child).Entries["child"].Value);
		AssertFiles(files, "child.ini", "root.ini");
		var timestamp = new DateTime(2001, 2, 3, 4, 5, 6, DateTimeKind.Utc);
		File.SetLastWriteTimeUtc(child, timestamp);
		profile.Save();
		Assert.Equal(timestamp, File.GetLastWriteTimeUtc(child));
		Assert.Equal("changed", Profile.Load(root).Entries["root"].Value);
		AssertFiles(files, "child.ini", "root.ini");
	}

	[Theory]
	[InlineData("value", "first\nsecond")]
	[InlineData("value", "first\rsecond")]
	[InlineData("value=invalid", "text")]
	[InlineData("#comment", "text")]
	[InlineData("[section]", null)]
	public void Save_InvalidEntryRejectsBeforeTruncatingExistingTarget(string name, string value)
	{
		using var files = new ProfileImportTest.ProfileFiles();
		var target = files.Write("target.ini", "original=keep");
		var original = File.ReadAllBytes(target);
		var profile = new Profile();
		profile.Entries.Add(name, value);

		Assert.Throws<ProfileException>(() => profile.Save(target));

		Assert.Equal(original, File.ReadAllBytes(target));
		AssertFiles(files, "target.ini");
	}

	[Fact]
	public void Save_ReadOnlyTargetRejectsWithoutChangingContentAndRetries()
	{
		using var files = new ProfileImportTest.ProfileFiles();
		var target = files.Write("target.ini", "value=original");
		var original = File.ReadAllBytes(target);
		var profile = Profile.Load(target);
		profile.Entries["value"].Value = "changed";
		File.SetAttributes(target, File.GetAttributes(target) | FileAttributes.ReadOnly);

		try
		{
			Assert.Throws<UnauthorizedAccessException>(() => profile.Save());
			Assert.Equal(original, File.ReadAllBytes(target));
			AssertFiles(files, "target.ini");
		}
		finally
		{
			File.SetAttributes(target, File.GetAttributes(target) & ~FileAttributes.ReadOnly);
		}

		profile.Save();
		Assert.Equal("changed", Profile.Load(target).Entries["value"].Value);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void Save_LinkPathPreservesLinkAndWritesTarget(bool directoryLink)
	{
		using var files = new ProfileImportTest.ProfileFiles();
		var target = files.Write("actual/value.ini", "value=original");
		var alias = files.PathFor(directoryLink ? "alias" : "alias.ini");
		try
		{
			if(directoryLink)
				Directory.CreateSymbolicLink(alias, files.PathFor("actual"));
			else
				File.CreateSymbolicLink(alias, target);
		}
		catch(Exception exception) when(exception is UnauthorizedAccessException or PlatformNotSupportedException or IOException)
		{
			Assert.Skip("Symbolic links unavailable: " + exception.Message);
		}

		try
		{
			var profile = Profile.Load(directoryLink ? Path.Combine(alias, "value.ini") : alias);
			profile.Entries["value"].Value = "changed";
			profile.Save();

			Assert.Equal("changed", Profile.Load(target).Entries["value"].Value);
			Assert.NotNull(directoryLink ? new DirectoryInfo(alias).LinkTarget : new FileInfo(alias).LinkTarget);
			Assert.Single(Directory.GetFiles(files.PathFor("actual")));
		}
		finally
		{
			if(directoryLink)
				Directory.Delete(alias);
			else
				File.Delete(alias);
		}
	}

	[Fact]
	public void Save_NewProfileEncodingMissingDirectoryAndStreamFailureHonorOwnership()
	{
		using var files = new ProfileImportTest.ProfileFiles();
		var path = files.PathFor("new.ini");
		var profile = new Profile(path);
		profile.Entries.Add("name", "café");
		profile.Save();
		Assert.Equal("café", Profile.Load(path).Entries["name"].Value);

		var copy = files.PathFor("latin1.ini");
		profile.Save(copy, Encoding.Latin1);
		Assert.Contains((byte)0xE9, File.ReadAllBytes(copy));
		using(var input = File.OpenRead(copy))
			Assert.Equal("café", Profile.Load(input, Encoding.Latin1).Entries["name"].Value);

		Assert.Throws<DirectoryNotFoundException>(() => profile.Save(files.PathFor("missing/file.ini")));
		Assert.False(Directory.Exists(files.PathFor("missing")));
		using var stream = new MemoryStream();
		Assert.Throws<EncoderFallbackException>(() => profile.Save(stream, Encoding.GetEncoding("us-ascii", EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback)));
		Assert.False(stream.CanWrite);
		AssertFiles(files, "latin1.ini", "new.ini");
	}

	[Fact]
	public void Save_BlankArrayMutationChangesOutputAndInvalidPositionsReject()
	{
		using var files = new ProfileImportTest.ProfileFiles();
		var path = files.Write("root.ini", "first=1\n\nsecond=2\n");
		var options = new ProfileOptions(true);
		var profile = Profile.Load(path, options);
		Assert.Equal([1], profile.Blanks);
		profile.Blanks[0] = 0;

		profile.Save(options);

		Assert.Equal("\nfirst=1\nsecond=2\n", File.ReadAllText(path).Replace("\r\n", "\n"));
		var saved = File.ReadAllBytes(path);
		profile.Blanks[0] = -1;
		Assert.Throws<ProfileException>(() => profile.Save(options));
		Assert.Equal(saved, File.ReadAllBytes(path));
		profile.Blanks[0] = 0;
		profile.Save(options);
		AssertFiles(files, "root.ini");
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void Save_SettingMethodsModifyImportedSource(bool optionMethod)
	{
		using var files = new ProfileImportTest.ProfileFiles();
		var child = files.Write("child.ini", "[network]\ntimeout=30");
		var root = files.Write("root.ini", "#@import child.ini");
		var originalRoot = File.ReadAllBytes(root);
		var profile = Profile.Load(root);

		if(optionMethod)
			profile.SetOptionValue("network/timeout", "60");
		else
			profile.Sections["network"].SetEntryValue("timeout", "60");

		profile.Save();

		Assert.Equal(originalRoot, File.ReadAllBytes(root));
		Assert.Equal("60", Profile.Load(child).Sections["network"].Entries["timeout"].Value);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void Save_ClearMixedLocalAndImportedCollectionsRejectsAtomically(bool sections)
	{
		using var files = new ProfileImportTest.ProfileFiles();
		files.Write("child.ini", sections ? "[imported]\nvalue=child" : "imported=child");
		var root = files.Write("root.ini", sections ? "[local]\nvalue=root\n#@import child.ini" : "local=root\n#@import child.ini");
		var profile = Profile.Load(root);
		var originalOutput = Write(profile);

		Assert.Throws<InvalidOperationException>(() =>
		{
			if(sections)
				profile.Sections.Clear();
			else
				profile.Entries.Clear();
		});

		Assert.Equal(originalOutput, Write(profile));
		if(sections)
		{
			Assert.Equal(2, profile.Sections.Count);
			Assert.Equal("root", profile.Sections["local"].Entries["value"].Value);
			Assert.Equal("child", profile.Sections["imported"].Entries["value"].Value);
		}
		else
		{
			Assert.Equal(2, profile.Entries.Count);
			Assert.Equal("root", profile.Entries["local"].Value);
			Assert.Equal("child", profile.Entries["imported"].Value);
		}
	}

	[Fact]
	public void Save_ImportedFileWithoutEntriesStillSavesItsComments()
	{
		using var files = new ProfileImportTest.ProfileFiles();
		var child = files.Write("child.ini", "# original\n[empty]");
		var root = files.Write("root.ini", "#@import child.ini");
		Profile imported = null;
		var profile = Profile.Load(root, Options(imported: item => imported = item));
		var originalRoot = File.ReadAllBytes(root);
		imported.Comments[0].Lines[0] = " changed";

		profile.Save();

		Assert.Equal(originalRoot, File.ReadAllBytes(root));
		Assert.Equal("# changed\n[empty]\n", File.ReadAllText(child).Replace("\r\n", "\n"));
	}

	[Fact]
	public void Save_EncodingFailurePreservesExplicitTargetAndCleansTemporaryFiles()
	{
		using var files = new ProfileImportTest.ProfileFiles();
		var path = files.Write("root.ini", "name=original");
		var original = File.ReadAllBytes(path);
		var profile = Profile.Load(path);
		profile.Entries["name"].Value = "中文";
		var encoding = Encoding.GetEncoding("us-ascii", EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);

		Assert.Throws<EncoderFallbackException>(() => profile.Save(path, encoding));

		Assert.Equal(original, File.ReadAllBytes(path));
		AssertFiles(files, "root.ini");
		profile.Save();
		Assert.Equal("中文", Profile.Load(path).Entries["name"].Value);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void Save_OutputFailureKeepsCallerWriterOpenAndClosesCallerStream(bool streamOutput)
	{
		var failure = new IOException("custom output failure");
		var profile = Read("# ordinary\nvalue=keep\n");

		if(streamOutput)
		{
			using var stream = new FailingStream(failure);
			Assert.Same(failure, Assert.Throws<IOException>(() => profile.Save(stream)));
			Assert.False(stream.CanWrite);
		}
		else
		{
			using var writer = new ObservingWriter(() => throw failure);
			Assert.Same(failure, Assert.Throws<IOException>(() => profile.Save(writer)));
			writer.Write("still open");
			Assert.EndsWith("still open", writer.ToString());
		}

		Assert.Equal("# ordinary\nvalue=keep\n", Write(profile));
	}

	[Fact]
	public void Save_ModelMutationDuringOutputRejectsAndAllowsRetry()
	{
		var profile = Read("# ordinary\nvalue=original\n");
		using var writer = new ObservingWriter(() => profile.Entries["value"].Value = "changed");

		Assert.Throws<ProfileException>(() => profile.Save(writer));

		Assert.Equal("# ordinary\nvalue=changed\n", Write(profile));
	}

	[Fact]
	public void Save_LinkAndOriginalImportsResolveToOneOutput()
	{
		using var files = new ProfileImportTest.ProfileFiles();
		var child = files.Write("child.ini", "#@trace child\nvalue=original");
		var alias = files.PathFor("alias.ini");
		try
		{
			File.CreateSymbolicLink(alias, child);
		}
		catch(Exception exception) when(exception is UnauthorizedAccessException or PlatformNotSupportedException or IOException)
		{
			Assert.Skip("Symbolic links unavailable: " + exception.Message);
		}

		try
		{
			var root = files.Write("root.ini", "#@import child.ini alias.ini");
			var imported = new List<Profile>();
			var options = Options(imported: imported.Add);
			var profile = Profile.Load(root, options);
			Assert.Equal(2, imported.Count);
			foreach(var item in imported)
				item.Entries["value"].Value = "changed";

			profile.Save(options);
			var timestamp = new DateTime(2001, 2, 3, 4, 5, 6, DateTimeKind.Utc);
			File.SetLastWriteTimeUtc(child, timestamp);
			profile.Save(options);

			Assert.Equal(timestamp, File.GetLastWriteTimeUtc(child));
			Assert.Equal("changed", Profile.Load(child).Entries["value"].Value);
			Assert.NotNull(new FileInfo(alias).LinkTarget);
			AssertFiles(files, "alias.ini", "child.ini", "root.ini");
		}
		finally
		{
			File.Delete(alias);
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void Save_MutableCommentsPreserveTextAndImportChangesApplyAfterReload(bool import)
	{
		using var files = new ProfileImportTest.ProfileFiles();
		files.Write("child.ini", "imported=child");
		var path = files.Write("root.ini", "# ordinary\nvalue=keep");
		var profile = Profile.Load(path);
		profile.Comments[0].Lines[0] = import ? "@import child.ini" : "@unknown changed";

		profile.Save();

		Assert.Null(profile.Entries["imported"]);
		Assert.Equal(import ? "#@import child.ini\nvalue=keep\n" : "#@unknown changed\nvalue=keep\n", File.ReadAllText(path).Replace("\r\n", "\n"));
		var reloaded = Profile.Load(path);
		Assert.Equal(import ? "child" : null, reloaded.Entries["imported"]?.Value);
		Assert.Equal("keep", reloaded.Entries["value"].Value);
		AssertFiles(files, "child.ini", "root.ini");
	}

	[Fact]
	public void Save_ImportCallbacksDoNotExecuteOrChangeLoadedSaveScope()
	{
		using var files = new ProfileImportTest.ProfileFiles();
		var child = files.Write("child.ini", "imported=original");
		var root = files.Write("root.ini", "#@import child.ini\nlocal=original");
		var profile = Profile.Load(root);
		profile.Entries["imported"].Value = "changed";
		profile.Entries["local"].Value = "changed";
		var options = new ProfileOptions
		{
			Importing = _ => throw new InvalidOperationException("Saving must not read imports."),
			Imported = _ => throw new InvalidOperationException("Saving must not merge imports."),
		};

		profile.Save(options);

		Assert.Equal("changed", Profile.Load(child).Entries["imported"].Value);
		Assert.Equal("changed", Profile.Load(root).Entries["local"].Value);
		Assert.Equal("#@import child.ini\nlocal=changed\n", Write(profile, options));
		AssertFiles(files, "child.ini", "root.ini");
	}

	[Theory]
	[InlineData("first\r\nsecond\r\n", "#first\n#second\n#\n")]
	[InlineData("first\nsecond", "#first\n#second\n")]
	public void Save_MultilineCommentsNormalizeLineEndings(string text, string expected)
	{
		var profile = new Profile();
		profile.Comments.Add(text);

		var output = Write(profile);

		Assert.Equal(expected, output);
		Assert.Equal(expected.Split('\n').Length - 1, Read(output).Comments.Count);
	}

	[Fact]
	public void Save_DifferentImportsPreserveSequentialPrecedenceAndDeclarations()
	{
		using var files = new ProfileImportTest.ProfileFiles();
		var first = files.Write("first.ini", "value=first\n[first]\nvalue=first section");
		var second = files.Write("second.ini", "value=second\n[second]\nvalue=second section");
		var root = files.Write("root.ini", "value=local\n#@import first.ini second.ini");
		var imported = new List<Profile>();
		var profile = Profile.Load(root, Options(imported: imported.Add));
		Assert.Equal(second, profile.Entries["value"].Profile.FilePath);
		Assert.Equal("second", profile.Entries["value"].Value);
		Assert.Equal("first", imported[0].Entries["value"].Value);
		Assert.Equal("value=local\n#@import first.ini second.ini\n", Write(profile));
		imported[0].Entries["value"].Value = "changed first";

		profile.Save();

		Assert.Equal("changed first", Profile.Load(first).Entries["value"].Value);
		var reloaded = Profile.Load(root);
		Assert.Equal("second", reloaded.Entries["value"].Value);
		Assert.Equal("first section", reloaded.Sections["first"].Entries["value"].Value);
		Assert.Equal("second section", reloaded.Sections["second"].Entries["value"].Value);
		Assert.Equal("value=local\n#@import first.ini second.ini\n", Write(reloaded));
	}

	private static Profile Read(string content, ProfileOptions options = null)
	{
		using var input = new MemoryStream(Encoding.UTF8.GetBytes(content));
		return Profile.Load(input, options);
	}

	private static string Write(Profile profile, ProfileOptions options = null)
	{
		using var output = new StringWriter { NewLine = "\n" };
		profile.Save(output, options);
		return output.ToString();
	}

	private static ProfileOptions Options(Action<Profile> imported = null) =>
		new() { Imported = imported == null ? null : context => imported(context.Profile) };

	private static void AssertFiles(ProfileImportTest.ProfileFiles files, params string[] expected) =>
		Assert.Equal(expected.OrderBy(name => name), Directory.GetFiles(files.Root).Select(Path.GetFileName).OrderBy(name => name));

	private sealed class ObservingWriter(Action writing) : StringWriter
	{
		public override void WriteLine(string value)
		{
			writing();
			base.WriteLine(value);
		}
	}

	private sealed class FailingStream(IOException failure) : MemoryStream
	{
		public override void Write(byte[] buffer, int offset, int count) => throw failure;
		public override void Write(ReadOnlySpan<byte> buffer) => throw failure;
	}
}
