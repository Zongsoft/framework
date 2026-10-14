using System;
using System.IO;
using System.Linq;
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
	public void ReloadRetainsNestedPluginOptions(bool loadTreeFirst)
	{
		using var scope = new PluginTestScope(false);
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
	public void RepeatedFileReloadsNotifyConfigurationAndDetachOnDispose()
	{
		using var scope = new PluginTestScope();
		scope.WritePlugin("");
		scope.WriteFile("Feature.option", Options("value"));
		using var configuration = (ConfigurationRoot)new ConfigurationBuilder().Add(new PluginConfigurationSource(scope.Options)).Build();
		scope.Tree.Load();
		var provider = Assert.IsType<PluginConfigurationProvider>(Assert.Single(configuration.Providers));
		var composite = Assert.IsType<CompositeConfigurationProvider>(Assert.Single(provider.Providers));
		var file = Assert.IsAssignableFrom<FileConfigurationProvider>(Assert.Single(composite.Providers));

		for(var i = 0; i < 2; i++)
		{
			var changed = configuration.GetReloadToken();
			scope.WriteFile("Feature.option", Options($"value-{i}"));
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
	public void ConflictingPluginKeysRetainEffectiveValuesAndFileOrigins()
	{
		using var scope = new PluginTestScope(false);
		scope.WriteFile("First.plugin", "<plugin name=\"First\" />");
		scope.WriteFile("Second.plugin", "<plugin name=\"Second\" />");
		scope.WriteFile("First.option", "<options><option path=\"Kestrel/Endpoints\"><telemetry url=\"http://*:4317/old\" /></option></options>");
		var first = scope.WriteFile("First.development.option", "<options><option path=\"Kestrel/Endpoints\"><telemetry url=\"http://*:4317/first\" /></option></options>");
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
	public void FailedLoadClearsPartialConfigurationAndCanRetry()
	{
		using var scope = new PluginTestScope(false);
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
	public void ProviderLoadsExistingOrNewlyLoadedPlugins(bool loadTreeFirst)
	{
		using var scope = new PluginTestScope();
		scope.WritePlugin("");
		scope.WriteFile("Feature.option", "<options><option path=\"/Fixture\"><settings number=\"7\" enabled=\"true\" /></option></options>");

		if(loadTreeFirst)
			scope.Tree.Load();

		var source = new PluginConfigurationSource(scope.Options);
		using var provider = Assert.IsType<PluginConfigurationProvider>(source.Build(null));
		provider.Load();
		scope.Tree.Load();

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
	[InlineData("Feature.option", "base")]
	[InlineData("Feature.development.option", "environment")]
	[InlineData("Feature.development-extra.option", "extra")]
	[InlineData("Feature.web.option", "host")]
	[InlineData("Feature.web.development-extra.option", "host-extra")]
	[InlineData("Feature.portal.option", "site")]
	[InlineData("Feature.portal.development-extra.option", "site-extra")]
	public void ProviderSelectsDocumentedOptionFilePatterns(string fileName, string value)
	{
		using var scope = new PluginTestScope(settings: new Dictionary<string, string> { ["host"] = "web", ["site"] = "portal" });
		scope.WritePlugin("");
		scope.WriteFile(fileName, Options(value));
		using var provider = new PluginConfigurationProvider(new PluginConfigurationSource(scope.Options));

		scope.Tree.Load();

		Assert.True(provider.TryGet("Fixture:settings:value", out var actual));
		Assert.Equal(value, actual);
	}

	[Fact]
	public void SiteOverridesHostEnvironmentAndBaseWithoutMergingUnrelatedFiles()
	{
		using var scope = new PluginTestScope(settings: new Dictionary<string, string> { ["host"] = "web", ["site"] = "portal" });
		scope.WritePlugin("");
		scope.WriteFile("Feature.option", Options("base"));
		scope.WriteFile("Feature.development.option", Options("environment"));
		scope.WriteFile("Feature.development-extra.option", Options("extra"));
		scope.WriteFile("Feature.web.option", Options("host"));
		scope.WriteFile("Feature.portal.option", Options("site"));
		scope.WriteFile("Feature.production.option", Options("wrong-environment"));
		scope.WriteFile("Unrelated.option", Options("unrelated"));
		using var provider = new PluginConfigurationProvider(new PluginConfigurationSource(scope.Options));

		scope.Tree.Load();

		Assert.True(provider.TryGet("Fixture:settings:value", out var value));
		Assert.Equal("site", value);
		Assert.Single(provider.Providers);
	}

	[Fact]
	public void MatchingUsesFileStemAndHostSiteAreDeduplicated()
	{
		using var scope = new PluginTestScope(false, new Dictionary<string, string> { ["host"] = "web", ["site"] = "WEB" });
		scope.WriteFile("Artifact.plugin", "<plugin name=\"LogicalName\" />");
		scope.WriteFile("Artifact.web.option", Options("matched"));
		scope.WriteFile("LogicalName.option", Options("wrong-name"));
		using var provider = new PluginConfigurationProvider(new PluginConfigurationSource(scope.Options));

		scope.Tree.Load();

		Assert.True(provider.TryGet("Fixture:settings:value", out var value));
		Assert.Equal("matched", value);
		Assert.Single(Assert.IsType<CompositeConfigurationProvider>(Assert.Single(provider.Providers)).Providers);
	}

	[Fact]
	public void SetOnlyUpdatesExistingKeysAndChildKeysIncludeEarlierProviders()
	{
		using var scope = new PluginTestScope();
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
	public void LoadReplacesProvidersAndDisposeDetachesLoaderEvents()
	{
		using var scope = new PluginTestScope();
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
	public void DisposedProviderIgnoresLaterPluginLoads()
	{
		using var scope = new PluginTestScope();
		scope.WritePlugin("");
		scope.WriteFile("Feature.option", Options("fixture"));
		using var provider = new PluginConfigurationProvider(new PluginConfigurationSource(scope.Options));
		provider.Dispose();

		scope.Tree.Load();

		Assert.Empty(provider.Providers);
	}

	private static string Options(string value) => $"<options><option path=\"/Fixture\"><settings value=\"{value}\" /></option></options>";
}
