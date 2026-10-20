using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Xunit;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Zongsoft.Plugins.IntegrationTests;

public class TerminalLifecycleTests
{
	[Fact]
	[Trait("Category", "Integration")]
	public Task TerminalStartReturnsAndStopWaitsForCancelledInputLoop() => HostApplicationLifecycleTests.RunScenarioAsync("terminal-loop-stop", ["terminal-loop-stop"]);

	[Fact]
	[Trait("Category", "Integration")]
	public Task TerminalLoopCompletionRequestsExitWithoutTerminatingCaller() => HostApplicationLifecycleTests.RunScenarioAsync("terminal-loop-complete", ["terminal-loop-complete"]);

	[Fact]
	[Trait("Category", "Integration")]
	public Task ShippedManifestsMountCommandsOnRunningTerminalExecutor() => HostApplicationLifecycleTests.RunScenarioAsync("terminal-shipped-commands", ["terminal-shipped-commands"]);

	internal static async Task RunShippedCommandsAsync(string directory)
	{
		var timeout = TimeSpan.FromSeconds(10);
		var terminal = Terminals.Terminal.Console;
		using var input = new PendingInput();
		terminal.Input = input;
		var host = await Hosting.Application.TerminalAsync(typeof(TestProgram).Assembly.GetName().Name, [$"--contentRoot={directory}"], builder => builder.Environment.EnvironmentName = "Development");
		await using var disposal = (IAsyncDisposable)host;
		var application = host.Services.GetRequiredService<PluginApplicationContext>();

		try
		{
			await host.StartAsync().WaitAsync(timeout);
			await input.Entered.Task.WaitAsync(timeout);

			Assert.Equal(WorkbenchStatus.Running, application.Workbench.Status);
			Assert.Same(terminal.Executor, application.PluginTree.Find("/Workbench/Executor").UnwrapValue(ObtainMode.Never));
			Assert.Same(terminal.Executor, Components.CommandExecutor.Default);
			Assert.Same(terminal.Executor.Root.Children, application.PluginTree.Find("/Workbench/Executor/Commands").UnwrapValue(ObtainMode.Never));

			var node = terminal.Executor.Find("/plugin/list");
			Assert.NotNull(node);
			Assert.IsType<Commands.ListCommand>(node.Command);
			Assert.Same(application.PluginTree.Find("/Workbench/Executor/Commands/Plugin/List").UnwrapValue(ObtainMode.Never), node.Command);

			var result = await terminal.Executor.ExecuteAsync("/plugin/list");
			var plugins = Assert.IsAssignableFrom<IEnumerable<Plugin>>(result);

			Assert.Contains(plugins, plugin => plugin.Name == "Main");
			Assert.Contains(plugins, plugin => plugin.Name == "Zongsoft.Terminal");
		}
		finally
		{
			try { await host.StopAsync().WaitAsync(timeout); }
			finally { terminal.Input = null; }
		}
	}

	internal static async Task RunAsync(string scenario, string directory)
	{
		var timeout = TimeSpan.FromSeconds(10);
		var host = await Hosting.Application.TerminalAsync(typeof(TestProgram).Assembly.GetName().Name, [$"--contentRoot={directory}"], builder => builder.Environment.EnvironmentName = "Development");
		await using var hostDisposal = (IAsyncDisposable)host;
		var application = host.Services.GetRequiredService<PluginApplicationContext>();
		var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
		var workbench = application.Workbench;
		var executor = Assert.IsType<ControlledExecutor>(application.PluginTree.Find("/Workbench/Executor").UnwrapValue(ObtainMode.Never));
		var stopRequested = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		using var registration = lifetime.ApplicationStopping.Register(() => stopRequested.TrySetResult());

		try
		{
			await host.StartAsync().WaitAsync(timeout);
			await executor.Entered.Task.WaitAsync(timeout);

			Assert.Equal(WorkbenchStatus.Running, workbench.Status);
			Assert.Equal(1, executor.Runs);
			Assert.False(executor.Exited.Task.IsCompleted);
			Assert.False(stopRequested.Task.IsCompleted);

			if(scenario == "terminal-loop-stop")
			{
				var stopping = host.StopAsync();
				await executor.Cancelled.Task.WaitAsync(timeout);

				Assert.False(stopping.IsCompleted);
				Assert.False(executor.Exited.Task.IsCompleted);

				executor.StopRelease.TrySetResult();
				await stopping.WaitAsync(timeout);
			}
			else
			{
				executor.Completion.TrySetResult(0);
				await stopRequested.Task.WaitAsync(timeout);

				Assert.True(executor.Exited.Task.IsCompletedSuccessfully);
				Assert.False(executor.Cancelled.Task.IsCompleted);
				await host.StopAsync().WaitAsync(timeout);
			}

			Assert.True(executor.Exited.Task.IsCompletedSuccessfully);
			Assert.Equal(WorkbenchStatus.None, workbench.Status);
			Assert.Equal(1, executor.Runs);
		}
		finally
		{
			executor.Completion.TrySetResult(0);
			executor.StopRelease.TrySetResult();
		}
	}

	private sealed class PendingInput : TextReader
	{
		private readonly TaskCompletionSource<string> _pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public override ValueTask<string> ReadLineAsync(CancellationToken cancellationToken)
		{
			this.Entered.TrySetResult();
			return new(_pending.Task.WaitAsync(cancellationToken));
		}
	}

	public sealed class ControlledExecutor : Components.CommandExecutor, Terminals.ITerminalExecutor
	{
		public event EventHandler<Terminals.Terminal.ExitEventArgs> Exit { add { } remove { } }
		public Terminals.ITerminal Terminal => null;
		public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource Cancelled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource Exited { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource StopRelease { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource<int> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public int Runs { get; private set; }

		public Task<int> RunAsync(string splash, CancellationToken cancellation = default) => this.RunAsync(cancellation);
		public Task<int> RunAsync(Components.CommandOutletContent splash, CancellationToken cancellation = default) => this.RunAsync(cancellation);
		public async Task<int> RunAsync(CancellationToken cancellation = default)
		{
			this.Runs++;
			this.Entered.TrySetResult();

			try { return await this.Completion.Task.WaitAsync(cancellation); }
			catch(OperationCanceledException) when(cancellation.IsCancellationRequested)
			{
				this.Cancelled.TrySetResult();
				await this.StopRelease.Task;
				throw;
			}
			finally { this.Exited.TrySetResult(); }
		}
	}
}
