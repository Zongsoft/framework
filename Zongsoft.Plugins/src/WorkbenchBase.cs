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
using System.ComponentModel;
using System.Collections.Generic;

using Zongsoft.Services;

namespace Zongsoft.Plugins
{
	/// <summary>
	/// 提供工作台的基本封装，建议自定义工作台从此类继承。
	/// </summary>
	public abstract class WorkbenchBase : MarshalByRefObject, IWorkbenchBase
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
		private PluginApplicationContext _applicationContext;
		private string _startupPath;
		#endregion

		#region 构造函数
		protected WorkbenchBase(PluginApplicationContext applicationContext)
		{
			if(applicationContext == null)
				throw new ArgumentNullException("applicationContext");

			_applicationContext = applicationContext;
			_title = applicationContext.Title;
			_status = WorkbenchStatus.None;
			_startupPath = applicationContext.PluginContext.Options.Mountion.WorkbenchPath + "/Startup";
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取工作台所属的应用程序上下文。
		/// </summary>
		public PluginApplicationContext ApplicationContext
		{
			get
			{
				return _applicationContext;
			}
		}

		/// <summary>
		/// 获取工作台的运行状态。
		/// </summary>
		public WorkbenchStatus Status
		{
			get
			{
				return _status;
			}
		}

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

		/// <summary>
		/// 获取当前插件运行时的上下文对象。
		/// </summary>
		public PluginContext PluginContext
		{
			get
			{
				return _applicationContext.PluginContext;
			}
		}

		/// <summary>
		/// 获取或设置启动的工作者所挂载的插件路径，默认值为当前工作台路径下名为 Startup 的子路径。
		/// </summary>
		/// <remarks>
		/// 该属性只能在工作台未启动时进行设置。
		/// </remarks>
		/// <exception cref="System.InvalidOperationException">当工作台未处于<seealso cref="WorkbenchStatus.None"/>状态时设置该属性。</exception>
		public string StartupPath
		{
			get
			{
				return _startupPath;
			}
			protected set
			{
				if(_status != WorkbenchStatus.None)
					throw new InvalidOperationException();

				_startupPath = (value ?? string.Empty).Trim().TrimEnd('/');
			}
		}
		#endregion

		#region 公共方法
		/// <summary>
		/// 关闭工作台。
		/// </summary>
		/// <returns>如果关闭成功返回真(true)，否则返回假(false)。如果取消关闭操作，亦返回假(false)。</returns>
		public bool Close()
		{
			if(_status == WorkbenchStatus.None || _status == WorkbenchStatus.Closing)
				return false;

			if(_status == WorkbenchStatus.Opening)
				throw new InvalidOperationException();

			//设置工作台状态为“Closing”
			_status = WorkbenchStatus.Closing;

			//创建“Closing”事件的参数对象
			CancelEventArgs args = new CancelEventArgs();

			try
			{
				//激发“Closing”事件
				this.OnClosing(args);
			}
			catch
			{
				//注意：由于事件处理程序出错，必须重置工作台状态为“Running”
				_status = WorkbenchStatus.Running;

				//重抛异常，导致后续的关闭代码不能继续，故而上面代码重置了工作台状态
				throw;
			}

			//如果事件处理程序要取消后续的关闭操作，则重置工作台状态
			if(args.Cancel)
			{
				//重置工作台状态为“Running”
				_status = WorkbenchStatus.Running;

				//因为取消关闭，所以退出后续关闭操作
				return false;
			}

			try
			{
				//调用虚拟方法以进行实际的关闭操作
				this.OnStop();
			}
			catch
			{
				//注意：如果在实际关闭操作中，子类已经通过OnClosed方法设置了工作台状态为关闭，则无需再重置工作台状态；否则必须重置工作台状态为“Running”
				if(_status == WorkbenchStatus.Closing)
					_status = WorkbenchStatus.Running;

				//重抛异常，导致后续的关闭代码不能继续，故而上面代码重置了工作台状态
				throw;
			}

			//设置工作台状态为“None”
			_status = WorkbenchStatus.None;

			//如果没有激发过“Closed”事件则激发该事件
			if(_status != WorkbenchStatus.None)
				this.OnClosed(EventArgs.Empty);

			//返回成功
			return true;
		}

		public void Open(string[] args)
		{
			if(_status == WorkbenchStatus.Running || _status == WorkbenchStatus.Opening)
				return;

			if(_status == WorkbenchStatus.Closing)
				throw new InvalidOperationException();

			//设置工作台状态为“Opening”
			_status = WorkbenchStatus.Opening;

			try
			{
				//激发“Opening”事件
				this.OnOpening(EventArgs.Empty);
			}
			catch
			{
				//注意：可能因为预打开事件处理程序或工作台构建过程出错，都必须重置工作台状态为“None”
				_status = WorkbenchStatus.None;

				//重抛异常，导致后续的关闭代码不能继续，故而上面代码重置了工作台状态
				throw;
			}

			try
			{
				//调用虚拟方法以执行实际启动的操作
				this.OnStart(args);

				//查找当前工作台的插件节点
				var node = _applicationContext.PluginContext.PluginTree.Find(_applicationContext.PluginContext.Options.Mountion.WorkbenchPath);

				//如果工作台对象未挂载或不是通过插件文件挂载的话，则将当前工作台对象挂载到指定的插件路径中
				if(node == null || node.NodeType != PluginTreeNodeType.Builtin)
					_applicationContext.PluginContext.PluginTree.Mount(_applicationContext.PluginContext.Options.Mountion.WorkbenchPath, this);
			}
			catch
			{
				//注意：如果在实际启动操作中，子类已经通过OnOpened方法设置了工作台状态为运行，则无需再重置工作台状态；否则必须重置工作台状态为“None”
				if(_status == WorkbenchStatus.Opening)
					_status = WorkbenchStatus.None;

				//重抛异常，导致后续的关闭代码不能继续，故而上面代码重置了工作台状态
				throw;
			}

			//如果没有激发过“Opened”事件则激发该事件
			if(_status == WorkbenchStatus.Opening)
				this.OnOpened(EventArgs.Empty);
		}
		#endregion

		#region 虚拟方法
		protected virtual void OnStart(string[] args)
		{
			if(string.IsNullOrEmpty(_startupPath))
				return;

			//获取启动路径对应的节点对象
			PluginTreeNode startupNode = _applicationContext.PluginContext.PluginTree.Find(_startupPath);

			//运行启动路径下的所有工作者
			if(startupNode != null)
				this.StartWorkers(startupNode, args);
		}

		protected virtual void OnStop()
		{
			if(string.IsNullOrEmpty(_startupPath))
				return;

			//获取启动路径对应的节点对象
			PluginTreeNode startupNode = _applicationContext.PluginContext.PluginTree.Find(_startupPath);

			//停止启动路径下的所有工作者
			if(startupNode != null)
				this.StopWorkers(startupNode);
		}
		#endregion

		#region 事件激发
		protected virtual void OnOpened(EventArgs args)
		{
			_status = WorkbenchStatus.Running;

			if(this.Opened != null)
				this.Opened(this, args);
		}

		public virtual void OnOpening(EventArgs args)
		{
			_status = WorkbenchStatus.Opening;

			if(this.Opening != null)
				this.Opening(this, args);
		}

		protected virtual void OnClosed(EventArgs args)
		{
			_status = WorkbenchStatus.None;

			if(this.Closed != null)
				this.Closed(this, args);
		}

		protected virtual void OnClosing(CancelEventArgs args)
		{
			_status = WorkbenchStatus.Closing;

			if(this.Closing != null)
				this.Closing(this, args);
		}

		protected virtual void OnTitleChanged(EventArgs args)
		{
			if(this.TitleChanged != null)
				this.TitleChanged(this, args);
		}
		#endregion

		#region 私有方法
		private void StartWorkers(PluginTreeNode node, string[] args)
		{
			if(node == null)
				return;

			object target = node.UnwrapValue(ObtainMode.Auto);

			if(target is IWorker worker && worker.Enabled)
				worker.Start(args);

			foreach(PluginTreeNode child in node.Children)
				this.StartWorkers(child, args);
		}

		private void StopWorkers(PluginTreeNode node)
		{
			if(node == null)
				return;

			foreach(PluginTreeNode child in node.Children)
				this.StopWorkers(child);

			object target = node.UnwrapValue(ObtainMode.Never);

			if(target is IWorker worker)
				worker.Stop();
		}
		#endregion
	}
}
