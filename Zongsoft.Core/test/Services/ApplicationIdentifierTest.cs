using System;
using System.IO;
using System.Text;

using Xunit;

namespace Zongsoft.Services.Tests;

public class ApplicationIdentifierTest : IDisposable
{
	private static readonly string _root = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Zongsoft.ApplicationIdentifier.Tests"));
	private readonly string _directory = System.IO.Path.Combine(_root, Guid.NewGuid().ToString("N"));

	public void Dispose()
	{
		var directory = System.IO.Path.GetFullPath(_directory);
		if(!string.Equals(System.IO.Path.GetDirectoryName(directory), _root, StringComparison.OrdinalIgnoreCase))
			throw new InvalidOperationException("The test directory must remain an immediate child of its temporary root.");
		if(Directory.Exists(directory))
			Directory.Delete(directory, true);
	}

	[Fact]
	public void TryParse()
	{
		Assert.False(ApplicationIdentifier.TryParse("", out _));
		Assert.False(ApplicationIdentifier.TryParse(" ", out _));
		Assert.False(ApplicationIdentifier.TryParse("\t", out _));

		Assert.True(ApplicationIdentifier.TryParse("name:edition@1.0.0", out var identifier));
		Assert.Equal("name", identifier.Name);
		Assert.Equal("edition", identifier.Edition);
		Assert.Equal(new Version(1, 0, 0), identifier.Version);
		Assert.Equal("name-edition@1.0.0", identifier.ToString());

		Assert.True(ApplicationIdentifier.TryParse("name-edition@1.0.0", out identifier));
		Assert.Equal("name", identifier.Name);
		Assert.Equal("edition", identifier.Edition);
		Assert.Equal(new Version(1, 0, 0), identifier.Version);
		Assert.Equal("name-edition@1.0.0", identifier.ToString());

		Assert.True(ApplicationIdentifier.TryParse("name(edition)@1.1.0", out identifier));
		Assert.Equal("name", identifier.Name);
		Assert.Equal("edition", identifier.Edition);
		Assert.Equal(new Version(1, 1, 0), identifier.Version);
		Assert.Equal("name-edition@1.1.0", identifier.ToString());

		Assert.True(ApplicationIdentifier.TryParse("name:edition", out identifier));
		Assert.Equal("name", identifier.Name);
		Assert.Equal("edition", identifier.Edition);
		Assert.Null(identifier.Version);
		Assert.Equal("name-edition", identifier.ToString());

		Assert.True(ApplicationIdentifier.TryParse("name-edition", out identifier));
		Assert.Equal("name", identifier.Name);
		Assert.Equal("edition", identifier.Edition);
		Assert.Null(identifier.Version);
		Assert.Equal("name-edition", identifier.ToString());

		Assert.True(ApplicationIdentifier.TryParse("name(edition)", out identifier));
		Assert.Equal("name", identifier.Name);
		Assert.Equal("edition", identifier.Edition);
		Assert.Null(identifier.Version);
		Assert.Equal("name-edition", identifier.ToString());

		Assert.True(ApplicationIdentifier.TryParse("name", out identifier));
		Assert.Equal("name", identifier.Name);
		Assert.Null(identifier.Edition);
		Assert.Null(identifier.Version);
		Assert.Equal("name", identifier.ToString());

		Assert.True(ApplicationIdentifier.TryParse("1.2.3.4", out identifier));
		Assert.Null(identifier.Name);
		Assert.Null(identifier.Edition);
		Assert.Equal(new Version(1, 2, 3, 4), identifier.Version);
		Assert.Equal("1.2.3.4", identifier.ToString());

		Assert.True(ApplicationIdentifier.TryParse("name@1.2.3.4", out identifier));
		Assert.Equal("name", identifier.Name);
		Assert.Null(identifier.Edition);
		Assert.Equal(new Version(1, 2, 3, 4), identifier.Version);
		Assert.Equal("name@1.2.3.4", identifier.ToString());
	}
	[Fact]
	public void StreamOverloads_RejectNullArguments()
	{
		var identifier = new ApplicationIdentifier("App", new Version(1, 0));

		Assert.Throws<ArgumentNullException>("stream", () => ApplicationIdentifier.Load((Stream)null));
		Assert.Throws<ArgumentNullException>("reader", () => ApplicationIdentifier.Load((TextReader)null));
		Assert.Throws<ArgumentNullException>("stream", () => identifier.Save((Stream)null));
		Assert.Throws<ArgumentNullException>("writer", () => identifier.Save((TextWriter)null));
	}

	[Fact]
	public void Load_TextReaderStopsAfterFirstNonblankLine()
	{
		using var reader = new StringReader("skip\n \t\r\n\n App-Community@1.2.3 \r\ninvalid@version\n");
		Assert.Equal("skip", reader.ReadLine());

		var identifier = ApplicationIdentifier.Load(reader);

		Assert.Equal("App", identifier.Name);
		Assert.Equal("Community", identifier.Edition);
		Assert.Equal(new Version(1, 2, 3), identifier.Version);
		Assert.Equal("invalid@version", reader.ReadLine());
		Assert.Equal(-1, reader.Peek());
	}

	[Theory]
	[InlineData("utf-8", false)]
	[InlineData("utf-8", true)]
	[InlineData("utf-16", true)]
	public void Load_StreamReadsCurrentPositionAndDetectsBom(string encodingName, bool bom)
	{
		using var stream = new MemoryStream();
		var encoding = Encoding.GetEncoding(encodingName);
		var prefix = Encoding.ASCII.GetBytes("invalid prefix");
		stream.Write(prefix);
		if(bom)
			stream.Write(encoding.GetPreamble());
		stream.Write(encoding.GetBytes("\r\n 应用-社区版@1.2.3\ninvalid@version"));
		stream.Position = prefix.Length;

		var identifier = ApplicationIdentifier.Load(stream);

		Assert.Equal("应用", identifier.Name);
		Assert.Equal("社区版", identifier.Edition);
		Assert.Equal(new Version(1, 2, 3), identifier.Version);
		Assert.True(stream.CanRead);
		stream.Position = 0;
		Assert.Equal((int)'i', stream.ReadByte());
	}

	[Theory]
	[InlineData("")]
	[InlineData(" \r\n\t\n")]
	public void Load_EmptyInputsReturnDefaultAndStayOpen(string content)
	{
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
		using var reader = new StringReader(content);

		Assert.Equal(default, ApplicationIdentifier.Load(stream));
		Assert.True(stream.CanRead);
		Assert.Equal(default, ApplicationIdentifier.Load(reader));
		Assert.Equal(-1, reader.Peek());
	}

	[Fact]
	public void Load_InvalidFirstLineLeavesInputsOpen()
	{
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes("invalid@version\nApp@1.0"));
		using var reader = new StringReader("\ninvalid@version\nApp@1.0");

		Assert.Throws<FormatException>(() => ApplicationIdentifier.Load(stream));
		Assert.True(stream.CanRead);
		Assert.Throws<FormatException>(() => ApplicationIdentifier.Load(reader));
		Assert.Equal("App@1.0", reader.ReadLine());
	}

	[Fact]
	public void Load_IoFailuresLeaveInputsOpen()
	{
		using var stream = new ObservedStream(Encoding.UTF8.GetBytes("App@1.0")) { FailRead = true };
		using var reader = new FailingReader();

		Assert.Same(stream.Error, Assert.Throws<IOException>(() => ApplicationIdentifier.Load(stream)));
		Assert.False(stream.IsDisposed);
		stream.FailRead = false;
		Assert.Equal((int)'A', stream.ReadByte());
		Assert.Same(reader.Error, Assert.Throws<IOException>(() => ApplicationIdentifier.Load(reader)));
		Assert.False(reader.IsDisposed);
	}

	[Fact]
	public void Load_NonSeekableStreamHasNoFileSizeLimit()
	{
		var name = new string('A', 32768);
		var content = name + "@1.2.3\ninvalid@version";
		using var stream = new ObservedStream(Encoding.UTF8.GetBytes(content));
		using var reader = new StringReader(content);

		var identifier = ApplicationIdentifier.Load(stream);
		Assert.Equal(name, identifier.Name);
		Assert.Equal(new Version(1, 2, 3), identifier.Version);
		Assert.False(stream.IsDisposed);
		identifier = ApplicationIdentifier.Load(reader);
		Assert.Equal(name, identifier.Name);
		Assert.Equal(new Version(1, 2, 3), identifier.Version);
		Assert.Equal("invalid@version", reader.ReadLine());
	}

	[Theory]
	[InlineData("应用", "社区版", "1.2.3", "应用-社区版@1.2.3")]
	[InlineData("App", null, "1.2", "App@1.2")]
	[InlineData(null, null, "1.2.3.4", "1.2.3.4")]
	[InlineData("App", null, null, "App")]
	[InlineData("App", "Community", null, "App-Community")]
	public void Save_OverloadsPreserveIdentifierFormats(string name, string edition, string version, string expected)
	{
		var identifier = new ApplicationIdentifier(name, edition, version == null ? null : Version.Parse(version));
		using var stream = new MemoryStream();
		using var writer = new ObservedWriter { NewLine = "custom newline" };

		identifier.Save(stream);
		identifier.Save(writer);

		Assert.Equal(Encoding.UTF8.GetBytes(expected), stream.ToArray());
		Assert.Equal(expected, writer.ToString());
		Assert.Equal("custom newline", writer.NewLine);
		Assert.True(writer.FlushCount > 0);
		Assert.False(writer.IsDisposed);
		writer.Write('!');
		Assert.Equal(expected + "!", writer.ToString());
		Assert.True(stream.CanWrite);
	}

	[Fact]
	public void Save_StreamPreservesPrefixAndTail()
	{
		using var stream = new MemoryStream();
		stream.Write(Encoding.UTF8.GetBytes("prefix:xxxxxxxtail"));
		stream.Position = 7;

		new ApplicationIdentifier("App", new Version(1, 0)).Save(stream);

		Assert.Equal(Encoding.UTF8.GetBytes("prefix:App@1.0tail"), stream.ToArray());
		Assert.Equal(14, stream.Position);
		stream.WriteByte((byte)'T');
		Assert.Equal(Encoding.UTF8.GetBytes("prefix:App@1.0Tail"), stream.ToArray());
	}

	[Fact]
	public void Save_NonSeekableStreamFlushesAndLeavesOpen()
	{
		using var stream = new ObservedStream();

		new ApplicationIdentifier("应用", "社区版", new Version(1, 2, 3)).Save(stream);

		Assert.Equal(Encoding.UTF8.GetBytes("应用-社区版@1.2.3"), stream.ToArray());
		Assert.True(stream.FlushCount > 0);
		Assert.False(stream.IsDisposed);
		stream.WriteByte((byte)'!');
		Assert.Equal((byte)'!', stream.ToArray()[^1]);
	}

	[Fact]
	public void Save_TextWriterUsesCallerEncoding()
	{
		using var stream = new MemoryStream();
		using var writer = new StreamWriter(stream, new UnicodeEncoding(false, false), 1024, true) { NewLine = "custom newline" };

		new ApplicationIdentifier("应用", "社区版", new Version(1, 2)).Save(writer);

		Assert.Equal(Encoding.Unicode.GetBytes("应用-社区版@1.2"), stream.ToArray());
		Assert.Equal("custom newline", writer.NewLine);
		writer.Write('!');
		writer.Flush();
		Assert.Equal(Encoding.Unicode.GetBytes("应用-社区版@1.2!"), stream.ToArray());
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void Save_EmptyIdentifierDoesNotTouchOutputs(bool editionOnly)
	{
		var identifier = editionOnly ? new ApplicationIdentifier(null, "Community", null) : default;
		using var stream = new ObservedStream(Encoding.UTF8.GetBytes("original"));
		using var writer = new ObservedWriter { NewLine = "custom newline" };
		writer.Write("original");
		var writes = writer.WriteCount;

		identifier.Save(stream);
		identifier.Save(writer);

		Assert.Equal(Encoding.UTF8.GetBytes("original"), stream.ToArray());
		Assert.Equal(0, stream.WriteCount);
		Assert.Equal(0, stream.FlushCount);
		Assert.False(stream.IsDisposed);
		Assert.Equal((int)'o', stream.ReadByte());
		Assert.Equal("original", writer.ToString());
		Assert.Equal(writes, writer.WriteCount);
		Assert.Equal(0, writer.FlushCount);
		Assert.False(writer.IsDisposed);
		Assert.Equal("custom newline", writer.NewLine);
		writer.Write('!');
		Assert.Equal("original!", writer.ToString());
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void Save_IoFailuresLeaveOutputsOpen(bool flushFailure)
	{
		using var stream = new ObservedStream { FailWrite = !flushFailure, FailFlush = flushFailure };
		using var writer = new ObservedWriter { FailWrite = !flushFailure, FailFlush = flushFailure, NewLine = "custom newline" };
		var identifier = new ApplicationIdentifier("App", new Version(1, 0));

		Assert.Same(stream.Error, Assert.Throws<IOException>(() => identifier.Save(stream)));
		Assert.False(stream.IsDisposed);
		stream.FailWrite = false;
		stream.FailFlush = false;
		stream.WriteByte((byte)'!');
		Assert.Equal((byte)'!', stream.ToArray()[^1]);
		Assert.Same(writer.Error, Assert.Throws<IOException>(() => identifier.Save(writer)));
		Assert.False(writer.IsDisposed);
		Assert.Equal("custom newline", writer.NewLine);
		writer.FailWrite = false;
		writer.FailFlush = false;
		writer.Write('!');
		Assert.EndsWith("!", writer.ToString());
	}

	[Fact]
	public void DirectoryOperations_PreserveReturnValues()
	{
		Directory.CreateDirectory(_directory);
		var missing = System.IO.Path.Combine(_directory, "missing");
		var path = System.IO.Path.Combine(_directory, ".version");

		Assert.Equal(default, ApplicationIdentifier.Load(path: _directory));
		Assert.Null(ApplicationIdentifier.Save(path: missing, "App", null, new Version(1, 0)));
		Assert.False(Directory.Exists(missing));
		Assert.Null(ApplicationIdentifier.Save(path: _directory, null, null, null));
		Assert.False(File.Exists(path));

		Assert.Equal(path, ApplicationIdentifier.Save(path: _directory, "App", "Community", new Version(1, 2, 3)));
		Assert.Equal(Encoding.UTF8.GetBytes("App-Community@1.2.3"), File.ReadAllBytes(path));
		var identifier = ApplicationIdentifier.Load(path: _directory);
		Assert.Equal("App", identifier.Name);
		Assert.Equal("Community", identifier.Edition);
		Assert.Equal(new Version(1, 2, 3), identifier.Version);
		Assert.Null(ApplicationIdentifier.Save(path: _directory, null, null, null));
		Assert.Equal("App-Community@1.2.3", File.ReadAllText(path));
		Assert.Equal(default, ApplicationIdentifier.Load((IApplicationModule)null));
	}

	[Theory]
	[InlineData(16384, false)]
	[InlineData(16385, true)]
	public void Load_DirectoryKeepsFileSizeLimit(int size, bool empty)
	{
		Directory.CreateDirectory(_directory);
		var bytes = Encoding.UTF8.GetBytes("App@1.0\n" + new string(' ', size - 8));
		File.WriteAllBytes(System.IO.Path.Combine(_directory, ".version"), bytes);

		var identifier = ApplicationIdentifier.Load(path: _directory);

		Assert.Equal(empty, identifier.IsEmpty);
		if(empty)
			Assert.Equal(default, identifier);
		else
		{
			Assert.Equal("App", identifier.Name);
			Assert.Equal(new Version(1, 0), identifier.Version);
		}
	}

	[Fact]
	public void PathOperations_UseExistingFileDirectly()
	{
		Directory.CreateDirectory(_directory);
		var path = System.IO.Path.Combine(_directory, "deployment.identity");
		var sibling = System.IO.Path.Combine(_directory, ".version");
		File.WriteAllText(path, "Original-Community@1.0", new UTF8Encoding(false));
		File.WriteAllText(sibling, "Other@9.0", new UTF8Encoding(false));

		var identifier = ApplicationIdentifier.Load(path: path);
		Assert.Equal("Original", identifier.Name);
		Assert.Equal("Community", identifier.Edition);
		Assert.Equal(new Version(1, 0), identifier.Version);

		Assert.Equal(path, ApplicationIdentifier.Save(path: path, name: "Replacement", edition: "Enterprise", version: new Version(2, 3, 4)));
		Assert.Equal(Encoding.UTF8.GetBytes("Replacement-Enterprise@2.3.4"), File.ReadAllBytes(path));
		Assert.Equal("Other@9.0", File.ReadAllText(sibling));
		identifier = ApplicationIdentifier.Load(path: path);
		Assert.Equal("Replacement", identifier.Name);
		Assert.Equal("Enterprise", identifier.Edition);
		Assert.Equal(new Version(2, 3, 4), identifier.Version);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void PathOperations_MissingPathsAreNotCreated(bool missingParent)
	{
		Directory.CreateDirectory(_directory);
		var path = missingParent ?
			System.IO.Path.Combine(_directory, "missing", "deployment.identity") :
			System.IO.Path.Combine(_directory, "deployment.identity");

		Assert.Equal(default, ApplicationIdentifier.Load(path: path));
		Assert.Null(ApplicationIdentifier.Save(path: path, name: "App", edition: null, version: new Version(1, 0)));
		Assert.False(File.Exists(path));
		Assert.False(Directory.Exists(path));
		Assert.Empty(Directory.EnumerateFileSystemEntries(_directory));
	}

	[Theory]
	[InlineData(16384, false)]
	[InlineData(16385, true)]
	public void Load_FilePathKeepsFileSizeLimit(int size, bool empty)
	{
		Directory.CreateDirectory(_directory);
		var path = System.IO.Path.Combine(_directory, "deployment.identity");
		File.WriteAllBytes(path, Encoding.UTF8.GetBytes("App@1.0\n" + new string(' ', size - 8)));

		var identifier = ApplicationIdentifier.Load(path: path);

		Assert.Equal(empty, identifier.IsEmpty);
		if(empty)
			Assert.Equal(default, identifier);
		else
		{
			Assert.Equal("App", identifier.Name);
			Assert.Equal(new Version(1, 0), identifier.Version);
		}
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

}
