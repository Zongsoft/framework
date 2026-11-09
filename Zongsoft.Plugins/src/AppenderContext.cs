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

namespace Zongsoft.Plugins;

public class AppenderContext
{
	#region 同步变量
	private readonly object _syncRoot;
	#endregion

	#region 成员字段
	private AppenderBehavior _behaviour;
	private object _value;
	private object _container;
	private PluginTreeNode _node;
	private PluginTreeNode _containerNode;
	#endregion

	#region 构造函数
	internal AppenderContext(object value, PluginTreeNode node, object container, PluginTreeNode containerNode, AppenderBehavior behaviour)
	{
		_syncRoot = new object();
		_node = node ?? throw new ArgumentNullException(nameof(node));
		_value = value;
		_container = container;
		_containerNode = containerNode;
		_behaviour = behaviour;
	}
	#endregion

	#region 公共属性
	/// <summary>获取当前处理器被激发的原因。</summary>
	public AppenderBehavior Behaviour => _behaviour;

	/// <summary>获取当前节点对象。</summary>
	/// <remarks>当前节点即表示处理器要操作插件位置对应的插件树节点。</remarks>
	public PluginTreeNode Node => _node;

	/// <summary>获取当前处理器对应的新值。</summary>
	public object Value => _value;

	/// <summary>获取当前节点的所有者对象，即所有者节点对应的目标对象。</summary>
	/// <remarks>获取该属性值不会激发对所有者节点的创建动作，以避免在构建过程中发生无限递归调用。</remarks>
	public object Container
	{
		get
		{
			if(_container == null)
			{
				var containerNode = this.ContainerNode;

				//注意：解析所有者节点的目标对象。该操作绝不能激发创建动作，不然将可能导致无限递归调用。
				if(containerNode != null)
					_container = containerNode.UnwrapValue(ObtainMode.Never);
			}

			return _container;
		}
	}

	/// <summary>获取当前节点的所有者节点。</summary>
	public PluginTreeNode ContainerNode
	{
		get
		{
			if(_containerNode == null)
			{
				lock(_syncRoot)
				{
					_containerNode ??= _node.Tree.GetOwnerNode(_node);
				}
			}

			return _containerNode;
		}
	}
	#endregion
}
