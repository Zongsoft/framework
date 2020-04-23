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
	public class PluginApplicationContext : Zongsoft.Services.ApplicationContext
	{
		#region 事件声明
		public event EventHandler WorkbenchCreated;
		#endregion

		#region 私有变量
		private readonly object _syncRoot;
		private IWorkbenchBase _workbench;
		#endregion

		#region 构造函数
		protected PluginApplicationContext()
		{
			_syncRoot = new object();
			this.PluginTree = new PluginTree(this);
		}

		protected PluginApplicationContext(IServiceProvider services) : base(services)
		{
			_syncRoot = new object();
			this.PluginTree = new PluginTree(this);
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取当前插件运行时的插件树。
		/// </summary>
		public PluginTree PluginTree
		{
			get;
		}

		/// <summary>
		/// 获取加载的根插件集。
		/// </summary>
		public IEnumerable<Plugin> Plugins
		{
			get => this.PluginTree.Plugins;
		}

		/// <summary>
		/// 获取当前应用程序的工作台(主界面)。
		/// </summary>
		/// <remarks>
		///		<para>必须使用<seealso cref="Zongsoft.Plugins.Application"/>类的Start方法，启动应用程序后才能使用该属性获取到创建成功的工作台对象。</para>
		/// </remarks>
		public IWorkbenchBase Workbench
		{
			get
			{
				if(_workbench == null)
				{
					lock(_syncRoot)
					{
						if(_workbench == null)
						{
							//创建工作台对象
							_workbench = this.CreateWorkbench(out var node) ?? throw new InvalidOperationException("Failed to create the Workbench.");

							//将当前工作台对象挂载到插件结构中
							this.PluginTree.Mount(node, _workbench);

							//确认工作台路径及其下属所有节点均已构建完成
							this.EnsureNodes(node);

							//激发“WorkbenchCreated”事件
							this.OnWorkbenchCreated(EventArgs.Empty);
						}
					}
				}

				return _workbench;
			}
		}

		/// <summary>
		/// 获取当前插件上下文对应的设置。
		/// </summary>
		public PluginOptions Options
		{
			get;
		}
		#endregion

		#region 虚拟方法
		/// <summary>
		/// 创建一个主控台对象。
		/// </summary>
		/// <returns>返回的主控台对象。</returns>
		protected virtual IWorkbenchBase CreateWorkbench(out PluginTreeNode node)
		{
			node = this.PluginTree.Find(this.Options.Mountion.WorkbenchPath);

			if(node != null && node.NodeType == PluginTreeNodeType.Builtin)
				return node.UnwrapValue(ObtainMode.Auto) as IWorkbenchBase;
			else
				return null;
		}
		#endregion

		#region 激发事件
		internal void RaiseStarted(string[] args)
		{
			this.OnStarted(EventArgs.Empty);
		}

		internal void RaiseExiting()
		{
			this.OnExiting(EventArgs.Empty);
		}

		protected virtual void OnWorkbenchCreated(EventArgs e)
		{
			this.WorkbenchCreated?.Invoke(this, e);
		}
		#endregion

		#region 私有方法
		private void EnsureNodes(PluginTreeNode node)
		{
			if(node == null)
				return;

			if(node.NodeType == PluginTreeNodeType.Builtin)
			{
				var builtin = (Builtin)node.Value;

				if(!builtin.IsBuilded)
					builtin.Build();

				return;
			}

			foreach(var child in node.Children)
				this.EnsureNodes(child);
		}
		#endregion
	}
}
