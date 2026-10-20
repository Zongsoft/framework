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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Runtime.InteropServices;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Zongsoft.Terminals;

namespace Zongsoft.Plugins.Hosting;

partial class Application
{
	//进程信号策略由终端宿主持有，终端对象只负责发送中断通知。
	private sealed class TerminalLifetime(IHostApplicationLifetime application, IHostEnvironment environment, IOptions<ConsoleLifetimeOptions> options, ILoggerFactory logging, Func<ITerminal> terminal) : IHostLifetime, IDisposable, IAsyncDisposable
	{
		#region 成员字段
		private readonly object _sync = new();
		private readonly ILogger _logger = logging.CreateLogger("Microsoft.Hosting.Lifetime");
		private PosixSignalRegistration _interrupt;
		private PosixSignalRegistration _quit;
		private PosixSignalRegistration _terminate;
		private CancellationTokenRegistration _startedRegistration;
		private CancellationTokenRegistration _stoppingRegistration;
		private TaskCompletionSource _interruption;
		private bool _stopping;
		#endregion

		#region 公共方法
		public Task WaitForStartAsync(CancellationToken cancellation)
		{
			cancellation.ThrowIfCancellationRequested();

			try
			{
				_interrupt = PosixSignalRegistration.Create(PosixSignal.SIGINT, this.OnSignal);
				_quit = PosixSignalRegistration.Create(PosixSignal.SIGQUIT, this.OnSignal);
				_terminate = PosixSignalRegistration.Create(PosixSignal.SIGTERM, this.OnSignal);

				if(!options.Value.SuppressStatusMessages)
				{
					_startedRegistration = application.ApplicationStarted.Register(this.OnStarted);
					_stoppingRegistration = application.ApplicationStopping.Register(this.OnStopping);
				}
			}
			catch
			{
				this.Dispose();
				throw;
			}

			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellation)
		{
			lock(_sync)
			{
				_stopping = true;
				//中断通知必须及时返回；等待它结束后宿主才能释放通知依赖的资源。
				return _interruption?.Task ?? Task.CompletedTask;
			}
		}
		#endregion

		#region 信号处理
		private void OnSignal(PosixSignalContext context)
		{
			//包括收尾期间的重复信号，均交由宿主有序停止，避免系统默认动作提前终止清理。
			context.Cancel = true;
			if(context.Signal != PosixSignal.SIGINT)
			{
				this.RequestStop();
				return;
			}

			TaskCompletionSource interruption;

			lock(_sync)
			{
				if(_stopping || application.ApplicationStopping.IsCancellationRequested)
					return;
				if(_interruption != null && !_interruption.Task.IsCompleted)
					return;

				_interruption = interruption = new(TaskCreationOptions.RunContinuationsAsynchronously);
			}

			Exception error = null;

			try
			{
				if(terminal()?.Abort() != true)
					this.RequestStop();
			}
			catch(Exception exception)
			{
				error = exception;
				this.RequestStop();
			}
			finally
			{
				if(error == null)
					interruption.TrySetResult();
				else
				{
					interruption.TrySetException(error);
					_ = interruption.Task.Exception;
				}
			}
		}

		private void RequestStop()
		{
			lock(_sync)
			{
				if(_stopping)
					return;

				_stopping = true;
			}

			application.StopApplication();
		}
		#endregion

		#region 状态通知
		private void OnStarted()
		{
			_logger.LogInformation("Application started. Press Ctrl+C to shut down.");
			_logger.LogInformation("Hosting environment: {EnvName}", environment.EnvironmentName);
			_logger.LogInformation("Content root path: {ContentRoot}", environment.ContentRootPath);
		}

		private void OnStopping() => _logger.LogInformation("Application is shutting down...");
		#endregion

		#region 释放资源
		public void Dispose()
		{
			lock(_sync)
				_stopping = true;

			_interrupt?.Dispose();
			_quit?.Dispose();
			_terminate?.Dispose();
			_startedRegistration.Dispose();
			_stoppingRegistration.Dispose();
		}

		public async ValueTask DisposeAsync()
		{
			this.Dispose();
			await this.StopAsync(CancellationToken.None).ConfigureAwait(false);
		}
		#endregion
	}
}
