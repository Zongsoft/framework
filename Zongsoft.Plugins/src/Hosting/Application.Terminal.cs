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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
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
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.DependencyInjection;

using Zongsoft.Services;
using Zongsoft.Terminals;

namespace Zongsoft.Plugins.Hosting;

partial class Application
{
	#region 终端宿主
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

			//只替换通用宿主的默认控制台生命周期，保留应用显式指定的实现。
			for(int i = services.Count - 1; i >= 0; i--)
			{
				if(services[i].ServiceType != typeof(IHostLifetime))
					continue;

				if(services[i].ImplementationType == typeof(ConsoleLifetime))
				{
					services[i] = ServiceDescriptor.Singleton<IHostLifetime>(provider => new TerminalLifetime(
						provider.GetRequiredService<IHostApplicationLifetime>(),
						provider.GetRequiredService<IHostEnvironment>(),
						provider.GetRequiredService<IOptions<ConsoleLifetimeOptions>>(),
						provider.GetRequiredService<ILoggerFactory>(),
						() => provider.GetRequiredService<TerminalApplicationContext>().GetTerminal()));
				}

				break;
			}

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
		private TerminalWorkbench _terminal;
		public override string ApplicationType => "Terminal";
		public ITerminal GetTerminal() => _terminal?.Terminal ?? Terminals.Terminal.Default;
		protected override IWorkbenchBase CreateWorkbench(out PluginTreeNode node) => base.CreateWorkbench(out node) ?? (_terminal = new TerminalWorkbench(this));

		protected override async Task OnStoppingAsync(CancellationToken cancellation)
		{
			var stopping = _terminal?.StopAsync() ?? Task.CompletedTask;
			var disposal = new PluginDisposal();

			try { await base.OnStoppingAsync(cancellation).ConfigureAwait(false); }
			catch(Exception exception) { disposal.Errors.Add(exception); }

			try { await stopping.ConfigureAwait(false); }
			catch(Exception exception) { disposal.Errors.Add(exception); }

			disposal.ThrowIfFailed();
		}
	}

	private sealed class TerminalWorkbench(PluginApplicationContext applicationContext) : WorkbenchBase(applicationContext), IAsyncDisposable
	{
		#region 成员字段
		private ITerminalExecutor _executor;
		private ITerminalExecutor _activeExecutor;
		private readonly CancellationTokenSource _cancellation = new();
		private Task _running = Task.CompletedTask;
		#endregion

		#region 公共属性
		public ITerminal Terminal => (Volatile.Read(ref _activeExecutor) ?? this.Executor)?.Terminal;
		public ITerminalExecutor Executor
		{
			get => _executor ?? Terminals.Terminal.Default.Executor;
			set => _executor = value;
		}
		#endregion

		#region 重写方法
		protected override void OnOpen()
		{
			this.ApplicationContext.Started += this.Run;
			base.OnOpen();
		}

		protected override void OnClose()
		{
			this.ApplicationContext.Started -= this.Run;
			base.OnClose();
		}
		#endregion

		#region 私有方法
		private void Run(object sender, EventArgs e)
		{
			var executor = this.Executor ?? throw new InvalidOperationException(Properties.Resources.Terminal_ExecutorRequired_Message);
			Volatile.Write(ref _activeExecutor, executor);
			var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			_running = completion.Task;
			_ = this.RunAsync(executor, completion);
		}

		private async Task RunAsync(ITerminalExecutor executor, TaskCompletionSource completion)
		{
			Exception failure = null;

			try { Environment.ExitCode = await executor.RunAsync(_cancellation.Token).ConfigureAwait(false); }
			catch(OperationCanceledException) when(_cancellation.IsCancellationRequested) { }
			catch(Exception exception)
			{
				Environment.ExitCode = 1;
				failure = exception;
			}

			try { this.ApplicationContext.Exit(); }
			catch(Exception exception) { failure = failure == null ? exception : new AggregateException(failure, exception); }

			if(failure == null)
				completion.TrySetResult();
			else
			{
				completion.TrySetException(failure);
				_ = completion.Task.Exception;
			}
		}

		public async Task StopAsync()
		{
			var disposal = new PluginDisposal();

			try { await _cancellation.CancelAsync().ConfigureAwait(false); }
			catch(Exception exception) { disposal.Errors.Add(exception); }

			try { await _running.ConfigureAwait(false); }
			catch(Exception exception) { disposal.Errors.Add(exception); }

			disposal.ThrowIfFailed();
		}

		public async ValueTask DisposeAsync()
		{
			try { await this.StopAsync().ConfigureAwait(false); }
			finally
			{
				_cancellation.Dispose();
				this.Dispose();
			}
		}

		#endregion
	}
	#endregion
}
