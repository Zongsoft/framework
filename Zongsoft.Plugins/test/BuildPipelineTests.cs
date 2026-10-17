using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Xunit;

using Zongsoft.Plugins.Builders;

namespace Zongsoft.Plugins.Tests;

public class BuildPipelineTests
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void BuildControlsEachInstanceAndCachedTraversalPreservesItsDecision(bool childFirst)
	{
		using var scope = new PluginTestScope();
		scope.WritePlugin($"<builders><builder name=\"conditional\" type=\"{typeof(ConditionalBuilder).AssemblyQualifiedName}\" /></builders><extension path=\"/Items\"><object name=\"Parent\" type=\"System.Collections.ArrayList\"><conditional name=\"Hidden\"><object name=\"Child\" type=\"System.Object\" /></conditional><conditional name=\"Deferred\"><object name=\"Child\" type=\"System.Object\" /></conditional><conditional name=\"Normal\"><object name=\"Child\" type=\"System.Object\" /></conditional></object></extension>");
		scope.Tree.Load();
		var builder = (ConditionalBuilder)scope.Tree.Plugins["Feature"].GetBuilder("conditional");

		if(childFirst)
		{
			foreach(var name in new[] { "Hidden", "Deferred", "Normal" })
			{
				scope.Builtin("/Items/Parent/" + name).GetValue(ObtainMode.Auto);
			}

			//缓存复用不能重新询问构建器或受之后改变的条件影响。
			builder.EnableHidden = true;
		}

		var parent = Assert.IsType<ArrayList>(scope.Builtin("/Items/Parent").GetValue(ObtainMode.Auto));

		Assert.Equal(3, builder.Builds);
		Assert.Equal(2, parent.Count);
		Assert.DoesNotContain(scope.Builtin("/Items/Parent/Hidden").Value, parent.Cast<object>());
		Assert.True(scope.Builtin("/Items/Parent/Hidden/Child").IsBuilded);
		Assert.False(scope.Builtin("/Items/Parent/Deferred/Child").IsBuilded);
		Assert.True(scope.Builtin("/Items/Parent/Normal/Child").IsBuilded);
		var deferred = Assert.IsType<ArrayList>(scope.Builtin("/Items/Parent/Deferred").Value);
		var child = scope.Builtin("/Items/Parent/Deferred/Child").GetValue(ObtainMode.Auto);
		Assert.Same(child, Assert.Single(deferred));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void OwnerBuilderReceivesChildrenRegardlessOfTheirBuildOrder(bool childFirst)
	{
		using var scope = new PluginTestScope();
		scope.WritePlugin($"<builders><builder name=\"receiver\" type=\"{typeof(ReceivingBuilder).AssemblyQualifiedName}\" /><builder name=\"item\" type=\"{typeof(ItemBuilder).AssemblyQualifiedName}\" /></builders><extension path=\"/Items\"><receiver name=\"Parent\"><item name=\"Child\" /></receiver></extension>");
		scope.Tree.Load();
		var child = scope.Builtin("/Items/Parent/Child");

		if(childFirst)
			child.GetValue(ObtainMode.Auto);

		var parent = Assert.IsType<ArrayList>(scope.Builtin("/Items/Parent").GetValue(ObtainMode.Auto));

		Assert.Same(child.Value, Assert.Single(parent));
		Assert.Same(child.Value, child.GetValue(ObtainMode.Auto));
		Assert.Single(parent);
		Assert.Equal(1, ((ReceivingBuilder)scope.Tree.Plugins["Feature"].GetBuilder("receiver")).Appends);
	}

	[Fact]
	public void ChildAppenderCannotSubstituteForAnOwnerWithoutOne()
	{
		using var scope = new PluginTestScope();
		scope.WritePlugin($"<builders><builder name=\"container\" type=\"{typeof(ContainerBuilder).AssemblyQualifiedName}\" /></builders><extension path=\"/Items\"><container name=\"Parent\"><object name=\"Child\" type=\"System.Object\" /></container></extension>");
		scope.Tree.Load();

		Assert.Empty(Assert.IsType<ArrayList>(scope.Builtin("/Items/Parent").Build()));
		Assert.True(scope.Builtin("/Items/Parent/Child").IsBuilded);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void ExposedPropertyReceivesChildrenWithoutAppendingItself(bool exposeFirst)
	{
		using var scope = new PluginTestScope();
		scope.WritePlugin($$"""
			<extension path="/Items">
				<object name="Parent" type="{{typeof(PropertyContainer).AssemblyQualifiedName}}">
					<expose name="Items" value="{path:../@Items}">
						<object name="Child" type="System.Object" />
					</expose>
				</object>
			</extension>
			""");
		scope.Tree.Load();

		if(exposeFirst)
			scope.Builtin("/Items/Parent/Items").GetValue(ObtainMode.Auto);

		var parent = Assert.IsType<PropertyContainer>(scope.Builtin("/Items/Parent").GetValue(ObtainMode.Auto));
		Assert.Same(scope.Builtin("/Items/Parent/Items/Child").Value, Assert.Single(parent.Items));
		Assert.Same(parent.Items, scope.Builtin("/Items/Parent/Items").Value);
		Assert.True(scope.Builtin("/Items/Parent").IsBuilded);
		Assert.True(scope.Builtin("/Items/Parent/Items").IsBuilded);
		Assert.True(scope.Builtin("/Items/Parent/Items/Child").IsBuilded);
	}

	[Fact]
	public void ReferencedSiblingIsAppendedOnlyOnceWhenTraversalReachesIt()
	{
		using var scope = new PluginTestScope();
		scope.WritePlugin($$"""
			<extension path="/Items">
				<object name="Parent" type="System.Collections.ArrayList">
					<object name="First" type="{{typeof(ObjectBuilderTests.ReferenceObject).AssemblyQualifiedName}}" Reference="{path:../Second}" />
					<object name="Second" type="System.Object" />
				</object>
			</extension>
			""");
		scope.Tree.Load();

		var parent = Assert.IsType<ArrayList>(scope.Builtin("/Items/Parent").Build());
		var first = Assert.IsType<ObjectBuilderTests.ReferenceObject>(scope.Builtin("/Items/Parent/First").Value);

		Assert.Equal(2, parent.Count);
		Assert.Same(scope.Builtin("/Items/Parent/Second").Value, first.Reference);
		Assert.Single(parent.Cast<object>(), item => ReferenceEquals(item, first.Reference));
	}

	[Fact]
	public void FindingModuleDefersItsChildrenUntilNormalConstruction()
	{
		using var scope = new PluginTestScope();
		scope.WritePlugin($"<extension path=\"/Modules\"><object name=\"Orders\" type=\"{typeof(TestModule).AssemblyQualifiedName}\"><object name=\"Consumer\" type=\"{typeof(ModuleConsumer).AssemblyQualifiedName}\" /><object name=\"Other\" type=\"System.Object\" /></object></extension>");
		scope.Tree.Load();

		var consumer = Assert.IsType<ModuleConsumer>(scope.Builtin("/Modules/Orders/Consumer").GetValue(ObtainMode.Auto));
		var module = scope.Builtin("/Modules/Orders");
		Assert.Same(module.Value, consumer.Module);
		Assert.False(module.IsBuilded);
		Assert.False(scope.Builtin("/Modules/Orders/Other").IsBuilded);

		Assert.Same(consumer.Module, module.GetValue(ObtainMode.Auto));
		Assert.True(module.IsBuilded);
		Assert.True(scope.Builtin("/Modules/Orders/Other").IsBuilded);
		Assert.Same(consumer, scope.Builtin("/Modules/Orders/Consumer").Value);
	}

	[Fact]
	public void IncompleteObjectIsNotReturnedAsSuccessfulAfterChildFailure()
	{
		using var scope = new PluginTestScope();
		scope.WritePlugin($"<extension path=\"/Items\"><object name=\"Parent\" type=\"System.Collections.ArrayList\"><object name=\"Good\" type=\"System.Object\" /><object name=\"Broken\" type=\"{typeof(ObjectBuilderTests.ThrowingObject).AssemblyQualifiedName}\" /></object><object name=\"Unrelated\" type=\"System.Object\" /></extension>");
		scope.Tree.Load();
		var parent = scope.Builtin("/Items/Parent");

		var error = Assert.Throws<PluginException>(() => parent.GetValue(ObtainMode.Auto));

		Assert.True(parent.HasValue);
		Assert.False(parent.IsBuilded);
		Assert.Same(error, Assert.Throws<PluginException>(() => parent.GetValue(ObtainMode.Auto)));
		Assert.Single(Assert.IsType<ArrayList>(parent.GetValue(ObtainMode.Never)));
		Assert.NotNull(scope.Builtin("/Items/Unrelated").GetValue(ObtainMode.Auto));
	}

	[Fact]
	public void ConstructorCycleFailsWithoutPassingNullOrPoisoningOtherBuilds()
	{
		using var scope = new PluginTestScope();
		scope.WritePlugin("<extension path=\"/Items\"><object name=\"First\" value=\"{path:../Second}\" /><object name=\"Second\" value=\"{path:../First}\" /><object name=\"Good\" type=\"System.Object\" /></extension>");
		scope.Tree.Load();

		Assert.Throws<PluginException>(() => scope.Builtin("/Items/First").GetValue(ObtainMode.Auto));
		Assert.Null(scope.Builtin("/Items/First").Value);
		Assert.Null(scope.Builtin("/Items/Second").Value);
		Assert.NotNull(scope.Builtin("/Items/Good").GetValue(ObtainMode.Auto));
	}

	[Fact]
	public void ReusingSettingsDoesNotCarryExposeOrRebuildSuppression()
	{
		using var scope = new PluginTestScope();
		scope.WritePlugin($"<builders><builder name=\"conditional\" type=\"{typeof(ConditionalBuilder).AssemblyQualifiedName}\" /></builders><extension path=\"/Items\"><conditional name=\"Deferred\"><expose name=\"Exposed\" type=\"System.Object\" /><object name=\"First\" type=\"System.Object\" /><object name=\"Second\" type=\"System.Object\" /></conditional></extension>");
		scope.Tree.Load();
		var parent = Assert.IsType<ArrayList>(scope.Builtin("/Items/Deferred").Build());
		var settings = new BuilderSettings(null);

		scope.Builtin("/Items/Deferred/Exposed").GetValue(ObtainMode.Auto, settings);
		scope.Builtin("/Items/Deferred/First").GetValue(ObtainMode.Alway, settings);
		var second = scope.Builtin("/Items/Deferred/Second").GetValue(ObtainMode.Auto, settings);

		Assert.Same(second, Assert.Single(parent));
		Assert.True(scope.Builtin("/Items/Deferred/Exposed").IsBuilded);
		Assert.True(scope.Builtin("/Items/Deferred/First").IsBuilded);
		Assert.True(scope.Builtin("/Items/Deferred/Second").IsBuilded);
	}

	public sealed class PropertyContainer
	{
		public ArrayList Items { get; } = new();
		public void Add(object value) => this.Items.Add(value);
	}

	public sealed class TestModule() : Services.ApplicationModule("Orders");
	public sealed class ModuleConsumer(Services.IApplicationModule module)
	{
		public Services.IApplicationModule Module { get; } = module;
	}

	public class ContainerBuilder : BuilderBase
	{
		public override object Build(BuilderContext context) => new ArrayList();
	}

	public sealed class ReceivingBuilder : ContainerBuilder, IAppender
	{
		public int Appends { get; private set; }
		public bool Append(AppenderContext context)
		{
			Assert.Same(context.ContainerNode, context.Node.Parent);
			Assert.IsType<ArrayList>(context.Container).Add(context.Value);
			this.Appends++;
			return true;
		}
	}

	public sealed class ItemBuilder : BuilderBase
	{
		public override object Build(BuilderContext context) => new object();
	}

	public sealed class ConditionalBuilder : BuilderBase, IAppender
	{
		public bool EnableHidden { get; set; }
		public int Builds { get; private set; }
		public override object Build(BuilderContext context)
		{
			this.Builds++;
			context.AppendToOwner = this.EnableHidden || context.Builtin.Name != "Hidden";
			context.BuildChildren = context.Builtin.Name != "Deferred";
			return new ArrayList();
		}
		public bool Append(AppenderContext context)
		{
			((ArrayList)context.Container).Add(context.Value);
			return true;
		}
	}
}
