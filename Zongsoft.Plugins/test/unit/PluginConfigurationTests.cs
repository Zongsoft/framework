using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using Xunit;

using Microsoft.Extensions.Configuration;

using Zongsoft.Configuration;

namespace Zongsoft.Plugins.Tests;

public class PluginConfigurationTests
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task ReloadRetainsNestedPluginOptions(bool loadTreeFirst)
	{
		await using var scope = new PluginTestScope(false);
		scope.WriteFile("Root.plugin", "<plugin name=\"Root\" />");
		scope.WriteFile("nested/Child.plugin", "<plugin name=\"Child\" />");
		scope.WriteFile("nested/deep/Grandchild.plugin", "<plugin name=\"Grandchild\" />");
		scope.WriteFile("nested/Child.option", Options("child"));
		scope.WriteFile("nested/deep/Grandchild.option", "<options><option path=\"Kestrel/Endpoints\"><telemetry url=\"http://*:4317/opentelemetry\" protocols=\"grpc+http2\" /></option></options>");

		if(loadTreeFirst)
			scope.Tree.Load();

		using var provider = new PluginConfigurationProvider(new PluginConfigurationSource(scope.Options));
		provider.Load();
		scope.Tree.Load();

		for(var i = 0; i < 2; i++)
		{
			Assert.True(provider.TryGet("Fixture:settings:value", out var value));
			Assert.Equal("child", value);

			Assert.True(provider.TryGet("Kestrel:Endpoints:telemetry:Url", out var url));
			Assert.Equal("http://*:4317/opentelemetry", url);

			Assert.Equal(2, provider.Providers.Count());
			provider.Load();
		}
	}

	[Fact]
	public async Task RepeatedFileReloadsNotifyConfigurationAndDetachOnDispose()
	{
		await using var scope = new PluginTestScope();
		scope.WritePlugin("");
		scope.WriteFile("Feature.option", Options("base") + "<?import path='Feature-values.option'?>");
		scope.WriteFile("Feature-values.option", Options("value"));
		using var configuration = (ConfigurationRoot)new ConfigurationBuilder().Add(new PluginConfigurationSource(scope.Options)).Build();
		scope.Tree.Load();
		var provider = Assert.IsType<PluginConfigurationProvider>(Assert.Single(configuration.Providers));
		var composite = Assert.IsType<CompositeConfigurationProvider>(Assert.Single(provider.Providers));
		var file = Assert.IsAssignableFrom<FileConfigurationProvider>(Assert.Single(composite.Providers));

		for(var i = 0; i < 2; i++)
		{
			var changed = configuration.GetReloadToken();
			scope.WriteFile("Feature-values.option", Options($"value-{i}"));
			file.Load();

			Assert.True(changed.HasChanged);
			Assert.NotSame(changed, configuration.GetReloadToken());
			Assert.Equal($"value-{i}", configuration["Fixture:settings:value"]);
		}

		var stopped = configuration.GetReloadToken();
		provider.Dispose();
		file.Load();

		Assert.False(stopped.HasChanged);
		Assert.Empty(provider.Providers);
	}

	[Fact]
	public async Task ConflictingPluginKeysRetainEffectiveValuesAndFileOrigins()
	{
		await using var scope = new PluginTestScope(false);
		scope.WriteFile("First.plugin", "<plugin name=\"First\" />");
		scope.WriteFile("Second.plugin", "<plugin name=\"Second\" />");
		var first = scope.WriteFile("First.option", "<options><option path=\"Kestrel/Endpoints\"><telemetry url=\"http://*:4317/old\" /></option></options><?import path='First-deployment.option'?>");
		scope.WriteFile("First-deployment.option", "<options><option path=\"Kestrel/Endpoints\"><telemetry url=\"http://*:4317/first\" /></option></options>");
		var second = scope.WriteFile("Second.option", "<options><option path=\"Kestrel/Endpoints\"><telemetry url=\"http://*:4318/second\" /></option></options>");
		using var provider = new PluginConfigurationProvider(new PluginConfigurationSource(scope.Options));
		scope.Tree.Load();
		const string KEY = "Kestrel:Endpoints:telemetry:Url";
		var origins = new Dictionary<string, string>();

		foreach(var item in provider.Providers.Cast<CompositeConfigurationProvider>())
		{
			Assert.True(item.TryGet(KEY, out var effective));
			var file = item.Providers.OfType<FileConfigurationProvider>().First(candidate => candidate.TryGet(KEY, out _));
			var path = file.Source.FileProvider.GetFileInfo(file.Source.Path).PhysicalPath;
			origins.Add(Path.GetFullPath(path), effective);
		}

		Assert.Equal(2, origins.Count);
		Assert.Equal("http://*:4317/first", origins[first]);
		Assert.Equal("http://*:4318/second", origins[second]);
	}

	[Fact]
	public async Task FailedLoadClearsPartialConfigurationAndCanRetry()
	{
		await using var scope = new PluginTestScope(false);
		scope.WriteFile("Root.plugin", "<plugin name=\"Root\" />");
		scope.WriteFile("Root.option", Options("root"));
		scope.WriteFile("nested/Child.plugin", "<plugin name=\"Child\" />");
		scope.WriteFile("nested/Child.option", "<options>");
		scope.Tree.Load();
		using var provider = new PluginConfigurationProvider(new PluginConfigurationSource(scope.Options));

		Assert.Throws<InvalidDataException>(provider.Load);
		Assert.Empty(provider.Providers);
		Assert.False(provider.TryGet("Fixture:settings:value", out _));

		scope.WriteFile("nested/Child.option", "<options><option path=\"Child\"><settings enabled=\"true\" /></option></options>");
		provider.Load();

		Assert.Equal(2, provider.Providers.Count());
		Assert.True(provider.TryGet("Fixture:settings:value", out var value));
		Assert.Equal("root", value);

		Assert.True(provider.TryGet("Child:settings:enabled", out var enabled));
		Assert.Equal("true", enabled);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task ProviderLoadsExistingOrNewlyLoadedPlugins(bool loadTreeFirst)
	{
		await using var scope = new PluginTestScope();
		scope.WritePlugin("");
		scope.WriteFile("Feature.option", "<options><option path=\"/Fixture\"><settings number=\"7\" enabled=\"true\" /></option></options>");

		if(loadTreeFirst)
			scope.Tree.Load();

		var source = new PluginConfigurationSource(scope.Options);
		using var provider = Assert.IsType<PluginConfigurationProvider>(source.Build(null));
		provider.Load();
		var changed = provider.GetReloadToken();
		scope.Tree.Load();

		Assert.Equal(!loadTreeFirst, changed.HasChanged);
		Assert.True(provider.TryGet("Fixture:settings:number", out var number));
		Assert.Equal("7", number);
		Assert.True(provider.TryGet("fixture:SETTINGS:ENABLED", out var enabled));
		Assert.Equal("true", enabled);
		Assert.False(provider.TryGet("missing", out var absent));
		Assert.Null(absent);
		Assert.Single(provider.Providers);
		Assert.Same(provider, source.Build(null));
	}

	[Theory]
	[InlineData(true, "logical", "manifest")]
	[InlineData(true, "invalid", "manifest")]
	[InlineData(true, null, "manifest")]
	[InlineData(false, "logical", "logical")]
	[InlineData(false, null, null)]
	public async Task EntrySelectionUsesManifestThenLogicalName(bool manifestExists, string logical, string expected)
	{
		await using var scope = new PluginTestScope(false);
		scope.WriteFile("Artifact.plugin", "<plugin name=\"LogicalName\" />");
		if(manifestExists)
			scope.WriteFile("Artifact.option", Options("manifest"));
		if(logical != null)
			scope.WriteFile("LogicalName.option", logical == "invalid" ? "<options>" : Options(logical));

		using var provider = new PluginConfigurationProvider(new PluginConfigurationSource(scope.Options));
		scope.Tree.Load();

		Assert.Equal(expected != null, provider.TryGet("Fixture:settings:value", out var actual));
		Assert.Equal(expected, actual);

		if(expected == null)
			Assert.Empty(provider.Providers);
		else
		{
			var composite = Assert.IsType<CompositeConfigurationProvider>(Assert.Single(provider.Providers));
			var entry = Assert.IsAssignableFrom<FileConfigurationProvider>(Assert.Single(composite.Providers));
			Assert.Equal(manifestExists ? "Artifact.option" : "LogicalName.option", Path.GetFileName(entry.Source.Path));
		}
	}

	[Fact]
	public async Task ExplicitImportsMergeInDeclaredOrderThroughOneEntry()
	{
		await using var scope = new PluginTestScope(false);
		scope.WriteFile("Automao.Externals.Wechat.plugin", "<plugin name=\"Wechat\" />");
		scope.WriteFile("Wechat-z.option", Options("first"));
		scope.WriteFile("Wechat-a.option", Options("last"));
		scope.WriteFile("Wechat-unused.option", Options("excluded"));
		scope.WriteFile("Automao.Externals.Wechat.option", "<options><option path=\"Fixture\"><settings value=\"base\" inherited=\"retained\" /></option></options><?import path='Wechat-z.option'?><?import path='Wechat-a.option'?>");
		using var provider = new PluginConfigurationProvider(new PluginConfigurationSource(scope.Options));

		scope.Tree.Load();

		Assert.True(provider.TryGet("Fixture:settings:value", out var value));
		Assert.Equal("last", value);

		Assert.True(provider.TryGet("Fixture:settings:inherited", out var inherited));
		Assert.Equal("retained", inherited);

		var composite = Assert.IsType<CompositeConfigurationProvider>(Assert.Single(provider.Providers));
		Assert.Equal("Automao.Externals.Wechat.option", Path.GetFileName(Assert.IsAssignableFrom<FileConfigurationProvider>(Assert.Single(composite.Providers)).Source.Path));
	}

	[Theory]
	[InlineData("Feature.development.option")]
	[InlineData("Feature.web.option")]
	[InlineData("Feature.portal.option")]
	[InlineData("Feature-production.option")]
	[InlineData("Feature-daemon.option")]
	[InlineData("Feature-backoffice.option")]
	[InlineData("Feature-50-network.private.option")]
	[InlineData("LogicalName-production.option")]
	[InlineData("Feature-.option")]
	[InlineData("FeatureOther.option")]
	[InlineData("Feature-custom.option.template")]
	[InlineData("Feature-custom.option/Unrelated.txt")]
	[InlineData("nested/Feature-custom.option")]
	public async Task ProviderIgnoresFilesOutsideItsExactEntryNames(string fileName)
	{
		await using var scope = new PluginTestScope(false, new Dictionary<string, string> { ["host"] = "web", ["site"] = "portal" });
		scope.WriteFile("Feature.plugin", "<plugin name=\"LogicalName\" />");
		scope.WriteFile(fileName, Options("excluded"));
		using var provider = new PluginConfigurationProvider(new PluginConfigurationSource(scope.Options));

		scope.Tree.Load();

		Assert.False(provider.TryGet("Fixture:settings:value", out _));
		Assert.Empty(provider.Providers);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task LogicalNameFallbackStaysInManifestDirectory(bool localExists)
	{
		await using var scope = new PluginTestScope(false);
		scope.WriteFile("nested/Artifact.plugin", "<plugin name=\"LogicalName\" />");
		scope.WriteFile("LogicalName.option", Options("wrong-directory"));

		if(localExists)
			scope.WriteFile("nested/LogicalName.option", Options("local"));

		using var provider = new PluginConfigurationProvider(new PluginConfigurationSource(scope.Options));

		scope.Tree.Load();

		Assert.Equal(localExists, provider.TryGet("Fixture:settings:value", out var value));
		Assert.Equal(localExists ? "local" : null, value);
		Assert.Equal(localExists ? 1 : 0, provider.Providers.Count());
	}

	[Fact]
	public async Task InvalidManifestEntryDoesNotFallBackToLogicalName()
	{
		await using var scope = new PluginTestScope(false);
		scope.WriteFile("Artifact.plugin", "<plugin name=\"LogicalName\" />");
		scope.WriteFile("Artifact.option", "<options>");
		scope.WriteFile("LogicalName.option", Options("fallback"));
		scope.Tree.Load();
		using var provider = new PluginConfigurationProvider(new PluginConfigurationSource(scope.Options));

		Assert.Throws<InvalidDataException>(provider.Load);
		Assert.Empty(provider.Providers);
		Assert.False(provider.TryGet("Fixture:settings:value", out _));
	}

	[Fact]
	public async Task PrefixNamedManifestsLoadOnlyTheirExactEntries()
	{
		await using var scope = new PluginTestScope(false);
		scope.WriteFile("Feature.plugin", "<plugin name=\"Feature\" />");
		scope.WriteFile("Feature-daemon.plugin", "<plugin name=\"Daemon\" />");
		scope.WriteFile("Feature-daemon-worker.plugin", "<plugin name=\"Worker\" />");
		scope.WriteFile("Feature.option", Options("base"));
		scope.WriteFile("Feature-daemon.option", Options("daemon"));
		scope.WriteFile("Feature-daemon-worker.option", Options("worker"));
		scope.WriteFile("Feature-daemon-worker-50-deployment.option", Options("excluded"));
		using var provider = new PluginConfigurationProvider(new PluginConfigurationSource(scope.Options));

		scope.Tree.Load();

		var owners = provider.Providers.Cast<CompositeConfigurationProvider>().ToDictionary(composite =>
		{
			Assert.True(composite.TryGet("Fixture:settings:value", out var value));
			return value;
		}, composite => composite.Providers.Cast<FileConfigurationProvider>().Select(file => Path.GetFileName(file.Source.Path)).ToArray());

		Assert.Equal(3, owners.Count);
		Assert.Equal(new[] { "Feature.option" }, owners["base"]);
		Assert.Equal(new[] { "Feature-daemon.option" }, owners["daemon"]);
		Assert.Equal(new[] { "Feature-daemon-worker.option" }, owners["worker"]);
	}

	[Fact]
	public async Task UnloadedManifestEntriesAreNotScanned()
	{
		await using var scope = new PluginTestScope(false);
		scope.WriteFile("Feature.plugin", "<plugin name=\"Feature\" />");
		scope.WriteFile("Feature.option", Options("base"));
		scope.Tree.Load();
		scope.WriteFile("Feature-daemon.plugin", "<plugin name=\"Daemon\" />");
		scope.WriteFile("Feature-daemon.option", Options("daemon"));
		scope.WriteFile("Feature-daemon-extra.option", Options("daemon-extra"));
		using var provider = new PluginConfigurationProvider(new PluginConfigurationSource(scope.Options));

		provider.Load();

		Assert.False(scope.Tree.Plugins.Contains("Daemon"));
		Assert.True(provider.TryGet("Fixture:settings:value", out var value));
		Assert.Equal("base", value);
		var composite = Assert.IsType<CompositeConfigurationProvider>(Assert.Single(provider.Providers));
		Assert.Equal("Feature.option", Path.GetFileName(Assert.IsAssignableFrom<FileConfigurationProvider>(Assert.Single(composite.Providers)).Source.Path));
	}

	[Fact]
	public async Task ExplicitReloadReevaluatesEntryPrecedence()
	{
		await using var scope = new PluginTestScope(false);
		scope.WriteFile("Artifact.plugin", "<plugin name=\"LogicalName\" />");
		scope.WriteFile("LogicalName.option", Options("fallback"));
		using var provider = new PluginConfigurationProvider(new PluginConfigurationSource(scope.Options));
		scope.Tree.Load();
		Assert.True(provider.TryGet("Fixture:settings:value", out var initial));
		Assert.Equal("fallback", initial);
		var added = scope.WriteFile("Artifact.option", Options("manifest"));
		var changed = provider.GetReloadToken();

		provider.Load();

		Assert.True(changed.HasChanged);
		Assert.True(provider.TryGet("Fixture:settings:value", out var addedValue));
		Assert.Equal("manifest", addedValue);
		Assert.Single(Assert.IsType<CompositeConfigurationProvider>(Assert.Single(provider.Providers)).Providers);

		File.Delete(added);
		changed = provider.GetReloadToken();
		provider.Load();

		Assert.True(changed.HasChanged);
		Assert.True(provider.TryGet("Fixture:settings:value", out var remainingValue));
		Assert.Equal("fallback", remainingValue);
		Assert.Single(Assert.IsType<CompositeConfigurationProvider>(Assert.Single(provider.Providers)).Providers);
	}

	[Fact]
	public async Task SetOnlyUpdatesExistingKeysAndChildKeysIncludeEarlierProviders()
	{
		await using var scope = new PluginTestScope();
		scope.WritePlugin("");
		scope.WriteFile("Feature.option", Options("initial"));
		using var provider = new PluginConfigurationProvider(new PluginConfigurationSource(scope.Options));
		scope.Tree.Load();

		provider.Set("Fixture:settings:value", "updated");
		provider.Set("Fixture:settings:missing", "ignored");

		Assert.True(provider.TryGet("Fixture:settings:value", out var value));
		Assert.Equal("updated", value);
		Assert.False(provider.TryGet("Fixture:settings:missing", out _));
		Assert.Equal(new[] { "earlier", "value" }, provider.GetChildKeys(["earlier"], "Fixture:settings"));
	}

	[Fact]
	public async Task LoadReplacesProvidersAndDisposeDetachesLoaderEvents()
	{
		await using var scope = new PluginTestScope();
		scope.WritePlugin("");
		scope.WriteFile("Feature.option", Options("fixture"));
		using var provider = new PluginConfigurationProvider(new PluginConfigurationSource(scope.Options));
		scope.Tree.Load();
		var original = Assert.Single(provider.Providers);
		var file = Assert.IsAssignableFrom<FileConfigurationProvider>(Assert.Single(Assert.IsType<CompositeConfigurationProvider>(original).Providers));
		var changed = provider.GetReloadToken();

		provider.Load();

		Assert.True(changed.HasChanged);
		Assert.NotSame(original, Assert.Single(provider.Providers));
		Assert.Empty(Assert.IsType<CompositeConfigurationProvider>(original).Providers);

		var current = provider.GetReloadToken();
		file.Load();

		Assert.False(current.HasChanged);

		provider.Dispose();
		provider.Dispose();

		Assert.Empty(provider.Providers);
		Assert.False(provider.TryGet("Fixture:settings:value", out _));
		Assert.Throws<ObjectDisposedException>(provider.Load);
	}

	[Fact]
	public async Task DisposedProviderIgnoresLaterPluginLoads()
	{
		await using var scope = new PluginTestScope();
		scope.WritePlugin("");
		scope.WriteFile("Feature.option", Options("fixture"));
		using var provider = new PluginConfigurationProvider(new PluginConfigurationSource(scope.Options));
		provider.Dispose();

		scope.Tree.Load();

		Assert.Empty(provider.Providers);
	}

	private static string Options(string value) => $"<options><option path=\"/Fixture\"><settings value=\"{value}\" /></option></options>";
}
