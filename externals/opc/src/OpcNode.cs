/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@qq.com>
 *
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;

using Opc.Ua;

namespace Zongsoft.Externals.Opc;

public partial class OpcNode
{
	#region 构造函数
	internal OpcNode(NodeId id, OpcNodeKind kind, OpcNodeType type, string label = null, string description = null)
	{
		this.Id = id.IsNull ? throw new ArgumentNullException(nameof(id)) : id;
		this.Name = id.ToString();
		this.Kind = kind;
		this.Type = type;
		this.Label = label;
		this.Description = description;
	}

	public OpcNode(string name, OpcNodeKind kind, OpcNodeType type, string label = null, string description = null)
	{
		ArgumentException.ThrowIfNullOrEmpty(name);
		this.Id = NodeId.Parse(name);
		this.Name = name;
		this.Kind = kind;
		this.Type = type;
		this.Label = label;
		this.Description = description;
	}
	#endregion

	#region 内部字段
	internal readonly NodeId Id;
	#endregion

	#region 公共属性
	public string Name { get; }
	public string Label { get; set; }
	public OpcNodeKind Kind { get; set; }
	public OpcNodeType Type { get; set; }
	public string Description { get; set; }
	public bool IsBuiltin => this.Id != null && this.Id.NamespaceIndex == 0;
	#endregion

	#region 公共方法
	public bool HasChildren() => this.HasChildren(out _);
	public virtual bool HasChildren(out OpcNodeCollection children)
	{
		children = null;
		return false;
	}
	#endregion

	#region 重写方法
	public override string ToString() => $"[{this.Kind}]{this.Name}({this.Type})";
	#endregion

	#region 嵌套子类
	internal static OpcNode Hierarchy(NodeId id, OpcNodeKind kind, OpcNodeType type, string label = null, string description = null) => new HierarchicalNode(id, kind, type, label, description);
	internal static OpcNode Hierarchy(string name, OpcNodeKind kind, OpcNodeType type, string label = null, string description = null) => new HierarchicalNode(name, kind, type, label, description);

	private sealed class HierarchicalNode : OpcNode
	{
		internal HierarchicalNode(NodeId id, OpcNodeKind kind, OpcNodeType type, string label = null, string description = null) : base(id, kind, type, label, description) => this.Children = [];
		internal HierarchicalNode(string name, OpcNodeKind kind, OpcNodeType type, string label = null, string description = null) : base(name, kind, type, label, description) => this.Children = [];

		public OpcNodeCollection Children { get; }
		public override bool HasChildren(out OpcNodeCollection children)
		{
			children = this.Children;
			return children != null;
		}
	}
	#endregion
}
