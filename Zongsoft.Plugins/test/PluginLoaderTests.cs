using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Plugins.Tests;

public class PluginLoaderTests
{
	[Fact]
	public void DependenciesLoadBeforeConsumersAndExposeTransitiveRelationships()
	{
		using var scope = new PluginTestScope(false);
		scope.WriteFile("a.plugin", "<plugin name=\"Consumer\"><manifest><dependencies><dependency name=\"Adapter\" /></dependencies></manifest></plugin>");
		scope.WriteFile("b.plugin", "<plugin name=\"Adapter\"><manifest><dependencies><dependency name=\"Foundation\" /></dependencies></manifest></plugin>");
		scope.WriteFile("z.plugin", "<plugin name=\"Foundation\" title=\"Fixture\" author=\"Tests\" version=\"1.2.3.4\" description=\"Metadata\" copyright=\"Fixture copyright\" />");
		var events = new List<string>();
		scope.Tree.Loader.Loading += (_, args) =>
		{
			Assert.Same(scope.Options, args.Options);
			Assert.Equal(PluginTreeStatus.Loading, scope.Tree.Status);
			events.Add("loading");
		};
		scope.Tree.Loader.PluginLoaded += (_, args) =>
		{
			Assert.Equal(PluginStatus.Loaded, args.Plugin.Status);
			events.Add(args.Plugin.Name);
		};
		scope.Tree.Loader.Loaded += (_, _) => events.Add("loaded");

		scope.Tree.Load();
		scope.Tree.Load();

		Assert.Equal(new[] { "loading", "Foundation", "Adapter", "Consumer", "loaded" }, events);
		Assert.Equal(PluginTreeStatus.Loaded, scope.Tree.Status);
		var foundation = scope.Tree.Plugins["foundation"];
		var adapter = scope.Tree.Plugins["Adapter"];
		var consumer = scope.Tree.Plugins["Consumer"];
		Assert.True(foundation.IsMaster);
		Assert.True(consumer.IsSlave);
		Assert.Same(adapter, consumer.Manifest.Dependencies["adapter"].Plugin);
		Assert.Equal(new[] { foundation }, consumer.GetMasters());
		Assert.Equal(new[] { adapter }, foundation.GetSlaves());
		Assert.Equal(new[] { adapter, consumer }, foundation.GetSlaves(true));
		Assert.Empty(foundation.GetMasters());
		Assert.Equal(2, foundation.GetSiblings().Count());
		Assert.Empty(foundation.GetSiblings(false, false));

		Assert.Equal("Fixture", foundation.Manifest.Title);
		Assert.Equal("Tests", foundation.Manifest.Author);
		Assert.Equal(new Version(1, 2, 3, 4), foundation.Manifest.Version);
		Assert.Equal("Metadata", foundation.Manifest.Description);
		Assert.Equal("Fixture copyright", foundation.Manifest.Copyright);
	}

	[Fact]
	public void DirectoryHierarchyAndHiddenPluginHaveSeparateLoadingRules()
	{
		using var scope = new PluginTestScope(false);
		scope.WriteFile("Root.plugin", "<plugin name=\"Root\" />");
		scope.WriteFile("nested/Child.plugin", "<plugin name=\"Child\" />");
		scope.WriteFile("nested/.plugin", "<plugin name=\"Hidden\" />");
		scope.WriteFile("nested/deep/Grandchild.plugin", "<plugin name=\"Grandchild\" />");
		var loaded = new List<string>();
		scope.Tree.Loader.PluginLoaded += (_, args) => loaded.Add(args.Plugin.Name);

		scope.Tree.Load();

		Assert.Equal(new[] { "Root", "Child", "Grandchild", "Hidden" }, loaded);
		var root = Assert.Single(scope.Tree.Plugins);
		var child = root.Children["Child"];
		Assert.Same(root, child.Parent);
		Assert.Same(child, scope.Tree.Plugins.Find("grandchild").Parent);
		Assert.True(root.Children["Hidden"].IsHidden);
		Assert.True(root.Children["Hidden"].IsSlave);
		Assert.False(root.Children["Hidden"].IsMaster);
		Assert.Null(scope.Tree.Plugins.Find("missing"));
	}

	[Theory]
	[InlineData("<plugin name=\"Feature\"><unknown /></plugin>")]
	[InlineData("<wrong name=\"Feature\" />")]
	[InlineData("<plugin name=\"Feature\"><manifest><unknown /></manifest></plugin>")]
	[InlineData("<plugin name=\"Feature\"><manifest><dependencies><dependency name=\"Absent\" /></dependencies></manifest></plugin>")]
	[InlineData("<plugin name=\"Feature\"><manifest><assemblies><assembly name=\"Absent.Fixture.Assembly\" /></assemblies></manifest></plugin>")]
	public void InvalidManifestsFailWithoutReportingLoaded(string xml)
	{
		using var scope = new PluginTestScope(false);
		scope.WriteFile("Feature.plugin", xml);
		var completed = false;
		scope.Tree.Loader.Loaded += (_, _) => completed = true;

		Assert.ThrowsAny<PluginException>(() => scope.Tree.Load());

		Assert.False(completed);
		Assert.NotEqual(PluginTreeStatus.Loaded, scope.Tree.Status);
	}

	[Fact]
	public void DuplicatePluginNamesAreCaseInsensitive()
	{
		using var scope = new PluginTestScope(false);
		scope.WriteFile("One.plugin", "<plugin name=\"Feature\" />");
		scope.WriteFile("Two.plugin", "<plugin name=\"feature\" />");

		Assert.Throws<PluginFileException>(() => scope.Tree.Load());
	}

	[Fact]
	public void HiddenPluginCannotDeclareDependencies()
	{
		using var scope = new PluginTestScope();
		scope.WriteFile(".plugin", "<plugin name=\"Hidden\"><manifest><dependencies><dependency name=\"Main\" /></dependencies></manifest></plugin>");

		Assert.Throws<PluginFileException>(() => scope.Tree.Load());
	}

	[Theory]
	[InlineData(null)]
	[InlineData("not-a-version")]
	[InlineData("2.3")]
	public void OptionalAssembliesAndVersionFallbackAreSupported(string version)
	{
		using var scope = new PluginTestScope(false);
		scope.WriteFile("Feature.plugin", $"<plugin name=\"Feature\" version=\"{version}\"><manifest><assemblies><assembly name=\"Absent.Fixture.Assembly\" optional=\"true\" /><assembly name=\"Zongsoft.Plugins\" /></assemblies></manifest></plugin>");

		scope.Tree.Load();

		var plugin = Assert.Single(scope.Tree.Plugins);
		Assert.Same(typeof(Plugin).Assembly, Assert.Single(plugin.Manifest.Assemblies));
		Assert.Equal(version == "2.3" ? new Version(2, 3) : new Version(), plugin.Manifest.Version);
	}

	[Fact]
	public void InvalidExtensionKeepsFileContextAndRemovesFailedPlugin()
	{
		using var scope = new PluginTestScope();
		scope.WritePlugin("<extension path=\"/Items\"><object name=\"Invalid\"><other.constructor /></object></extension>");

		var error = Assert.Throws<PluginFileException>(() => scope.Tree.Load());

		Assert.Equal(Path.Combine(scope.DirectoryPath, "plugins", "Feature.plugin"), error.FileName);
		Assert.IsType<PluginException>(error.InnerException);
		Assert.False(scope.Tree.Plugins.Contains("Feature"));
	}

	[Fact]
	public void MissingPluginDirectoryFailsBeforeLoadingEvent()
	{
		using var scope = new PluginTestScope(false);
		var options = new PluginOptions(scope.Configuration, scope.DirectoryPath, "Development", "missing");
		var tree = PluginTree.Get(options);
		var started = false;
		tree.Loader.Loading += (_, _) => started = true;

		Assert.Throws<DirectoryNotFoundException>(() => tree.Load());

		Assert.False(started);
		Assert.Equal(PluginTreeStatus.None, tree.Status);
	}

	[Fact]
	public void OverlayReplacesBuiltinWhileKeepingTheNodeAndExistingChildren()
	{
		using var scope = new PluginTestScope();
		scope.WritePlugin("<extension path=\"/Items\"><object name=\"Parent\" type=\"System.Collections.ArrayList\"><object name=\"Child\" type=\"System.Object\" /></object></extension>");
		scope.WriteFile("Overlay.plugin", "<plugin name=\"Overlay\"><manifest><dependencies><dependency name=\"Main\" /><dependency name=\"Feature\" /></dependencies></manifest><extension path=\"/Items\"><object name=\"Parent\" type=\"System.Collections.ArrayList\"><object name=\"First\" position=\"^\" type=\"System.Object\" /></object></extension></plugin>");
		PluginTreeNode original = null;
		scope.Tree.Loader.PluginLoaded += (_, args) =>
		{
			if(args.Plugin.Name == "Feature")
				original = scope.Tree.Find("/Items/Parent");
		};

		scope.Tree.Load();
		var parent = scope.Tree.Find("/Items/Parent");
		var value = Assert.IsType<System.Collections.ArrayList>(parent.UnwrapValue(ObtainMode.Auto));

		Assert.Same(original, parent);
		Assert.Equal("Overlay", parent.Plugin.Name);
		Assert.Equal(new[] { "First", "Child" }, parent.Children.Select(child => child.Name));
		Assert.Equal(parent.Children.Select(child => child.UnwrapValue(ObtainMode.Never)), value.Cast<object>());
	}

	[Theory]
	[InlineData("<extension path=\"/Items\"><object name=\"Same\"><object.custom><invalid /></object.custom></object></extension>")]
	[InlineData("<extension path=\"/Items\"><object name=\"Same\"><object.constructor /></object></extension>")]
	[InlineData("<builders><builder name=\"fixture\" /></builders>")]
	[InlineData("<parsers><parser name=\"fixture\" /></parsers>")]
	public void InvalidDeclarationsRetainTheOwningFileName(string content)
	{
		using var scope = new PluginTestScope();
		scope.WritePlugin(content);

		var error = Assert.Throws<PluginFileException>(() => scope.Tree.Load());

		Assert.Equal(Path.Combine(scope.DirectoryPath, "plugins", "Feature.plugin"), error.FileName);
		Assert.NotNull(error.InnerException);
	}

	[Fact]
	public void ShippedManifestsLoadWithoutConstructingComponents()
	{
		using var scope = new PluginTestScope(false);
		scope.WriteFile("Main.plugin", File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Main.plugin")));
		scope.WriteFile("Terminal.plugin", File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Terminal.plugin")));

		scope.Tree.Load();

		Assert.Equal(2, scope.Tree.Plugins.Count);
		Assert.Equal(3, scope.Tree.Plugins["Main"].Builders.Count);
		Assert.Equal(9, scope.Tree.Plugins["Main"].Parsers.Count);
		Assert.Equal("Zongsoft.Terminal", scope.Tree.Find("/Workbench/Executor").Plugin.Name);
		Assert.NotNull(scope.Tree.Find("/Workbench/Events/Exchanger/Channels"));
		Assert.NotNull(scope.Tree.Find("/Workbench/Startup/EventExchanger"));
		Assert.All(scope.Tree.Plugins.SelectMany(plugin => plugin.Builtins), builtin => Assert.False(builtin.IsBuilded));
	}
}
