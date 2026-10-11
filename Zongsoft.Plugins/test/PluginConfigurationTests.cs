using System;
using System.Linq;
using System.Collections.Generic;

using Xunit;

using Zongsoft.Configuration;

namespace Zongsoft.Plugins.Tests;

public class PluginConfigurationTests
{
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

		provider.Load();

		Assert.NotSame(original, Assert.Single(provider.Providers));
		Assert.Empty(Assert.IsType<CompositeConfigurationProvider>(original).Providers);

		provider.Dispose();
		provider.Dispose();

		Assert.Empty(provider.Providers);
		Assert.False(provider.TryGet("Fixture:settings:value", out _));
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
