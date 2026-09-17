using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Services.Tests;

public class ApplicationVersionTest : IDisposable
{
	private static readonly string _root = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Zongsoft.ApplicationVersion.Tests"));
	private readonly string _directory = System.IO.Path.Combine(_root, Guid.NewGuid().ToString("N"));

	public ApplicationVersionTest() => Directory.CreateDirectory(_directory);
	public void Dispose()
	{
		var directory = System.IO.Path.GetFullPath(_directory);
		if(!string.Equals(System.IO.Path.GetDirectoryName(directory), _root, StringComparison.OrdinalIgnoreCase))
			throw new InvalidOperationException("The test directory must remain an immediate child of its temporary root.");

		Directory.Delete(directory, true);
	}

	[Fact]
	public void Constructor_NormalizesNames()
	{
		var application = new ApplicationVersion(" \tMyApplication \t");
		var edition = new ApplicationVersion.Edition(" \tCommunity \t", new Version(1, 2));

		Assert.Equal("MyApplication", application.Name);
		Assert.Null(application.Version);
		Assert.Empty(application.Editions);
		Assert.Equal("Community", edition.Name);
		Assert.Equal(new Version(1, 2), edition.Version);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData(" \t")]
	[InlineData("App\rName")]
	[InlineData("App\nName")]
	[InlineData("App[Name")]
	[InlineData("App]Name")]
	[InlineData("App@1.0")]
	[InlineData("App\0Name")]
	[InlineData("App\uFEFFName")]
	[InlineData(";App")]
	[InlineData("#App")]
	public void Constructor_RejectsInvalidNames(string name)
	{
		Assert.ThrowsAny<ArgumentException>(() => new ApplicationVersion(name, new Version(1, 0)));
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData(" \t")]
	[InlineData("Commu\rnity")]
	[InlineData("Commu\nnity")]
	[InlineData("[Community")]
	[InlineData("Community]")]
	[InlineData("Commu\0nity")]
	[InlineData("Commu\uFEFFnity")]
	public void Edition_RejectsInvalidNames(string name)
	{
		Assert.ThrowsAny<ArgumentException>(() => new ApplicationVersion.Edition(name, new Version(1, 0)));
	}

	[Fact]
	public void Edition_RejectsNullVersion()
	{
		Assert.Throws<ArgumentNullException>(() => new ApplicationVersion.Edition("Community", null));
	}

	[Theory]
	[InlineData("1.0")]
	[InlineData("1.2.3")]
	[InlineData("1.2.3.4")]
	[InlineData("0.0.0.0")]
	[InlineData("2147483647.2147483647.2147483647.2147483647")]
	public void Load_SingleVersion(string version)
	{
		var application = ApplicationVersion.Load(this.Write(" MyApplication @ " + version + " \t"));

		Assert.Equal("MyApplication", application.Name);
		Assert.Equal(Version.Parse(version), application.Version);
		Assert.Empty(application.Editions);
	}

	[Fact]
	public void Load_Editions()
	{
		var application = ApplicationVersion.Load(this.Write("MyApplication\r\n\r\n[Community]\r\n1.0.1\r\n\r\n[Professional]\r\n1.1.0\r\n\r\n[Enterprise]\r\n1.1.2"));

		Assert.Equal("MyApplication", application.Name);
		Assert.Null(application.Version);
		Assert.Equal(new[] { "Community", "Professional", "Enterprise" }, application.Editions.Select(edition => edition.Name));
		Assert.Equal(new Version(1, 0, 1), application.Editions[0].Version);
		Assert.Equal(new Version(1, 1, 0), application.Editions["professional"].Version);
		Assert.Equal(new Version(1, 1, 2), application.Editions[2].Version);
	}

	[Theory]
	[InlineData("\r\n")]
	[InlineData("\n")]
	[InlineData("\r")]
	[InlineData("\r\n\n\r")]
	public void Load_AcceptsCommentsWhitespaceBomAndLineEndings(string newline)
	{
		var content = string.Join(newline, new[] { "\uFEFF ; header", "\t# ignored", " \tMyApplication \t", "", " [ Community ] \t", "; version", " \t1.2.3\t ", " # between", "[Professional]", "2.3.4", "", "; end" });
		var application = ApplicationVersion.Load(this.Write(content));

		Assert.Equal("MyApplication", application.Name);
		Assert.Null(application.Version);
		Assert.Equal(new[] { "Community", "Professional" }, application.Editions.Select(edition => edition.Name));
		Assert.Equal(new Version(1, 2, 3), application.Editions["COMMUNITY"].Version);
		Assert.Equal(new Version(2, 3, 4), application.Editions[1].Version);

		application.Save(_directory);
		Assert.Equal("MyApplication\r\n\r\n[Community]\r\n1.2.3\r\n\r\n[Professional]\r\n2.3.4\r\n", File.ReadAllText(this.GetPath()));
	}

	[Theory]
	[InlineData("", 1)]
	[InlineData("\uFEFF", 1)]
	[InlineData(" \r\n\t", 2)]
	[InlineData("; comment\r\n# ignored", 2)]
	[InlineData("MyApplication", 1)]
	[InlineData("MyApplication@", 1)]
	[InlineData("@1.0", 1)]
	[InlineData("App[Name@1.0", 1)]
	[InlineData("App]Name@1.0", 1)]
	[InlineData("App@1.0@2.0", 1)]
	[InlineData("App@1", 1)]
	[InlineData("App@1.2.3.4.5", 1)]
	[InlineData("App@-1.0", 1)]
	[InlineData("App@2147483648.0", 1)]
	[InlineData("App@1.0-beta", 1)]
	[InlineData("App@1.0 ; comment", 1)]
	[InlineData("[Community]\n1.0", 1)]
	[InlineData("App\n1.0", 2)]
	[InlineData("App\n[Community]", 2)]
	[InlineData("App\n[]\n1.0", 2)]
	[InlineData("App\n[ ]\n1.0", 2)]
	[InlineData("App\n[Community\n1.0", 2)]
	[InlineData("App\n[Community] extra\n1.0", 2)]
	[InlineData("App\n[[Community]]\n1.0", 2)]
	[InlineData("App\n[Community]\n[Professional]\n1.0", 2)]
	[InlineData("App\n[Community]\ninvalid", 3)]
	[InlineData("App\n[Community]\nVersion=1.0", 3)]
	[InlineData("App\n[Community]\n1.0 # comment", 3)]
	[InlineData("App\n[Community]\n1.0\n2.0", 4)]
	[InlineData("App\n[Community]\n1.0\n[community]\n2.0", 4)]
	[InlineData("App@1.0\n[Community]\n1.0", 2)]
	[InlineData("App@1.0\n2.0", 2)]
	[InlineData("App\n\n# comment\n[Community]\n\tbad", 5)]
	public void Load_InvalidContentReportsLineNumber(string content, int line)
	{
		var exception = Assert.Throws<FormatException>(() => ApplicationVersion.Load(this.Write(content)));

		Assert.Matches($@"(?<!\d){line}(?!\d)", exception.Message);
	}

	[Fact]
	public void VersionAndEditions_RejectConflictsWithoutChangingState()
	{
		var application = new ApplicationVersion("App", new Version(1, 0));
		var edition = new ApplicationVersion.Edition("Community", new Version(2, 0));

		Assert.Throws<InvalidOperationException>(() => application.Editions.Add(edition));
		Assert.Equal(new Version(1, 0), application.Version);
		Assert.Empty(application.Editions);

		application.Version = null;
		application.Editions.Add(edition);

		Assert.Throws<InvalidOperationException>(() => application.Version = new Version(3, 0));
		Assert.Null(application.Version);
		Assert.Single(application.Editions);
		Assert.Equal(new Version(2, 0), application.Editions["Community"].Version);
	}

	[Fact]
	public void VersionAndEditions_CanSwitchRepresentations()
	{
		var application = new ApplicationVersion("App", new Version(1, 0));
		application.Version = new Version(1, 1);
		application.Save(_directory);
		Assert.Equal("App@1.1\r\n", File.ReadAllText(this.GetPath()));

		application.Version = null;
		application.Editions.Add(new("Community", new Version(2, 0)));
		application.Version = null;
		application.Save(_directory);
		Assert.Equal("App\r\n\r\n[Community]\r\n2.0\r\n", File.ReadAllText(this.GetPath()));

		application.Editions.Clear();
		Assert.Empty(application.Editions);
		Assert.Throws<KeyNotFoundException>(() => application.Editions["Community"]);
		application.Version = new Version(3, 0);
		application.Save(_directory);
		Assert.Equal("App@3.0\r\n", File.ReadAllText(this.GetPath()));
	}

	[Fact]
	public void Editions_PreserveOrderAndUseCaseInsensitiveLookup()
	{
		var editions = new ApplicationVersion("App").Editions;
		editions.Add(new("Professional", new Version(2, 0)));
		editions.Add(new("Community", new Version(1, 0)));
		editions.Add(new("Enterprise", new Version(3, 0)));

		Assert.Equal(new[] { "Professional", "Community", "Enterprise" }, editions.Select(edition => edition.Name));
		Assert.Equal(new Version(1, 0), editions["COMMUNITY"].Version);
		Assert.Equal("Professional", editions[0].Name);
		Assert.Equal("Enterprise", editions[2].Name);
		Assert.Throws<KeyNotFoundException>(() => editions["missing"]);
		Assert.Throws<ArgumentOutOfRangeException>(() => editions[-1]);
		Assert.Throws<ArgumentOutOfRangeException>(() => editions[3]);
	}

	[Fact]
	public void Editions_RejectDuplicateWithoutChangingState()
	{
		var editions = new ApplicationVersion("App").Editions;
		editions.Add(new("Community", new Version(1, 0)));
		editions.Add(new("Enterprise", new Version(3, 0)));

		Assert.Throws<ArgumentException>(() => editions.Add(new(" community ", new Version(2, 0))));
		Assert.Equal(new[] { "Community", "Enterprise" }, editions.Select(edition => edition.Name));
		Assert.Equal(new Version(1, 0), editions["COMMUNITY"].Version);
	}

	[Fact]
	public void Editions_RejectDefaultWithoutChangingState()
	{
		var editions = new ApplicationVersion("App").Editions;
		editions.Add(new("Community", new Version(1, 0)));

		Assert.ThrowsAny<ArgumentException>(() => editions.Add(default));
		Assert.Single(editions);
		Assert.Equal("Community", editions[0].Name);
	}

	[Fact]
	public void Editions_MatchBothNameAndVersion()
	{
		var editions = new ApplicationVersion("App").Editions;
		editions.Add(new("Community", new Version(1, 0)));
		editions.Add(new("Professional", new Version(2, 0)));
		editions.Add(new("Enterprise", new Version(3, 0)));

		//直接验证自定义 Contains 的名称和版本匹配规则，不能改用枚举集合的断言。
#pragma warning disable xUnit2017
		Assert.True(editions.Contains(new("PROFESSIONAL", new Version(2, 0))));
		Assert.False(editions.Contains(new("Professional", new Version(2, 1))));
		Assert.False(editions.Contains(new("Missing", new Version(2, 0))));
		Assert.False(editions.Contains(default));
#pragma warning restore xUnit2017
		Assert.False(editions.Remove(new("Professional", new Version(2, 1))));
		Assert.False(editions.Remove(new("Missing", new Version(2, 0))));
		Assert.False(editions.Remove(default));
		Assert.Equal(new[] { "Community", "Professional", "Enterprise" }, editions.Select(edition => edition.Name));

		Assert.True(editions.Remove(new("PROFESSIONAL", new Version(2, 0))));
		Assert.Equal(new[] { "Community", "Enterprise" }, editions.Select(edition => edition.Name));
		Assert.Equal(new Version(3, 0), editions[1].Version);
		Assert.Equal(new Version(3, 0), editions["Enterprise"].Version);
		Assert.Throws<KeyNotFoundException>(() => editions["Professional"]);

		editions.Add(new("professional", new Version(2, 1)));
		Assert.Equal(new[] { "Community", "Enterprise", "professional" }, editions.Select(edition => edition.Name));
		Assert.Equal(new Version(2, 1), editions["Professional"].Version);
	}

	[Fact]
	public void Editions_CopyToAndEnumerate()
	{
		ICollection<ApplicationVersion.Edition> editions = new ApplicationVersion("App").Editions;
		editions.Add(new("Community", new Version(1, 0)));
		editions.Add(new("Enterprise", new Version(3, 0)));
		var destination = new ApplicationVersion.Edition[4];
		editions.CopyTo(destination, 1);

		Assert.False(editions.IsReadOnly);
		Assert.Equal(2, editions.Count);
		Assert.Null(destination[0].Name);
		Assert.Equal("Community", destination[1].Name);
		Assert.Equal(new Version(3, 0), destination[2].Version);
		Assert.Null(destination[3].Name);
		Assert.Equal(new[] { "Community", "Enterprise" }, ((IEnumerable)editions).Cast<ApplicationVersion.Edition>().Select(edition => edition.Name));
		Assert.Throws<ArgumentNullException>(() => editions.CopyTo(null, 0));
		Assert.Throws<ArgumentOutOfRangeException>(() => editions.CopyTo(destination, -1));
		Assert.Throws<ArgumentException>(() => editions.CopyTo(destination, 3));
	}

	[Theory]
	[InlineData("1.0")]
	[InlineData("1.2.3")]
	[InlineData("1.2.3.4")]
	[InlineData("2147483647.2147483647.2147483647.2147483647")]
	public void Save_SingleVersion(string version)
	{
		new ApplicationVersion("示例应用", Version.Parse(version)).Save(_directory);

		Assert.Equal(Encoding.UTF8.GetBytes("示例应用@" + version + "\r\n"), File.ReadAllBytes(this.GetPath()));
		var loaded = ApplicationVersion.Load(this.GetPath());
		Assert.Equal("示例应用", loaded.Name);
		Assert.Equal(Version.Parse(version), loaded.Version);
		Assert.Empty(loaded.Editions);
	}

	[Fact]
	public void Save_Editions()
	{
		var application = new ApplicationVersion("示例应用");
		application.Editions.Add(new("Professional", new Version(1, 1, 0)));
		application.Editions.Add(new("社区版", new Version(1, 0, 1)));
		application.Editions.Add(new("Enterprise", new Version(1, 1, 2)));
		application.Save(_directory);

		Assert.Equal(Encoding.UTF8.GetBytes("示例应用\r\n\r\n[Professional]\r\n1.1.0\r\n\r\n[社区版]\r\n1.0.1\r\n\r\n[Enterprise]\r\n1.1.2\r\n"), File.ReadAllBytes(this.GetPath()));
		var loaded = ApplicationVersion.Load(this.GetPath());
		Assert.Equal("示例应用", loaded.Name);
		Assert.Null(loaded.Version);
		Assert.Equal(new[] { "Professional", "社区版", "Enterprise" }, loaded.Editions.Select(edition => edition.Name));
		Assert.Equal(new Version(1, 0, 1), loaded.Editions["社区版"].Version);
	}

	[Fact]
	public void Save_TruncatesPreviousContent()
	{
		this.Write(new string('x', 4096));
		new ApplicationVersion("App", new Version(1, 0)).Save(_directory);

		Assert.Equal(Encoding.UTF8.GetBytes("App@1.0\r\n"), File.ReadAllBytes(this.GetPath()));
		Assert.Equal(new Version(1, 0), ApplicationVersion.Load(this.GetPath()).Version);
	}

	[Fact]
	public void Save_InvalidStatePreservesFile()
	{
		var path = this.Write("Original content\r\n");
		var application = new ApplicationVersion("App");

		Assert.Throws<InvalidOperationException>(() => application.Save(path));
		Assert.Equal("Original content\r\n", File.ReadAllText(path));
		Assert.Throws<InvalidOperationException>(() => application.Save(this.GetPath("new.version")));
		Assert.False(File.Exists(this.GetPath("new.version")));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void FileOperations_MissingPathsAreNotCreated(bool missingParent)
	{
		var path = missingParent ? System.IO.Path.Combine(_directory, "missing", ".version") : this.GetPath();

		Assert.Null(ApplicationVersion.Load(path: path));
		new ApplicationVersion("App", new Version(1, 0)).Save(path: path);
		Assert.False(File.Exists(path));
		Assert.Empty(Directory.EnumerateFileSystemEntries(_directory));
		Assert.Throws<InvalidOperationException>(() => new ApplicationVersion("App").Save(path: path));
		Assert.Empty(Directory.EnumerateFileSystemEntries(_directory));
	}

	[Fact]
	public void FileOperations_DirectoryUsesVersionFile()
	{
		Assert.Null(ApplicationVersion.Load(path: _directory));
		Assert.Throws<InvalidOperationException>(() => new ApplicationVersion("App").Save(path: _directory));
		Assert.Empty(Directory.EnumerateFileSystemEntries(_directory));

		new ApplicationVersion("App", new Version(1, 2, 3)).Save(path: _directory);

		Assert.Equal(Encoding.UTF8.GetBytes("App@1.2.3\r\n"), File.ReadAllBytes(this.GetPath()));
		var loaded = ApplicationVersion.Load(path: _directory);
		Assert.Equal("App", loaded.Name);
		Assert.Equal(new Version(1, 2, 3), loaded.Version);
		Assert.Empty(loaded.Editions);
		Assert.Throws<InvalidOperationException>(() => new ApplicationVersion("App").Save(path: _directory));
		Assert.Equal("App@1.2.3\r\n", File.ReadAllText(this.GetPath()));
	}

	[Theory]
	[InlineData("My-App:服务", "Community")]
	[InlineData("App;#Name", ";#Community@1:2")]
	public void Names_PreserveUnreservedPunctuation(string name, string editionName)
	{
		var application = new ApplicationVersion(name, new Version(1, 0));
		application.Save(_directory);
		Assert.Equal(name + "@1.0\r\n", File.ReadAllText(this.GetPath()));
		Assert.Equal(name, ApplicationVersion.Load(this.GetPath()).Name);

		application.Version = null;
		application.Editions.Add(new(editionName, new Version(2, 0)));
		application.Save(_directory);
		Assert.Equal(name + "\r\n\r\n[" + editionName + "]\r\n2.0\r\n", File.ReadAllText(this.GetPath()));
		var loaded = ApplicationVersion.Load(this.GetPath());
		Assert.Equal(name, loaded.Name);
		Assert.Equal(editionName, loaded.Editions[0].Name);
		Assert.Equal(new Version(2, 0), loaded.Editions[0].Version);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	public void FileOperations_EmptyPathUsesBaseDirectoryWithoutWriting(string path)
	{
		ApplicationVersion expected = null;
		var expectedError = Record.Exception(() => expected = ApplicationVersion.Load(path: AppContext.BaseDirectory));
		ApplicationVersion actual = null;
		var actualError = Record.Exception(() => actual = ApplicationVersion.Load(path: path));

		Assert.Equal(expectedError?.GetType(), actualError?.GetType());
		if(expectedError == null && expected != null)
		{
			Assert.NotNull(actual);
			Assert.Equal(expected.Name, actual.Name);
			Assert.Equal(expected.Version, actual.Version);
			Assert.Equal(expected.Editions.ToArray(), actual.Editions.ToArray());
		}
		else
			Assert.Null(actual);

		Assert.Throws<InvalidOperationException>(() => new ApplicationVersion("App").Save(path: path));
	}

	[Fact]
	public void Load_SingleVersionAllowsSurroundingComments()
	{
		var application = ApplicationVersion.Load(this.Write("; header\r\nApp@1.2.3\n # trailing comment\r\n\t"));

		Assert.Equal("App", application.Name);
		Assert.Equal(new Version(1, 2, 3), application.Version);
		Assert.Empty(application.Editions);
		application.Save(_directory);
		Assert.Equal("App@1.2.3\r\n", File.ReadAllText(this.GetPath()));
	}

	[Fact]
	public void FileOperations_RoundTripBeyondSixteenKilobytes()
	{
		var name = new string('A', 32768) + "-应用";
		new ApplicationVersion(name, new Version(1, 2, 3)).Save(_directory);

		Assert.Equal(Encoding.UTF8.GetBytes(name + "@1.2.3\r\n"), File.ReadAllBytes(this.GetPath()));
		var loaded = ApplicationVersion.Load(this.GetPath());
		Assert.Equal(name, loaded.Name);
		Assert.Equal(new Version(1, 2, 3), loaded.Version);
		Assert.Empty(loaded.Editions);
	}

	[Fact]
	public void Editions_TryGetUsesCaseInsensitiveNamesWithoutTrimming()
	{
		var editions = new ApplicationVersion("App").Editions;
		editions.Add(new("Community", new Version(1, 0)));
		editions.Add(new("Enterprise", new Version(3, 0)));

		Assert.True(editions.TryGetValue("COMMUNITY", out var result));
		Assert.Equal("Community", result.Name);
		Assert.Equal(new Version(1, 0), result.Version);
		Assert.Equal(editions["community"], result);
		Assert.False(editions.TryGetValue(" Community ", out result));
		Assert.Null(result.Name);
		Assert.Null(result.Version);
		Assert.Throws<KeyNotFoundException>(() => editions[" Community "]);
		Assert.Equal(new[] { "Community", "Enterprise" }, editions.Select(edition => edition.Name));
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData(" \t")]
	[InlineData("Missing")]
	public void Editions_TryGetMissingNamesReturnsDefault(string name)
	{
		var editions = new ApplicationVersion("App").Editions;
		editions.Add(new("Community", new Version(1, 0)));
		Assert.True(editions.TryGetValue("Community", out var result));

		Assert.False(editions.TryGetValue(name, out result));
		Assert.Null(result.Name);
		Assert.Null(result.Version);
		Assert.Single(editions);
		Assert.Equal(new Version(1, 0), editions["Community"].Version);
	}

	[Fact]
	public void Editions_IsEmptyTracksCollectionChanges()
	{
		var editions = new ApplicationVersion("App").Editions;
		Assert.True(editions.IsEmpty);

		editions.Add(new("Community", new Version(1, 0)));
		Assert.False(editions.IsEmpty);
		editions.Add(new("Enterprise", new Version(3, 0)));
		Assert.False(editions.IsEmpty);
		Assert.True(editions.Remove(new("COMMUNITY", new Version(1, 0))));
		Assert.False(editions.IsEmpty);
		Assert.Equal("Enterprise", editions[0].Name);
		Assert.True(editions.Remove(new("Enterprise", new Version(3, 0))));
		Assert.True(editions.IsEmpty);
		Assert.False(editions.TryGetValue("Enterprise", out _));

		editions.Add(new("Community", new Version(2, 0)));
		Assert.False(editions.IsEmpty);
		editions.Clear();
		Assert.True(editions.IsEmpty);
		Assert.False(editions.TryGetValue("Community", out _));
		Assert.Empty(editions);
	}

	[Fact]
	public void StreamOverloads_RejectNullArguments()
	{
		var application = new ApplicationVersion("App", new Version(1, 0));

		Assert.Throws<ArgumentNullException>("stream", () => ApplicationVersion.Load((Stream)null));
		Assert.Throws<ArgumentNullException>("reader", () => ApplicationVersion.Load((TextReader)null));
		Assert.Throws<ArgumentNullException>("stream", () => application.Save((Stream)null));
		Assert.Throws<ArgumentNullException>("writer", () => application.Save((TextWriter)null));
	}

	[Theory]
	[InlineData("utf-8", false)]
	[InlineData("utf-8", true)]
	[InlineData("utf-16", true)]
	public void Load_StreamReadsCurrentPositionAndDetectsBom(string encodingName, bool bom)
	{
		var encoding = Encoding.GetEncoding(encodingName);
		var prefix = Encoding.ASCII.GetBytes("not a version file:");
		using var stream = new MemoryStream();
		stream.Write(prefix);
		if(bom)
			stream.Write(encoding.GetPreamble());
		stream.Write(encoding.GetBytes("应用@1.2.3\r\n"));
		stream.Position = prefix.Length;

		var application = ApplicationVersion.Load(stream);

		Assert.Equal("应用", application.Name);
		Assert.Equal(new Version(1, 2, 3), application.Version);
		Assert.Empty(application.Editions);
		Assert.True(stream.CanRead);
		Assert.Equal(stream.Length, stream.Position);
		Assert.Equal(-1, stream.ReadByte());
	}

	[Fact]
	public void Load_TextReaderReadsCurrentPositionAndLeavesOpen()
	{
		using var reader = new StringReader("skip this line\n应用\n[Community]\n1.0.1\n[Enterprise]\n2.0.3");
		Assert.Equal("skip this line", reader.ReadLine());

		var application = ApplicationVersion.Load(reader);

		Assert.Equal("应用", application.Name);
		Assert.Null(application.Version);
		Assert.Equal(new[] { "Community", "Enterprise" }, application.Editions.Select(edition => edition.Name));
		Assert.Equal(new Version(1, 0, 1), application.Editions[0].Version);
		Assert.Equal(new Version(2, 0, 3), application.Editions[1].Version);
		Assert.Equal(-1, reader.Peek());
	}

	[Fact]
	public void Load_NonSeekableStreamLeavesOpen()
	{
		using var stream = new ObservedStream(Encoding.UTF8.GetBytes("应用\n[Community]\n1.0\n[Enterprise]\n3.0"));

		var application = ApplicationVersion.Load(stream);

		Assert.Equal("应用", application.Name);
		Assert.Equal(new Version(1, 0), application.Editions["Community"].Version);
		Assert.Equal(new Version(3, 0), application.Editions["Enterprise"].Version);
		Assert.False(stream.IsDisposed);
		Assert.Equal(-1, stream.ReadByte());
	}

	[Fact]
	public void Load_InvalidFormatLeavesInputsOpen()
	{
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes("App@invalid"));
		using var reader = new StringReader("App\n[Community]");

		Assert.Throws<FormatException>(() => ApplicationVersion.Load(stream));
		Assert.True(stream.CanRead);
		Assert.Equal(stream.Length, stream.Position);
		Assert.Throws<FormatException>(() => ApplicationVersion.Load(reader));
		Assert.Equal(-1, reader.Peek());
	}

	[Fact]
	public void Load_IoFailuresLeaveInputsOpen()
	{
		using var stream = new ObservedStream(Encoding.UTF8.GetBytes("App@1.0")) { FailRead = true };
		using var reader = new FailingReader();

		Assert.Same(stream.Error, Assert.Throws<IOException>(() => ApplicationVersion.Load(stream)));
		Assert.False(stream.IsDisposed);
		stream.FailRead = false;
		Assert.Equal((int)'A', stream.ReadByte());
		Assert.Same(reader.Error, Assert.Throws<IOException>(() => ApplicationVersion.Load(reader)));
		Assert.False(reader.IsDisposed);
	}

	[Fact]
	public void Save_StreamPreservesPrefixAndTail()
	{
		using var stream = new MemoryStream();
		stream.Write(Encoding.UTF8.GetBytes("prefix:" + new string('x', 9) + "tail"));
		stream.Position = 7;

		new ApplicationVersion("App", new Version(1, 0)).Save(stream);

		Assert.Equal(Encoding.UTF8.GetBytes("prefix:App@1.0\r\ntail"), stream.ToArray());
		Assert.Equal(16, stream.Position);
		Assert.True(stream.CanWrite);
		stream.WriteByte((byte)'T');
		Assert.Equal(Encoding.UTF8.GetBytes("prefix:App@1.0\r\nTail"), stream.ToArray());
	}

	[Fact]
	public void Save_NonSeekableStreamFlushesAndLeavesOpen()
	{
		using var stream = new ObservedStream();
		var application = new ApplicationVersion("应用");
		application.Editions.Add(new("Community", new Version(1, 2, 3)));
		application.Editions.Add(new("Enterprise", new Version(4, 5, 6)));

		application.Save(stream);

		Assert.Equal(Encoding.UTF8.GetBytes("应用\r\n\r\n[Community]\r\n1.2.3\r\n\r\n[Enterprise]\r\n4.5.6\r\n"), stream.ToArray());
		Assert.True(stream.FlushCount > 0);
		Assert.False(stream.IsDisposed);
		stream.WriteByte((byte)'!');
		Assert.Equal((byte)'!', stream.ToArray()[^1]);
	}

	[Fact]
	public void Save_TextWriterPreservesNewLineAndFlushes()
	{
		using var writer = new ObservedWriter { NewLine = "custom newline" };
		writer.Write("prefix:");
		var application = new ApplicationVersion("App");
		application.Editions.Add(new("Community", new Version(1, 0)));
		application.Editions.Add(new("Enterprise", new Version(3, 0)));

		application.Save(writer);

		Assert.Equal("prefix:App\r\n\r\n[Community]\r\n1.0\r\n\r\n[Enterprise]\r\n3.0\r\n", writer.ToString());
		Assert.Equal("custom newline", writer.NewLine);
		Assert.True(writer.FlushCount > 0);
		Assert.False(writer.IsDisposed);
		writer.Write('!');
		Assert.EndsWith("\r\n!", writer.ToString());
	}

	[Fact]
	public void Save_TextWriterUsesCallerEncoding()
	{
		using var stream = new MemoryStream();
		using var writer = new StreamWriter(stream, new UnicodeEncoding(false, false), 1024, true) { NewLine = "\n" };

		new ApplicationVersion("应用", new Version(1, 2)).Save(writer);

		Assert.Equal(Encoding.Unicode.GetBytes("应用@1.2\r\n"), stream.ToArray());
		Assert.Equal("\n", writer.NewLine);
		writer.Write('!');
		writer.Flush();
		Assert.Equal(Encoding.Unicode.GetBytes("应用@1.2\r\n!"), stream.ToArray());
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void Save_IoFailuresLeaveOutputsOpen(bool flushFailure)
	{
		using var stream = new ObservedStream { FailWrite = !flushFailure, FailFlush = flushFailure };
		using var writer = new ObservedWriter { FailWrite = !flushFailure, FailFlush = flushFailure, NewLine = "custom newline" };
		var application = new ApplicationVersion("App", new Version(1, 0));

		Assert.Same(stream.Error, Assert.Throws<IOException>(() => application.Save(stream)));
		Assert.False(stream.IsDisposed);
		stream.FailWrite = false;
		stream.FailFlush = false;
		stream.WriteByte((byte)'!');
		Assert.Equal((byte)'!', stream.ToArray()[^1]);

		Assert.Same(writer.Error, Assert.Throws<IOException>(() => application.Save(writer)));
		Assert.False(writer.IsDisposed);
		Assert.Equal("custom newline", writer.NewLine);
		writer.FailWrite = false;
		writer.FailFlush = false;
		writer.Write('!');
		Assert.EndsWith("!", writer.ToString());
	}

	[Fact]
	public void Save_InvalidStateDoesNotTouchOutputs()
	{
		using var stream = new ObservedStream(Encoding.UTF8.GetBytes("original"));
		using var writer = new ObservedWriter { NewLine = "custom newline" };
		writer.Write("original");
		var writes = writer.WriteCount;
		var application = new ApplicationVersion("App");

		Assert.Throws<InvalidOperationException>(() => application.Save(stream));
		Assert.Equal(Encoding.UTF8.GetBytes("original"), stream.ToArray());
		Assert.Equal(0, stream.WriteCount);
		Assert.Equal(0, stream.FlushCount);
		Assert.False(stream.IsDisposed);
		Assert.Equal((int)'o', stream.ReadByte());

		Assert.Throws<InvalidOperationException>(() => application.Save(writer));
		Assert.Equal("original", writer.ToString());
		Assert.Equal(writes, writer.WriteCount);
		Assert.Equal(0, writer.FlushCount);
		Assert.False(writer.IsDisposed);
		Assert.Equal("custom newline", writer.NewLine);
		writer.Write('!');
		Assert.Equal("original!", writer.ToString());
	}

	[Fact]
	public void FileOperations_ExistingFileUsesExactPathAndTruncates()
	{
		var path = this.GetPath("deployment.identity");
		File.WriteAllText(path, "Original\r\n\r\n[Community]\r\n1.0.1\r\n\r\n[Enterprise]\r\n2.0.3\r\n", new UTF8Encoding(false));
		this.Write("Sibling@9.0\r\n");

		var application = ApplicationVersion.Load(path: path);
		Assert.Equal("Original", application.Name);
		Assert.Null(application.Version);
		Assert.Equal(new[] { "Community", "Enterprise" }, application.Editions.Select(edition => edition.Name));
		Assert.Equal(new Version(1, 0, 1), application.Editions[0].Version);
		Assert.Equal(new Version(2, 0, 3), application.Editions[1].Version);

		new ApplicationVersion("App", new Version(1, 0)).Save(path: path);

		Assert.Equal(Encoding.UTF8.GetBytes("App@1.0\r\n"), File.ReadAllBytes(path));
		Assert.Equal("Sibling@9.0\r\n", File.ReadAllText(this.GetPath()));
		var loaded = ApplicationVersion.Load(path: path);
		Assert.Equal("App", loaded.Name);
		Assert.Equal(new Version(1, 0), loaded.Version);
		Assert.Empty(loaded.Editions);
	}

	[Fact]
	public void FileOperations_PropagateIoFailures()
	{
		var path = this.Write("App@1.0\r\n");
		using(var locked = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
		{
			Assert.Throws<IOException>(() => ApplicationVersion.Load(path: path));
			Assert.Throws<IOException>(() => new ApplicationVersion("App", new Version(2, 0)).Save(path: path));
		}

		Assert.Equal("App@1.0\r\n", File.ReadAllText(path));
	}

	private sealed class ObservedStream : Stream
	{
		private readonly MemoryStream _stream = new();
		public ObservedStream(byte[] content = null)
		{
			if(content != null)
				_stream.Write(content);
			_stream.Position = 0;
		}

		public IOException Error { get; } = new("Test stream failure.");
		public bool FailRead { get; set; }
		public bool FailWrite { get; set; }
		public bool FailFlush { get; set; }
		public bool IsDisposed { get; private set; }
		public int WriteCount { get; private set; }
		public int FlushCount { get; private set; }
		public override bool CanRead => !this.IsDisposed;
		public override bool CanWrite => !this.IsDisposed;
		public override bool CanSeek => false;
		public override long Length => throw new NotSupportedException();
		public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
		public byte[] ToArray() => _stream.ToArray();
		public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
		public override void SetLength(long value) => throw new NotSupportedException();
		public override int Read(byte[] buffer, int offset, int count) => this.FailRead ? throw this.Error : _stream.Read(buffer, offset, count);
		public override void Write(byte[] buffer, int offset, int count)
		{
			this.WriteCount++;
			if(this.FailWrite)
				throw this.Error;
			_stream.Write(buffer, offset, count);
		}
		public override void Flush()
		{
			this.FlushCount++;
			if(this.FailFlush)
				throw this.Error;
			_stream.Flush();
		}
		protected override void Dispose(bool disposing)
		{
			this.IsDisposed = true;
			if(disposing)
				_stream.Dispose();
			base.Dispose(disposing);
		}
	}

	private sealed class FailingReader : TextReader
	{
		public IOException Error { get; } = new("Test reader failure.");
		public bool IsDisposed { get; private set; }
		public override string ReadToEnd() => throw this.Error;
		protected override void Dispose(bool disposing)
		{
			this.IsDisposed = true;
			base.Dispose(disposing);
		}
	}

	private sealed class ObservedWriter : StringWriter
	{
		public IOException Error { get; } = new("Test writer failure.");
		public bool FailWrite { get; set; }
		public bool FailFlush { get; set; }
		public bool IsDisposed { get; private set; }
		public int FlushCount { get; private set; }
		public int WriteCount { get; private set; }
		public override void Write(char value)
		{
			this.WriteCount++;
			if(this.FailWrite)
				throw this.Error;
			base.Write(value);
		}
		public override void Write(string value)
		{
			this.WriteCount++;
			if(this.FailWrite)
				throw this.Error;
			base.Write(value);
		}
		public override void Write(ReadOnlySpan<char> buffer)
		{
			this.WriteCount++;
			if(this.FailWrite)
				throw this.Error;
			base.Write(buffer);
		}
		public override void Flush()
		{
			this.FlushCount++;
			if(this.FailFlush)
				throw this.Error;
			base.Flush();
		}
		protected override void Dispose(bool disposing)
		{
			this.IsDisposed = true;
			base.Dispose(disposing);
		}
	}

	private string GetPath(string name = ".version") => System.IO.Path.Combine(_directory, name);
	private string Write(string content)
	{
		var path = this.GetPath();
		File.WriteAllText(path, content, new UTF8Encoding(false));
		return path;
	}
}
