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
using System.Threading;
using System.ComponentModel;

using Zongsoft.Components;

namespace Zongsoft.Plugins
{
	/// <summary>
	/// 表示工作台的基类。
	/// </summary>
	public abstract class WorkbenchBase : IWorkbenchBase, IDisposable
	{
		#region 事件声明
		public event EventHandler Opened;
		public event EventHandler Opening;
		public event EventHandler Closed;
		public event CancelEventHandler Closing;
		public event EventHandler TitleChanged;
		#endregion

		#region 成员变量
		private string _title;
		private WorkbenchStatus _status;
		private readonly string _startupPath;
		private int _disposed;
		#endregion

		#region 构造函数
		protected WorkbenchBase(PluginApplicationContext applicationContext)
		{
			this.ApplicationContext = applicationContext ?? throw new ArgumentNullException(nameof(applicationContext));
			_status = WorkbenchStatus.None;
			_title = applicationContext.Title;
			_startupPath = applicationContext.Options.GetStartupMountion();
		}
		#endregion

		#region 公共属性
		/// <summary>获取工作台所属的应用程序上下文。</summary>
		public PluginApplicationContext ApplicationContext { get; }

		/// <summary>获取工作台的运行状态。</summary>
		public WorkbenchStatus Status => _status;

		/// <summary>获取启动的插件树节点。</summary>
		public PluginTreeNode Startup => field ??= this.ApplicationContext.PluginTree.Find(_startupPath);

		/// <summary>获取或设置工作台的标题。</summary>
		public virtual string Title
		{
			get => _title;
			set
			{
				if(string.Equals(_title, value, StringComparison.Ordinal))
					return;

				_title = value ?? string.Empty;

				//激发“TitleChanged”事件
				this.OnTitleChanged(EventArgs.Empty);
			}
		}
		#endregion

		#region 公共方法
		public void Open()
		{
			var mountable = false;
			PluginTreeNode node = null;

			try
			{
				if(_status != WorkbenchStatus.None)
					return;

				//设置工作台状态为“Opening”
				_status = WorkbenchStatus.Opening;

				//激发“Opening”事件
				this.OnOpening(EventArgs.Empty);

				//查找当前工作台的插件节点
				node = this.ApplicationContext.PluginTree.Find(this.ApplicationContext.Options.GetWorkbenchMountion());

				//确定当前工作台是否能挂载
				mountable = node == null || node.NodeType != PluginTreeNodeType.Builtin;

				//如果能挂载则将当前工作台挂载到插件树
				if(mountable)
					this.ApplicationContext.PluginTree.Mount(node, this);

				//调用虚拟方法以执行实际启动的操作
				this.OnOpen();

				//设置工作台状态为“Running”
				_status = WorkbenchStatus.Running;

				//激发“Opened”事件
				this.OnOpened(EventArgs.Empty);
			}
			catch
			{
				//注意：在实际启动操作中子类可能已经重新设置了状态为运行，因此无需再重置状态；否则必须还原状态
				if(_status == WorkbenchStatus.Opening)
					_status = WorkbenchStatus.None;

				//如果状态被重置为了已关闭并且当前工作台已经被挂载过，则必须将其卸载
				if(_status == WorkbenchStatus.None && mountable)
					this.ApplicationContext.PluginTree.Unmount(node);

				//重抛异常，导致后续的关闭代码不能继续，故而上面代码重置了工作台状态
				throw;
			}
		}

		public void Close()
		{
			try
			{
				if(_status != WorkbenchStatus.Running)
					return;

				//设置工作台状态为“Closing”
				_status = WorkbenchStatus.Closing;

				//创建“Closing”事件的参数对象
				var args = new CancelEventArgs();

				//激发“Closing”事件
				this.OnClosing(args);

				if(args.Cancel)
				{
					//重置工作台状态为“Running”
					_status = WorkbenchStatus.Running;

					//因为取消关闭，所以退出后续操作
					return;
				}

				//调用虚拟方法以进行实际的关闭操作
				this.OnClose();

				//设置工作台状态为“None”
				_status = WorkbenchStatus.None;

				//激发“Closed”事件
				this.OnClosed(EventArgs.Empty);

				//退出应用程序
				this.ApplicationContext.Exit();
			}
			catch
			{
				//注意：在实际关闭操作中子类可能已经重新设置了状态为关闭，因此无需再重置状态；否则必须还原状态
				if(_status == WorkbenchStatus.Closing)
					_status = WorkbenchStatus.Running;

				//重抛异常，导致后续的关闭代码不能继续，故而上面代码重置了工作台状态
				throw;
			}
		}
		#endregion

		#region 虚拟方法
		protected virtual void OnClose() { }
		protected virtual void OnOpen() => this.LoadWorkers(this.Startup);
		#endregion

		#region 处置方法
		public void Dispose()
		{
			var disposed = Interlocked.CompareExchange(ref _disposed, 1, 0);

			if(disposed == 0)
				this.Close();
		}
		#endregion

		#region 事件激发
		protected virtual void OnOpened(EventArgs args) => this.Opened?.Invoke(this, args);
		protected virtual void OnOpening(EventArgs args) => this.Opening?.Invoke(this, args);
		protected virtual void OnClosed(EventArgs args) => this.Closed?.Invoke(this, args);
		protected virtual void OnClosing(CancelEventArgs args) => this.Closing?.Invoke(this, args);
		protected virtual void OnTitleChanged(EventArgs args) => this.TitleChanged?.Invoke(this, args);
		#endregion

		#region 私有方法
		private void LoadWorkers(PluginTreeNode node)
		{
			if(node == null)
				return;

			object target = node.UnwrapValue(ObtainMode.Auto);

			if(target is IWorker worker && worker.Enabled)
				this.ApplicationContext.Workers.Add(worker);

			foreach(PluginTreeNode child in node.Children)
				this.LoadWorkers(child);
		}
		#endregion
	}
}
