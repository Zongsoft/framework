using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Xunit;

using Zongsoft.Plugins.Builders;

namespace Zongsoft.Plugins.Tests;

public class BuilderLifecycleTests
{
	[Fact]
	public void LifecycleDoesNotRequireBuilderBase()
	{
		using var scope = new PluginTestScope();
		scope.WritePlugin($"<builders><builder name=\"direct\" type=\"{typeof(DirectLifecycleBuilder).AssemblyQualifiedName}\" /></builders><extension path=\"/Items\"><direct name=\"Item\" /></extension>");
		scope.Tree.Load();
		var builder = Assert.IsType<DirectLifecycleBuilder>(scope.Tree.Plugins["Feature"].GetBuilder("direct"));
		var builtin = scope.Builtin("/Items/Item");

		var result = builtin.GetValue(ObtainMode.Auto);

		Assert.Same(result, builtin.GetValue(ObtainMode.Auto));
		Assert.Equal(1, builder.Started);
		Assert.Equal(1, builder.Completed);
	}

	[Fact]
	public void BuilderCanOperateWithoutLifecycleCallbacks()
	{
		using var scope = new PluginTestScope();
		scope.WritePlugin($"<builders><builder name=\"plain\" type=\"{typeof(PlainBuilder).AssemblyQualifiedName}\" /></builders><extension path=\"/Items\"><plain name=\"Item\" /></extension>");
		scope.Tree.Load();
		var builtin = scope.Builtin("/Items/Item");

		var result = builtin.GetValue(ObtainMode.Auto);

		Assert.NotNull(result);
		Assert.True(builtin.IsBuilded);
		Assert.Same(result, builtin.GetValue(ObtainMode.Auto));
		Assert.IsNotAssignableFrom<IBuilderLifecycle>(scope.Tree.Plugins["Feature"].GetBuilder("plain"));
	}

	[Fact]
	public void LifecycleSurroundsConstructionAndCompletesBeforeOwnerAppending()
	{
		using var scope = new PluginTestScope();
		var builder = Prepare(scope, "<object name=\"Parent\" type=\"System.Collections.ArrayList\"><object name=\"Child\" type=\"System.Object\" /></object>");
		builder.BeforeBuild = context =>
		{
			Assert.Null(context.Result);
			Assert.False(context.Builtin.IsBuilded);
			Assert.Same(scope.Tree, context.PluginTree);
		};
		builder.AfterBuild = context =>
		{
			Assert.False(context.Builtin.IsBuilded);
			Assert.Same(context.Result, context.Builtin.Value);
			Assert.Same(context.Result, context.Builtin.GetValue(ObtainMode.Auto));

			if(context.Builtin.Name == "Child")
			{
				Assert.Equal(1, context.Depth);
				Assert.Same(scope.Tree.Find("/Items/Parent"), context.OwnerNode);
				Assert.Empty(Assert.IsType<ArrayList>(context.Owner));
			}
			else
			{
				Assert.Same(scope.Builtin("/Items/Parent/Child").Value, Assert.Single(Assert.IsType<ArrayList>(context.Result)));
				Assert.True(scope.Builtin("/Items/Parent/Child").IsBuilded);
			}
		};

		var result = Assert.IsType<ArrayList>(scope.Builtin("/Items/Parent").GetValue(ObtainMode.Auto));

		Assert.Single(result);
		Assert.True(scope.Builtin("/Items/Parent").IsBuilded);
		Assert.Equal(new[] { "Building:Parent", "Build:Parent", "Building:Child", "Build:Child", "Builded:Child", "Builded:Parent" }, builder.Calls);
	}

	[Fact]
	public void CachedChildCanAppendLaterWithoutRepeatingItsLifecycle()
	{
		using var scope = new PluginTestScope();
		var builder = Prepare(scope, "<object name=\"Parent\" type=\"System.Collections.ArrayList\"><object name=\"Child\" type=\"System.Object\" /></object>");
		var child = scope.Builtin("/Items/Parent/Child");
		Assert.Equal(typeof(object), child.GetValueType());
		Assert.Null(child.GetValue(ObtainMode.Never));
		Assert.Empty(builder.Calls);

		var item = child.GetValue(ObtainMode.Auto);
		var parent = Assert.IsType<ArrayList>(scope.Builtin("/Items/Parent").GetValue(ObtainMode.Auto));

		Assert.Same(item, Assert.Single(parent));
		Assert.Same(item, child.GetValue(ObtainMode.Auto));
		Assert.Same(parent, scope.Builtin("/Items/Parent").GetValue(ObtainMode.Auto));
		Assert.Equal(new[] { "Building:Child", "Build:Child", "Builded:Child", "Building:Parent", "Build:Parent", "Builded:Parent" }, builder.Calls);
	}

	[Fact]
	public void DeferredParentCompletesOnceAfterItsActiveExposedChild()
	{
		using var scope = new PluginTestScope();
		var builder = Prepare(scope, $"<object name=\"Parent\" type=\"{typeof(BuildPipelineTests.PropertyContainer).AssemblyQualifiedName}\"><expose name=\"Items\" value=\"{{path:../@Items}}\"><object name=\"Child\" type=\"System.Object\" /></expose></object>",
			$"<builder name=\"expose\" type=\"{typeof(RecordingExposeBuilder).AssemblyQualifiedName}\" />");
		var expose = Assert.IsType<RecordingExposeBuilder>(scope.Tree.Plugins["Feature"].GetBuilder("expose"));
		expose.Calls = builder.Calls;
		builder.AfterBuild = context =>
		{
			if(context.Builtin.Name == "Parent")
			{
				Assert.True(scope.Builtin("/Items/Parent/Items").IsBuilded);
				Assert.Same(scope.Builtin("/Items/Parent/Items/Child").Value, Assert.Single(Assert.IsType<BuildPipelineTests.PropertyContainer>(context.Result).Items));
			}
		};

		var items = scope.Builtin("/Items/Parent/Items").GetValue(ObtainMode.Auto);
		var parent = Assert.IsType<BuildPipelineTests.PropertyContainer>(scope.Builtin("/Items/Parent").GetValue(ObtainMode.Auto));

		Assert.Same(parent.Items, items);
		Assert.Equal(new[] { "Child", "Items", "Parent" }, builder.Calls.Where(call => call.StartsWith("Builded:")).Select(call => call[8..]));
		Assert.Equal(1, builder.Calls.Count(call => call == "Building:Parent"));
		Assert.Equal(1, builder.Calls.Count(call => call == "Building:Items"));
	}

	[Fact]
	public void ModuleDiscoveryStartsLifecycleAndNormalAccessCompletesIt()
	{
		using var scope = new PluginTestScope();
		var builder = Prepare(scope, $"<object name=\"Orders\" type=\"{typeof(BuildPipelineTests.TestModule).AssemblyQualifiedName}\"><object name=\"Consumer\" type=\"{typeof(BuildPipelineTests.ModuleConsumer).AssemblyQualifiedName}\" /><object name=\"Other\" type=\"System.Object\" /></object>");
		var module = scope.Builtin("/Items/Orders");

		var consumer = Assert.IsType<BuildPipelineTests.ModuleConsumer>(scope.Builtin("/Items/Orders/Consumer").GetValue(ObtainMode.Auto));

		Assert.Same(consumer.Module, module.Value);
		Assert.False(module.IsBuilded);
		Assert.Contains("Building:Orders", builder.Calls);
		Assert.DoesNotContain("Builded:Orders", builder.Calls);
		Assert.False(scope.Builtin("/Items/Orders/Other").HasValue);

		Assert.Same(consumer.Module, module.GetValue(ObtainMode.Auto));

		Assert.True(module.IsBuilded);
		Assert.Equal(1, builder.Calls.Count(call => call == "Building:Orders"));
		Assert.Equal(1, builder.Calls.Count(call => call == "Builded:Orders"));
		Assert.Equal("Builded:Orders", builder.Calls[^1]);
	}

	[Fact]
	public void PropertyBehaviorRunsBeforeDerivedBuildedHook()
	{
		using var scope = new PluginTestScope();
		var builder = Prepare(scope, $"<object name=\"Target\" type=\"{typeof(ObjectBuilderTests.ComplexObject).AssemblyQualifiedName}\" /><object name=\"Child\" type=\"{typeof(ObjectBuilderTests.SimpleObject).AssemblyQualifiedName}\" Number=\"21\"><object.property name=\"Child\" target=\"{{path:/Items/Target}}\" value=\"{{path:.}}\" /></object>");
		var observed = false;
		builder.AfterBuild = context =>
		{
			if(context.Builtin.Name == "Child")
			{
				var target = Assert.IsType<ObjectBuilderTests.ComplexObject>(scope.Builtin("/Items/Target").Value);
				Assert.Same(context.Result, target.Child);
				Assert.Equal(21, target.Child.Number);
				observed = true;
			}
		};

		scope.Builtin("/Items/Child").GetValue(ObtainMode.Auto);

		Assert.True(observed);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void ExplicitReconstructionStartsAnotherLifecycle(bool always)
	{
		using var scope = new PluginTestScope();
		var builder = Prepare(scope, "<object name=\"Parent\" type=\"System.Collections.ArrayList\"><object name=\"Child\" type=\"System.Object\" /></object>");
		var parent = Assert.IsType<ArrayList>(scope.Builtin("/Items/Parent").GetValue(ObtainMode.Auto));
		var child = scope.Builtin("/Items/Parent/Child");
		var original = child.Value;
		builder.Calls.Clear();

		var replacement = always ? child.GetValue(ObtainMode.Alway) : child.Build();

		Assert.NotSame(original, replacement);
		Assert.Same(replacement, child.Value);
		Assert.Equal(new[] { "Building:Child", "Build:Child", "Builded:Child" }, builder.Calls);
		Assert.Same(original, parent[0]);
		Assert.Equal(always ? 1 : 2, parent.Count);

		if(!always)
			Assert.Same(replacement, parent[1]);
	}

	[Fact]
	public void BuildingFailurePreventsConstructionAndCanRetry()
	{
		using var scope = new PluginTestScope();
		var builder = Prepare(scope, "<object name=\"Item\" type=\"System.Object\" />");
		var builtin = scope.Builtin("/Items/Item");
		var failure = new InvalidOperationException();
		builder.BeforeBuild = context =>
		{
			Assert.Throws<PluginException>(() => context.Builtin.GetValue(ObtainMode.Auto));
			throw failure;
		};

		Assert.Same(failure, Assert.Throws<InvalidOperationException>(() => builtin.GetValue(ObtainMode.Auto)));
		Assert.Null(builtin.Value);
		Assert.False(builtin.IsBuilded);
		Assert.Equal(new[] { "Building:Item" }, builder.Calls);

		builder.BeforeBuild = null;
		Assert.NotNull(builtin.GetValue(ObtainMode.Auto));
		Assert.True(builtin.IsBuilded);
		Assert.Equal(new[] { "Building:Item", "Building:Item", "Build:Item", "Builded:Item" }, builder.Calls);
	}

	[Fact]
	public void BuildedFailurePreservesFailureAndDoesNotAppendOrReplay()
	{
		using var scope = new PluginTestScope();
		var builder = Prepare(scope, "<object name=\"Parent\" type=\"System.Collections.ArrayList\"><object name=\"Child\" type=\"System.Object\" /></object><object name=\"Other\" type=\"System.Object\" />");
		var failure = new InvalidOperationException();
		builder.AfterBuild = context =>
		{
			if(context.Builtin.Name == "Child")
				throw failure;
		};
		var parent = scope.Builtin("/Items/Parent");
		var child = scope.Builtin("/Items/Parent/Child");

		Assert.Same(failure, Assert.Throws<InvalidOperationException>(() => parent.GetValue(ObtainMode.Auto)));
		Assert.NotNull(child.Value);
		Assert.False(child.IsBuilded);
		Assert.False(parent.IsBuilded);
		Assert.Empty(Assert.IsType<ArrayList>(parent.GetValue(ObtainMode.Never)));
		Assert.DoesNotContain("Builded:Parent", builder.Calls);
		var calls = builder.Calls.ToArray();

		Assert.Same(failure, Assert.Throws<InvalidOperationException>(() => child.GetValue(ObtainMode.Auto)));
		Assert.Same(failure, Assert.Throws<InvalidOperationException>(() => parent.GetValue(ObtainMode.Auto)));
		Assert.Equal(calls, builder.Calls);
		Assert.NotNull(scope.Builtin("/Items/Other").GetValue(ObtainMode.Auto));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void NullOrFailedResultDoesNotInvokeBuilded(bool throws)
	{
		using var scope = new PluginTestScope();
		var declaration = throws ?
			$"<object name=\"Item\" type=\"{typeof(ObjectBuilderTests.ThrowingObject).AssemblyQualifiedName}\" />" :
			"<object name=\"Item\" value=\"{path:/Empty}\" />";
		var builder = Prepare(scope, declaration);
		scope.Tree.EnsurePath("/Empty");
		var builtin = scope.Builtin("/Items/Item");

		if(throws)
			Assert.Throws<PluginException>(() => builtin.GetValue(ObtainMode.Auto));
		else
			Assert.Null(builtin.GetValue(ObtainMode.Auto));

		Assert.Null(builtin.Value);
		Assert.False(builtin.IsBuilded);
		Assert.Equal(new[] { "Building:Item", "Build:Item" }, builder.Calls);
	}

	private static RecordingBuilder Prepare(PluginTestScope scope, string content, string builders = null)
	{
		scope.WritePlugin($"<builders><builder name=\"object\" type=\"{typeof(RecordingBuilder).AssemblyQualifiedName}\" />{builders}</builders><extension path=\"/Items\">{content}</extension>");
		scope.Tree.Load();
		return Assert.IsType<RecordingBuilder>(scope.Tree.Plugins["Feature"].GetBuilder("object"));
	}

	public class PlainBuilder : IBuilder
	{
		public Type GetValueType(Builtin builtin) => typeof(object);
		public object Build(BuilderContext context) => new object();
		public void Destroy(BuilderContext context) { }
		public void Dispose() { }
	}

	public sealed class DirectLifecycleBuilder : PlainBuilder, IBuilderLifecycle
	{
		public int Started { get; private set; }
		public int Completed { get; private set; }
		public void OnBuilding(BuilderContext context)
		{
			Assert.Null(context.Result);
			this.Started++;
		}
		public void OnBuilded(BuilderContext context)
		{
			Assert.Same(context.Result, context.Builtin.Value);
			Assert.NotNull(context.Result);
			this.Completed++;
		}
	}

	public sealed class RecordingBuilder : ObjectBuilder
	{
		public List<string> Calls { get; } = [];
		public List<BuilderContext> Completed { get; } = [];
		public Action<BuilderContext> BeforeBuild { get; set; }
		public Action<BuilderContext> AfterBuild { get; set; }

		public override object Build(BuilderContext context)
		{
			this.Calls.Add("Build:" + context.Builtin.Name);
			return base.Build(context);
		}

		protected override void OnBuilding(BuilderContext context)
		{
			this.Calls.Add("Building:" + context.Builtin.Name);
			this.BeforeBuild?.Invoke(context);
		}

		protected override void OnBuilded(BuilderContext context)
		{
			this.Calls.Add("Builded:" + context.Builtin.Name);
			this.Completed.Add(context);
			this.AfterBuild?.Invoke(context);
		}
	}

	public sealed class RecordingExposeBuilder : ExposeBuilder
	{
		public List<string> Calls { get; set; } = [];
		protected override void OnBuilding(BuilderContext context) => this.Calls.Add("Building:" + context.Builtin.Name);
		protected override void OnBuilded(BuilderContext context) => this.Calls.Add("Builded:" + context.Builtin.Name);
	}
}

