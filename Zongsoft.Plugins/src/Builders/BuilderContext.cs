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

namespace Zongsoft.Plugins.Builders
{
	public class BuilderContext
	{
		#region 同步字段
		private readonly object _syncRoot;
		#endregion

		#region 成员字段
		private IBuilder _builder;
		private IAppender _appender;
		private int _depth;
		private bool _cancel;
		private Builtin _builtin;
		private BuilderSettings _settings;
		private object _result;
		private object _owner;
		private PluginTreeNode _ownerNode;
		#endregion

		#region 构造函数
		private BuilderContext(IBuilder builder, Builtin builtin, BuilderSettings settings, int depth, object owner, PluginTreeNode ownerNode)
		{
			_builder = builder ?? throw new ArgumentNullException(nameof(builder));
			_builtin = builtin ?? throw new ArgumentNullException(nameof(builtin));
			_appender = builder as IAppender;
			_settings = settings;

			_depth = depth;
			_owner = owner;
			_ownerNode = ownerNode;

			if(builtin.HasBehaviors)
				_cancel = builtin.Behaviors.GetBehaviorValue<bool>(builtin.BuilderName + ".break");

			_syncRoot = new object();
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取当前的插件上下文对象。
		/// </summary>
		public PluginContext PluginContext
		{
			get
			{
				return _builtin.Context;
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
				var pluginContext = this.PluginContext;
				return pluginContext == null ? null : pluginContext.PluginTree;
			}
		}

		/// <summary>
		/// 获取当前的构建器对象。
		/// </summary>
		public IBuilder Builder
		{
			get
			{
				return _builder;
			}
		}

		/// <summary>
		/// 获取或设置构建过程的追加器。
		/// </summary>
		/// <remarks>
		///		<para>注意：该属性可能会被构建过程设置为空(null)，以阻止后续的追加动作。</para>
		/// </remarks>
		public IAppender Appender
		{
			get
			{
				return _appender;
			}
			set
			{
				_appender = value;
			}
		}

		/// <summary>
		/// 获取构建选项参数。
		/// </summary>
		public BuilderSettings Settings
		{
			get
			{
				return _settings;
			}
		}

		/// <summary>
		/// 获取或设置是否取消后续构建。
		/// </summary>
		public bool Cancel
		{
			get
			{
				return _cancel;
			}
			set
			{
				_cancel = value;
			}
		}

		/// <summary>
		/// 获取当前构建器要操作的构件。
		/// </summary>
		public Builtin Builtin
		{
			get
			{
				return _builtin;
			}
		}

		/// <summary>
		/// 获取当前构建器需要操作的插件节点，即为<see cref="Builtin"/>属性所指定的构件所属的<see cref="PluginTreeNode"/>插件树节点。
		/// </summary>
		public PluginTreeNode Node
		{
			get
			{
				return _builtin.Node;
			}
		}

		/// <summary>
		/// 获取当前构建的深度，如果大于零则表示处于子构件的构建中。
		/// </summary>
		public int Depth
		{
			get
			{
				return _depth;
			}
		}

		/// <summary>
		/// 获取当前节点的所有者对象，即所有者节点对应的目标对象。
		/// </summary>
		/// <remarks>
		///		<para>获取该属性值不会激发对所有者节点的创建动作，以避免在构建过程中发生无限递归调用。</para>
		/// </remarks>
		public object Owner
		{
			get
			{
				if(_owner == null)
				{
					var ownerNode = this.OwnerNode;

					//注意：解析所有者节点的目标对象。该操作绝不能激发创建动作，不然将可能导致无限递归调用。
					if(ownerNode != null)
						_owner = ownerNode.UnwrapValue(ObtainMode.Never, _settings);
				}

				return _owner;
			}
		}

		/// <summary>
		/// 获取当前节点的所有者节点。
		/// </summary>
		public PluginTreeNode OwnerNode
		{
			get
			{
				if(_ownerNode == null)
				{
					lock(_syncRoot)
					{
						if(_ownerNode == null)
						{
							var node = this.Node;
							var tree = this.PluginTree;

							if(tree == null)
								return null;

							if(node == null)
								_ownerNode = tree.GetOwnerNode(_builtin.FullPath);
							else
								_ownerNode = tree.GetOwnerNode(node);
						}
					}
				}

				return _ownerNode;
			}
		}

		/// <summary>
		/// 获取或设置由构建器创建的目标对象。
		/// </summary>
		/// <remarks>
		///		<para>该属性返回值会被添加到<see cref="Owner"/>对象的子集中。</para>
		/// </remarks>
		public object Result
		{
			get
			{
				return _result;
			}
			set
			{
				_result = value;
			}
		}
		#endregion

		#region 创建方法
		internal static BuilderContext CreateContext(IBuilder builder, Builtin builtin, BuilderSettings settings = null, int depth = 0, object owner = null, PluginTreeNode ownerNode = null)
		{
			return new BuilderContext(builder, builtin, settings, depth, owner, ownerNode);
		}
		#endregion
	}
}
