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

using Zongsoft.Services;

namespace Zongsoft.Plugins
{
	/// <summary>
	/// 提供工作台的基本封装，建议自定义工作台从此类继承。
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
		private AutoResetEvent _semaphore;
		private int _disposed;
		#endregion

		#region 构造函数
		protected WorkbenchBase(PluginApplicationContext applicationContext)
		{
			this.ApplicationContext = applicationContext ?? throw new ArgumentNullException(nameof(applicationContext));
			_status = WorkbenchStatus.None;
			_title = applicationContext.Title;
			_startupPath = PluginPath.Combine(applicationContext.Options.GetWorkbenchMountion(), "Startup");
			_semaphore = new AutoResetEvent(true);
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取工作台所属的应用程序上下文。
		/// </summary>
		public PluginApplicationContext ApplicationContext { get; }

		/// <summary>
		/// 获取工作台的运行状态。
		/// </summary>
		public WorkbenchStatus Status { get => _status; }

		/// <summary>
		/// 获取或设置工作台的标题。
		/// </summary>
		public virtual string Title
		{
			get
			{
				return _title;
			}
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
			//等待信号
			_semaphore.WaitOne();

			if(_status != WorkbenchStatus.None)
				return;

			//设置工作台状态为“Opening”
			_status = WorkbenchStatus.Opening;

			var mountable = false;
			PluginTreeNode node = null;

			try
			{
				//激发“Opening”事件
				this.OnOpening(EventArgs.Empty);

				//查找当前工作台的插件节点
				node = ApplicationContext.PluginTree.Find(ApplicationContext.Options.GetWorkbenchMountion());

				//确定当前工作台是否能挂载
				mountable = node == null || node.NodeType != PluginTreeNodeType.Builtin;

				//如果能挂载则将当前工作台挂载到插件树
				if(mountable)
					ApplicationContext.PluginTree.Mount(node, this);

				//调用虚拟方法以执行实际启动的操作
				this.OnOpen();

				//尝试激发“Opened”事件
				this.RaiseOpened();
			}
			catch
			{
				//注意：在实际启动操作中子类可能已经重新设置了状态为运行，因此无需再重置状态；否则必须还原状态
				if(_status == WorkbenchStatus.Opening)
					_status = WorkbenchStatus.None;

				//如果状态被重置为了已关闭并且当前工作台已经被挂载过，则必须将其卸载
				if(_status == WorkbenchStatus.None && mountable)
					ApplicationContext.PluginTree.Unmount(node);

				//重抛异常，导致后续的关闭代码不能继续，故而上面代码重置了工作台状态
				throw;
			}
			finally
			{
				_semaphore.Set();
			}
		}

		public void Close()
		{
			//等待信号
			_semaphore.WaitOne();

			if(_status != WorkbenchStatus.Running)
				return;

			//设置工作台状态为“Closing”
			_status = WorkbenchStatus.Closing;

			try
			{
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

				//尝试激发“Closed”事件
				this.RaiseClosed();
			}
			catch
			{
				//注意：在实际关闭操作中子类可能已经重新设置了状态为关闭，因此无需再重置状态；否则必须还原状态
				if(_status == WorkbenchStatus.Closing)
					_status = WorkbenchStatus.Running;

				//重抛异常，导致后续的关闭代码不能继续，故而上面代码重置了工作台状态
				throw;
			}
			finally
			{
				_semaphore.Set();
			}
		}
		#endregion

		#region 虚拟方法
		protected virtual void OnOpen()
		{
			if(string.IsNullOrEmpty(_startupPath))
				return;

			//获取启动路径对应的节点对象
			PluginTreeNode startupNode = ApplicationContext.PluginTree.Find(_startupPath);

			//运行启动路径下的所有工作者
			if(startupNode != null)
				this.StartWorkers(startupNode);
		}

		protected virtual void OnClose()
		{
			if(string.IsNullOrEmpty(_startupPath))
				return;

			//获取启动路径对应的节点对象
			PluginTreeNode startupNode = ApplicationContext.PluginTree.Find(_startupPath);

			//停止启动路径下的所有工作者
			if(startupNode != null)
				this.StopWorkers(startupNode);
		}
		#endregion

		#region 处置方法
		public void Dispose()
		{
			var disposed = Interlocked.CompareExchange(ref _disposed, 1, 0);

			if(disposed == 0)
			{
				this.Close();
				_semaphore.Dispose();
			}
		}
		#endregion

		#region 事件激发
		protected void RaiseOpened()
		{
			if(Status == WorkbenchStatus.Opening)
			{
				_status = WorkbenchStatus.Running;
				this.OnOpened(EventArgs.Empty);
				ApplicationContext.RaiseStarted();
			}
		}

		protected void RaiseClosed()
		{
			if(Status == WorkbenchStatus.Closing)
			{
				_status = WorkbenchStatus.None;
				this.OnClosed(EventArgs.Empty);
				ApplicationContext.RaiseStopped();
			}
		}

		protected virtual void OnOpened(EventArgs args)
		{
			this.Opened?.Invoke(this, args);
		}

		public virtual void OnOpening(EventArgs args)
		{
			this.Opening?.Invoke(this, args);
		}

		protected virtual void OnClosed(EventArgs args)
		{
			this.Closed?.Invoke(this, args);
		}

		protected virtual void OnClosing(CancelEventArgs args)
		{
			this.Closing?.Invoke(this, args);
		}

		protected virtual void OnTitleChanged(EventArgs args)
		{
			this.TitleChanged?.Invoke(this, args);
		}
		#endregion

		#region 私有方法
		private void StartWorkers(PluginTreeNode node)
		{
			if(node == null)
				return;

			object target = node.UnwrapValue(ObtainMode.Auto);

			if(target is IWorker worker && worker.Enabled)
			{
				worker.Start();
				this.ApplicationContext.Workers.Add(worker);
			}

			foreach(PluginTreeNode child in node.Children)
				this.StartWorkers(child);
		}

		private void StopWorkers(PluginTreeNode node)
		{
			if(node == null)
				return;

			foreach(PluginTreeNode child in node.Children)
				this.StopWorkers(child);

			object target = node.UnwrapValue(ObtainMode.Never);

			if(target is IWorker worker)
			{
				worker.Stop();
				this.ApplicationContext.Workers.Remove(worker);
			}
		}
		#endregion
	}
}
