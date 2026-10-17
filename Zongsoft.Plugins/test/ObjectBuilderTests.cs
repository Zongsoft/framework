using System;
using System.Linq;
using System.Reflection;
using System.Globalization;
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
	public void ValueEventsPublishTheInstanceBeforeChildrenAreAssembled()
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
			Assert.Empty(Assert.IsType<ArrayList>(args.Value));
			Assert.False(builtin.IsBuilded);
			events.Add("changed");
		};

		var result = Assert.IsType<ArrayList>(builtin.GetValue(ObtainMode.Auto));

		Assert.Single(result);
		Assert.True(builtin.IsBuilded);
		Assert.True(scope.Builtin("/Items/Container/Child").IsBuilded);
		Assert.Equal(new[] { "changing", "changed" }, events);
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

	[Theory]
	[InlineData("{option:/Fixture/Url}", "tcp://127.0.0.1:1883")]
	[InlineData("{path:/Values/Text}", "resolved path text")]
	[InlineData("{static:System.String.Empty, System.Private.CoreLib}", "")]
	public void StringPropertiesResolveExpressionsBeforeConversion(string expression, string expected)
	{
		using var scope = new PluginTestScope(settings: new Dictionary<string, string> { ["Fixture:Url"] = "tcp://127.0.0.1:1883" });
		scope.WritePlugin($"<extension path=\"/Items\"><object name=\"Item\" type=\"{typeof(ComplexObject).AssemblyQualifiedName}\" Text=\"{expression}\" /></extension>");
		scope.Tree.Load();
		scope.Tree.Mount("/Values/Text", "resolved path text");

		var value = Assert.IsType<ComplexObject>(scope.Builtin("/Items/Item").Build());

		Assert.Equal(expected, value.Text);
		Assert.NotEqual(expression, value.Text);
	}

	[Fact]
	public void ObjectPropertyPreservesResolvedNodeIdentity()
	{
		using var scope = new PluginTestScope();
		scope.WritePlugin($$"""
			<extension path="/Items">
				<object name="Source" type="{{typeof(SimpleObject).AssemblyQualifiedName}}" Number="29" />
				<object name="Target" type="{{typeof(ReferenceObject).AssemblyQualifiedName}}" Reference="{path:/Items/Source}" />
			</extension>
			""");
		scope.Tree.Load();

		var target = Assert.IsType<ReferenceObject>(scope.Builtin("/Items/Target").Build());

		Assert.Same(scope.Builtin("/Items/Source").GetValue(ObtainMode.Never), target.Reference);
		Assert.Equal(29, Assert.IsType<SimpleObject>(target.Reference).Number);
	}

	[Fact]
	public void ConstructorAndPropertyConvertTheSameConfiguredText()
	{
		ConvertedNumberConverter.Calls = 0;
		using var scope = new PluginTestScope();
		scope.WritePlugin($"<extension path=\"/Items\"><object name=\"Item\" type=\"{typeof(ConvertedConstructorObject).AssemblyQualifiedName}\" Token=\"37\" /></extension>");
		scope.Tree.Load();

		var value = Assert.IsType<ConvertedConstructorObject>(scope.Builtin("/Items/Item").Build());

		Assert.Equal("37", value.ConstructorToken);
		Assert.Equal(37, value.Token.Number);
		Assert.Equal(1, ConvertedNumberConverter.Calls);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void PropertyConverterAppliesToColdAndCachedValues(bool cached)
	{
		ConvertedNumberConverter.Calls = 0;
		using var scope = new PluginTestScope();
		scope.WritePlugin($"<extension path=\"/Items\"><object name=\"Item\" type=\"{typeof(ConvertedObject).AssemblyQualifiedName}\" Token=\"43\" /></extension>");
		scope.Tree.Load();
		var builtin = scope.Builtin("/Items/Item");
		Assert.False(TypeDescriptor.GetConverter(typeof(ConvertedNumber)).CanConvertFrom(typeof(string)));

		if(cached)
			Assert.Equal("43", builtin.Properties["Token"].Value);

		Assert.Equal(0, ConvertedNumberConverter.Calls);

		var value = Assert.IsType<ConvertedObject>(builtin.Build());

		Assert.Equal(43, value.Token.Number);
		Assert.Equal(1, ConvertedNumberConverter.Calls);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void UnconvertiblePropertyFailsWithoutCachingAnObject(bool cached)
	{
		using var scope = new PluginTestScope();
		scope.WritePlugin($"<extension path=\"/Items\"><object name=\"Item\" type=\"{typeof(SimpleObject).AssemblyQualifiedName}\" Number=\"not-a-number\" /></extension>");
		scope.Tree.Load();
		var builtin = scope.Builtin("/Items/Item");

		if(cached)
			Assert.Equal("not-a-number", builtin.Properties["Number"].Value);

		var error = Assert.Throws<PluginException>(() => builtin.Build());

		Assert.NotNull(error.InnerException);
		Assert.False(builtin.IsBuilded);
		Assert.Null(builtin.Value);
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
		scope.WritePlugin($"<builders><builder name=\"object\" type=\"{typeof(BuilderLifecycleTests.RecordingBuilder).AssemblyQualifiedName}\" /></builders><extension path=\"/Items\"><object name=\"Item\" type=\"{typeof(DisposableObject).AssemblyQualifiedName}\" /></extension>");
		scope.Tree.Load();
		var builder = Assert.IsType<BuilderLifecycleTests.RecordingBuilder>(scope.Tree.Plugins["Feature"].GetBuilder("object"));
		var builtin = scope.Builtin("/Items/Item");
		var value = Assert.IsType<DisposableObject>(builtin.Build());
		var captured = Assert.Single(builder.Completed);

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

	public sealed class ReferenceObject
	{
		public object Reference { get; set; }
	}

	public sealed class ConvertedConstructorObject(string token)
	{
		public string ConstructorToken { get; } = token;
		[TypeConverter(typeof(ConvertedNumberConverter))]
		public ConvertedNumber Token { get; set; }
	}

	public sealed class ConvertedObject
	{
		[TypeConverter(typeof(ConvertedNumberConverter))]
		public ConvertedNumber Token { get; set; }
	}

	public sealed class ConvertedNumber(int number)
	{
		public int Number { get; } = number;
	}

	public sealed class ConvertedNumberConverter : TypeConverter
	{
		public static int Calls { get; set; }
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			if(value is not string text)
				return base.ConvertFrom(context, culture, value);

			Calls++;
			return new ConvertedNumber(int.Parse(text, CultureInfo.InvariantCulture));
		}
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
