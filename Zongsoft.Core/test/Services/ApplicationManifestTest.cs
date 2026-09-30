using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Xunit;
using Edition = Zongsoft.Services.ApplicationManifest.Edition;

namespace Zongsoft.Services.Tests;

public class ApplicationManifestTest : IDisposable
{
	private static readonly string _root = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Zongsoft.ApplicationManifest.Tests"));
	private readonly string _directory = System.IO.Path.Combine(_root, Guid.NewGuid().ToString("N"));

	public ApplicationManifestTest() => Directory.CreateDirectory(_directory);
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
		var application = new ApplicationManifest(" \tMyApplication \t");
		var edition = new ApplicationManifest.Edition(" \tCommunity \t", new Version(1, 2));

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
	public void Constructor_RejectsEmptyNames(string name)
	{
		Assert.ThrowsAny<ArgumentException>(() => new ApplicationManifest(name, new Version(1, 0)));
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData(" \t")]
	public void Edition_RejectsEmptyNames(string name)
	{
		Assert.ThrowsAny<ArgumentException>(() => new Edition(name, new Version(1, 0)));
	}

	[Theory]
	[InlineData("App@Name", "Pro Name")]
	[InlineData("App=Name", "Pro=Name")]
	[InlineData("App[Name", "[Community]")]
	[InlineData("#App", "Pro/Name")]
	public void Constructors_DoNotValidateReservedCharacters(string name, string editionName)
	{
		var manifest = new ApplicationManifest(name);
		var edition = manifest.Editions.Add(editionName, new Version(1, 0));
		manifest.Editions.Current = editionName;
		Assert.Equal(name, manifest.Name);
		Assert.Equal(editionName, edition.Name);
		Assert.Equal(edition, manifest.Editions.Current);
	}

	[Fact]
	public void Edition_RejectsNullVersion()
	{
		Assert.Throws<ArgumentNullException>(() => new ApplicationManifest.Edition("Community", null));
	}

	[Theory]
	[InlineData("1.0")]
	[InlineData("1.2.3")]
	[InlineData("1.2.3.4")]
	[InlineData("0.0.0.0")]
	[InlineData("2147483647.2147483647.2147483647.2147483647")]
	public void Load_SingleVersion(string version)
	{
		var application = ApplicationManifest.Load(this.Write(" MyApplication @ " + version + " \t"));

		Assert.Equal("MyApplication", application.Name);
		Assert.Equal(Version.Parse(version), application.Version);
		Assert.Empty(application.Editions);
	}

	[Fact]
	public void Load_Editions()
	{
		var application = ApplicationManifest.Load(this.Write("MyApplication\r\n\r\n[Community]\r\n1.0.1\r\n\r\n[Professional]\r\n1.1.0\r\n\r\n[Enterprise]\r\n1.1.2"));

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
		var application = ApplicationManifest.Load(this.Write(content));

		Assert.Equal("MyApplication", application.Name);
		Assert.Null(application.Version);
		Assert.Equal(new[] { "Community", "Professional" }, application.Editions.Select(edition => edition.Name));
		Assert.Equal(new Version(1, 2, 3), application.Editions["COMMUNITY"].Version);
		Assert.Equal(new Version(2, 3, 4), application.Editions[1].Version);

		application.Save(_directory);
		var expected = string.Join(newline == "\r\n\n\r" ? string.Concat(Enumerable.Repeat(Environment.NewLine, 3)) : Environment.NewLine, new[] { "# header", "# ignored", "MyApplication", "", "[Community]", "# version", "1.2.3", "# between", "[Professional]", "2.3.4", "", "# end" }) + Environment.NewLine;
		Assert.Equal(expected, File.ReadAllText(this.GetPath()));
	}

	[Theory]
	[InlineData("")]
	[InlineData("\uFEFF")]
	[InlineData(" \r\n\t")]
	[InlineData("; comment\r\n# ignored")]
	[InlineData("MyApplication")]
	[InlineData("MyApplication@")]
	[InlineData("@1.0")]
	[InlineData("App@1.0@2.0")]
	[InlineData("App@1")]
	[InlineData("App@1.2.3.4.5")]
	[InlineData("App@-1.0")]
	[InlineData("App@2147483648.0")]
	[InlineData("App@1.0-beta")]
	[InlineData("App@1.0 ; comment")]
	[InlineData("[Community]\n1.0")]
	[InlineData("App\n1.0")]
	[InlineData("App\n[Community]")]
	[InlineData("App\n[]\n1.0")]
	[InlineData("App\n[ ]\n1.0")]
	[InlineData("App\n[Community\n1.0")]
	[InlineData("App\n[Community] extra\n1.0")]
	[InlineData("App\n[Community]\n[Professional]\n1.0")]
	[InlineData("App\n[Community]\ninvalid")]
	[InlineData("App\n[Community]\nVersion=1.0")]
	[InlineData("App\n[Community]\n1.0 # comment")]
	[InlineData("App\n[Community]\n1.0\n2.0")]
	[InlineData("App\n[Community]\n1.0\n[community]\n2.0")]
	[InlineData("App@1.0\n[Community]\n1.0")]
	[InlineData("App@1.0\n2.0")]
	[InlineData("App\n\n# comment\n[Community]\n\tbad")]
	public void Load_InvalidContentThrowsFormatException(string content)
	{
		Assert.Throws<FormatException>(() => ApplicationManifest.Load(this.Write(content)));
	}

	[Fact]
	public void VersionAndEditions_RejectConflictsWithoutChangingState()
	{
		var application = new ApplicationManifest("App", new Version(1, 0));
		var edition = new ApplicationManifest.Edition("Community", new Version(2, 0));

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
		var application = new ApplicationManifest("App", new Version(1, 0));
		application.Version = new Version(1, 1);
		application.Save(_directory);
		Assert.Equal(Output("App@1.1\r\n"), File.ReadAllText(this.GetPath()));

		application.Version = null;
		application.Editions.Add(new("Community", new Version(2, 0)));
		application.Version = null;
		application.Save(_directory);
		Assert.Equal(Output("App\r\n\r\n[Community]\r\n2.0\r\n"), File.ReadAllText(this.GetPath()));

		application.Editions.Clear();
		Assert.Empty(application.Editions);
		Assert.Throws<KeyNotFoundException>(() => application.Editions["Community"]);
		application.Version = new Version(3, 0);
		application.Save(_directory);
		Assert.Equal(Output("App@3.0\r\n\r\n"), File.ReadAllText(this.GetPath()));
	}

	[Fact]
	public void Editions_PreserveOrderAndUseCaseInsensitiveLookup()
	{
		var editions = new ApplicationManifest("App").Editions;
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
		var editions = new ApplicationManifest("App").Editions;
		editions.Add(new("Community", new Version(1, 0)));
		editions.Add(new("Enterprise", new Version(3, 0)));

		Assert.Throws<ArgumentException>(() => editions.Add(new(" community ", new Version(2, 0))));
		Assert.Equal(new[] { "Community", "Enterprise" }, editions.Select(edition => edition.Name));
		Assert.Equal(new Version(1, 0), editions["COMMUNITY"].Version);
	}

	[Fact]
	public void Editions_RejectDefaultWithoutChangingState()
	{
		var editions = new ApplicationManifest("App").Editions;
		editions.Add(new("Community", new Version(1, 0)));

		Assert.ThrowsAny<ArgumentException>(() => editions.Add(default(Edition)));
		Assert.Single(editions);
		Assert.Equal("Community", editions[0].Name);
	}

	[Fact]
	public void Editions_MatchBothNameAndVersion()
	{
		var editions = new ApplicationManifest("App").Editions;
		editions.Add(new("Community", new Version(1, 0)));
		editions.Add(new("Professional", new Version(2, 0)));
		editions.Add(new("Enterprise", new Version(3, 0)));

		//验证集合按条目匹配名称和版本号的规则。
#pragma warning disable xUnit2017
		Assert.True(editions.Contains(new Edition("PROFESSIONAL", new Version(2, 0))));
		Assert.False(editions.Contains(new Edition("Professional", new Version(2, 1))));
		Assert.False(editions.Contains(new Edition("Missing", new Version(2, 0))));
		Assert.False(editions.Contains(default(Edition)));
#pragma warning restore xUnit2017
		Assert.False(editions.Remove(new Edition("Professional", new Version(2, 1))));
		Assert.False(editions.Remove(new Edition("Missing", new Version(2, 0))));
		Assert.False(editions.Remove(default(Edition)));
		Assert.Equal(new[] { "Community", "Professional", "Enterprise" }, editions.Select(edition => edition.Name));

		Assert.True(editions.Remove(new Edition("PROFESSIONAL", new Version(2, 0))));
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
		ICollection<ApplicationManifest.Edition> editions = new ApplicationManifest("App").Editions;
		editions.Add(new("Community", new Version(1, 0)));
		editions.Add(new("Enterprise", new Version(3, 0)));
		var destination = new ApplicationManifest.Edition[4];
		editions.CopyTo(destination, 1);

		Assert.False(editions.IsReadOnly);
		Assert.Equal(2, editions.Count);
		Assert.Null(destination[0].Name);
		Assert.Equal("Community", destination[1].Name);
		Assert.Equal(new Version(3, 0), destination[2].Version);
		Assert.Null(destination[3].Name);
		Assert.Equal(new[] { "Community", "Enterprise" }, ((IEnumerable)editions).Cast<ApplicationManifest.Edition>().Select(edition => edition.Name));
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
		new ApplicationManifest("示例应用", Version.Parse(version)).Save(_directory);

		Assert.Equal(Encoding.UTF8.GetBytes(Output("示例应用@" + version + "\r\n")), File.ReadAllBytes(this.GetPath()));
		var loaded = ApplicationManifest.Load(this.GetPath());
		Assert.Equal("示例应用", loaded.Name);
		Assert.Equal(Version.Parse(version), loaded.Version);
		Assert.Empty(loaded.Editions);
	}

	[Fact]
	public void Save_Editions()
	{
		var application = new ApplicationManifest("示例应用");
		application.Editions.Add(new("Professional", new Version(1, 1, 0)));
		application.Editions.Add(new("社区版", new Version(1, 0, 1)));
		application.Editions.Add(new("Enterprise", new Version(1, 1, 2)));
		application.Save(_directory);

		Assert.Equal(Encoding.UTF8.GetBytes(Output("示例应用\r\n\r\n[Professional]\r\n1.1.0\r\n\r\n[社区版]\r\n1.0.1\r\n\r\n[Enterprise]\r\n1.1.2\r\n")), File.ReadAllBytes(this.GetPath()));
		var loaded = ApplicationManifest.Load(this.GetPath());
		Assert.Equal("示例应用", loaded.Name);
		Assert.Null(loaded.Version);
		Assert.Equal(new[] { "Professional", "社区版", "Enterprise" }, loaded.Editions.Select(edition => edition.Name));
		Assert.Equal(new Version(1, 0, 1), loaded.Editions["社区版"].Version);
	}

	[Fact]
	public void Save_TruncatesPreviousContent()
	{
		this.Write(new string('x', 4096));
		new ApplicationManifest("App", new Version(1, 0)).Save(_directory);

		Assert.Equal(Encoding.UTF8.GetBytes(Output("App@1.0\r\n")), File.ReadAllBytes(this.GetPath()));
		Assert.Equal(new Version(1, 0), ApplicationManifest.Load(this.GetPath()).Version);
	}

	[Fact]
	public void Save_InvalidStatePreservesFile()
	{
		var path = this.Write("Original content\r\n");
		var application = new ApplicationManifest("App");

		Assert.Throws<InvalidOperationException>(() => application.Save(path));
		Assert.Equal("Original content\r\n", File.ReadAllText(path));
		Assert.Throws<InvalidOperationException>(() => application.Save(this.GetPath("new.edition")));
		Assert.False(File.Exists(this.GetPath("new.edition")));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void FileOperations_MissingPathsAreNotCreated(bool missingParent)
	{
		var path = missingParent ? System.IO.Path.Combine(_directory, "missing", ".edition") : this.GetPath();

		Assert.Null(ApplicationManifest.Load(path: path));
		new ApplicationManifest("App", new Version(1, 0)).Save(path: path);
		Assert.False(File.Exists(path));
		Assert.Empty(Directory.EnumerateFileSystemEntries(_directory));
		Assert.Throws<InvalidOperationException>(() => new ApplicationManifest("App").Save(path: path));
		Assert.Empty(Directory.EnumerateFileSystemEntries(_directory));
	}

	[Fact]
	public void FileOperations_DirectoryUsesEditionFile()
	{
		Assert.Null(ApplicationManifest.Load(path: _directory));
		Assert.Throws<InvalidOperationException>(() => new ApplicationManifest("App").Save(path: _directory));
		Assert.Empty(Directory.EnumerateFileSystemEntries(_directory));

		new ApplicationManifest("App", new Version(1, 2, 3)).Save(path: _directory);

		Assert.Equal(Encoding.UTF8.GetBytes(Output("App@1.2.3\r\n")), File.ReadAllBytes(this.GetPath()));
		var loaded = ApplicationManifest.Load(path: _directory);
		Assert.Equal("App", loaded.Name);
		Assert.Equal(new Version(1, 2, 3), loaded.Version);
		Assert.Empty(loaded.Editions);
		Assert.Throws<InvalidOperationException>(() => new ApplicationManifest("App").Save(path: _directory));
		Assert.Equal(Output("App@1.2.3\r\n"), File.ReadAllText(this.GetPath()));
	}

	[Theory]
	[InlineData("My-App:服务", "Community")]
	[InlineData("App;#Name", ";Community@Preview")]
	public void Names_PreserveUnreservedPunctuation(string name, string editionName)
	{
		var application = new ApplicationManifest(name, new Version(1, 0));
		application.Save(_directory);
		Assert.Equal(Output(name + "@1.0\r\n"), File.ReadAllText(this.GetPath()));
		Assert.Equal(name, ApplicationManifest.Load(this.GetPath()).Name);

		application.Version = null;
		application.Editions.Add(new(editionName, new Version(2, 0)));
		application.Save(_directory);
		Assert.Equal(Output(name + "\r\n\r\n[" + editionName + "]\r\n2.0\r\n"), File.ReadAllText(this.GetPath()));
		var loaded = ApplicationManifest.Load(this.GetPath());
		Assert.Equal(name, loaded.Name);
		Assert.Equal(editionName, loaded.Editions[0].Name);
		Assert.Equal(new Version(2, 0), loaded.Editions[0].Version);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	public void FileOperations_EmptyPathUsesBaseDirectoryWithoutWriting(string path)
	{
		ApplicationManifest expected = null;
		var expectedError = Record.Exception(() => expected = ApplicationManifest.Load(path: AppContext.BaseDirectory));
		ApplicationManifest actual = null;
		var actualError = Record.Exception(() => actual = ApplicationManifest.Load(path: path));

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

		Assert.Throws<InvalidOperationException>(() => new ApplicationManifest("App").Save(path: path));
	}

	[Fact]
	public void Load_SingleVersionAllowsSurroundingComments()
	{
		var application = ApplicationManifest.Load(this.Write("; header\r\nApp@1.2.3\n # trailing comment\r\n\t"));

		Assert.Equal("App", application.Name);
		Assert.Equal(new Version(1, 2, 3), application.Version);
		Assert.Empty(application.Editions);
		application.Save(_directory);
		Assert.Equal(Output("# header\r\nApp@1.2.3\r\n# trailing comment\r\n\r\n"), File.ReadAllText(this.GetPath()));
	}

	[Fact]
	public void FileOperations_RoundTripBeyondSixteenKilobytes()
	{
		var name = new string('A', 32768) + "-应用";
		new ApplicationManifest(name, new Version(1, 2, 3)).Save(_directory);

		Assert.Equal(Encoding.UTF8.GetBytes(Output(name + "@1.2.3\r\n")), File.ReadAllBytes(this.GetPath()));
		var loaded = ApplicationManifest.Load(this.GetPath());
		Assert.Equal(name, loaded.Name);
		Assert.Equal(new Version(1, 2, 3), loaded.Version);
		Assert.Empty(loaded.Editions);
	}

	[Fact]
	public void Editions_TryGetUsesCaseInsensitiveNamesWithoutTrimming()
	{
		var editions = new ApplicationManifest("App").Editions;
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
	[InlineData("")]
	[InlineData(" \t")]
	[InlineData("Missing")]
	public void Editions_TryGetMissingNamesReturnsDefault(string name)
	{
		var editions = new ApplicationManifest("App").Editions;
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
		var editions = new ApplicationManifest("App").Editions;
		Assert.True(editions.IsEmpty);

		editions.Add(new("Community", new Version(1, 0)));
		Assert.False(editions.IsEmpty);
		editions.Add(new("Enterprise", new Version(3, 0)));
		Assert.False(editions.IsEmpty);
		Assert.True(editions.Remove(new Edition("COMMUNITY", new Version(1, 0))));
		Assert.False(editions.IsEmpty);
		Assert.Equal("Enterprise", editions[0].Name);
		Assert.True(editions.Remove(new Edition("Enterprise", new Version(3, 0))));
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
		var application = new ApplicationManifest("App", new Version(1, 0));

		Assert.Throws<ArgumentNullException>("stream", () => ApplicationManifest.Load((Stream)null));
		Assert.Throws<ArgumentNullException>("reader", () => ApplicationManifest.Load((TextReader)null));
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

		var application = ApplicationManifest.Load(stream);

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

		var application = ApplicationManifest.Load(reader);

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

		var application = ApplicationManifest.Load(stream);

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

		Assert.Throws<FormatException>(() => ApplicationManifest.Load(stream));
		Assert.True(stream.CanRead);
		Assert.Equal(stream.Length, stream.Position);
		Assert.Throws<FormatException>(() => ApplicationManifest.Load(reader));
		Assert.Equal(-1, reader.Peek());
	}

	[Fact]
	public void Load_IoFailuresLeaveInputsOpen()
	{
		using var stream = new ObservedStream(Encoding.UTF8.GetBytes("App@1.0")) { FailRead = true };
		using var reader = new FailingReader();

		Assert.Same(stream.Error, Assert.Throws<IOException>(() => ApplicationManifest.Load(stream)));
		Assert.False(stream.IsDisposed);
		stream.FailRead = false;
		Assert.Equal((int)'A', stream.ReadByte());
		Assert.Same(reader.Error, Assert.Throws<IOException>(() => ApplicationManifest.Load(reader)));
		Assert.False(reader.IsDisposed);
	}

	[Fact]
	public void Save_StreamPreservesPrefixAndTail()
	{
		using var stream = new MemoryStream();
		stream.Write(Encoding.UTF8.GetBytes("prefix:" + new string('x', 7 + Environment.NewLine.Length) + "tail"));
		stream.Position = 7;

		new ApplicationManifest("App", new Version(1, 0)).Save(stream);

		Assert.Equal(Encoding.UTF8.GetBytes(Output("prefix:App@1.0\r\ntail")), stream.ToArray());
		Assert.Equal(14 + Environment.NewLine.Length, stream.Position);
		Assert.True(stream.CanWrite);
		stream.WriteByte((byte)'T');
		Assert.Equal(Encoding.UTF8.GetBytes(Output("prefix:App@1.0\r\nTail")), stream.ToArray());
	}

	[Fact]
	public void Save_NonSeekableStreamFlushesAndLeavesOpen()
	{
		using var stream = new ObservedStream();
		var application = new ApplicationManifest("应用");
		application.Editions.Add(new("Community", new Version(1, 2, 3)));
		application.Editions.Add(new("Enterprise", new Version(4, 5, 6)));

		application.Save(stream);

		Assert.Equal(Encoding.UTF8.GetBytes(Output("应用\r\n\r\n[Community]\r\n1.2.3\r\n\r\n[Enterprise]\r\n4.5.6\r\n")), stream.ToArray());
		Assert.True(stream.FlushCount > 0);
		Assert.False(stream.IsDisposed);
		stream.WriteByte((byte)'!');
		Assert.Equal((byte)'!', stream.ToArray()[^1]);
	}

	[Theory]
	[InlineData("\r\n")]
	[InlineData("\n")]
	public void Save_TextWriterUsesConfiguredNewLineAndFlushes(string newline)
	{
		using var writer = new ObservedWriter { NewLine = newline };
		writer.Write("prefix:");
		var application = new ApplicationManifest("App");
		application.Editions.Add(new("Community", new Version(1, 0)));
		application.Editions.Add(new("Enterprise", new Version(3, 0)));

		application.Save(writer);

		Assert.Equal("prefix:App\r\n\r\n[Community]\r\n1.0\r\n\r\n[Enterprise]\r\n3.0\r\n".Replace("\r\n", newline), writer.ToString());
		Assert.Equal(newline, writer.NewLine);
		Assert.True(writer.WriteLineCount > 0);
		Assert.True(writer.FlushCount > 0);
		Assert.False(writer.IsDisposed);
		writer.Write('!');
		Assert.EndsWith(newline + "!", writer.ToString());
	}

	[Fact]
	public void Save_TextWriterUsesCallerEncoding()
	{
		using var stream = new MemoryStream();
		using var writer = new StreamWriter(stream, new UnicodeEncoding(false, false), 1024, true) { NewLine = "\n" };

		new ApplicationManifest("应用", new Version(1, 2)).Save(writer);

		Assert.Equal(Encoding.Unicode.GetBytes("应用@1.2\n"), stream.ToArray());
		Assert.Equal("\n", writer.NewLine);
		writer.Write('!');
		writer.Flush();
		Assert.Equal(Encoding.Unicode.GetBytes("应用@1.2\n!"), stream.ToArray());
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void Save_IoFailuresLeaveOutputsOpen(bool flushFailure)
	{
		using var stream = new ObservedStream { FailWrite = !flushFailure, FailFlush = flushFailure };
		using var writer = new ObservedWriter { FailWrite = !flushFailure, FailFlush = flushFailure, NewLine = "custom newline" };
		var application = new ApplicationManifest("App", new Version(1, 0));

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
		var application = new ApplicationManifest("App");

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

		var application = ApplicationManifest.Load(path: path);
		Assert.Equal("Original", application.Name);
		Assert.Null(application.Version);
		Assert.Equal(new[] { "Community", "Enterprise" }, application.Editions.Select(edition => edition.Name));
		Assert.Equal(new Version(1, 0, 1), application.Editions[0].Version);
		Assert.Equal(new Version(2, 0, 3), application.Editions[1].Version);

		new ApplicationManifest("App", new Version(1, 0)).Save(path: path);

		Assert.Equal(Encoding.UTF8.GetBytes(Output("App@1.0\r\n\r\n\r\n")), File.ReadAllBytes(path));
		Assert.Equal("Sibling@9.0\r\n", File.ReadAllText(this.GetPath()));
		var loaded = ApplicationManifest.Load(path: path);
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
			Assert.Throws<IOException>(() => ApplicationManifest.Load(path: path));
			Assert.Throws<IOException>(() => new ApplicationManifest("App", new Version(2, 0)).Save(path: path));
		}

		Assert.Equal("App@1.0\r\n", File.ReadAllText(path));
	}

	[Theory]
	[InlineData("App", null)]
	[InlineData("App=", null)]
	[InlineData(" App = \t", null)]
	[InlineData("App=Professional", "Professional")]
	[InlineData(" App = professional \t", "Professional")]
	public void Current_LoadAndSave(string header, string expectedName)
	{
		const string SECTIONS = "\r\n\r\n[Community]\r\n1.0.1\r\n\r\n[Professional]\r\n1.1.0\r\n\r\n[Enterprise]\r\n1.1.2\r\n";
		var manifest = ApplicationManifest.Load(this.Write(header + SECTIONS));
		Assert.Equal("App", manifest.Name);
		Assert.Null(manifest.Version);
		Assert.Equal(new[] { "Community", "Professional", "Enterprise" }, manifest.Editions.Select(edition => edition.Name));
		Assert.Equal(expectedName, manifest.Editions.Current.Name);
		Assert.Equal(expectedName == null ? null : new Version(1, 1, 0), manifest.Editions.Current.Version);

		manifest.Save(_directory);
		Assert.Equal(Output((expectedName == null ? "App" : "App=" + expectedName) + SECTIONS), File.ReadAllText(this.GetPath()));
		Assert.Equal(manifest.Editions.Current, ApplicationManifest.Load(_directory).Editions.Current);
	}

	[Theory]
	[InlineData("Professional@Preview")]
	[InlineData("@Professional")]
	[InlineData("#Professional")]
	[InlineData("Professional:Preview")]
	[InlineData(";Professional")]
	public void Current_PreservesEditionPunctuation(string name)
	{
		var content = $"App={name}\r\n\r\n[{name}]\r\n1.2.3\r\n";
		using var reader = new StringReader(content);
		var manifest = ApplicationManifest.Load(reader);
		Assert.Equal("App", manifest.Name);
		Assert.Equal(name, manifest.Editions.Current.Name);
		using var stream = new MemoryStream();
		manifest.Save(stream);
		Assert.Equal(Encoding.UTF8.GetBytes(Output(content)), stream.ToArray());
		stream.Position = 0;
		Assert.Equal(manifest.Editions.Current, ApplicationManifest.Load(stream).Editions.Current);
	}

	[Theory]
	[InlineData("App=Missing\n[Community]\n1.0")]
	[InlineData("; comment\n\nApp=Missing")]
	[InlineData("App=Pro[\n[Professional]\n1.0")]
	[InlineData("App=Pro\0\n[Professional]\n1.0")]
	[InlineData("App=Pro\uFEFF\n[Professional]\n1.0")]
	[InlineData("App@1.0=Professional")]
	[InlineData("=Professional\n[Professional]\n1.0")]
	public void Current_LoadRejectsInvalidSelection(string content)
	{
		using var reader = new StringReader(content);
		Assert.Throws<FormatException>(() => ApplicationManifest.Load(reader));
	}

	[Fact]
	public void Edition_EqualityUsesNameAndVersion()
	{
		var edition = new Edition("Professional", new Version(1, 2));
		var same = new Edition("PROFESSIONAL", new Version(1, 2));
		var updated = new Edition("Professional", new Version(1, 3));
		var other = new Edition("Community", new Version(1, 2));
		Assert.True(edition.Equals(same));
		Assert.True(edition.Equals((object)same));
		Assert.True(edition == same);
		Assert.False(edition != same);
		Assert.Equal(edition.GetHashCode(), same.GetHashCode());
		Assert.NotEqual(edition, updated);
		Assert.NotEqual(edition, other);
		Assert.NotEqual(default(Edition), edition);
		Assert.False(edition.Equals(null));
		Assert.False(edition.Equals("Professional"));
		Assert.Equal(4, new HashSet<Edition> { edition, same, updated, other, default(Edition), default(Edition) }.Count);
	}

	[Fact]
	public void Current_SelectsExistingItemAndCanBeCleared()
	{
		var editions = new ApplicationManifest("App").Editions;
		Assert.Equal(default(Edition), editions.Current);
		editions.Add(new("Professional", new Version(1, 0)));
		Assert.Equal(default(Edition), editions.Current);
		editions.Current = new("PROFESSIONAL", new Version(1, 0));
		Assert.Equal("Professional", editions.Current.Name);
		Assert.Equal(editions[0], editions.Current);
		Assert.Throws<ArgumentException>("value", () => editions.Current = new("Missing", new Version(1, 0)));
		Assert.Equal(editions[0], editions.Current);
		Assert.Throws<ArgumentException>("value", () => editions.Current = new("Professional", new Version(2, 0)));
		Assert.Equal(editions[0], editions.Current);
		editions.Insert(0, new("Community", new Version(1, 0)));
		Assert.Equal(editions[1], editions.Current);
		editions.Current = default(Edition);
		Assert.Equal(default(Edition), editions.Current);
		Assert.Equal(2, editions.Count);
	}

	[Fact]
	public void Current_FollowsSameNameReplacementAcrossViews()
	{
		var editions = new ApplicationManifest("App").Editions;
		editions.Add(new("Professional", new Version(1, 0)));
		editions.Add(new("Community", new Version(1, 0)));
		editions.Current = editions[0];
		editions[0] = new("PROFESSIONAL", new Version(2, 0));
		Assert.Equal("PROFESSIONAL", editions.Current.Name);
		Assert.Equal(new Version(2, 0), editions.Current.Version);
		((IList<Edition>)editions)[0] = new("Professional", new Version(3, 0));
		Assert.Equal(new Version(3, 0), editions.Current.Version);
		((IList)editions)[0] = new Edition("professional", new Version(4, 0));
		Assert.Equal(new Version(4, 0), editions.Current.Version);
		((KeyedCollection<string, Edition>)editions)[0] = new("Professional", new Version(5, 0));
		Assert.Equal(new Version(5, 0), editions.Current.Version);
		editions[1] = new("Enterprise", new Version(6, 0));
		Assert.Equal(editions[0], editions.Current);
		editions[0] = new("Community", new Version(7, 0));
		Assert.Equal(default(Edition), editions.Current);
		Assert.False(editions.Contains("Professional"));
		Assert.Equal(new Version(7, 0), editions["Community"].Version);
	}

	[Theory]
	[InlineData("Community")]
	[InlineData("COMMUNITY")]
	[InlineData(" \tcommunity \t")]
	public void Current_StringSelectsCompleteStoredEdition(string name)
	{
		var manifest = new ApplicationManifest("App");
		manifest.Editions.Add(new("Professional", new Version(2, 0)));
		manifest.Editions.Add(new("Community", new Version(1, 0)));
		manifest.Editions.Current = name;

		Assert.Equal("Community", manifest.Editions.Current.Name);
		Assert.Equal(new Version(1, 0), manifest.Editions.Current.Version);
		Assert.Null(manifest.Version);
		using var writer = new StringWriter();
		manifest.Save(writer);
		Assert.StartsWith("App=Community" + writer.NewLine, writer.ToString());
		using var reader = new StringReader(writer.ToString());
		Assert.Equal(manifest.Editions.Current, ApplicationManifest.Load(reader).Editions.Current);

		manifest.Editions[1] = new("Community", new Version(1, 1));
		Assert.Equal(new Version(1, 1), manifest.Editions.Current.Version);
		manifest.Editions.Current = null;
		Assert.Equal(default(Edition), manifest.Editions.Current);
		Assert.Equal(2, manifest.Editions.Count);
	}

	[Theory]
	[InlineData("", "name")]
	[InlineData(" \t", "name")]
	[InlineData("[Community]", "value")]
	public void Current_InvalidStringLeavesSelectionUnchanged(string name, string parameter)
	{
		var editions = new ApplicationManifest("App").Editions;
		editions.Add(new("Community", new Version(1, 0)));
		editions.Current = "Community";
		Assert.Throws<ArgumentException>(parameter, () => editions.Current = name);
		Assert.Equal(editions[0], editions.Current);
		Assert.Throws<ArgumentException>("value", () => editions.Current = "Missing");
		Assert.Equal(editions[0], editions.Current);
	}

	[Fact]
	public void Edition_NameReferenceCannotBeStoredAsAnEdition()
	{
		Edition reference = " Community ";
		Assert.Equal("Community", reference.Name);
		Assert.Null(reference.Version);
		Assert.Equal("Community", reference.ToString());
		Assert.Equal(string.Empty, default(Edition).ToString());
		Assert.Equal("Community@1.0", new Edition("Community", new Version(1, 0)).ToString());

		var editions = new ApplicationManifest("App").Editions;
		editions.Add(new("Community", new Version(1, 0)));
		editions.Current = reference;
		Assert.Throws<ArgumentException>("item", () => editions.Add(reference));
		Assert.Throws<ArgumentException>("item", () => editions.Insert(0, reference));
		Assert.Throws<ArgumentException>("item", () => ((IList)editions)[0] = reference);
		Assert.Single(editions);
		Assert.Equal(new Version(1, 0), editions[0].Version);
		Assert.Equal(editions[0], editions.Current);
	}

	[Theory]
	[InlineData("item")]
	[InlineData("key")]
	[InlineData("index")]
	[InlineData("collection")]
	[InlineData("list")]
	[InlineData("clear")]
	public void Current_ClearsWhenRemovedThroughAnyView(string operation)
	{
		var editions = new ApplicationManifest("App").Editions;
		editions.Add(new("Community", new Version(1, 0)));
		editions.Add(new("Professional", new Version(2, 0)));
		editions.Current = editions[1];
		var item = new Edition("PROFESSIONAL", new Version(2, 0));
		switch(operation)
		{
			case "item":
				Assert.True(editions.Remove(item));
				break;
			case "key":
				Assert.True(((KeyedCollection<string, Edition>)editions).Remove(item.Name));
				break;
			case "index":
				editions.RemoveAt(1);
				break;
			case "collection":
				Assert.True(((ICollection<Edition>)editions).Remove(item));
				break;
			case "list":
				((IList)editions).Remove(item);
				break;
			case "clear":
				((Collection<Edition>)editions).Clear();
				break;
		}
		Assert.Equal(default(Edition), editions.Current);
		Assert.False(editions.Contains("Professional"));
		Assert.Equal(operation == "clear" ? 0 : 1, editions.Count);
		editions.Add(item);
		Assert.Equal(default(Edition), editions.Current);
	}

	[Fact]
	public void Current_UnrelatedAndFailedChangesPreserveSelection()
	{
		var editions = new ApplicationManifest("App").Editions;
		editions.Add(new("Professional", new Version(1, 0)));
		editions.Add(new("Community", new Version(2, 0)));
		editions.Current = editions[0];
		var expected = editions.ToArray();
		Assert.Throws<ArgumentException>(() => ((IList<Edition>)editions)[0] = new("COMMUNITY", new Version(3, 0)));
		Assert.Throws<ArgumentException>("item", () => ((IList)editions)[0] = default(Edition));
		Assert.Throws<ArgumentException>(() => editions.Insert(0, new("PROFESSIONAL", new Version(4, 0))));
		Assert.Throws<ArgumentException>("item", () => ((IList<Edition>)editions).Insert(0, default(Edition)));
		Assert.Throws<ArgumentOutOfRangeException>(() => editions[-1] = new("Enterprise", new Version(1, 0)));
		Assert.Throws<ArgumentOutOfRangeException>(() => editions.RemoveAt(2));
		Assert.False(editions.Remove(new Edition("Professional", new Version(5, 0))));
		Assert.False(editions.Remove("Missing"));
		Assert.Equal(expected, editions.ToArray());
		Assert.Equal(expected[0], editions.Current);
		Assert.True(editions.Remove("Community"));
		Assert.Equal(expected[0], editions.Current);
	}

	[Fact]
	public void Editions_EnforceVersionConflictThroughInterfaces()
	{
		var manifest = new ApplicationManifest("App", new Version(1, 0));
		var item = new Edition("Professional", new Version(2, 0));
		Assert.Throws<InvalidOperationException>(() => ((ICollection<Edition>)manifest.Editions).Add(item));
		Assert.Throws<InvalidOperationException>(() => ((IList)manifest.Editions).Insert(0, item));
		Assert.Empty(manifest.Editions);
		Assert.Equal(default(Edition), manifest.Editions.Current);
		Assert.Equal(new Version(1, 0), manifest.Version);
	}

	[Fact]
	public void Editions_UseKeyedCollectionLookupAndEquality()
	{
		var editions = new ApplicationManifest("App").Editions;
		editions.Add(new("Professional", new Version(1, 0)));
		var item = new Edition("PROFESSIONAL", new Version(1, 0));
		KeyedCollection<string, Edition> keyed = editions;
		Assert.Throws<ArgumentNullException>("key", () => editions.TryGetValue(null, out _));
		Assert.True(keyed.Contains(item.Name));
		Assert.Equal(0, ((IList<Edition>)editions).IndexOf(item));
		Assert.Equal(0, ((IList)editions).IndexOf(item));
#pragma warning disable xUnit2017
		Assert.True(keyed.Contains(item));
		Assert.True(((ICollection<Edition>)editions).Contains(item));
#pragma warning restore xUnit2017
	}

	[Fact]
	public void Editions_AddNameAndVersionReturnsStoredEdition()
	{
		var manifest = new ApplicationManifest("App");
		var edition = manifest.Editions.Add(" Community ", new Version(1, 0));
		Assert.Equal("Community", edition.Name);
		Assert.Equal(new Version(1, 0), edition.Version);
		Assert.Equal(edition, manifest.Editions[0]);
		Assert.Equal(edition, manifest.Editions["community"]);
		Assert.Equal(default(Edition), manifest.Editions.Current);

		Assert.Throws<ArgumentException>(() => manifest.Editions.Add("COMMUNITY", new Version(2, 0)));
		Assert.Throws<ArgumentNullException>("name", () => manifest.Editions.Add(null, new Version(1, 0)));
		Assert.Throws<ArgumentNullException>("version", () => manifest.Editions.Add("Professional", null));
		Assert.Single(manifest.Editions);
		Assert.Equal(edition, manifest.Editions[0]);

		manifest.Editions.Clear();
		manifest.Version = new Version(3, 0);
		Assert.Throws<InvalidOperationException>(() => manifest.Editions.Add("Community", new Version(1, 0)));
		Assert.Empty(manifest.Editions);
		Assert.Equal(new Version(3, 0), manifest.Version);
	}

	[Theory]
	[InlineData("#@import editions.ini")]
	[InlineData(";@IMPORT editions.ini")]
	[InlineData(" #@import missing.ini")]
	[InlineData("#@import")]
	public void Load_RejectsImportsBeforeOpeningFiles(string directive)
	{
		var imported = this.GetPath("editions.ini");
		File.WriteAllText(imported, "[Community]\r\n1.0\r\n", new UTF8Encoding(false));
		var path = this.Write("App@1.0\r\n" + directive + "\r\n");
		using var locked = new FileStream(imported, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
		var exception = Assert.Throws<FormatException>(() => ApplicationManifest.Load(_directory));
		Assert.IsType<Zongsoft.Configuration.Profiles.ProfileException>(exception.InnerException);
		using var stream = File.OpenRead(path);
		Assert.Throws<FormatException>(() => ApplicationManifest.Load(stream));
		Assert.True(stream.CanRead);
		using var reader = new StringReader("App@1.0\n" + directive);
		Assert.Throws<FormatException>(() => ApplicationManifest.Load(reader));
		Assert.Equal(-1, reader.Peek());
	}

	[Fact]
	public void Save_PreservesCurrentFileLayoutWhenValuesChange()
	{
		var path = this.Write("# original\r\nApp=Community\r\n\r\n[Community]\r\n1.0\r\n");
		var manifest = ApplicationManifest.Load(_directory);
		File.WriteAllText(path, "\r\n; updated notes\r\nApp=Community\r\n\r\n\r\n[Community]\r\n# release\r\n1.0\r\n\r\n# end\r\n\r\n", new UTF8Encoding(false));
		manifest.Editions[0] = new("COMMUNITY", new Version(2, 0));
		manifest.Save(_directory);
		const string EXPECTED = "\r\n# updated notes\r\nApp=COMMUNITY\r\n\r\n\r\n[COMMUNITY]\r\n# release\r\n2.0\r\n\r\n# end\r\n\r\n";
		Assert.Equal(Encoding.UTF8.GetBytes(Output(EXPECTED)), File.ReadAllBytes(path));
		Assert.Equal(new Version(2, 0), ApplicationManifest.Load(path).Editions.Current.Version);
		manifest.Save(path);
		Assert.Equal(Output(EXPECTED), File.ReadAllText(path));
	}

	[Theory]
	[InlineData("file")]
	[InlineData("stream")]
	[InlineData("writer")]
	public void Save_PreservesLoadedLayoutAcrossOutputs(string output)
	{
		const string CONTENT = "\r\n# notes\r\n\r\nApp@1.0\r\n\r\n# trailing\r\n\r\n";
		using var reader = new StringReader(CONTENT);
		var manifest = ApplicationManifest.Load(reader);
		manifest.Version = new Version(2, 0);
		var expected = CONTENT.Replace("App@1.0", "App@2.0").Replace("\r\n", output == "writer" ? "\n" : Environment.NewLine);

		if(output == "file")
		{
			var path = this.GetPath("copy.edition");
			File.WriteAllText(path, string.Empty);
			manifest.Save(path);
			Assert.Equal(Encoding.UTF8.GetBytes(expected), File.ReadAllBytes(path));
		}
		else if(output == "stream")
		{
			using var stream = new MemoryStream();
			manifest.Save(stream);
			Assert.Equal(Encoding.UTF8.GetBytes(expected), stream.ToArray());
			Assert.True(stream.CanWrite);
		}
		else
		{
			using var writer = new StringWriter { NewLine = "\n" };
			manifest.Save(writer);
			Assert.Equal(expected, writer.ToString());
			Assert.Equal("\n", writer.NewLine);
		}
	}

	[Fact]
	public void Save_StructuralChangesKeepCommentsAndBlankLines()
	{
		const string CONTENT = "# root\nApp=Community\n\n[Community]\n# community\n1.0\n\n[Professional]\n# professional\n2.0\n\n";
		using var reader = new StringReader(CONTENT);
		var manifest = ApplicationManifest.Load(reader);
		manifest.Editions.Remove("Community");
		manifest.Editions.Insert(0, new("Enterprise", new Version(3, 0)));
		using var writer = new StringWriter();
		manifest.Save(writer);
		var saved = writer.ToString();
		Assert.Contains(Output("# root\r\n"), saved);
		Assert.Contains(Output("# community\r\n"), saved);
		Assert.Contains(Output("# professional\r\n"), saved);
		using var savedReader = new StringReader(saved);
		var loaded = ApplicationManifest.Load(savedReader);
		Assert.Equal(new[] { "Enterprise", "Professional" }, loaded.Editions.Select(edition => edition.Name));
		Assert.Equal(default(Edition), loaded.Editions.Current);
		Assert.Equal(3, saved.Split(Environment.NewLine).Count(line => line.Length == 0) - 1);

		manifest.Editions.Clear();
		manifest.Version = new Version(4, 0);
		using var singleWriter = new StringWriter();
		manifest.Save(singleWriter);
		var single = singleWriter.ToString();
		Assert.Contains(Output("App@4.0\r\n"), single);
		Assert.Contains(Output("# community\r\n"), single);
		Assert.Contains(Output("# professional\r\n"), single);
		using var singleReader = new StringReader(single);
		Assert.Empty(ApplicationManifest.Load(singleReader).Editions);
	}

	[Fact]
	public void Save_RejectsExistingImportBeforeTruncatingFile()
	{
		const string CONTENT = "# notes\r\nApp@1.0\r\n#@import missing.ini\r\n";
		var path = this.Write(CONTENT);
		Assert.Throws<FormatException>(() => new ApplicationManifest("App", new Version(2, 0)).Save(path));
		Assert.Equal(CONTENT, File.ReadAllText(path));
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void Save_ProfileValidationPreservesExistingFile(bool singleVersion)
	{
		const string CONTENT = "# existing notes\nOriginal@1.0\n";
		var path = this.Write(CONTENT);
		var manifest = new ApplicationManifest(singleVersion ? "App=Name" : "App", singleVersion ? new Version(2, 0) : null);

		if(!singleVersion)
			manifest.Editions.Add("Professional Preview", new Version(2, 0));

		Assert.Throws<Zongsoft.Configuration.Profiles.ProfileException>(() => manifest.Save(path));
		Assert.Equal(CONTENT, File.ReadAllText(path));
	}

	[Fact]
	public void Save_OrdinaryCommentsKeepImportLikeText()
	{
		const string CONTENT = "#@imported is a note\r\n# @import is also a note\r\nApp@1.0\r\n";
		using var reader = new StringReader(CONTENT);
		var manifest = ApplicationManifest.Load(reader);
		using var writer = new StringWriter();
		manifest.Save(writer);
		Assert.Equal(Output(CONTENT), writer.ToString());
	}

	[Fact]
	public void Load_RejectsNestedEditionsAndKeyValueVersions()
	{
		Assert.Throws<FormatException>(() => ApplicationManifest.Load(this.Write("App\n[Professional Preview]\n1.0")));
		Assert.Throws<FormatException>(() => ApplicationManifest.Load(this.Write("App\n[Professional]\n1.0=")));
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
		public override string ReadLine() => throw this.Error;
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
		public int WriteLineCount { get; private set; }
		public override void WriteLine()
		{
			this.WriteCount++;
			this.WriteLineCount++;
			if(this.FailWrite)
				throw this.Error;
			base.WriteLine();
		}
		public override void WriteLine(string value)
		{
			this.WriteCount++;
			this.WriteLineCount++;
			if(this.FailWrite)
				throw this.Error;
			base.WriteLine(value);
		}
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

	private static string Output(string content) => content.Replace("\r\n", Environment.NewLine);

	private string GetPath(string name = ".edition") => System.IO.Path.Combine(_directory, name);
	private string Write(string content)
	{
		var path = this.GetPath();
		File.WriteAllText(path, content, new UTF8Encoding(false));
		return path;
	}
}
