using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Plugins.Tests;

public class PluginTreeTests
{
	[Fact]
	public async Task EnsurePathReusesNodesAndFindsRelativePaths()
	{
		await using var scope = new PluginTestScope();
		var node = scope.Tree.EnsurePath("/Workspace/Services/Child", out var existed);

		Assert.False(existed);
		Assert.Equal("/Workspace/Services", node.Path);
		Assert.Equal("/Workspace/Services/Child", node.FullPath);
		Assert.Equal(PluginTreeNodeType.Empty, node.NodeType);
		Assert.Same(node, scope.Tree.EnsurePath("/workspace/services/child", out existed));
		Assert.True(existed);

		Assert.Same(node, node.Find("."));
		Assert.Same(node.Parent, node.Find(".."));
		Assert.Same(node, node.Parent.Find("Child"));
		Assert.Same(node, scope.Tree.Find("/Workspace", "Services", "Child"));
		Assert.Same(scope.Tree.Root, node.Find("/"));
		Assert.Null(node.Find("missing"));
	}

	[Theory]
	[InlineData("^", new[] { "X", "A", "B", "C" })]
	[InlineData("B", new[] { "A", "X", "B", "C" })]
	[InlineData(":B", new[] { "A", "B", "X", "C" })]
	[InlineData("-B", new[] { "A", "B", "X", "C" })]
	[InlineData("missing", new[] { "A", "B", "C", "X" })]
	[InlineData(null, new[] { "A", "B", "C", "X" })]
	public async Task PositionControlsSiblingOrder(string position, string[] expected)
	{
		await using var scope = new PluginTestScope();
		scope.Tree.EnsurePath("/A");
		scope.Tree.EnsurePath("/B");
		scope.Tree.EnsurePath("/C");

		scope.Tree.EnsurePath("/X", position);

		Assert.Equal(expected, scope.Tree.Root.Children.Select(node => node.Name));
		Assert.Equal(1, scope.Tree.Root.Children.IndexOf(expected[1].ToLowerInvariant()));
	}

	[Fact]
	public async Task MountEventsBracketValueChangeAndUnmountPreservesChildren()
	{
		await using var scope = new PluginTestScope();
		var parent = scope.Tree.EnsurePath("/Parent");
		var child = scope.Tree.EnsurePath("/Parent/Empty/Child");
		var value = new object();
		var events = new List<string>();
		scope.Tree.Mounting += (_, args) =>
		{
			Assert.Null(parent.Value);
			Assert.Equal("/Parent", args.Path);
			Assert.Same(value, args.Value);
			events.Add("mounting");
		};
		scope.Tree.Mounted += (_, args) =>
		{
			Assert.Same(value, parent.Value);
			events.Add("mounted");
		};

		Assert.True(scope.Tree.Mount(parent, value));

		Assert.Equal(new[] { "mounting", "mounted" }, events);
		Assert.Equal(PluginTreeNodeType.Custom, parent.NodeType);
		Assert.Equal(typeof(object), parent.ValueType);
		Assert.Same(parent, scope.Tree.GetOwnerNode(child));
		Assert.Same(value, parent.UnwrapValue(ObtainMode.Never));

		Assert.Same(value, scope.Tree.Unmount(parent));

		Assert.Same(parent, scope.Tree.Find("/Parent"));
		Assert.Equal(PluginTreeNodeType.Empty, parent.NodeType);
		Assert.Null(scope.Tree.GetOwnerNode(child));
		Assert.Null(scope.Tree.Unmount(child));
		Assert.Null(scope.Tree.Find("/Parent/Empty/Child"));
		Assert.Null(child.Parent);
	}

	[Fact]
	public async Task MountRejectsFrameworkObjectsAndUnmountRejectsForeignNodes()
	{
		await using var scope = new PluginTestScope();
		await using var other = new PluginTestScope();
		scope.WritePlugin("<extension path=\"/Items\"><object name=\"Item\" type=\"System.Object\" /></extension>");
		scope.Tree.Load();
		var node = scope.Tree.EnsurePath("/Target");

		Assert.False(scope.Tree.Mount(node, scope.Tree));
		Assert.False(scope.Tree.Mount(node, scope.Tree.Root));
		Assert.False(scope.Tree.Mount(node, scope.Builtin("/Items/Item")));
		Assert.Null(node.Value);
		Assert.Throws<ArgumentException>(() => scope.Tree.Unmount(other.Tree.Root));
		Assert.Null(scope.Tree.Unmount("/Absent"));
		Assert.Null(scope.Tree.Unmount((PluginTreeNode)null));
	}

	[Theory]
	[InlineData("/A//B")]
	[InlineData("/A/")]
	public async Task EnsurePathRejectsEmptyInteriorSegments(string path)
	{
		await using var scope = new PluginTestScope();

		Assert.Throws<PluginException>(() => scope.Tree.EnsurePath(path));
	}

	[Theory]
	[InlineData("invalid name")]
	[InlineData("a/b")]
	[InlineData("a@b")]
	[InlineData("中文")]
	public async Task NodeNamesRejectUnsupportedCharacters(string name)
	{
		await using var scope = new PluginTestScope();

		Assert.Throws<ArgumentException>(() => new PluginTreeNode(scope.Tree, name));
	}

	[Fact]
	public async Task ChildrenExposeSnapshotsAndRejectExternalMutation()
	{
		await using var scope = new PluginTestScope();
		var node = scope.Tree.EnsurePath("/A");
		var snapshot = scope.Tree.Root.Children.Values;
		scope.Tree.EnsurePath("/B");

		Assert.Single(snapshot);
		Assert.Equal(2, scope.Tree.Root.Children.Count);
		Assert.Same(node, scope.Tree.Root.Children[" a "]);
		Assert.Null(scope.Tree.Root.Children["Missing"]);
		var children = (ICollection<PluginTreeNode>)scope.Tree.Root.Children;
		Assert.True(children.IsReadOnly);
		Assert.Throws<InvalidOperationException>(() => children.Add(new PluginTreeNode(scope.Tree, "C")));
		Assert.Throws<InvalidOperationException>(() => children.Remove(node));
		Assert.Throws<InvalidOperationException>(() => children.Clear());
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData(" ")]
	public async Task RequiredPathsRejectBlankValues(string path)
	{
		await using var scope = new PluginTestScope();

		Assert.Throws<ArgumentNullException>("path", () => scope.Tree.EnsurePath(path));
		Assert.Throws<ArgumentNullException>("path", () => scope.Tree.Mount(path, new object()));
		Assert.Throws<ArgumentNullException>("path", () => scope.Tree.Unmount(path));
	}
}
