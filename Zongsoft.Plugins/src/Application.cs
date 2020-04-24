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
using System.Threading;

namespace Zongsoft.Plugins
{
	[Obsolete]
	public static class Application
	{
		#region 事件声明
		public static event EventHandler Exiting;
		public static event EventHandler Started;
		#endregion

		#region 成员变量
		private static PluginApplicationContext _context;
		#endregion

		#region 公共属性
		public static PluginApplicationContext Context
		{
			get
			{
				return _context;
			}
		}
		#endregion

		#region 启动应用
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
		public static void Start(PluginApplicationContext context, params string[] args)
		{
			//保存当前上下文对象
			_context = context ?? throw new ArgumentNullException(nameof(context));

			#if !DEBUG
			try
			#endif
			{
				context.PluginTree.Loader.Loaded += delegate
				{
					//插件树加载完成即保存当前应用上下文
					_context = context;

					//将应用上下文对象挂载到插件结构中
					_context.PluginTree.Mount(_context.Options.Mountion.ApplicationContextPath, _context);
				};

				//初始化
				foreach(var initializer in context.Initializers)
				{
					initializer.Initialize(context);
				}

				//加载插件树
				context.PluginTree.Load();

				//如果工作台对象不为空则运行工作台
				if(context.Workbench != null)
				{
					//注意：因为工作台很可能会阻塞当前主线程，所以需要利用其Opened事件进行注册
					context.Workbench.Opened += delegate
					{
						//激发应用启动完成事件
						OnStarted();
					};

					context.Workbench.Closed += delegate
					{
						Exit();
					};

					//启动工作台
					context.Workbench.Open();
				}

				//激发应用启动完成事件
				OnStarted();
			}
			#if !DEBUG
			catch(Exception ex)
			{
				//应用无法启动，写入日志
				Zongsoft.Diagnostics.Logger.Fatal(ex);

				//重抛异常
				throw;
			}
			#endif
		}
		#endregion

		#region 关闭应用
		/// <summary>
		/// 关闭当前应用程序。
		/// </summary>
		public static void Exit()
		{
			var context = System.Threading.Interlocked.Exchange(ref _context, null);

			//如果上下文对象为空，则表示尚未启动
			if(context == null)
				return;

			//激发“Exiting”事件
			OnExiting(context);

			//关闭工作台
			if(context.Workbench != null)
				context.Workbench.Close();

			//处置初始化器
			foreach(var initializer in context.Initializers)
			{
				if(initializer is IAsyncDisposable asyncDisposable)
					asyncDisposable.DisposeAsync().GetAwaiter().GetResult();
				else if(initializer is IDisposable disposable)
					disposable.Dispose();
			}
		}
		#endregion

		#region 激发事件
		private static void OnExiting(PluginApplicationContext context)
		{
			if(context == null)
				return;

			Exiting?.Invoke(context, EventArgs.Empty);

			//激发当前上下文的“Exiting”事件
			context.RaiseStopped();
		}

		private static void OnStarted()
		{
			Started?.Invoke(null, EventArgs.Empty);

			//激发当前上下文的“Started”事件
			_context.RaiseStarted();
		}
		#endregion
	}
}
