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
using System.Collections.ObjectModel;

namespace Zongsoft.Collections;

public abstract class HierarchicalNodeCollection<TNode>(TNode owner) :
	KeyedCollection<string, TNode>(StringComparer.OrdinalIgnoreCase),
	IHierarchicalNodeCollection<TNode>
	where TNode : IHierarchicalNode<TNode>
{
	#region 成员字段
	private readonly TNode _owner = owner;
	#endregion

	#region 保护属性
	protected TNode Owner => _owner;
	#endregion

	#region 重写方法
	protected override string GetKeyForItem(TNode node) => node.Name;

	protected override void RemoveItem(int index)
	{
		var node = base.Items[index];
		this.SetOwner(default, node);
	}

	protected override void InsertItem(int index, TNode node)
	{
		if(node == null)
			throw new ArgumentNullException(nameof(node));

		if(node.IsRoot())
			throw new ArgumentException(Properties.Resources.HierarchicalNode_RootAsChild_Message);

		base.InsertItem(index, node);
		this.SetOwner(_owner, node);
	}

	protected override void SetItem(int index, TNode node)
	{
		if(node == null)
			throw new ArgumentNullException(nameof(node));

		if(node.IsRoot())
			throw new ArgumentException(Properties.Resources.HierarchicalNode_RootAsChild_Message);

		base.SetItem(index, node);
		this.SetOwner(_owner, node);
	}
	#endregion

	#region 抽象方法
	protected abstract void SetOwner(TNode owner, TNode node);
	#endregion
}
