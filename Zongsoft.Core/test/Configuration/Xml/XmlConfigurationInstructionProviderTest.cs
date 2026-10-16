using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

using Zongsoft.Configuration.Xml;

using Xunit;

namespace Zongsoft.Configuration.Tests;

public class XmlConfigurationInstructionProviderTest
{
	[Theory]
	[InlineData(null)]
	[InlineData("supplied.option")]
	public void StreamBuilder_CollectsInstructionsByDefault(string filePath)
	{
		using var stream = CreateStream("first", "one");
		var builder = new ConfigurationBuilder();

		if(filePath == null)
			builder.AddOptionStream(stream);
		else
			builder.AddOptionStream(stream, filePath);

		using var configuration = (ConfigurationRoot)builder.Build();
		var provider = Assert.IsType<XmlStreamConfigurationProvider>(Assert.Single(configuration.Providers));

		var instruction = Assert.Single(provider.Instructions);

		Assert.Equal("first", instruction.Name);
		Assert.Equal("value='one'", instruction.Data);
		Assert.Equal(filePath ?? string.Empty, instruction.FilePath);
		Assert.Equal(string.Empty, instruction.Path);
		Assert.Equal("one", configuration["Example:item:value"]);
		Assert.True(stream.CanRead);
	}

	[Fact]
	public void StreamLoad_ReplacesSnapshotsOnlyAfterSuccess()
	{
		var provider = new XmlStreamConfigurationProvider(new XmlStreamConfigurationSource { FilePath = "stream.option" });
		Assert.Empty(provider.Instructions);
		using var first = CreateStream("first", "one");
		provider.Load(first);
		var original = provider.Instructions;

		using var second = CreateStream("second", "two");
		provider.Load(second);
		var current = provider.Instructions;

		Assert.NotSame(original, current);
		Assert.Equal("first", Assert.Single(original).Name);
		Assert.Equal("value='one'", original[0].Data);
		Assert.Equal("second", Assert.Single(current).Name);
		Assert.Equal("stream.option", current[0].FilePath);
		Assert.True(provider.TryGet("Example:item:value", out var value));
		Assert.Equal("two", value);

		if(current is IList<XmlConfigurationInstruction> list)
		{
			Assert.True(list.IsReadOnly);
			Assert.Throws<NotSupportedException>(() => list[0] = original[0]);
			Assert.Throws<NotSupportedException>(list.Clear);
		}

		using var invalid = new MemoryStream(Encoding.UTF8.GetBytes("<?unfinished?><configuration><option>"));
		Assert.Throws<System.Xml.XmlException>(() => provider.Load(invalid));
		Assert.Same(current, provider.Instructions);
		Assert.True(provider.TryGet("Example:item:value", out value));
		Assert.Equal("two", value);
		Assert.True(invalid.CanRead);

		using var empty = new MemoryStream(Encoding.UTF8.GetBytes("<configuration />"));
		provider.Load(empty);
		Assert.Empty(provider.Instructions);
		Assert.False(provider.TryGet("Example:item:value", out _));
		Assert.Equal("second", Assert.Single(current).Name);
		Assert.True(first.CanRead);
		Assert.True(second.CanRead);
	}

	[Fact]
	public void Read_FileStreamInfersSourceUnlessExplicitlyOverridden()
	{
		var path = Path.GetTempFileName();

		try
		{
			File.WriteAllText(path, "<?zongsoft.tools.binding path='Example'?><configuration />");
			using var stream = File.OpenRead(path);

			XmlStreamConfigurationProvider.Read(stream, out var inferred);
			Assert.Equal(stream.Name, Assert.Single(inferred).FilePath);
			stream.Position = 0;
			XmlStreamConfigurationProvider.Read(stream, out var explicitSource, "virtual.option");
			Assert.Equal("virtual.option", Assert.Single(explicitSource).FilePath);
			Assert.Equal(path, inferred[0].FilePath);
			Assert.True(stream.CanRead);
		}
		finally
		{
			File.Delete(path);
		}
	}

	[Fact]
	public void FileBuilder_CollectsInstructionsByDefault()
	{
		var path = Path.GetTempFileName();

		try
		{
			File.WriteAllText(path, "<?zongsoft.tools.binding path='Example'?><configuration><option path='Example'><item value='loaded' /></option></configuration>");
			using var configuration = (ConfigurationRoot)new ConfigurationBuilder().AddOptionFile(path).Build();
			var provider = Assert.IsType<XmlConfigurationProvider>(Assert.Single(configuration.Providers));

			var instruction = Assert.Single(provider.Instructions);

			Assert.Equal("zongsoft.tools.binding", instruction.Name);
			Assert.Equal(path, instruction.FilePath);
			Assert.Equal("loaded", configuration["Example:item:value"]);
			Assert.Equal("Example", instruction.GetAttributes()["PATH"]);
		}
		finally
		{
			File.Delete(path);
		}
	}

	[Fact]
	public void FileReload_ReplacesSnapshotsAndClearsMissingOrInvalidData()
	{
		using var files = new ReloadFileProvider("<?first?><configuration><option path='Example'><item value='one' /></option></configuration>");
		var errors = new List<Exception>();
		using var configuration = (ConfigurationRoot)new ConfigurationBuilder().AddOptionFile(source =>
		{
			source.FileProvider = files;
			source.Path = "memory.option";
			source.ReloadOnChange = true;
			source.ReloadDelay = 0;
			source.OnLoadException = context =>
			{
				errors.Add(context.Exception);
				context.Ignore = true;
			};
		}).Build();
		var provider = Assert.IsType<XmlConfigurationProvider>(Assert.Single(configuration.Providers));
		var original = provider.Instructions;
		Assert.Equal("first", Assert.Single(original).Name);
		Assert.Equal("memory.option", original[0].FilePath);
		Assert.Equal("one", configuration["Example:item:value"]);

		var reload = configuration.GetReloadToken();
		files.Change("<?second?><configuration><option path='Example'><item value='two' /></option></configuration>");
		var current = provider.Instructions;
		Assert.True(reload.HasChanged);
		Assert.NotSame(original, current);
		Assert.Equal("second", Assert.Single(current).Name);
		Assert.Equal("two", configuration["Example:item:value"]);
		Assert.Equal("first", original[0].Name);

		files.Change("<?partial?><configuration><option>");
		Assert.IsType<InvalidDataException>(Assert.Single(errors));
		Assert.Empty(provider.Instructions);
		Assert.Null(configuration["Example:item:value"]);
		Assert.Equal("first", original[0].Name);
		Assert.Equal("second", Assert.Single(current).Name);

		files.Change("<?intermediate?><configuration><option path='Example'><item value='ready' /></option></configuration>");
		Assert.Equal("intermediate", Assert.Single(provider.Instructions).Name);
		Assert.Equal("ready", configuration["Example:item:value"]);
		files.Change(null);
		Assert.Empty(provider.Instructions);
		Assert.Null(configuration["Example:item:value"]);
		Assert.Equal("second", Assert.Single(current).Name);

		files.Change("<?recovered?><configuration><option path='Example'><item value='three' /></option></configuration>");
		Assert.Equal("recovered", Assert.Single(provider.Instructions).Name);
		Assert.Equal("three", configuration["Example:item:value"]);
		Assert.Single(errors);
	}

	[Fact]
	public void FileLoad_MissingOptionalFileClearsInstructions()
	{
		using var files = new ReloadFileProvider("<?first?><configuration><option path='Example'><item value='one' /></option></configuration>");
		using var provider = new XmlConfigurationProvider(new XmlConfigurationSource
		{
			FileProvider = files,
			Path = "memory.option",
			Optional = true,
		});
		provider.Load();
		var original = provider.Instructions;

		files.Change(null);
		provider.Load();

		Assert.Empty(provider.Instructions);
		Assert.False(provider.TryGet("Example:item:value", out _));
		Assert.Equal("first", Assert.Single(original).Name);
	}

	private static MemoryStream CreateStream(string name, string value) => new(Encoding.UTF8.GetBytes(
		$"<?{name} value='{value}'?><configuration><option path='Example'><item value='{value}' /></option></configuration>"));

	private sealed class ReloadFileProvider(string content) : IFileProvider, IDisposable
	{
		private string _content = content;
		private CancellationTokenSource _change = new();

		public IFileInfo GetFileInfo(string subpath) => new MemoryFile(subpath, _content);
		public IDirectoryContents GetDirectoryContents(string subpath) => NotFoundDirectoryContents.Singleton;
		public IChangeToken Watch(string filter) => new CancellationChangeToken(_change.Token);

		public void Change(string content)
		{
			_content = content;
			var previous = _change;
			_change = new();
			previous.Cancel();
			previous.Dispose();
		}

		public void Dispose() => _change.Dispose();

		private sealed class MemoryFile(string name, string content) : IFileInfo
		{
			public bool Exists => content != null;
			public long Length => content == null ? -1 : Encoding.UTF8.GetByteCount(content);
			public string PhysicalPath => null;
			public string Name => name;
			public DateTimeOffset LastModified => DateTimeOffset.UnixEpoch;
			public bool IsDirectory => false;
			public Stream CreateReadStream() => new MemoryStream(Encoding.UTF8.GetBytes(content));
		}
	}
}
