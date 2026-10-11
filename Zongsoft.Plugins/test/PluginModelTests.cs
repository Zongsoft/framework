using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using Xunit;

using Zongsoft.Plugins.Builders;
using Zongsoft.Plugins.Parsers;

namespace Zongsoft.Plugins.Tests;

public class PluginModelTests
{
	[Fact]
	public void OptionsIdentifyTreesByPluginDirectory()
	{
		using var scope = new PluginTestScope();
		var equivalent = new PluginOptions(scope.Configuration, $" {scope.DirectoryPath} ", "Production");
		var different = new PluginOptions(scope.Configuration, scope.DirectoryPath, "Development", "extensions");

		Assert.Equal(scope.Options, equivalent);
		Assert.Equal(scope.Options.GetHashCode(), equivalent.GetHashCode());
		Assert.Same(scope.Tree, PluginTree.Get(equivalent));
		Assert.NotSame(scope.Tree, PluginTree.Get(different));
		Assert.Equal(Path.Combine(scope.DirectoryPath, "extensions"), different.PluginsPath);
		Assert.Same(scope.Configuration, equivalent.Configuration);
		equivalent.Properties["Example"] = "value";
		Assert.Equal("value", equivalent.Properties["example"]);
		Assert.Throws<ArgumentException>(() => new PluginOptions(scope.Configuration, "relative", "Development"));
		Assert.Throws<DirectoryNotFoundException>(() => new PluginOptions(scope.Configuration, Path.Combine(scope.DirectoryPath, "missing"), "Development"));
		Assert.Throws<ArgumentNullException>("options", () => PluginTree.Get(null));
	}

	[Fact]
	public void PropertyValuesConvertAndInvalidateCachedValuesAfterAssignment()
	{
		using var scope = new PluginTestScope();
		var properties = new PluginElementPropertyCollection(scope.Tree.Root);
		properties.Set("Number", "17");
		var property = properties["number"];
		property.Type = typeof(int);

		Assert.Equal(17, property.Value);
		Assert.Same(scope.Tree.Root, property.Owner);
		properties.Set("NUMBER", "29");

		Assert.Single(properties);
		Assert.Same(property, properties["number"]);
		Assert.Equal(29, property.Value);
		Assert.Equal(29L, property.GetValue(typeof(long)));

		properties.Set("Number", "invalid");

		Assert.Equal(41, property.GetValue(typeof(int), 41));
	}

	[Fact]
	public void ExtendedPropertiesFallBackToBuiltinProperties()
	{
		using var scope = new PluginTestScope();
		scope.WritePlugin("<extension path=\"/Items\"><object name=\"Item\" type=\"System.Object\" count=\"4\" /></extension>");
		scope.Tree.Load();
		var builtin = scope.Builtin("/Items/Item");
		var properties = builtin.Node.Properties;

		Assert.Equal(4, properties.GetValue<int>("COUNT"));
		Assert.Equal("4", properties.GetRawValue("count"));
		Assert.Equal(12, properties.GetValue("missing", typeof(int), 12));
		Assert.True(properties.TryGetValue("count", typeof(int), out var count));
		Assert.Equal(4, count);
		Assert.False(properties.TryGetValue("absent", typeof(int), out var missing));
		Assert.Null(missing);
		Assert.Same(builtin.Plugin, Assert.Single(builtin.GetProperties()).Plugin);
	}

	[Fact]
	public void BehaviorsUseCaseInsensitiveNamesAndTypedDefaults()
	{
		using var scope = new PluginTestScope();
		scope.WritePlugin("<extension path=\"/Items\"><object name=\"Item\" type=\"System.Object\"><object.custom enabled=\"true\" count=\"7\">fixture payload</object.custom></object></extension>");
		scope.Tree.Load();
		var builtin = scope.Builtin("/Items/Item");

		Assert.True(builtin.Behaviors.TryGet("CUSTOM", out var behavior));
		Assert.Same(builtin, behavior.Builtin);
		Assert.Equal("fixture payload", behavior.Text);
		Assert.True(behavior.GetPropertyValue<bool>("enabled"));
		Assert.Equal(7, builtin.Behaviors.GetBehaviorValue<int>("custom.count"));
		Assert.Equal(23, builtin.Behaviors.GetBehaviorValue("missing.count", 23));
		Assert.Equal(23, behavior.GetPropertyValue("missing", typeof(int), 23));
		Assert.Throws<ArgumentException>(() => builtin.Behaviors.GetBehaviorValue<int>("custom"));
		Assert.Throws<ArgumentException>(() => builtin.Behaviors.Add("custom"));
	}

	[Fact]
	public void FoundationElementsAreLazyCachedAndInheritedByChildren()
	{
		using var scope = new PluginTestScope();
		scope.WriteFile("nested/Child.plugin", "<plugin name=\"Child\" />");
		scope.Tree.Load();
		var main = scope.Tree.Plugins["Main"];
		var child = main.Children["Child"];
		var builder = main.Builders["object"];
		Assert.False(builder.HasValue);
		Assert.Equal(typeof(ObjectBuilder), builder.Type);
		Assert.False(builder.HasValue);

		Assert.Same(main.GetBuilder("object"), child.GetBuilder("OBJECT"));
		Assert.Same(builder.Value, main.GetBuilder("object"));
		Assert.True(builder.HasValue);
		Assert.Same(main.GetParser("type"), child.GetParser("TYPE"));
		Assert.Null(child.GetParser("unknown"));
		Assert.Null(child.GetBuilder("unknown"));

		child.Parsers.Add(typeof(StaticParser), "type", child);

		Assert.IsType<StaticParser>(child.GetParser("type"));
		Assert.IsType<TypeParser>(main.GetParser("type"));
		child.Parsers.Remove("TYPE");
		Assert.Same(main.GetParser("type"), child.GetParser("type"));
	}

	[Fact]
	public void DependenciesCollapseDuplicateNamesAndRejectMutation()
	{
		using var scope = new PluginTestScope();
		scope.WriteFile("Feature.plugin", "<plugin name=\"Feature\"><manifest><dependencies><dependency name=\"Main\" /><dependency name=\"MAIN\" /></dependencies></manifest></plugin>");
		scope.Tree.Load();
		var dependencies = scope.Tree.Plugins["Feature"].Manifest.Dependencies;

		Assert.Single(dependencies);
		Assert.True(dependencies.Contains(" main "));
		var containsEquivalent = dependencies.Contains(new PluginDependency("main"));
		Assert.True(containsEquivalent);
		Assert.False(dependencies.Contains((string)null));
		var containsNull = dependencies.Contains((PluginDependency)null);
		Assert.False(containsNull);
		var collection = (ICollection<PluginDependency>)dependencies;
		var copy = new PluginDependency[2];
		collection.CopyTo(copy, 1);
		Assert.Same(dependencies["main"], copy[1]);
		Assert.True(collection.IsReadOnly);
		Assert.Throws<NotSupportedException>(() => collection.Clear());
		Assert.Throws<NotSupportedException>(() => collection.Add(new PluginDependency("Other")));
		Assert.Throws<NotSupportedException>(() => collection.Remove(copy[1]));
	}

	[Fact]
	public void SettingsFlagsAccumulateWithoutDiscardingExistingOptions()
	{
		var settings = BuilderSettings.Create(BuilderSettingsFlags.IgnoreChildren);
		settings.SetFlags(BuilderSettingsFlags.IgnoreAppending);

		Assert.True(settings.HasFlags(BuilderSettingsFlags.IgnoreChildren | BuilderSettingsFlags.IgnoreAppending));
		Assert.Equal(BuilderSettingsFlags.IgnoreChildren | BuilderSettingsFlags.IgnoreAppending, settings.Flags);
	}
}
