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
 * Copyright (C) 2010-2023 Zongsoft Studio <http://www.zongsoft.com>
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

using Zongsoft.Services;
using Zongsoft.Terminals;

namespace Zongsoft.Plugins.Hosting;

partial class Application
{
	private sealed class TerminalApplicationBuilder : ApplicationBuilder
	{
		#if NET7_0_OR_GREATER
		public TerminalApplicationBuilder(string name, string[] args, Action<HostApplicationBuilder> configure = null) : base(name, args, configure)
		{
			_logging = Zongsoft.Diagnostics.Logging.GetLogging(this.Environment.ApplicationName);
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
		}
		#else
		public TerminalApplicationBuilder(string name, string[] args, Action<IHostBuilder> configure = null) : base(name, args, configure)
		{
			_logging = Zongsoft.Diagnostics.Logging.GetLogging(this.Environment.ApplicationName);
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
		}
		#endif

		protected override void RegisterServices(IServiceCollection services, PluginOptions options)
		{
			services.AddSingleton(provider => new TerminalApplicationContext(provider, options));
			services.AddSingleton<PluginApplicationContext>(provider => provider.GetRequiredService<TerminalApplicationContext>());
			services.AddSingleton<IApplicationContext>(provider => provider.GetRequiredService<TerminalApplicationContext>());

			base.RegisterServices(services, options);
		}

		#region 全局异常
		private static Zongsoft.Diagnostics.Logging _logging;
		private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			if(e.ExceptionObject is Exception ex)
				_logging.Fatal(ex);
			else
				_logging.Fatal(e.ExceptionObject?.ToString());
		}
		#endregion
	}

	private sealed class TerminalApplicationContext(IServiceProvider services, PluginOptions options) : PluginApplicationContext(services, options)
	{
		public override string ApplicationType => "Terminal";
		protected override IWorkbenchBase CreateWorkbench(out PluginTreeNode node) => base.CreateWorkbench(out node) ?? new TerminalWorkbench(this);
	}

	private sealed class TerminalWorkbench(PluginApplicationContext applicationContext) : WorkbenchBase(applicationContext)
	{
		#region 成员字段
		private ITerminalExecutor _executor;
		#endregion

		#region 公共属性
		public ITerminalExecutor Executor
		{
			get => _executor ?? Terminals.Terminal.Default.Executor;
			set => _executor = value;
		}
		#endregion

		#region 打开方法
		protected override void OnOpen()
		{
			var executor = this.Executor ?? throw new InvalidOperationException("Missing the required command executor of the terminal.");

			//调用基类同名方法
			base.OnOpen();

			//激发“Opened”事件
			this.RaiseOpened();

			//启动命令运行器
			executor.Run();

			//关闭命令执行器
			//注意：因为基类中的线程同步锁独占机制，因此不能由“开启临界区”直接跳入“关闭临界区”
			System.Threading.Tasks.Task.Delay(500).ContinueWith(task => this.Close());
		}
		#endregion
	}
}