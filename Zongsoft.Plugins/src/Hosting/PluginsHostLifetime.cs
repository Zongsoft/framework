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
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Zongsoft.Plugins.Hosting
{
	public class PluginsHostLifetime : IHostLifetime, IDisposable
	{
		#region 私有变量
		private CancellationTokenRegistration _applicationStartedRegistration;
		private readonly PluginApplicationContext _applicationContext;
		private readonly IHostApplicationLifetime _applicationLifetime;
		private readonly HostOptions _hostOptions;
		private readonly ILogger _logger;

		private readonly ManualResetEvent _shutdownBlock = new ManualResetEvent(false);
		#endregion

		#region 构造函数
		public PluginsHostLifetime(PluginApplicationContext applicationContext, IHostApplicationLifetime applicationLifetime, IOptions<HostOptions> hostOptions)
			: this(applicationContext, applicationLifetime, hostOptions, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance) { }

		public PluginsHostLifetime(PluginApplicationContext applicationContext, IHostApplicationLifetime applicationLifetime, IOptions<HostOptions> hostOptions, ILoggerFactory loggerFactory)
		{
			_applicationContext = applicationContext ?? throw new ArgumentNullException(nameof(applicationContext));
			_applicationLifetime = applicationLifetime ?? throw new ArgumentNullException(nameof(applicationLifetime));
			_hostOptions = hostOptions?.Value ?? throw new ArgumentNullException(nameof(hostOptions));
			_logger = loggerFactory.CreateLogger("Zongsoft.Hosting.Lifetime");
		}
		#endregion

		#region 公共方法
		public Task StopAsync(CancellationToken cancellationToken)
		{
			if(!cancellationToken.IsCancellationRequested)
				_applicationContext.Dispose();

			return Task.CompletedTask;
		}

		public Task WaitForStartAsync(CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();

			_applicationStartedRegistration = _applicationLifetime.ApplicationStarted.Register(state =>
			{
				((PluginsHostLifetime)state).OnApplicationStarted();
			}, this);

			AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

#if !DEBUG
			try
#endif
			{
				_applicationContext.Initialize();
				_applicationContext.Workbench.Open();

				return Task.CompletedTask;
			}
#if !DEBUG
			catch(Exception ex)
			{
				_logger.LogError(ex, $"The {_applicationContext.Name} application failed to initialize.");
				throw;
			}
#endif
		}
		#endregion

		#region 事件响应
		private void OnApplicationStarted()
		{
#if DEBUG
			_applicationContext.Stopped += (_, __) => _applicationLifetime.StopApplication();
			//_applicationContext.Workbench.Open();
#else
			try
			{
				_applicationContext.Stopped += (_, __) => _applicationLifetime.StopApplication();
				//_applicationContext.Workbench.Open();
			}
			catch(Exception ex)
			{
				_logger.LogError(ex, $"The {_applicationContext.Name} application failed to start.");
				throw;
			}
#endif
		}

		private void OnProcessExit(object sender, EventArgs e)
		{
			_applicationLifetime.StopApplication();

			if(!_shutdownBlock.WaitOne(_hostOptions.ShutdownTimeout))
				_logger.LogInformation("Waiting for the host to be disposed. Ensure all 'IHost' instances are wrapped in 'using' blocks.");

			_shutdownBlock.WaitOne();
			Environment.ExitCode = 0;
		}
#endregion

		#region 处置方法
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if(disposing)
			{
				_shutdownBlock.Set();
				AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
				_applicationStartedRegistration.Dispose();
			}
		}
		#endregion
	}
}
