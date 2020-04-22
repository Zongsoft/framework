/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Plugins library.
 *
 * The Zongsoft.Plugins is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Plugins is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Plugins library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;

namespace Zongsoft.Plugins
{
	public class AppenderContext
	{
		#region 同步变量
		private readonly object _syncRoot;
		#endregion

		#region 成员字段
		private PluginContext _pluginContext;
		private AppenderBehavior _behaviour;
		private object _value;
		private object _container;
		private PluginTreeNode _node;
		private PluginTreeNode _containerNode;
		#endregion

		#region 构造函数
		internal AppenderContext(PluginContext pluginContext, object value, PluginTreeNode node, object container, PluginTreeNode containerNode, AppenderBehavior behaviour)
		{
			if(pluginContext == null)
				throw new ArgumentNullException("pluginContext");

			if(node == null)
				throw new ArgumentNullException("node");

			_syncRoot = new object();
			_pluginContext = pluginContext;
			_node = node;
			_value = value;
			_container = container;
			_containerNode = containerNode;
			_behaviour = behaviour;
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取当前处理器被激发的原因。
		/// </summary>
		public AppenderBehavior Behaviour
		{
			get
			{
				return _behaviour;
			}
		}

		/// <summary>
		/// 获取当前的插件上下文对象。
		/// </summary>
		public PluginContext PluginContext
		{
			get
			{
				return _pluginContext;
			}
		}

		/// <summary>
		/// 获取当前插件上下文中的插件树。
		/// </summary>
		/// <remarks>
		///		该属性返回值完全等同于<see cref="PluginContext"/>属性返回的<seealso cref="Zongsoft.Plugins.PluginTree"/>对象。
		/// </remarks>
		public PluginTree PluginTree
		{
			get
			{
				return _pluginContext.PluginTree;
			}
		}

		/// <summary>
		/// 获取当前节点的所有者对象，即所有者节点对应的目标对象。
		/// </summary>
		/// <remarks>
		///		<para>获取该属性值不会激发对所有者节点的创建动作，以避免在构建过程中发生无限递归调用。</para>
		/// </remarks>
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

		/// <summary>
		/// 获取当前节点的所有者节点。
		/// </summary>
		public PluginTreeNode ContainerNode
		{
			get
			{
				if(_containerNode == null)
				{
					lock(_syncRoot)
					{
						if(_containerNode == null)
							_containerNode = this.PluginTree.GetOwnerNode(_node);
					}
				}

				return _containerNode;
			}
		}

		/// <summary>
		/// 获取当前节点对象。
		/// </summary>
		/// <remarks>
		///		<para>当前节点即表示处理器要操作插件位置对应的插件树节点。</para>
		/// </remarks>
		public PluginTreeNode Node
		{
			get
			{
				return _node;
			}
		}

		/// <summary>
		/// 获取当前处理器对应的新值。
		/// </summary>
		public object Value
		{
			get
			{
				return _value;
			}
		}
		#endregion
	}
}
