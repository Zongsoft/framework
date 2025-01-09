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

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

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
		protected PluginApplicationContext(IServiceProvider services) : base(services)
		{
			_syncRoot = new object();
			this.Options = services.GetService<PluginOptions>() ?? new PluginOptions(services.GetRequiredService<IHostEnvironment>());
			this.PluginTree = PluginTree.Get(this.Options);
		}
		#endregion

		#region 公共属性
		/// <summary>获取当前插件上下文对应的设置。</summary>
		public PluginOptions Options { get; }

		/// <summary>获取当前插件运行时的插件树。</summary>
		public PluginTree PluginTree { get; }

		/// <summary>获取加载的根插件集。</summary>
		public IEnumerable<Plugin> Plugins => this.PluginTree.Plugins;

		/// <summary>获取当前应用程序的主控台(工作台)。</summary>
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

							//查找“Startup”启动目录节点
							var startup = this.PluginTree.Find(this.Options.GetStartupMountion());

							//确认工作台路径及其下属所有节点均已构建完成
							//注意：忽略“Startup”节点及其子节点，必须将“Startup”节点置于最后构建
							BuildWorkbenchChildren(node, n => n == startup);

							//激发“WorkbenchCreated”事件
							this.OnWorkbenchCreated();
						}
					}
				}

				return _workbench;
			}
		}
		#endregion

		#region 虚拟方法
		/// <summary>创建一个主控台对象。</summary>
		/// <returns>返回的主控台对象。</returns>
		protected virtual IWorkbenchBase CreateWorkbench(out PluginTreeNode node)
		{
			node = this.PluginTree.EnsurePath(this.Options.GetWorkbenchMountion());

			if(node != null && node.NodeType == PluginTreeNodeType.Builtin)
				return node.UnwrapValue(ObtainMode.Auto) as IWorkbenchBase;
			else
				return null;
		}
		#endregion

		#region 初始方法
		public override bool Initialize()
		{
			//首先调用基类的初始化
			if(!base.Initialize())
				return false;

			//加载插件树
			this.PluginTree.Load();

			//挂载当前应用上下文
			this.PluginTree.Mount(this.Options.GetApplicationContextMountion(), this);

			//返回初始化完成
			return true;
		}
		#endregion

		#region 处置方法
		protected override void Dispose(bool disposing)
		{
			if(disposing)
			{
				if(_workbench is IDisposable disposable)
					disposable.Dispose();
				else
					_workbench?.Close();
			}

			//执行基类的处置操作
			base.Dispose(disposing);
		}
		#endregion

		#region 激发事件
		internal void RaiseStarted() => this.OnStarted(EventArgs.Empty);
		internal void RaiseStopped() => this.OnStopped(EventArgs.Empty);
		protected virtual void OnWorkbenchCreated(EventArgs args = null) => this.WorkbenchCreated?.Invoke(this, args ?? EventArgs.Empty);
		#endregion

		#region 私有方法
		private static void BuildWorkbenchChildren(PluginTreeNode node, Predicate<PluginTreeNode> predicate)
		{
			if(node == null || predicate(node))
				return;

			if(node.NodeType == PluginTreeNodeType.Builtin)
			{
				var builtin = (Builtin)node.Value;

				if(!builtin.IsBuilded)
					builtin.Build();
			}

			foreach(var child in node.Children)
				BuildWorkbenchChildren(child, predicate);
		}
		#endregion
	}
}
