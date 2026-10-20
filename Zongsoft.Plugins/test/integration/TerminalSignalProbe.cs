using System;
using System.IO;
using System.Threading;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Xunit;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

using Zongsoft.Terminals;
using Zongsoft.Components;

namespace Zongsoft.Plugins.IntegrationTests;

internal static class TerminalSignalProbe
{
	private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);

	public static async Task RunAsync(string scenario, string directory)
	{
		var reactive = scenario is "reactive" or "quit" or "terminate";
		var custom = scenario == "custom" ? new CustomLifetime() : null;
		var terminal = Terminal.Console;
		using var input = new ObservedReader(reactive ? ["watch", "next"] : []);
		terminal.Input = input;
		var watch = new WatchCommand();
		var next = new NextCommand();
		terminal.Executor.Root.Children.Add(watch);
		terminal.Executor.Root.Children.Add(next);
		var host = await Hosting.Application.TerminalAsync(typeof(TestProgram).Assembly.GetName().Name, [$"--contentRoot={directory}"], builder =>
		{
			builder.Environment.EnvironmentName = "Development";
			builder.Services.Configure<ConsoleLifetimeOptions>(options => options.SuppressStatusMessages = true);

			if(custom != null)
				builder.Services.AddSingleton<IHostLifetime>(custom);
		});
		var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
		var stopping = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		using var stoppingRegistration = lifetime.ApplicationStopping.Register(() => stopping.TrySetResult());
		var failure = new IOException("The signal subscriber failed.");
		var aborts = 0;
		var handled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		CancelEventHandler subscriber = (_, _) => Interlocked.Increment(ref aborts);

		if(scenario == "failure")
			subscriber = (_, _) => throw failure;
		else if(scenario == "handled-idle")
			subscriber = (_, args) =>
			{
				args.Cancel = true;
				handled.TrySetResult();
			};

		terminal.Aborting += subscriber;

		try
		{
			await host.StartAsync().WaitAsync(_timeout);

			if(reactive)
			{
				await watch.Entered.Task.WaitAsync(_timeout);
				var shutdown = host.WaitForShutdownAsync();
				var signal = scenario switch { "quit" => PosixSignal.SIGQUIT, "terminate" => PosixSignal.SIGTERM, _ => PosixSignal.SIGINT };
				TerminalSignalNative.Send(signal);
				await watch.CleanupEntered.Task.WaitAsync(_timeout);

				Assert.False(watch.CleanupFinished.Task.IsCompleted);
				Assert.False(shutdown.IsCompleted);
				Assert.False(next.Invoked.Task.IsCompleted);
				Assert.False(watch.CleanupCancellation.CanBeCanceled);

				if(scenario == "reactive")
				{
					Assert.False(lifetime.ApplicationStopping.IsCancellationRequested);
					Assert.Null(watch.Failure);
					Assert.Equal(1, Volatile.Read(ref aborts));
					watch.CleanupRelease.SetResult();
					await next.Invoked.Task.WaitAsync(_timeout);
					await input.Entered.Task.WaitAsync(_timeout);

					Assert.True(watch.CleanupFinished.Task.IsCompletedSuccessfully);
					Assert.False(lifetime.ApplicationStopping.IsCancellationRequested);
					Assert.False(shutdown.IsCompleted);

					TerminalSignalNative.Send(PosixSignal.SIGINT);
					await shutdown.WaitAsync(_timeout);

					Assert.Equal(2, Volatile.Read(ref aborts));
					Assert.True(input.Cancelled.Task.IsCompletedSuccessfully);
				}
				else
				{
					Assert.True(lifetime.ApplicationStopping.IsCancellationRequested);
					Assert.IsAssignableFrom<OperationCanceledException>(watch.Failure);
					Assert.Equal(0, Volatile.Read(ref aborts));
					var repeated = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
					using var observer = PosixSignalRegistration.Create(PosixSignal.SIGINT, context =>
					{
						context.Cancel = true;
						repeated.TrySetResult();
					});
					TerminalSignalNative.Send(PosixSignal.SIGINT);
					await repeated.Task.WaitAsync(_timeout);

					Assert.False(shutdown.IsCompleted);
					Assert.Equal(0, Volatile.Read(ref aborts));

					watch.CleanupRelease.SetResult();
					await shutdown.WaitAsync(_timeout);

					Assert.True(watch.CleanupFinished.Task.IsCompletedSuccessfully);
					Assert.False(next.Invoked.Task.IsCompleted);
				}
			}
			else
			{
				await input.Entered.Task.WaitAsync(_timeout);

				if(scenario == "handled-idle")
				{
					TerminalSignalNative.Send(PosixSignal.SIGINT);
					await handled.Task.WaitAsync(_timeout);
					TerminalSignalNative.WriteInput("next");
					await next.Invoked.Task.WaitAsync(_timeout);

					Assert.False(lifetime.ApplicationStopping.IsCancellationRequested);

					terminal.Aborting -= subscriber;
					var shutdown = host.WaitForShutdownAsync();
					TerminalSignalNative.Send(PosixSignal.SIGINT);
					await shutdown.WaitAsync(_timeout);
				}
				else if(scenario == "custom")
				{
					Assert.Same(custom, host.Services.GetRequiredService<IHostLifetime>());
					Assert.Equal(1, custom.Starts);
					await host.StopAsync().WaitAsync(_timeout);

					Assert.Equal(1, custom.Stops);
					Assert.True(input.Cancelled.Task.IsCompletedSuccessfully);
				}
				else if(scenario == "disposed")
				{
					((IDisposable)host.Services.GetRequiredService<IHostLifetime>()).Dispose();
					File.AppendAllLines(Path.Combine(directory, "lifecycle.txt"), ["disposed-ready"]);
					TerminalSignalNative.Send(PosixSignal.SIGINT);
					await Task.Delay(_timeout);
					Assert.Fail("The disposed lifetime still suppresses the default signal action.");
				}
				else if(scenario == "failure")
				{
					TerminalSignalNative.Send(PosixSignal.SIGINT);
					await stopping.Task.WaitAsync(_timeout);
					var observed = await Assert.ThrowsAnyAsync<Exception>(() => host.StopAsync().WaitAsync(_timeout));

					Assert.Contains(failure, Flatten(observed));
					Assert.True(input.Cancelled.Task.IsCompletedSuccessfully);
				}
				else if(scenario.StartsWith("blocking-", StringComparison.Ordinal))
					await StopWhileInterruptingAsync(
						scenario.StartsWith("blocking-quit", StringComparison.Ordinal),
						scenario.EndsWith("-failure", StringComparison.Ordinal));
				else
				{
					var shutdown = host.WaitForShutdownAsync();
					TerminalSignalNative.Send(PosixSignal.SIGINT);
					await shutdown.WaitAsync(_timeout);

					Assert.True(lifetime.ApplicationStopping.IsCancellationRequested);
					Assert.True(input.Cancelled.Task.IsCompletedSuccessfully);
					Assert.Equal(1, Volatile.Read(ref aborts));
				}
			}

			Assert.Equal(0, Environment.ExitCode);
		}
		finally
		{
			terminal.Aborting -= subscriber;
			watch.CleanupRelease.TrySetResult();

			try { await ((IAsyncDisposable)host).DisposeAsync(); }
			catch(Exception exception) when(scenario == "failure" || scenario.EndsWith("-failure", StringComparison.Ordinal))
			{
				Assert.Contains(failure, Flatten(exception));
			}

			terminal.Input = null;
		}

		async Task StopWhileInterruptingAsync(bool signal, bool throws)
		{
			using var release = new ManualResetEventSlim();
			var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			CancelEventHandler blocker = (_, args) =>
			{
				args.Cancel = true;
				entered.TrySetResult();

				if(!release.Wait(_timeout + _timeout))
					throw new TimeoutException();

				if(throws)
					throw failure;
			};
			terminal.Aborting += blocker;

			try
			{
				await Task.Run(() => TerminalSignalNative.Send(PosixSignal.SIGINT));
				await entered.Task.WaitAsync(_timeout);

				Assert.False(lifetime.ApplicationStopping.IsCancellationRequested);

				if(signal)
				{
					await Task.Run(() => TerminalSignalNative.Send(PosixSignal.SIGQUIT));
					await stopping.Task.WaitAsync(_timeout);
				}

				var shutdown = host.StopAsync();
				await stopping.Task.WaitAsync(_timeout);
				var draining = host.Services.GetRequiredService<IHostLifetime>().StopAsync(CancellationToken.None);

				Assert.False(shutdown.IsCompleted);
				Assert.False(draining.IsCompleted);
				Assert.Equal(1, Volatile.Read(ref aborts));

				release.Set();

				if(throws)
				{
					var stopped = await Assert.ThrowsAnyAsync<Exception>(() => shutdown.WaitAsync(_timeout));
					var drained = await Assert.ThrowsAnyAsync<Exception>(() => draining.WaitAsync(_timeout));

					Assert.Contains(failure, Flatten(stopped));
					Assert.Contains(failure, Flatten(drained));
				}
				else
					await Task.WhenAll(shutdown, draining).WaitAsync(_timeout);

				Assert.True(lifetime.ApplicationStopping.IsCancellationRequested);
				Assert.Equal(1, Volatile.Read(ref aborts));
			}
			finally
			{
				release.Set();
				terminal.Aborting -= blocker;
			}
		}
	}

	private static IEnumerable<Exception> Flatten(Exception exception) => exception is AggregateException aggregate ? aggregate.Flatten().InnerExceptions : [exception];

	private sealed class CustomLifetime : IHostLifetime
	{
		public int Starts { get; private set; }
		public int Stops { get; private set; }
		public Task WaitForStartAsync(CancellationToken cancellation)
		{
			this.Starts++;
			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellation)
		{
			this.Stops++;
			return Task.CompletedTask;
		}
	}

	private sealed class ObservedReader(string[] commands) : TextReader
	{
		private readonly TextReader _standard = Terminal.Console.Input;
		private int _index;
		public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource Cancelled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public override async ValueTask<string> ReadLineAsync(CancellationToken cancellation = default)
		{
			if(_index < commands.Length)
				return commands[_index++];

			var reading = _standard.ReadLineAsync(cancellation);
			this.Entered.TrySetResult();

			try { return await reading; }
			catch(OperationCanceledException) when(cancellation.IsCancellationRequested)
			{
				this.Cancelled.TrySetResult();
				throw;
			}
		}
	}

	private sealed class WatchCommand() : CommandBase<CommandContext>("watch")
	{
		public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource CleanupEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource CleanupRelease { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource CleanupFinished { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public Exception Failure { get; private set; }
		public CancellationToken CleanupCancellation { get; private set; }
		protected override ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation) => context.ReactiveAsync(
			(_, _) =>
			{
				this.Entered.TrySetResult();
				return ValueTask.CompletedTask;
			},
			async (_, failure, token) =>
			{
				this.Failure = failure;
				this.CleanupCancellation = token;
				this.CleanupEntered.TrySetResult();
				await this.CleanupRelease.Task;
				this.CleanupFinished.TrySetResult();
			}, cancellation);
	}

	private sealed class NextCommand() : CommandBase<CommandContext>("next")
	{
		public TaskCompletionSource Invoked { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		protected override ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
		{
			this.Invoked.TrySetResult();
			return ValueTask.FromResult<object>(null);
		}
	}
}
