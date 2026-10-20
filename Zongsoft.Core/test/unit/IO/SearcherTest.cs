using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using System.Security.Principal;
using System.Security.AccessControl;

using Xunit;

namespace Zongsoft.IO.Tests;

public class SearcherTest
{
	[Theory]
	[InlineData("**/*.json")]
	[InlineData("**/**/*.json")]
	public void SearchFiles_GlobstarMatchesZeroAndManyLevels(string pattern)
	{
		using var files = new SearchFiles();
		files.Write("root.json");
		files.Write("a/child.json");
		files.Write("a/b/deep.json");
		files.Write("a/b/other.txt");

		var matches = files.Directory.Search(pattern, Searcher.Target.Files).ToArray();

		Assert.Equal(["a/b/deep.json", "a/child.json", "root.json"], files.Relative(matches));
		Assert.All(matches, match => Assert.Equal(files.Root, System.IO.Path.TrimEndingDirectorySeparator(match.Origin.FullName)));
	}

	[Fact]
	public void Search_DefaultTargetIncludesFilesAndDirectories()
	{
		using var files = new SearchFiles();
		files.Write("assets/file.txt");
		files.Write("assets/nested/child.txt");
		Directory.CreateDirectory(files.At("assets/empty"));

		var matches = files.Directory.Search("assets/**").ToArray();

		Assert.Equal(["assets", "assets/empty", "assets/file.txt", "assets/nested", "assets/nested/child.txt"], files.Relative(matches));
		Assert.Equal(2, matches.Count(match => match.IsFile(out _)));
		Assert.Equal(3, matches.Count(match => match.IsDirectory(out _)));
	}

	[Theory]
	[InlineData(Searcher.Target.Files, new[] { "assets/file.txt", "assets/nested/child.txt" }, 2, 0)]
	[InlineData(Searcher.Target.Directories, new[] { "assets", "assets/empty", "assets/nested" }, 0, 3)]
	[InlineData(Searcher.Target.Both, new[] { "assets", "assets/empty", "assets/file.txt", "assets/nested", "assets/nested/child.txt" }, 2, 3)]
	public void Search_TargetFiltersFilesAndDirectories(Searcher.Target target, string[] expected, int fileCount, int directoryCount)
	{
		using var files = new SearchFiles();
		files.Write("assets/file.txt");
		files.Write("assets/nested/child.txt");
		Directory.CreateDirectory(files.At("assets/empty"));

		var matches = files.Directory.Search("assets/**", target).ToArray();

		Assert.Equal(expected, files.Relative(matches));
		Assert.Equal(fileCount, matches.Count(match => match.IsFile(out _)));
		Assert.Equal(directoryCount, matches.Count(match => match.IsDirectory(out _)));
	}

	[Fact]
	public void Search_DefaultEnumTargetIncludesFilesAndDirectories()
	{
		using var files = new SearchFiles();
		files.Write("assets/item.txt");
		files.Write("assets/nested/child.txt");
		Directory.CreateDirectory(files.At("assets/empty"));

		var matches = files.Directory.Search("assets/**", default(Searcher.Target)).ToArray();

		Assert.Equal(0, (int)Searcher.Target.Both);
		Assert.Equal(["assets", "assets/empty", "assets/item.txt", "assets/nested", "assets/nested/child.txt"], files.Relative(matches));
		Assert.Equal(2, matches.Count(match => match.IsFile(out _)));
		Assert.Equal(3, matches.Count(match => match.IsDirectory(out _)));
	}

	[Fact]
	public void Search_DefaultTargetAcceptsNamedCancellation()
	{
		using var files = new SearchFiles();
		files.Write("assets/item.txt");
		using var cancellation = new CancellationTokenSource();
		var search = files.Directory.Search("assets/**", cancellation: cancellation.Token);

		Assert.Equal(["assets", "assets/item.txt"], files.Relative(search));

		cancellation.Cancel();
		var exception = Assert.ThrowsAny<OperationCanceledException>(() => search.ToArray());

		Assert.Equal(cancellation.Token, exception.CancellationToken);
	}

	[Fact]
	public void Search_TerminalGlobstarIncludesBaseAndEmptyDirectories()
	{
		using var files = new SearchFiles();
		files.Write("assets/file.txt");
		Directory.CreateDirectory(files.At("assets/empty"));

		Assert.Equal(["assets", "assets/empty", "assets/file.txt"], files.Relative(files.Directory.Search("assets/**")));
		Assert.Equal(["assets/empty", "assets/file.txt"], files.Relative(files.Directory.Search("assets/**/*")));
		Assert.Equal(["assets", "assets/empty"], files.Relative(files.Directory.Search("assets/**", Searcher.Target.Directories)));
		Assert.Equal(["assets/file.txt"], files.Relative(files.Directory.Search("assets/**", Searcher.Target.Files)));
	}

	[Fact]
	public void SearchFiles_CapturesPreservePatternSegments()
	{
		using var files = new SearchFiles();
		files.Write("plugins/orders/assets/config/site.json");
		files.Write("plugins/orders/assets/main.json");

		var matches = files.Directory.Search("plugins/*/assets/**/*.json", Searcher.Target.Files).ToArray();

		Assert.Equal(["plugins/orders/assets/config/site.json", "plugins/orders/assets/main.json"], files.Relative(matches));
		Assert.Equal(["orders", "config", "site.json"], matches[0].Captures);
		Assert.Equal(["orders", "", "main.json"], matches[1].Captures);
		Assert.All(matches, match => Assert.Equal(files.At("plugins"), System.IO.Path.TrimEndingDirectorySeparator(match.Origin.FullName)));
	}

	[Fact]
	public void SearchFiles_ConsecutiveGlobstarsChooseShortestFirstCapture()
	{
		using var files = new SearchFiles();
		files.Write("a/b/item.json");

		var match = Assert.Single(files.Directory.Search("**/**/*.json", Searcher.Target.Files));

		Assert.Equal(["", System.IO.Path.Combine("a", "b"), "item.json"], match.Captures);
		Assert.Equal(files.At("a/b/item.json"), match.Path);
	}

	[Theory]
	[InlineData("a?.txt", "a1.txt")]
	[InlineData("ab**cd.txt", "ab-middle-cd.txt")]
	[InlineData("[item].txt", "[item].txt")]
	public void SearchFiles_SingleSegmentWildcardsDoNotCrossDirectories(string pattern, string expected)
	{
		using var files = new SearchFiles();
		files.Write(expected);
		files.Write("nested/" + expected);
		files.Write("a12.txt");

		Assert.Equal([expected], files.Relative(files.Directory.Search(pattern, Searcher.Target.Files)));
	}

	[Fact]
	public void Search_ValidatesArgumentsBeforeEnumeration()
	{
		using var files = new SearchFiles();

		Assert.Throws<ArgumentNullException>(() => { Searcher.Search(null, "*", Searcher.Target.Files); });
		Assert.Throws<ArgumentNullException>(() => { files.Directory.Search(null, Searcher.Target.Files); });
		Assert.Throws<ArgumentException>(() => { files.Directory.Search("", Searcher.Target.Directories); });
		Assert.Throws<ArgumentException>(() => { files.Directory.Search(files.Root); });
		Assert.Throws<ArgumentException>(() => { files.Directory.Search("*/../item.txt", Searcher.Target.Files); });
	}

	[Fact]
	public void Search_FixedParentPrefixAndExactPaths()
	{
		using var files = new SearchFiles();
		files.Write("shared/item.txt");
		Directory.CreateDirectory(files.At("working"));
		var working = new System.IO.DirectoryInfo(files.At("working"));

		var match = Assert.Single(working.Search("../shared/./*.txt", Searcher.Target.Files));
		Assert.Equal(files.At("shared/item.txt"), match.Path);
		Assert.Equal(files.At("shared"), System.IO.Path.TrimEndingDirectorySeparator(match.Origin.FullName));
		Assert.Equal(["item.txt"], match.Captures);

		var exact = Assert.Single(files.Directory.Search("shared/item.txt", Searcher.Target.Files));
		Assert.Empty(exact.Captures);
		Assert.Equal(files.At("shared"), System.IO.Path.TrimEndingDirectorySeparator(exact.Origin.FullName));
		Assert.Equal(["shared"], files.Relative(files.Directory.Search("shared")));
		Assert.Empty(files.Directory.Search("shared", Searcher.Target.Files));
		Assert.Empty(files.Directory.Search("shared/item.txt", Searcher.Target.Directories));
	}

	[Fact]
	public void SearchFiles_UsesPlatformCaseAndOrdinalOrder()
	{
		using var files = new SearchFiles();
		files.Write("B.JSON");
		files.Write("a.json");
		files.Write("c.json");

		var expected = OperatingSystem.IsWindows() ? new[] { "B.JSON", "a.json", "c.json" } : ["a.json", "c.json"];

		Assert.Equal(expected, files.Relative(files.Directory.Search("*.json", Searcher.Target.Files)));
		Assert.Equal(["B.JSON", "a.json", "c.json"], files.Relative(files.Directory.Search("*", Searcher.Target.Files)));
	}

	[Fact]
	public void Match_TypeChecksDoNotReadDeletedObjects()
	{
		using var files = new SearchFiles();
		var path = files.Write("item.txt");
		var fileMatch = Assert.Single(files.Directory.Search("item.txt", Searcher.Target.Files));
		Directory.CreateDirectory(files.At("empty"));
		var directoryMatch = Assert.Single(files.Directory.Search("empty", Searcher.Target.Directories));

		File.Delete(path);
		Directory.Delete(files.At("empty"));

		Assert.True(fileMatch.IsFile(out var file));
		Assert.Same(fileMatch.Result, file);
		Assert.False(fileMatch.IsDirectory(out var notDirectory));
		Assert.Null(notDirectory);
		Assert.True(directoryMatch.IsDirectory(out var directory));
		Assert.Same(directoryMatch.Result, directory);
		Assert.False(directoryMatch.IsFile(out var notFile));
		Assert.Null(notFile);
	}

	[Fact]
	public void Match_DefaultReportsNeitherType()
	{
		var match = default(Searcher.Match);

		Assert.False(match.IsFile(out var file));
		Assert.Null(file);
		Assert.False(match.IsDirectory(out var directory));
		Assert.Null(directory);
	}

	[Fact]
	public void Match_CapturesCannotBeModified()
	{
		using var files = new SearchFiles();
		files.Write("item.txt");
		var match = Assert.Single(files.Directory.Search("*.txt", Searcher.Target.Files));
		var captures = Assert.IsAssignableFrom<IList<string>>(match.Captures);

		Assert.Throws<NotSupportedException>(() => captures[0] = "changed.txt");
		Assert.Equal(["item.txt"], match.Captures);
	}

	[Fact]
	public void SearchFiles_LinksMatchLogicalNamesAndReadPhysicalTarget()
	{
		using var files = new SearchFiles();
		var target = files.Write("storage/original.bin", "target-content");
		files.Link("config.json", target);
		files.Link("second.json", files.At("config.json"));

		var matches = files.Directory.Search("*.json", Searcher.Target.Files).ToArray();

		Assert.Equal(["config.json", "second.json"], files.Relative(matches));
		Assert.All(matches, match =>
		{
			Assert.Equal(target, match.Result.FullName);
			Assert.True(match.IsFile(out var file));
			Assert.Equal("target-content", File.ReadAllText(file.FullName));
			Assert.Equal([System.IO.Path.GetFileName(match.Path)], match.Captures);
		});
	}

	[Fact]
	public void Search_DirectoryLinkIsReturnedButNeverCrossed()
	{
		using var files = new SearchFiles();
		files.Write("target/data.json");
		files.Write("source/local.json");
		files.Link("source/alias", files.At("target"), true);
		var source = new System.IO.DirectoryInfo(files.At("source"));

		var match = Assert.Single(source.Search("alias", Searcher.Target.Directories));
		Assert.Equal(files.At("source/alias"), match.Path);
		Assert.Equal(files.At("target"), match.Result.FullName);
		Assert.Equal(["source/local.json"], files.Relative(source.Search("**/*.json", Searcher.Target.Files)));
		Assert.Empty(source.Search("alias/*.json", Searcher.Target.Files));
		Assert.Empty(source.Search("alias/data.json", Searcher.Target.Files));
		Assert.Empty(source.Search("a*/*.json", Searcher.Target.Files));
		Assert.Equal(["source/alias"], files.Relative(source.Search("*", Searcher.Target.Directories)));
		Assert.Contains(source.Search("**"), item => item.Path == files.At("source/alias"));
	}

	[Fact]
	public void Search_ExplicitLinkedReceiverResolvesAncestor()
	{
		using var files = new SearchFiles();
		var target = files.Write("target/nested/real.txt", "physical");
		files.Link("alias", files.At("target"), true);
		var directory = new System.IO.DirectoryInfo(files.At("alias"));

		var match = Assert.Single(directory.Search("nested/*.txt", Searcher.Target.Files));

		Assert.Equal(files.At("alias/nested/real.txt"), match.Path);
		Assert.Equal(target, match.Result.FullName);
		Assert.Equal(files.At("alias/nested"), System.IO.Path.TrimEndingDirectorySeparator(match.Origin.FullName));
		Assert.Equal(["real.txt"], match.Captures);
		Assert.Equal("physical", File.ReadAllText(match.Result.FullName));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void Search_SelectedBrokenOrCyclicLinksFail(bool cycle)
	{
		using var files = new SearchFiles();
		files.Link("selected.txt", files.At(cycle ? "other.txt" : "missing.txt"));

		if(cycle)
			files.Link("other.txt", files.At("selected.txt"));

		Assert.ThrowsAny<IOException>(() => files.Directory.Search("selected.txt", Searcher.Target.Files).ToArray());
	}

	[Fact]
	public void Search_UnselectedBrokenLinksAreIgnored()
	{
		using var files = new SearchFiles();
		files.Write("ok.json");
		files.Link("broken.txt", files.At("absent.txt"));
		files.Link("nested", files.At("absent-directory"), true);

		Assert.Equal(["ok.json"], files.Relative(files.Directory.Search("**/*.json", Searcher.Target.Files)));
	}

	[Fact]
	public void Search_MissingOrdinaryPathsAreEmpty()
	{
		using var files = new SearchFiles();

		Assert.Empty(files.Directory.Search("absent.txt", Searcher.Target.Files));
		Assert.Empty(files.Directory.Search("absent", Searcher.Target.Directories));
		Assert.Empty(files.Directory.Search("absent/**/*"));
		Assert.Empty(new System.IO.DirectoryInfo(files.At("missing")).Search("*", Searcher.Target.Files));
	}

	[Fact]
	public void Search_EnumerationIsFreshAndCancelledEnumerationCanRetry()
	{
		using var files = new SearchFiles();
		var search = files.Directory.Search("*.txt", Searcher.Target.Files);
		files.Write("first.txt");
		Assert.Equal(["first.txt"], files.Relative(search));

		files.Write("second.txt");
		Assert.Equal(["first.txt", "second.txt"], files.Relative(search));

		using var cancellation = new CancellationTokenSource();
		var cancelled = files.Directory.Search("**/*", Searcher.Target.Files, cancellation.Token);
		cancellation.Cancel();
		Assert.ThrowsAny<OperationCanceledException>(() => cancelled.ToArray());
		Assert.Equal(["first.txt", "second.txt"], files.Relative(search));
	}

	[Fact]
	public void Search_CancellationDuringResultEnumerationStops()
	{
		using var files = new SearchFiles();
		files.Write("first.txt");
		files.Write("second.txt");
		using var cancellation = new CancellationTokenSource();
		using var iterator = files.Directory.Search("*", Searcher.Target.Files, cancellation.Token).GetEnumerator();

		Assert.True(iterator.MoveNext());
		Assert.Equal("first.txt", System.IO.Path.GetFileName(iterator.Current.Path));
		cancellation.Cancel();
		Assert.ThrowsAny<OperationCanceledException>(() => iterator.MoveNext());
	}

	[Fact]
	public void Search_LinkedReceiverGlobstarIncludesRootAndNestedFiles()
	{
		using var files = new SearchFiles();
		var rootFile = files.Write("target/root.txt", "root");
		var childFile = files.Write("target/sub/child.txt", "child");
		files.Link("target/skip", files.At("target/sub"), true);
		files.Link("alias", files.At("target"), true);
		var directory = new System.IO.DirectoryInfo(files.At("alias"));

		var matches = directory.Search("**/*", Searcher.Target.Files).ToArray();

		Assert.Equal(["alias/root.txt", "alias/sub/child.txt"], files.Relative(matches));
		Assert.Equal([rootFile, childFile], matches.Select(match => match.Result.FullName));
		Assert.Equal(["", "root.txt"], matches[0].Captures);
		Assert.Equal(["sub", "child.txt"], matches[1].Captures);
	}

	[Fact]
	public void Search_FilesRemovedBeforeEnumerationDoNotReturnStaleResults()
	{
		using var files = new SearchFiles();
		var path = files.Write("gone/item.txt");
		var search = files.Directory.Search("gone/**/*.txt", Searcher.Target.Files);

		File.Delete(path);
		Directory.Delete(files.At("gone"));

		Assert.Empty(search);
		files.Write("gone/replaced.txt");
		Assert.Equal(["gone/replaced.txt"], files.Relative(search));
	}

	[Fact]
	public void Search_EarlyDisposalReleasesDirectoriesAndAllowsRepeat()
	{
		using var files = new SearchFiles();
		files.Write("nested/one.txt");
		files.Write("nested/two.txt");
		var search = files.Directory.Search("**/*.txt", Searcher.Target.Files);

		using(var iterator = search.GetEnumerator())
		{
			Assert.True(iterator.MoveNext());
			Assert.Equal(files.At("nested/one.txt"), iterator.Current.Path);
		}

		Directory.Move(files.At("nested"), files.At("moved"));
		Assert.Equal(["moved/one.txt", "moved/two.txt"], files.Relative(search));
	}

	[Fact]
	public void Search_LinkTargetAncestorsAreFullyResolved()
	{
		using var files = new SearchFiles();
		var target = files.Write("physical/real.bin", "physical-content");
		files.Link("target-alias", files.At("physical"), true);
		files.Link("logical.json", files.At("target-alias/real.bin"));

		var match = Assert.Single(files.Directory.Search("logical.json", Searcher.Target.Files));

		Assert.Equal(files.At("logical.json"), match.Path);
		Assert.Equal(target, match.Result.FullName);
		Assert.Equal("physical-content", File.ReadAllText(match.Result.FullName));
	}

	[Fact]
	public void Search_WindowsDirectoryAccessDeniedIsNotTreatedAsMissing()
	{
		if(!OperatingSystem.IsWindows())
		{
			Assert.Skip("This test verifies Windows ACL semantics.");
			return;
		}

		using var files = new SearchFiles();
		files.Write("protected/item.txt");
		var directory = new System.IO.DirectoryInfo(files.At("protected"));
		var original = directory.GetAccessControl(AccessControlSections.Access);
		var denied = directory.GetAccessControl(AccessControlSections.Access);
		using var identity = WindowsIdentity.GetCurrent();
		denied.AddAccessRule(new FileSystemAccessRule(identity.User, FileSystemRights.ListDirectory, AccessControlType.Deny));

		try
		{
			directory.SetAccessControl(denied);

			Assert.Throws<UnauthorizedAccessException>(() => directory.Search("*", Searcher.Target.Files).ToArray());
		}
		finally
		{
			var restored = new DirectorySecurity();
			restored.SetSecurityDescriptorBinaryForm(original.GetSecurityDescriptorBinaryForm(), AccessControlSections.Access);
			directory.SetAccessControl(restored);
		}

		Assert.Equal(["protected/item.txt"], files.Relative(directory.Search("*", Searcher.Target.Files)));
	}

	private sealed class SearchFiles : IDisposable
	{
		private readonly List<string> _links = [];

		public SearchFiles() => System.IO.Directory.CreateDirectory(this.Root);
		public string Root { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "zongsoft-search-tests", Guid.NewGuid().ToString("N"));
		public System.IO.DirectoryInfo Directory => new(this.Root);
		public string At(string relative) => System.IO.Path.GetFullPath(relative, this.Root);
		public string Write(string relative, string text = "content")
		{
			var path = this.At(relative);
			System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
			File.WriteAllText(path, text);
			return path;
		}

		public string[] Relative(IEnumerable<Searcher.Match> matches) => matches.Select(match => System.IO.Path.GetRelativePath(this.Root, match.Path).Replace(System.IO.Path.DirectorySeparatorChar, '/')).ToArray();
		public void Link(string relative, string target, bool directory = false)
		{
			var path = this.At(relative);
			System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));

			try
			{
				if(directory)
					System.IO.Directory.CreateSymbolicLink(path, target);
				else
					File.CreateSymbolicLink(path, target);

				_links.Add(path);
			}
			catch(Exception exception) when(exception is UnauthorizedAccessException or PlatformNotSupportedException)
			{
				Assert.Skip("Symbolic links unavailable: " + exception.Message);
			}
		}

		public void Dispose()
		{
			var parent = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "zongsoft-search-tests")) + System.IO.Path.DirectorySeparatorChar;
			Assert.StartsWith(parent, this.Root);

			foreach(var link in _links)
			{
				if((File.GetAttributes(link) & FileAttributes.Directory) != 0)
					System.IO.Directory.Delete(link);
				else
					File.Delete(link);
			}

			System.IO.Directory.Delete(this.Root, true);
		}
	}
}