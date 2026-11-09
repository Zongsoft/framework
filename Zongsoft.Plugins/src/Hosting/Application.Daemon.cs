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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.Collections.Generic;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Zongsoft.Plugins.Hosting;

partial class Application
{
	private sealed class DaemonApplicationBuilder : ApplicationBuilder
	{
		#if NET7_0_OR_GREATER
		public DaemonApplicationBuilder(string name, string[] args, Action<HostApplicationBuilder> configure = null) : base(name, args, configure)
		{
			_logging = Zongsoft.Diagnostics.Logging.GetLogging(this.Environment.ApplicationName);
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
		}
		#else
		public DaemonApplicationBuilder(string name, string[] args, Action<IHostBuilder> configure = null) : base(name, args, configure)
		{
			_logging = Zongsoft.Diagnostics.Logging.GetLogging(this.Environment.ApplicationName);
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
		}
		#endif

		protected override void RegisterServices(IServiceCollection services, PluginOptions options)
		{
			services.AddSingleton(provider => new DaemonApplicationContext(provider, options));
			services.AddSingleton<PluginApplicationContext>(provider => provider.GetRequiredService<DaemonApplicationContext>());
			services.AddSingleton<Services.IApplicationContext>(provider => provider.GetRequiredService<DaemonApplicationContext>());

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

	private sealed class DaemonApplicationContext(IServiceProvider serviceProvider, PluginOptions options) : PluginApplicationContext(serviceProvider, options)
	{
		public override string ApplicationType => "Daemon";
		protected override IWorkbenchBase CreateWorkbench(out PluginTreeNode node) => base.CreateWorkbench(out node) ?? new DaemonWorkbench(this);
	}

	private sealed class DaemonWorkbench(Application.DaemonApplicationContext applicationContext) : WorkbenchBase(applicationContext)
	{
	}
}