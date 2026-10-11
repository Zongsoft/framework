using System;
using System.Linq;
using System.Reflection;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;

using Xunit;

using Microsoft.Extensions.DependencyInjection;

using Zongsoft.Plugins.Builders;

namespace Zongsoft.Plugins.Tests;

public class ObjectBuilderTests
{
	[Fact]
	public void ConstructorReceivesPluginContextAndRegisteredServices()
	{
		var service = new TestService();
		using var scope = new PluginTestScope(configure: services => services.AddSingleton(service));
		scope.WritePlugin($"<extension path=\"/Items\"><object name=\"Constructed\" type=\"{typeof(ContextObject).AssemblyQualifiedName}\" Number=\"42\" /></extension>");
		scope.Tree.Load();
		var builtin = scope.Builtin("/Items/Constructed");

		var value = Assert.IsType<ContextObject>(builtin.GetValue(ObtainMode.Auto));

		Assert.Equal("Constructed", value.Name);
		Assert.Same(builtin, value.Builtin);
		Assert.Same(builtin.Node, value.Node);
		Assert.Same(builtin.Plugin, value.Plugin);
		Assert.Same(scope.Tree, value.Tree);
		Assert.Same(scope.Application, value.Application);
		Assert.Same(scope.Application.Services, value.Services);
		Assert.Same(service, value.Service);
		Assert.Same(service, value.Injected);
		Assert.Equal(42, value.Number);
	}

	[Theory]
	[InlineData("<parameter name=\"count\" value=\"17\" />")]
	[InlineData("<parameter type=\"System.Int32\" value=\"17\" />")]
	[InlineData("<parameter name=\"count\" value=\"1\">17</parameter>")]
	public void ExplicitConstructorParametersUseNamesTypesAndText(string parameter)
	{
		using var scope = new PluginTestScope();
		scope.WritePlugin($"<extension path=\"/Items\"><object name=\"Item\" type=\"{typeof(ConstructorObject).AssemblyQualifiedName}\"><object.constructor>{parameter}</object.constructor></object></extension>");
		scope.Tree.Load();
		var builtin = scope.Builtin("/Items/Item");

		var value = Assert.IsType<ConstructorObject>(builtin.GetValue(ObtainMode.Auto));

		Assert.Equal(17, value.Count);
		var arguments = builtin.BuiltinType.Constructor.Parameters;
		Assert.Single(arguments);
		Assert.Equal("17", arguments[0].RawValue);
		Assert.Equal(17, arguments[0].GetValue(typeof(int)));
		Assert.True(arguments[0].HasValue);
	}

	[Fact]
	public void ExplicitParametersCanBeCombinedWithInjectedServices()
	{
		var service = new TestService();
		using var scope = new PluginTestScope(configure: services => services.AddSingleton(service));
		scope.WritePlugin($"<extension path=\"/Items\"><object name=\"Item\" type=\"{typeof(MixedConstructorObject).AssemblyQualifiedName}\"><object.constructor><parameter name=\"count\" value=\"23\" /></object.constructor></object></extension>");
		scope.Tree.Load();

		var value = Assert.IsType<MixedConstructorObject>(scope.Builtin("/Items/Item").GetValue(ObtainMode.Auto));

		Assert.Equal(23, value.Count);
		Assert.Same(service, value.Service);
	}

	[Theory]
	[InlineData("object", false)]
	[InlineData("lazy", true)]
	public void LazyBuilderDefersChildren(string scheme, bool deferred)
	{
		using var scope = new PluginTestScope();
		scope.WritePlugin($"<extension path=\"/Items\"><{scheme} name=\"Container\" type=\"System.Collections.ArrayList\"><object name=\"Child\" type=\"System.Object\" /></{scheme}></extension>");
		scope.Tree.Load();
		var child = scope.Builtin("/Items/Container/Child");

		var value = Assert.IsType<ArrayList>(scope.Builtin("/Items/Container").GetValue(ObtainMode.Auto));

		Assert.Equal(deferred ? 0 : 1, value.Count);
		Assert.Equal(!deferred, child.IsBuilded);
		var childValue = child.GetValue(ObtainMode.Auto);
		Assert.Same(childValue, Assert.Single(value.Cast<object>()));
	}

	[Fact]
	public void ObtainModesPreserveCachedIdentityAndRebuildWithoutAppending()
	{
		using var scope = new PluginTestScope();
		scope.WritePlugin("<extension path=\"/Items\"><object name=\"Container\" type=\"System.Collections.ArrayList\"><object name=\"Child\" type=\"System.Object\" /></object></extension>");
		scope.Tree.Load();

		var builtin = scope.Builtin("/Items/Container/Child");
		Assert.Null(builtin.GetValue(ObtainMode.Never));
		Assert.False(builtin.IsBuilded);
		Assert.Equal(typeof(object), builtin.GetValueType());
		Assert.False(builtin.HasValue);

		var parent = Assert.IsType<ArrayList>(scope.Builtin("/Items/Container").GetValue(ObtainMode.Auto));
		var original = builtin.GetValue(ObtainMode.Auto);
		var replacement = builtin.GetValue(ObtainMode.Alway);

		Assert.NotSame(original, replacement);
		Assert.Same(replacement, builtin.GetValue(ObtainMode.Never));
		Assert.Same(replacement, builtin.GetValue(ObtainMode.Auto));
		Assert.Same(original, Assert.Single(parent.Cast<object>()));
	}

	[Fact]
	public async Task ConcurrentAutoReadsReturnOneConstructedInstanceAsync()
	{
		using var scope = new PluginTestScope();
		scope.WritePlugin("<extension path=\"/Items\"><object name=\"Item\" type=\"System.Object\" /></extension>");
		scope.Tree.Load();
		var builtin = scope.Builtin("/Items/Item");
		var builds = 0;
		builtin.ValueChanged += (_, _) => System.Threading.Interlocked.Increment(ref builds);

		var results = await Task.WhenAll(Enumerable.Range(0, 16).Select(_ => Task.Run(() => builtin.GetValue(ObtainMode.Auto))));

		Assert.Equal(1, builds);
		Assert.All(results, result => Assert.Same(results[0], result));
	}

	[Fact]
	public void BuildSettingsDeferChildrenAndExposeCompletionContext()
	{
		using var scope = new PluginTestScope();
		scope.WritePlugin("<extension path=\"/Items\"><object name=\"Container\" type=\"System.Collections.ArrayList\"><object name=\"Child\" type=\"System.Object\" /></object></extension>");
		scope.Tree.Load();
		var builtin = scope.Builtin("/Items/Container");
		var events = new List<string>();

		builtin.ValueChanging += (_, args) =>
		{
			Assert.Null(args.OldValue);
			Assert.Null(builtin.Value);
			Assert.IsType<ArrayList>(args.NewValue);
			events.Add("changing");
		};
		builtin.ValueChanged += (_, args) =>
		{
			Assert.Same(builtin.Value, args.Value);
			events.Add("changed");
		};

		var settings = BuilderSettings.Create(BuilderSettingsFlags.IgnoreChildren);
		settings.Builded = context =>
		{
			Assert.Same(builtin, context.Builtin);
			Assert.Same(scope.Tree, context.PluginTree);
			Assert.Same(builtin.Value, context.Result);
			Assert.True(builtin.IsBuilded);
			events.Add("callback");
		};

		var result = Assert.IsType<ArrayList>(builtin.GetValue(ObtainMode.Auto, settings));

		Assert.Empty(result);
		Assert.False(scope.Builtin("/Items/Container/Child").IsBuilded);
		Assert.Equal(new[] { "changing", "changed", "callback" }, events);
	}

	[Theory]
	[InlineData(typeof(List<SimpleObject>))]
	[InlineData(typeof(Dictionary<string, SimpleObject>))]
	[InlineData(typeof(DefaultContainer))]
	public void ChildTypesAreInferredFromCollectionsAndDefaultMembers(Type containerType)
	{
		using var scope = new PluginTestScope();
		scope.WritePlugin($"<extension path=\"/Items\"><object name=\"Container\" type=\"{containerType.AssemblyQualifiedName}\"><object name=\"Child\" Number=\"9\" /></object></extension>");
		scope.Tree.Load();

		var container = scope.Builtin("/Items/Container").GetValue(ObtainMode.Auto);
		var child = Assert.IsType<SimpleObject>(scope.Builtin("/Items/Container/Child").GetValue(ObtainMode.Never));

		Assert.Equal(9, child.Number);
		if(container is List<SimpleObject> list)
			Assert.Same(child, Assert.Single(list));
		else if(container is Dictionary<string, SimpleObject> dictionary)
			Assert.Same(child, Assert.Single(dictionary).Value);
		else
			Assert.Same(child, Assert.Single(((DefaultContainer)container).Items));
	}

	[Theory]
	[InlineData(typeof(ArrayList))]
	[InlineData(typeof(Hashtable))]
	[InlineData(typeof(RegisterContainer))]
	public void ChildrenAppendThroughNonGenericAndRegisterContracts(Type containerType)
	{
		using var scope = new PluginTestScope();
		scope.WritePlugin($"<extension path=\"/Items\"><object name=\"Container\" type=\"{containerType.AssemblyQualifiedName}\"><object name=\"Child\" type=\"System.Object\" /></object></extension>");
		scope.Tree.Load();

		var container = scope.Builtin("/Items/Container").GetValue(ObtainMode.Auto);
		var child = scope.Builtin("/Items/Container/Child").Value;

		if(container is ArrayList list)
			Assert.Same(child, Assert.Single(list.Cast<object>()));
		else if(container is Hashtable dictionary)
			Assert.Same(child, dictionary["Child"]);
		else
			Assert.Same(child, ((RegisterContainer)container).Items["Child"]);
	}

	[Fact]
	public void ExtendedPropertiesSupportTextNestedObjectsAndDottedMembers()
	{
		using var scope = new PluginTestScope();
		scope.WritePlugin($"""
			<extension path="/Items">
				<object name="Item" type="{typeof(ComplexObject).AssemblyQualifiedName}" Detail.Number="5">
					<Item.Text>fixture text</Item.Text>
					<Item.Child><object type="{typeof(SimpleObject).AssemblyQualifiedName}" Number="8" /></Item.Child>
				</object>
				<Item.Number>13</Item.Number>
			</extension>
			""");
		scope.Tree.Load();

		var value = Assert.IsType<ComplexObject>(scope.Builtin("/Items/Item").GetValue(ObtainMode.Auto));

		Assert.Equal("fixture text", value.Text);
		Assert.Equal(5, value.Detail.Number);
		Assert.Equal(8, value.Child.Number);
		Assert.Equal(13, value.Number);
	}

	[Fact]
	public void PropertyBehaviorAssignsBuiltObjectToAnotherTarget()
	{
		using var scope = new PluginTestScope();
		scope.WritePlugin($$"""
			<extension path="/Items">
				<object name="Target" type="{{typeof(ComplexObject).AssemblyQualifiedName}}" />
				<object name="Child" type="{{typeof(SimpleObject).AssemblyQualifiedName}}" Number="21">
					<object.property name="Child" target="{path:/Items/Target}" value="{path:.}" />
				</object>
			</extension>
			""");
		scope.Tree.Load();

		var child = scope.Builtin("/Items/Child").GetValue(ObtainMode.Auto);
		var target = Assert.IsType<ComplexObject>(scope.Builtin("/Items/Target").Value);

		Assert.Same(child, target.Child);
		Assert.Equal(21, target.Child.Number);
	}

	[Fact]
	public void FailedConstructionDoesNotPoisonSubsequentBuilds()
	{
		using var scope = new PluginTestScope();
		scope.WritePlugin($"<extension path=\"/Items\"><object name=\"Broken\" type=\"{typeof(ThrowingObject).AssemblyQualifiedName}\" /><object name=\"Good\" type=\"System.Object\" /></extension>");
		scope.Tree.Load();
		var broken = scope.Builtin("/Items/Broken");

		var error = Assert.Throws<PluginException>(() => broken.GetValue(ObtainMode.Auto));

		Assert.IsType<InvalidOperationException>(Assert.IsType<TargetInvocationException>(error.InnerException).InnerException);
		Assert.False(broken.IsBuilded);
		Assert.Null(broken.Value);
		Assert.IsType<object>(scope.Builtin("/Items/Good").GetValue(ObtainMode.Auto));
		Assert.Throws<PluginException>(() => broken.GetValue(ObtainMode.Auto));
	}

	[Fact]
	public void DestroyDisposesBuiltValueOnceAndClearsCache()
	{
		using var scope = new PluginTestScope();
		scope.WritePlugin($"<extension path=\"/Items\"><object name=\"Item\" type=\"{typeof(DisposableObject).AssemblyQualifiedName}\" /></extension>");
		scope.Tree.Load();
		BuilderContext captured = null;
		var builtin = scope.Builtin("/Items/Item");
		var value = Assert.IsType<DisposableObject>(builtin.Build(new BuilderSettings(null, context => captured = context)));

		captured.Builder.Destroy(captured);
		captured.Builder.Destroy(captured);

		Assert.Equal(1, value.Disposals);
		Assert.Null(builtin.GetValue(ObtainMode.Never));
		Assert.NotSame(value, builtin.GetValue(ObtainMode.Auto));
	}

	[Fact]
	public void AssemblyAndResourceConstructorArgumentsComeFromThePluginManifest()
	{
		using var scope = new PluginTestScope();
		scope.WriteFile("Feature.plugin", $"<plugin name=\"Feature\"><manifest><dependencies><dependency name=\"Main\" /></dependencies><assemblies><assembly name=\"Zongsoft.Plugins.Tests\" /></assemblies></manifest><extension path=\"/Items\"><object name=\"Item\" type=\"{typeof(ResourceObject).AssemblyQualifiedName}\" /></extension></plugin>");
		scope.Tree.Load();

		var value = Assert.IsType<ResourceObject>(scope.Builtin("/Items/Item").Build());

		Assert.Same(typeof(ResourceObject).Assembly, value.Assembly);
		Assert.Same(Resources.Resource.GetResource(typeof(ResourceObject).Assembly), value.Resource);
	}

	[Fact]
	public void ModuleConstructorAndPropertyInjectionUseTheOwningModuleServices()
	{
		var service = new TestService();
		using var localServices = new ServiceCollection().AddSingleton(service).BuildServiceProvider();
		using var scope = new PluginTestScope();
		scope.WritePlugin($"<extension path=\"/Modules/Orders\"><object name=\"Item\" type=\"{typeof(ModuleObject).AssemblyQualifiedName}\" /></extension>");
		scope.Tree.Load();
		var module = new TestModule(localServices);
		scope.Tree.Mount("/Modules/Orders", module);

		var value = Assert.IsType<ModuleObject>(scope.Builtin("/Modules/Orders/Item").Build());

		Assert.Same(module, value.Module);
		Assert.Same(localServices, value.Services);
		Assert.Same(service, value.Service);
		Assert.Same(service, value.Injected);
	}

	[Theory]
	[InlineData("System.IDisposable")]
	[InlineData("System.IO.Stream")]
	[InlineData("System.String")]
	public void UnconstructibleTypesFailWithoutCachingAValue(string typeName)
	{
		using var scope = new PluginTestScope();
		scope.WritePlugin($"<extension path=\"/Items\"><object name=\"Item\" type=\"{typeName}\" /></extension>");
		scope.Tree.Load();
		var builtin = scope.Builtin("/Items/Item");

		Assert.Throws<PluginException>(() => builtin.Build());

		Assert.False(builtin.IsBuilded);
		Assert.Null(builtin.Value);
	}

	[Fact]
	public void TargetTypeAndIgnoredPropertiesAreHonored()
	{
		using var scope = new PluginTestScope();
		scope.WritePlugin($"<extension path=\"/Items\"><object name=\"Item\" type=\"{typeof(SimpleObject).AssemblyQualifiedName}\" Number=\"7\" Ignored=\"anything\" /><object name=\"Inferred\" Number=\"11\" /></extension>");
		scope.Tree.Load();
		var builtin = scope.Builtin("/Items/Item");

		var value = Assert.IsType<SimpleObject>(PluginUtility.BuildBuiltin(builtin, new BuilderSettings(typeof(SimpleObject)), ["ignored"]));

		Assert.Equal(7, value.Number);
		Assert.Throws<PluginException>(() => builtin.Build());

		var inferred = scope.Builtin("/Items/Inferred").Build(new BuilderSettings(typeof(SimpleObject)));

		Assert.Equal(11, Assert.IsType<SimpleObject>(inferred).Number);
	}

	public sealed class TestService;
	public sealed class ResourceObject(Assembly assembly, Resources.IResource resource)
	{
		public Assembly Assembly { get; } = assembly;
		public Resources.IResource Resource { get; } = resource;
	}

	public sealed class ModuleObject(Services.IApplicationModule module, IServiceProvider services, TestService service)
	{
		public Services.IApplicationModule Module { get; } = module;
		public IServiceProvider Services { get; } = services;
		public TestService Service { get; } = service;
		[Services.ServiceDependency]
		public TestService Injected { get; set; }
	}

	private sealed class TestModule(IServiceProvider services) : Services.ApplicationModule("Orders")
	{
		public override IServiceProvider Services => services;
	}

	public sealed class ContextObject(string name, Builtin builtin, PluginTreeNode node, Plugin plugin, PluginTree tree, Services.IApplicationContext application, IServiceProvider services, TestService service)
	{
		public string Name { get; } = name;
		public Builtin Builtin { get; } = builtin;
		public PluginTreeNode Node { get; } = node;
		public Plugin Plugin { get; } = plugin;
		public PluginTree Tree { get; } = tree;
		public Services.IApplicationContext Application { get; } = application;
		public IServiceProvider Services { get; } = services;
		public TestService Service { get; } = service;
		public int Number { get; set; }
		[Services.ServiceDependency]
		public TestService Injected { get; set; }
	}

	public sealed class ConstructorObject(int count)
	{
		public int Count { get; } = count;
	}

	public sealed class MixedConstructorObject(int count, TestService service)
	{
		public int Count { get; } = count;
		public TestService Service { get; } = service;
	}

	public sealed class SimpleObject
	{
		public int Number { get; set; }
	}

	public sealed class ComplexObject
	{
		public int Number { get; set; }
		public string Text { get; set; }
		public SimpleObject Child { get; set; }
		public SimpleObject Detail { get; } = new();
	}

	[DefaultProperty(nameof(Items))]
	public sealed class DefaultContainer
	{
		public List<SimpleObject> Items { get; } = [];
	}

	public sealed class RegisterContainer
	{
		public Dictionary<string, object> Items { get; } = [];
		public void Register(string name, object value) => this.Items.Add(name, value);
	}

	public sealed class ThrowingObject
	{
		public ThrowingObject() => throw new InvalidOperationException();
	}

	public sealed class DisposableObject : IDisposable
	{
		public int Disposals { get; private set; }
		public void Dispose() => this.Disposals++;
	}
}
