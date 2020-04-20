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

namespace Zongsoft.Plugins
{
	public class PluginApplicationContext : Zongsoft.Services.ApplicationContext
	{
		#region 事件声明
		public event EventHandler WorkbenchCreated;
		#endregion

		#region 私有变量
		private readonly object _syncRoot;
		#endregion

		#region 构造函数
		protected PluginApplicationContext()
		{
			_syncRoot = new object();
			this.PluginContext = new PluginContext(this, this.CreateOptions());
		}

		protected PluginApplicationContext(string name) : base(name)
		{
			_syncRoot = new object();
			this.PluginContext = new PluginContext(this, this.CreateOptions());
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取当前应用程序的工作台(主界面)。
		/// </summary>
		/// <remarks>
		///		<para>必须使用<seealso cref="Zongsoft.Plugins.Application"/>类的Start方法，启动应用程序后才能使用该属性获取到创建成功的工作台对象。</para>
		/// </remarks>
		public IWorkbenchBase Workbench
		{
			get; private set;
		}

		/// <summary>
		/// 获取当前应用程序的插件上下文对象。
		/// </summary>
		/// <remarks>
		/// 本属性在首次创建<seealso cref="Zongsoft.Plugins.PluginContext"/>时，会调用<see cref="Zongsoft.Plugins.PluginApplicationContext.CreateOptions"/>方法以获得插件启动配置参数，如果要提供不同的启动信息，必须重写该虚拟方法。
		/// </remarks>
		public PluginContext PluginContext { get; }
		#endregion

		#region 虚拟方法
		/// <summary>
		/// 创建一个主窗体对象。
		/// </summary>
		/// <returns>返回的主窗体对象。</returns>
		/// <remarks>
		/// 通常子类中实现的该方法只是创建空的工作台对象，并没有构建出该工作台下面的子构件。
		/// 具体构建工作台子构件的最佳时机通常在 Workbench 类的 Open 方法内进行。
		/// </remarks>
		protected virtual IWorkbenchBase CreateWorkbench(string[] args)
		{
			return this.PluginContext.Workbench;
		}

		/// <summary>
		/// 创建插件启动配置对象。
		/// </summary>
		/// <returns>返回创建成功的插件启动配置对象。</returns>
		/// <remarks></remarks>
		protected virtual PluginOptions CreateOptions()
		{
			return new PluginOptions(this.ApplicationDirectory);
		}
		#endregion

		#region 内部方法
		/// <summary>
		/// 获取当前应用程序的工作台(主界面)。
		/// </summary>
		/// <param name="args">初始化的参数。</param>
		/// <returns>返回新建或者已创建的工作台对象。</returns>
		/// <remarks>
		/// <para>如果当前工作台为空(null)则调用 <seealso cref="CreateWorkbench"/> 虚拟方法，以创建工作台对象，并将创建后的对象挂入到由 <see cref="PluginOptions.MountionSettings.WorkbenchPath"/> 指定的插件树节点中。</para>
		/// <para>如果当前插件树还没加载，则将在插件树加载完成事件中将该工作台对象再挂入到由 <see cref="PluginOptions.MountionSettings.WorkbenchPath"/> 指定的插件树节点中。</para>
		/// <para>注意：该属性是线程安全的，在多线程中对该属性的多次调用不会导致重复生成工作台对象。</para>
		/// <para>有关子类实现 <seealso cref="CreateWorkbench"/> 虚拟方法的一般性机制请参考该方法的帮助。</para>
		/// </remarks>
		internal IWorkbenchBase GetWorkbench(string[] args)
		{
			if(Workbench == null)
			{
				lock(_syncRoot)
				{
					if(Workbench == null)
					{
						//创建工作台对象
						Workbench = this.CreateWorkbench(args);

						//将当前工作台对象挂载到插件结构中
						if(Workbench != null)
							this.PluginContext.PluginTree.Mount(this.PluginContext.Options.Mountion.WorkbenchPath, Workbench);

						//确认工作台路径及其下属所有节点均已构建完成
						this.EnsureNodes(this.PluginContext.PluginTree.Find(this.PluginContext.Options.Mountion.WorkbenchPath));

						//激发“WorkbenchCreated”事件
						if(Workbench != null)
							this.OnWorkbenchCreated(EventArgs.Empty);

						return Workbench;
					}
				}
			}

			return Workbench;
		}

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

		#region 激发事件
		internal void RaiseStarting(string[] args)
		{
			//更新当前应用的上下文
			Current = this;

			//激发“Starting”事件
			this.OnStarting(EventArgs.Empty);
		}

		internal void RaiseStarted(string[] args)
		{
			this.OnStarted(EventArgs.Empty);
		}

		internal void RaiseExiting()
		{
			this.OnExiting(EventArgs.Empty);
		}
		#endregion

		#region 保护方法
		protected virtual void OnWorkbenchCreated(EventArgs e)
		{
			if(this.WorkbenchCreated != null)
				this.WorkbenchCreated(this, e);
		}
		#endregion
	}
}
