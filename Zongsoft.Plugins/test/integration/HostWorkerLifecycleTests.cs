using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Zongsoft.Plugins.IntegrationTests;

public class HostWorkerLifecycleTests
{
	[Fact]
	[Trait("Category", "Integration")]
	public Task HostWaitsForEveryWorkerBeforePublishingStarted() => HostApplicationLifecycleTests.RunScenarioAsync("workers-start", ["workers-start"]);

	[Fact]
	[Trait("Category", "Integration")]
	public Task HostStartFailureWaitsForAllStartsAndRollsBack() => HostApplicationLifecycleTests.RunScenarioAsync("workers-rollback", ["workers-rollback"]);

	[Fact]
	[Trait("Category", "Integration")]
	public Task HostStopFailureStillStopsRemainingWorkers() => HostApplicationLifecycleTests.RunScenarioAsync("workers-stop-failure", ["workers-stop-failure"]);

	[Fact]
	[Trait("Category", "Integration")]
	public Task HostStopCancellationDoesNotAbandonCleanup() => HostApplicationLifecycleTests.RunScenarioAsync("workers-stop-cancellation", ["workers-stop-cancellation"]);

	[Fact]
	[Trait("Category", "Integration")]
	public Task HostStopAndExitRequestReturnWithoutTerminatingProcess() => HostApplicationLifecycleTests.RunScenarioAsync("workers-exit", ["workers-exit"]);

	[Fact]
	[Trait("Category", "Integration")]
	public Task HostStopCancelsPendingStartupAndWaitsForRollback() => HostApplicationLifecycleTests.RunScenarioAsync("workers-start-stop", ["workers-start-stop"]);

	[Fact]
	[Trait("Category", "Integration")]
	public Task HostNotifiesEveryWorkerBeforeWaitingForStopCompletion() => HostApplicationLifecycleTests.RunScenarioAsync("workers-stop-dependency", ["workers-stop-dependency"]);

}

internal static class HostWorkerProbe
{
	private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);

	public static async Task RunAsync(string scenario, string directory)
	{
		var host = await Hosting.Application.DaemonAsync(typeof(TestProgram).Assembly.GetName().Name, [$"--contentRoot={directory}"], builder => builder.Environment.EnvironmentName = "Development");
		var application = host.Services.GetRequiredService<PluginApplicationContext>();
		var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
		await using var first = new GateWorker("first");
		await using var second = new GateWorker("second");
		var disabled = new DisabledWorker();
		application.Workers.Add(first);
		application.Workers.Add(second);
		application.Workers.Add(disabled);
		Exception[] disposalFailures = [];
		var started = 0;
		application.Started += (_, _) => Interlocked.Increment(ref started);

		try
		{
			switch(scenario)
			{
				case "workers-start":
					{
						var starting = host.StartAsync();
						await Task.WhenAll(first.StartEntered.Task, second.StartEntered.Task).WaitAsync(_timeout);
						Assert.False(starting.IsCompleted);
						Assert.Equal(0, started);
						Assert.False(lifetime.ApplicationStarted.IsCancellationRequested);

						first.StartRelease.TrySetResult();
						await first.StartExited.Task.WaitAsync(_timeout);
						Assert.False(starting.IsCompleted);
						Assert.Equal(0, started);

						second.StartRelease.TrySetResult();
						await starting.WaitAsync(_timeout);
						Assert.Equal(1, started);
						Assert.True(lifetime.ApplicationStarted.IsCancellationRequested);
						Assert.Equal(1, first.Starts);
						Assert.Equal(1, second.Starts);
						Assert.Equal(0, disabled.Starts);
						first.StopRelease.TrySetResult();
						second.StopRelease.TrySetResult();
						await host.StopAsync().WaitAsync(_timeout);
						Assert.Equal(1, first.Stops);
						Assert.Equal(1, second.Stops);
						Assert.Equal(1, disabled.Stops);
						break;
					}
				case "workers-rollback":
					{
						var startupFailure = new ApplicationException("startup");
						var rollbackFailure = new InvalidOperationException("rollback");
						first.StartFailure = startupFailure;
						first.StopRelease.TrySetResult();
						second.StopFailure = rollbackFailure;
						second.IgnoreStartCancellation = true;
						var starting = host.StartAsync();
						await Task.WhenAll(first.StartEntered.Task, second.StartEntered.Task).WaitAsync(_timeout);

						first.StartRelease.TrySetResult();
						await first.StopExited.Task.WaitAsync(_timeout);
						Assert.False(starting.IsCompleted);
						Assert.Equal(0, second.Stops);
						Assert.Equal(0, started);

						second.StartRelease.TrySetResult();
						await second.StopEntered.Task.WaitAsync(_timeout);
						Assert.False(starting.IsCompleted);
						second.StopRelease.TrySetResult();
						var failure = await Assert.ThrowsAnyAsync<Exception>(() => starting.WaitAsync(_timeout));
						Assert.Contains(startupFailure, Flatten(failure));
						Assert.Contains(rollbackFailure, Flatten(failure));
						Assert.Equal(1, first.Stops);
						Assert.Equal(1, second.Stops);
						Assert.Equal(0, started);
						Assert.False(lifetime.ApplicationStarted.IsCancellationRequested);
						disposalFailures = [rollbackFailure];
						var stoppingFailure = await Assert.ThrowsAnyAsync<Exception>(() => host.StopAsync().WaitAsync(_timeout));
						Assert.Contains(rollbackFailure, Flatten(stoppingFailure));
						break;
					}
				case "workers-stop-failure":
					{
						first.StartRelease.TrySetResult();
						second.StartRelease.TrySetResult();
						await host.StartAsync().WaitAsync(_timeout);
						var firstFailure = new ApplicationException("first stop");
						var secondFailure = new InvalidOperationException("second stop");
						first.StopFailure = firstFailure;
						second.StopFailure = secondFailure;
						first.StopRelease.TrySetResult();
						second.StopRelease.TrySetResult();

						var failure = await Assert.ThrowsAnyAsync<Exception>(() => host.StopAsync().WaitAsync(_timeout));

						Assert.Contains(firstFailure, Flatten(failure));
						Assert.Contains(secondFailure, Flatten(failure));
						disposalFailures = [firstFailure, secondFailure];
						Assert.Equal(1, first.Stops);
						Assert.Equal(1, second.Stops);
						Assert.Equal(WorkbenchStatus.None, application.Workbench.Status);
						break;
					}
				case "workers-stop-cancellation":
					{
						first.StartRelease.TrySetResult();
						second.StartRelease.TrySetResult();
						await host.StartAsync().WaitAsync(_timeout);
						using var cancellation = new CancellationTokenSource();
						var stopping = host.StopAsync(cancellation.Token);
						await first.StopEntered.Task.WaitAsync(_timeout);
						cancellation.Cancel();
						var cancellationFailure = await Assert.ThrowsAnyAsync<Exception>(() => stopping.WaitAsync(_timeout));
						Assert.All(Flatten(cancellationFailure), error => Assert.IsAssignableFrom<OperationCanceledException>(error));
						var disposing = application.DisposeAsync().AsTask();

						Assert.False(disposing.IsCompleted);
						Assert.False(first.StopCancellation.CanBeCanceled);
						first.StopRelease.TrySetResult();
						await second.StopEntered.Task.WaitAsync(_timeout);
						Assert.False(disposing.IsCompleted);
						second.StopRelease.TrySetResult();
						await disposing.WaitAsync(_timeout);
						Assert.Equal(1, first.Stops);
						Assert.Equal(1, second.Stops);
						break;
					}
				case "workers-start-stop":
					{
						var starting = host.StartAsync();
						await Task.WhenAll(first.StartEntered.Task, second.StartEntered.Task).WaitAsync(_timeout);
						first.StopRelease.TrySetResult();
						second.StopRelease.TrySetResult();

						await host.StopAsync().WaitAsync(_timeout);
						var failure = await Assert.ThrowsAnyAsync<Exception>(() => starting.WaitAsync(_timeout));

						Assert.Contains(Flatten(failure), error => error is OperationCanceledException);
						Assert.False(first.StartRelease.Task.IsCompleted);
						Assert.False(second.StartRelease.Task.IsCompleted);
						Assert.Equal(1, first.Stops);
						Assert.Equal(1, second.Stops);
						Assert.Equal(0, started);
						Assert.Equal(WorkbenchStatus.None, application.Workbench.Status);
						break;
					}
				case "workers-stop-dependency":
					{
						first.StartRelease.TrySetResult();
						second.StartRelease.TrySetResult();
						await host.StartAsync().WaitAsync(_timeout);
						first.StopDependency = second.StopEntered.Task;
						second.StopDependency = first.StopEntered.Task;
						first.StopRelease.TrySetResult();
						second.StopRelease.TrySetResult();

						await host.StopAsync().WaitAsync(_timeout);

						Assert.True(first.StopExited.Task.IsCompletedSuccessfully);
						Assert.True(second.StopExited.Task.IsCompletedSuccessfully);
						Assert.Equal(1, first.Stops);
						Assert.Equal(1, second.Stops);
						Assert.Equal(WorkbenchStatus.None, application.Workbench.Status);
						break;
					}
				case "workers-exit":
					{
						first.StartRelease.TrySetResult();
						second.StartRelease.TrySetResult();
						await host.StartAsync().WaitAsync(_timeout);
						application.Exit();

						Assert.True(lifetime.ApplicationStopping.IsCancellationRequested);
						Assert.Equal(0, first.Stops);
						Assert.Equal(0, second.Stops);
						first.StopRelease.TrySetResult();
						second.StopRelease.TrySetResult();
						await host.StopAsync().WaitAsync(_timeout);
						Assert.Equal(1, first.Stops);
						Assert.Equal(1, second.Stops);
						Assert.Equal(WorkbenchStatus.None, application.Workbench.Status);
						break;
					}
				default:
					throw new ArgumentOutOfRangeException(nameof(scenario));
			}
		}
		finally
		{
			first.StartRelease.TrySetResult();
			second.StartRelease.TrySetResult();
			first.StopRelease.TrySetResult();
			second.StopRelease.TrySetResult();
			first.StopEntered.TrySetResult();
			second.StopEntered.TrySetResult();
			first.StopFailure = null;
			second.StopFailure = null;
			var disposalFailure = await Record.ExceptionAsync(() => ((IAsyncDisposable)host).DisposeAsync().AsTask());

			if(disposalFailures.Length == 0)
				Assert.Null(disposalFailure);
			else
			{
				Assert.NotNull(disposalFailure);
				Assert.Equal(disposalFailures, Flatten(disposalFailure).Distinct());
			}
		}
	}

	private static System.Collections.Generic.IEnumerable<Exception> Flatten(Exception exception) => exception is AggregateException aggregate ? aggregate.Flatten().InnerExceptions : [exception];

	private sealed class DisabledWorker : Components.IWorker
	{
		public event EventHandler<Components.WorkerStateChangedEventArgs> StateChanged { add { } remove { } }
		public string Name => "disabled";
		public Components.WorkerState State => Components.WorkerState.Stopped;
		public bool Enabled { get; set; }
		public bool CanPauseAndContinue => false;
		public int Starts { get; private set; }
		public int Stops { get; private set; }
		public Task StartAsync(string[] args, CancellationToken cancellation = default)
		{
			this.Starts++;
			return Task.CompletedTask;
		}
		public Task StopAsync(string[] args, CancellationToken cancellation = default)
		{
			this.Stops++;
			return Task.CompletedTask;
		}
		public Task PauseAsync(CancellationToken cancellation = default) => throw new NotSupportedException();
		public Task ResumeAsync(CancellationToken cancellation = default) => throw new NotSupportedException();
	}

	private sealed class GateWorker(string name) : Components.WorkerBase(name)
	{
		public TaskCompletionSource StartEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource StartExited { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource StopEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource StopExited { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource StartRelease { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource StopRelease { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public int Starts;
		public int Stops;
		public Exception StartFailure { get; set; }
		public bool IgnoreStartCancellation { get; set; }
		public Task StopDependency { get; set; } = Task.CompletedTask;
		public Exception StopFailure { get; set; }
		public CancellationToken StopCancellation { get; private set; }

		protected override async Task OnStartAsync(string[] args, CancellationToken cancellation)
		{
			Interlocked.Increment(ref this.Starts);
			this.StartEntered.TrySetResult();
			await this.StartRelease.Task.WaitAsync(this.IgnoreStartCancellation ? CancellationToken.None : cancellation);
			this.StartExited.TrySetResult();

			if(this.StartFailure != null)
				throw this.StartFailure;
		}

		protected override async Task OnStopAsync(string[] args, CancellationToken cancellation)
		{
			Interlocked.Increment(ref this.Stops);
			this.StopCancellation = cancellation;
			this.StopEntered.TrySetResult();
			await this.StopDependency;
			await this.StopRelease.Task;
			this.StopExited.TrySetResult();
			var failure = this.StopFailure;
			this.StopFailure = null;

			if(failure != null)
				throw failure;
		}
	}
}
