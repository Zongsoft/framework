using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using Xunit;

using Microsoft.Extensions.Configuration;

using Zongsoft.Configuration;

namespace Zongsoft.Plugins.Tests;

public class PluginUnloadTests
{
	[Fact]
	public void UnloadDestroysBuiltComponentsAndDetachesConfiguration()
	{
		using var scope = new PluginTestScope();
		scope.WritePlugin($"<extension path=\"/Items\"><object name=\"Built\" type=\"{typeof(ObjectBuilderTests.DisposableObject).AssemblyQualifiedName}\" /><object name=\"Deferred\" type=\"System.Object\" /></extension>");
		scope.WriteFile("Feature.option", "<options><option path=\"/Fixture\"><settings value=\"owned\" /></option></options>");
		using var provider = new PluginConfigurationProvider(new PluginConfigurationSource(scope.Options));
		scope.Tree.Load();
		var plugin = scope.Tree.Plugins["Feature"];
		var value = Assert.IsType<ObjectBuilderTests.DisposableObject>(scope.Builtin("/Items/Built").GetValue(ObtainMode.Auto));
		var deferred = scope.Builtin("/Items/Deferred");
		var states = new List<PluginStatus>();
		scope.Tree.Loader.PluginUnloading += (_, args) => states.Add(args.Plugin.Status);
		scope.Tree.Loader.PluginUnloaded += (_, args) => states.Add(args.Plugin.Status);
		var file = Assert.IsAssignableFrom<FileConfigurationProvider>(Assert.Single(Assert.IsType<CompositeConfigurationProvider>(Assert.Single(provider.Providers)).Providers));
		var changed = provider.GetReloadToken();

		Unload(scope.Tree.Loader, plugin);
		Unload(scope.Tree.Loader, plugin);

		Assert.True(changed.HasChanged);
		Assert.NotSame(changed, provider.GetReloadToken());

		Assert.Equal(new[] { PluginStatus.Loaded, PluginStatus.Unloaded }, states);
		Assert.Equal(PluginStatus.Unloaded, plugin.Status);
		Assert.Equal(1, value.Disposals);
		Assert.False(deferred.IsBuilded);
		Assert.Empty(plugin.Builtins);
		Assert.Null(scope.Tree.Find("/Items/Built"));
		Assert.Null(scope.Tree.Find("/Items/Deferred"));
		Assert.False(scope.Tree.Plugins.Contains("Feature"));
		Assert.Empty(provider.Providers);

		var current = provider.GetReloadToken();
		file.Load();

		Assert.False(current.HasChanged);
	}

	[Fact]
	public void UnloadingCanBeCancelledBeforeAnyMutation()
	{
		using var scope = new PluginTestScope();
		scope.WritePlugin("<extension path=\"/Items\"><object name=\"Item\" type=\"System.Object\" /></extension>");
		scope.Tree.Load();
		var plugin = scope.Tree.Plugins["Feature"];
		var node = scope.Tree.Find("/Items/Item");
		var unloaded = false;
		scope.Tree.Loader.PluginUnloading += (_, args) => args.Cancel = true;
		scope.Tree.Loader.PluginUnloaded += (_, _) => unloaded = true;

		Unload(scope.Tree.Loader, plugin);

		Assert.False(unloaded);
		Assert.Equal(PluginStatus.Loaded, plugin.Status);
		Assert.Same(plugin, scope.Tree.Plugins["Feature"]);
		Assert.Same(node, scope.Tree.Find("/Items/Item"));
		Assert.Single(plugin.Builtins);
	}

	[Fact]
	public void UnloadReleasesInstantiatedFoundationElementsOnly()
	{
		using var scope = new PluginTestScope(false);
		scope.WriteFile("Feature.plugin", $"<plugin name=\"Feature\"><builders><builder name=\"active\" type=\"{typeof(DisposableBuilder).AssemblyQualifiedName}\" /><builder name=\"unused\" type=\"{typeof(DisposableBuilder).AssemblyQualifiedName}\" /></builders><parsers><parser name=\"active\" type=\"{typeof(DisposableParser).AssemblyQualifiedName}\" /></parsers></plugin>");
		scope.Tree.Load();
		var plugin = Assert.Single(scope.Tree.Plugins);
		var builder = Assert.IsType<DisposableBuilder>(plugin.GetBuilder("active"));
		var parser = Assert.IsType<DisposableParser>(plugin.GetParser("active"));
		var unused = plugin.Builders["unused"];

		Unload(scope.Tree.Loader, plugin);

		Assert.Equal(1, builder.Disposals);
		Assert.Equal(1, parser.Disposals);
		Assert.False(unused.HasValue);
		Assert.Empty(plugin.Builders);
		Assert.Empty(plugin.Parsers);
	}

	//卸载入口是 internal；仅在这里反射进入，再断言节点、事件和资源的可观察结果。
	//不为测试扩大生产 API，也不访问加载器的私有字段。
	private static void Unload(PluginLoader loader, Plugin plugin)
	{
		var method = typeof(PluginLoader).GetMethod("Unload", BindingFlags.Instance | BindingFlags.NonPublic, [typeof(Plugin)]);
		Assert.NotNull(method);

		try
		{
			method.Invoke(loader, [plugin]);
		}
		catch(TargetInvocationException error) when(error.InnerException != null)
		{
			System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error.InnerException).Throw();
		}
	}

	public sealed class DisposableBuilder : Builders.ObjectBuilder
	{
		public int Disposals { get; private set; }
		protected override void Dispose(bool disposing) => this.Disposals++;
	}

	public sealed class DisposableParser : Parsers.Parser, IDisposable
	{
		public int Disposals { get; private set; }
		public override object Parse(Parsers.ParserContext context) => context.Text;
		public void Dispose() => this.Disposals++;
	}
}
