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

namespace Zongsoft.Plugins.Hosting
{
	partial class Application
	{
		private sealed class DaemonApplicationBuilder : ApplicationBuilder
		{
#if NET7_0_OR_GREATER
			public DaemonApplicationBuilder(string name, string[] args, Action<HostApplicationBuilder> configure = null) : base(name, args, configure)
			{
				_logger = Zongsoft.Diagnostics.Logger.GetLogger(this.Environment.ApplicationName);
				AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
			}
#else
			public DaemonApplicationBuilder(string name, string[] args, Action<IHostBuilder> configure = null) : base(name, args, configure)
			{
				_logger = Zongsoft.Diagnostics.Logger.GetLogger(this.Environment.ApplicationName);
				AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
			}
#endif
			protected override void RegisterServices(IServiceCollection services, PluginOptions options)
			{
				services.AddSingleton<DaemonApplicationContext>();
				services.AddSingleton<PluginApplicationContext>(provider => provider.GetRequiredService<DaemonApplicationContext>());
				services.AddSingleton<Services.IApplicationContext>(provider => provider.GetRequiredService<DaemonApplicationContext>());

				base.RegisterServices(services, options);
			}

			#region 全局异常
			private static Zongsoft.Diagnostics.Logger _logger;
			private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
			{
				if(e.ExceptionObject is Exception ex)
					_logger.Fatal(ex);
				else
					_logger.Fatal(e.ExceptionObject?.ToString());
			}
			#endregion
		}

		private sealed class DaemonApplicationContext : PluginApplicationContext
		{
			public DaemonApplicationContext(IServiceProvider serviceProvider) : base(serviceProvider) { }
			protected override IWorkbenchBase CreateWorkbench(out PluginTreeNode node) => base.CreateWorkbench(out node) ?? new DaemonWorkbench(this);
		}

		private sealed class DaemonWorkbench : WorkbenchBase
		{
			public DaemonWorkbench(DaemonApplicationContext applicationContext) : base(applicationContext) { }
		}
	}
}