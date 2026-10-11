using System;
using System.Collections;
using System.Collections.Generic;

using Xunit;

using Zongsoft.Plugins.Builders;

namespace Zongsoft.Plugins.Tests;

public class BuilderExtensionTests
{
	[Fact]
	public void CompletionEventFollowsChildConstructionAndAppending()
	{
		using var scope = new PluginTestScope();
		scope.WritePlugin("<extension path=\"/Items\"><object name=\"Parent\" type=\"System.Collections.ArrayList\"><object name=\"Child\" type=\"System.Object\" /></object></extension>");
		scope.Tree.Load();
		var built = new List<string>();
		BuilderManager.Current.BuildCompleted += Completed;

		try
		{
			scope.Builtin("/Items/Parent").Build();
			Assert.Equal(new[] { "Child", "Parent" }, built);
		}
		finally
		{
			BuilderManager.Current.BuildCompleted -= Completed;
		}

		void Completed(object sender, BuilderEventArgs args)
		{
			Assert.Same(BuilderManager.Current, sender);
			Assert.Same(scope.Tree, args.Context.PluginTree);
			built.Add(args.Context.Builtin.Name);

			if(args.Context.Depth == 1)
			{
				Assert.Same(scope.Tree.Find("/Items/Parent"), args.Context.OwnerNode);
				Assert.Same(args.Context.Result, Assert.Single(Assert.IsType<ArrayList>(args.Context.Owner)));
			}
		}
	}

	[Fact]
	public void CustomBuilderCanSupplyResultAndCancelDescendants()
	{
		using var scope = new PluginTestScope();
		scope.WritePlugin($"<builders><builder name=\"cancel\" type=\"{typeof(CancellingBuilder).AssemblyQualifiedName}\" /></builders><extension path=\"/Items\"><cancel name=\"Parent\"><object name=\"Child\" type=\"System.Object\" /></cancel></extension>");
		scope.Tree.Load();
		var builtin = scope.Builtin("/Items/Parent");
		Assert.Equal(typeof(ArrayList), builtin.GetValueType());
		Assert.False(builtin.IsBuilded);

		var value = Assert.IsType<ArrayList>(builtin.Build());

		Assert.Empty(value);
		Assert.True(builtin.IsBuilded);
		Assert.False(scope.Builtin("/Items/Parent/Child").IsBuilded);
	}

	[Fact]
	public void ValuesOverrideConstructorsAndCollectionsConvertOrExpandChildren()
	{
		using var scope = new PluginTestScope();
		scope.WritePlugin($$"""
			<extension path="/Items">
				<object name="Numbers" type="{{typeof(List<int>).AssemblyQualifiedName}}">
					<object name="Single" type="{{typeof(ObjectBuilderTests.ThrowingObject).AssemblyQualifiedName}}" value="17" />
					<object name="Multiple" value="{static:{{typeof(BuilderExtensionTests).FullName}}.Numbers, Zongsoft.Plugins.Tests}" />
				</object>
			</extension>
			""");
		scope.Tree.Load();

		var value = Assert.IsType<List<int>>(scope.Builtin("/Items/Numbers").Build());

		Assert.Equal(new[] { 17, 23, 31 }, value);
		Assert.Equal("17", scope.Builtin("/Items/Numbers/Single").Value);
	}

	[Fact]
	public void DiscriminatorRoutesChildrenToItsSelectedCollection()
	{
		using var scope = new PluginTestScope();
		scope.WritePlugin($"<extension path=\"/Items\"><object name=\"Parent\" type=\"{typeof(DiscriminatingContainer).AssemblyQualifiedName}\"><object name=\"Number\" value=\"17\" /></object></extension>");
		scope.Tree.Load();

		var value = Assert.IsType<DiscriminatingContainer>(scope.Builtin("/Items/Parent").Build());

		Assert.Equal(new[] { 17 }, value.Numbers);
	}

	[Fact]
	public void DestroyDisposesElementsOfAnOwnedCollection()
	{
		using var scope = new PluginTestScope();
		scope.WritePlugin($"<extension path=\"/Items\"><object name=\"Parent\" type=\"System.Collections.ArrayList\"><object name=\"Child\" type=\"{typeof(ObjectBuilderTests.DisposableObject).AssemblyQualifiedName}\" /></object></extension>");
		scope.Tree.Load();
		BuilderContext context = null;
		var parent = scope.Builtin("/Items/Parent");
		parent.Build(new BuilderSettings(null, value => context = value));
		var child = Assert.IsType<ObjectBuilderTests.DisposableObject>(scope.Builtin("/Items/Parent/Child").Value);

		context.Builder.Destroy(context);

		Assert.Equal(1, child.Disposals);
		Assert.Null(parent.Value);
	}

	public static int[] Numbers => [23, 31];

	[BuilderBehavior(typeof(ArrayList))]
	public sealed class CancellingBuilder : BuilderBase
	{
		public override object Build(BuilderContext context)
		{
			context.Cancel = true;
			context.Result = new ArrayList();
			return null;
		}
	}

	public sealed class DiscriminatingContainer : Components.IDiscriminator
	{
		public List<int> Numbers { get; } = [];
		public object Discriminate(object value) => this.Numbers;
	}
}
