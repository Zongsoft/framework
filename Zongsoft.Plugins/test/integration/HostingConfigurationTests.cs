using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using Xunit;

using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Configuration;

namespace Zongsoft.Plugins.IntegrationTests;

public class HostingConfigurationTests
{
	[Theory]
	[InlineData("", false)]
	[InlineData("nested", false)]
	[InlineData("nested", true)]
	public void HostLoadsExactEntryRelativeToConfiguredBasePath(string directory, bool absolute)
	{
		using var scope = new HostTestDirectory();
		var builder = new Hosting.ApplicationBuilder(typeof(HostingConfigurationTests).Assembly.GetName().Name, [$"--contentRoot={scope.DirectoryPath}", "--environment=Development"]);
		using var configuration = builder.Configuration;
		configuration.SetBasePath(Path.Combine(scope.DirectoryPath, "plugins"));
		var name = Path.Combine(directory, "HostFixture");
		var expected = Path.GetFullPath(scope.WriteFile($"{name}.option", "<options><option path=\"Fixture\"><settings value=\"base\" inherited=\"retained\" /></option></options>"));
		scope.WriteFile($"{name}-default.option", Options("excluded"));

		builder.LoadConfiguration(absolute ? Path.Combine(scope.DirectoryPath, "plugins", name) : name);

		Assert.Equal("base", configuration["Fixture:settings:value"]);
		Assert.Equal("retained", configuration["Fixture:settings:inherited"]);
		var file = Assert.IsAssignableFrom<FileConfigurationSource>(configuration.Sources.Last());
		Assert.Equal(expected, file.FileProvider.GetFileInfo(file.Path).PhysicalPath);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void HostLoadsOnlyExactEntryFromConfiguredProvider(bool includeMain)
	{
		using var scope = new HostTestDirectory();
		var builder = new Hosting.ApplicationBuilder(typeof(HostingConfigurationTests).Assembly.GetName().Name, [$"--contentRoot={scope.DirectoryPath}"]);
		using var configuration = builder.Configuration;
		configuration.SetFileProvider(new MemoryFileProvider(
			new("HostFixture-z.option", Options("last")),
			new("HostFixture-a.option", Options("lowercase")),
			new("HostFixture-B.option", Options("middle")),
			new("HostFixture-A.option", Options("uppercase")),
			new("HostFixture.option", includeMain ? Options("base") : null),
			new("HostFixture-.option", Options("invalid-suffix")),
			new("HostFixture-production.option.template", Options("template")),
			new("HostFixture-folder.option", null, true)));
		var original = configuration.Sources.Count;

		builder.LoadConfiguration("HostFixture");

		Assert.Equal(includeMain ? "base" : null, configuration["Fixture:settings:value"]);
		Assert.Equal("HostFixture.option", Assert.IsAssignableFrom<FileConfigurationSource>(Assert.Single(configuration.Sources.Skip(original))).Path);
	}

	[Fact]
	public void HostImportsLayersThroughConfiguredProvider()
	{
		using var scope = new HostTestDirectory();
		var builder = new Hosting.ApplicationBuilder(typeof(HostingConfigurationTests).Assembly.GetName().Name, [$"--contentRoot={scope.DirectoryPath}"]);
		using var configuration = builder.Configuration;
		var files = new MemoryFileProvider(
			new("HostFixture.option", "<options><option path='Fixture'><settings value='base' inherited='retained'/></option></options><?import path='HostFixture-z.option'?><?import path='HostFixture-a.option'?>"),
			new("HostFixture-z.option", Options("first")),
			new("HostFixture-a.option", Options("last")),
			new("HostFixture-unused.option", Options("excluded")));
		configuration.SetFileProvider(files);
		var original = configuration.Sources.Count;

		builder.LoadConfiguration("HostFixture");

		Assert.Equal("last", configuration["Fixture:settings:value"]);
		Assert.Equal("retained", configuration["Fixture:settings:inherited"]);
		var entry = Assert.IsAssignableFrom<FileConfigurationSource>(Assert.Single(configuration.Sources.Skip(original)));
		Assert.Equal("HostFixture.option", entry.Path);
		Assert.Same(files, entry.FileProvider);
	}

	[Fact]
	public void HostIgnoresImplicitEnvironmentAndSuffixLayers()
	{
		using var scope = new HostTestDirectory();
		var builder = new Hosting.ApplicationBuilder(typeof(HostingConfigurationTests).Assembly.GetName().Name, [$"--contentRoot={scope.DirectoryPath}", "--environment=Development"]);
		using var configuration = builder.Configuration;
		builder.Environment.EnvironmentName = "Development";
		configuration["host"] = "Blue";
		configuration["site"] = "Alpha";
		configuration.SetFileProvider(new MemoryFileProvider(
			new("HostFixture.production.option", Options("production")),
			new("HostFixture.development.option", Options("environment")),
			new("HostFixture.blue.option", Options("host")),
			new("HostFixture.alpha.option", Options("site")),
			new("HostFixture-blue.option", Options("selected"))));
		var original = configuration.Sources.Count;

		builder.LoadConfiguration("HostFixture");

		Assert.Null(configuration["Fixture:settings:value"]);
		Assert.Equal("HostFixture.option", Assert.IsAssignableFrom<FileConfigurationSource>(Assert.Single(configuration.Sources.Skip(original))).Path);
		Assert.Equal("Development", builder.Environment.EnvironmentName);
	}

	[Fact]
	public void InvalidHostEntryDoesNotLoadSuffixFiles()
	{
		using var scope = new HostTestDirectory();
		var builder = new Hosting.ApplicationBuilder(typeof(HostingConfigurationTests).Assembly.GetName().Name, [$"--contentRoot={scope.DirectoryPath}"]);
		using var configuration = builder.Configuration;
		configuration.SetFileProvider(new MemoryFileProvider(
			new("HostFixture.option", "<options>"),
			new("HostFixture-site.option", Options("fallback"))));

		Assert.Throws<InvalidDataException>(() => builder.LoadConfiguration("HostFixture"));
		Assert.Null(configuration["Fixture:settings:value"]);
	}

	private static string Options(string value) => $"<options><option path=\"Fixture\"><settings value=\"{value}\" /></option></options>";

	private sealed class MemoryFileProvider(params MemoryFile[] files) : IFileProvider, IDirectoryContents
	{
		public bool Exists => true;
		public IFileInfo GetFileInfo(string subpath) => files.FirstOrDefault(file => file.Name == subpath && file.Exists) is { } file ? file : new NotFoundFileInfo(subpath);
		public IDirectoryContents GetDirectoryContents(string subpath) => throw new NotSupportedException();
		public IChangeToken Watch(string filter) => NullChangeToken.Singleton;
		public IEnumerator<IFileInfo> GetEnumerator() => files.Where(file => file.Exists).Cast<IFileInfo>().GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
	}

	private sealed class MemoryFile(string name, string content, bool directory = false) : IFileInfo
	{
		public bool Exists => content != null || directory;
		public string Name => name;
		public long Length => content == null ? 0 : Encoding.UTF8.GetByteCount(content);
		public string PhysicalPath => null;
		public DateTimeOffset LastModified => DateTimeOffset.UnixEpoch;
		public bool IsDirectory => directory;
		public Stream CreateReadStream() => new MemoryStream(Encoding.UTF8.GetBytes(content));
	}
}
