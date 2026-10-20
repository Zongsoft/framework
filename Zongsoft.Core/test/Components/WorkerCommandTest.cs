/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Components.Commands;

namespace Zongsoft.Components.Tests;

public class WorkerCommandTest
{
	private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);

	[Theory]
	[InlineData("Start", WorkerState.Stopped, WorkerState.Running)]
	[InlineData("Stop", WorkerState.Running, WorkerState.Stopped)]
	[InlineData("Pause", WorkerState.Running, WorkerState.Paused)]
	[InlineData("Resume", WorkerState.Paused, WorkerState.Running)]
	public async Task CommandsAwaitAsynchronousLifecycleWithoutBlockingCaller(string operation, WorkerState initial, WorkerState expected)
	{
		var worker = new ProbeWorker { State = initial, Enabled = operation == "Start" };
		var command = CreateCommand(operation);
		var context = CreateContext(command, worker);
		var returned = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		Task<object> execution = null;
		var invocation = Task.Run(() =>
		{
			execution = command.ExecuteAsync(context).AsTask();
			returned.TrySetResult();
		});

		try
		{
			await returned.Task.WaitAsync(_timeout);
			if(execution.IsCompleted)
				await execution;

			await worker.Entered.Task.WaitAsync(_timeout);

			Assert.False(execution.IsCompleted);
			Assert.Equal(1, worker.AsynchronousCalls);
			Assert.Equal(operation, worker.Operation);

			if(operation is "Start" or "Stop")
				Assert.Equal(new[] { "first", "second" }, worker.Arguments);
			else
				Assert.Null(worker.Arguments);

			worker.Release.TrySetResult();
			var result = await execution.WaitAsync(_timeout);

			Assert.Same(worker, result);
			Assert.Equal(expected, worker.State);
			Assert.Equal(1, worker.AsynchronousCalls);
		}
		finally
		{
			worker.Release.TrySetResult();
			await invocation.WaitAsync(_timeout);
			if(execution != null)
				await ObserveAsync(execution);
		}
	}

	[Theory]
	[InlineData("Start", WorkerState.Stopped)]
	[InlineData("Stop", WorkerState.Running)]
	[InlineData("Pause", WorkerState.Running)]
	[InlineData("Resume", WorkerState.Paused)]
	public async Task CommandCancellationReachesPendingWorker(string operation, WorkerState initial)
	{
		using var cancellation = new CancellationTokenSource();
		var worker = new ProbeWorker { State = initial };
		var command = CreateCommand(operation);
		var execution = Task.Run(() => command.ExecuteAsync(CreateContext(command, worker), cancellation.Token).AsTask());

		try
		{
			if(execution.IsCompleted)
				await execution;

			await worker.Entered.Task.WaitAsync(_timeout);
			Assert.True(worker.Cancellation.CanBeCanceled);
			Assert.False(execution.IsCompleted);
			cancellation.Cancel();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution.WaitAsync(_timeout));
			Assert.True(worker.Cancellation.IsCancellationRequested);
			Assert.Equal(initial, worker.State);
			Assert.Equal(1, worker.AsynchronousCalls);
		}
		finally
		{
			worker.Release.TrySetResult();
			await ObserveAsync(execution);
		}
	}

	[Theory]
	[InlineData("Start", WorkerState.Stopped)]
	[InlineData("Stop", WorkerState.Running)]
	[InlineData("Pause", WorkerState.Running)]
	[InlineData("Resume", WorkerState.Paused)]
	public async Task CommandsPropagateWorkerFailure(string operation, WorkerState initial)
	{
		var failure = new ApplicationException();
		var worker = new ProbeWorker { State = initial, Failure = failure };
		var command = CreateCommand(operation);
		var execution = Task.Run(() => command.ExecuteAsync(CreateContext(command, worker)).AsTask());

		try
		{
			if(execution.IsCompleted)
				await execution;

			await worker.Entered.Task.WaitAsync(_timeout);
			Assert.False(execution.IsCompleted);
			worker.Release.TrySetResult();

			Assert.Same(failure, await Assert.ThrowsAsync<ApplicationException>(() => execution.WaitAsync(_timeout)));
			Assert.Equal(initial, worker.State);
			Assert.Equal(1, worker.AsynchronousCalls);
		}
		finally
		{
			worker.Release.TrySetResult();
			await ObserveAsync(execution);
		}
	}

	[Theory]
	[InlineData("Pause", WorkerState.Stopped)]
	[InlineData("Pause", WorkerState.Paused)]
	[InlineData("Resume", WorkerState.Stopped)]
	[InlineData("Resume", WorkerState.Running)]
	public async Task PauseAndResumeIgnoreInapplicableStates(string operation, WorkerState initial)
	{
		var worker = new ProbeWorker { State = initial, Enabled = false };
		var command = CreateCommand(operation);
		var execution = Task.Run(() => command.ExecuteAsync(CreateContext(command, worker)).AsTask());

		try
		{
			Assert.Same(worker, await execution.WaitAsync(_timeout));
			Assert.Equal(initial, worker.State);
			Assert.Equal(0, worker.AsynchronousCalls);
		}
		finally
		{
			worker.Release.TrySetResult();
			await ObserveAsync(execution);
		}
	}

	[Theory]
	[InlineData("Start", WorkerState.Starting, WorkerState.Running)]
	[InlineData("Stop", WorkerState.Stopping, WorkerState.Stopped)]
	[InlineData("Pause", WorkerState.Pausing, WorkerState.Paused)]
	[InlineData("Resume", WorkerState.Resuming, WorkerState.Running)]
	public async Task CommandsTimeoutWithoutStoppingLifecycle(string operation, WorkerState pending, WorkerState expected)
	{
		var worker = new GateWorker(operation);
		var completed = 0;
		var command = CreateObservedCommand(operation, () => Interlocked.Increment(ref completed));
		using var cancellation = new CancellationTokenSource();
		using var deadline = new CancellationTokenSource(_timeout);
		Task<object> execution = null;
		Task lifecycle = null;

		try
		{
			await PrepareAsync(worker, operation);
			execution = Task.Run(() => command.ExecuteAsync(CreateContext(command, worker, "--timeout:25ms"), cancellation.Token).AsTask());
			await worker.Entered.Task.WaitAsync(_timeout);
			await Assert.ThrowsAsync<TimeoutException>(() => execution.WaitAsync(deadline.Token));

			Assert.True(execution.IsFaulted);
			Assert.Equal(pending, worker.State);
			Assert.False(worker.Cancellation.IsCancellationRequested);
			Assert.False(cancellation.IsCancellationRequested);
			Assert.False(worker.Notified.Task.IsCompleted);
			Assert.Equal(0, completed);

			lifecycle = ContinueAsync(worker, operation);
			Assert.False(lifecycle.IsCompleted);
			worker.Release.TrySetResult();
			await lifecycle.WaitAsync(_timeout);
			var notification = await worker.Notified.Task.WaitAsync(_timeout);

			Assert.Equal(expected, worker.State);
			Assert.Equal(expected, notification.State);
			Assert.Equal(operation, notification.ActionName);
			Assert.Null(notification.Exception);
			Assert.Equal(1, worker.BlockedCalls);
			Assert.Equal(0, completed);
		}
		finally
		{
			worker.Release.TrySetResult();
			if(execution != null)
				await ObserveAsync(execution);
			if(lifecycle != null)
				await ObserveAsync(lifecycle);
			await worker.DisposeAsync().AsTask().WaitAsync(_timeout);
		}
	}

	[Fact]
	public async Task TimedOutStartupStillReportsLateFailure()
	{
		var failure = new ApplicationException();
		var worker = new GateWorker("Start") { Failure = failure };
		var completed = 0;
		var command = CreateObservedCommand("Start", () => Interlocked.Increment(ref completed));
		using var deadline = new CancellationTokenSource(_timeout);
		var execution = Task.Run(() => command.ExecuteAsync(CreateContext(command, worker, "--timeout:25ms")).AsTask());
		Task lifecycle = null;

		try
		{
			await worker.Entered.Task.WaitAsync(_timeout);
			await Assert.ThrowsAsync<TimeoutException>(() => execution.WaitAsync(deadline.Token));
			lifecycle = worker.StartAsync();
			worker.Release.TrySetResult();

			Assert.Same(failure, await Assert.ThrowsAsync<ApplicationException>(() => lifecycle.WaitAsync(_timeout)));
			var notification = await worker.Notified.Task.WaitAsync(_timeout);
			Assert.Same(failure, notification.Exception);
			Assert.Equal(WorkerState.Stopped, notification.State);
			Assert.Equal(WorkerState.Stopped, worker.State);
			Assert.Equal(1, worker.Starts);
			Assert.Equal(1, worker.Stops);
			Assert.Equal(0, completed);
			Assert.IsType<TimeoutException>(execution.Exception.InnerException);
		}
		finally
		{
			worker.Release.TrySetResult();
			await ObserveAsync(execution);
			if(lifecycle != null)
				await ObserveAsync(lifecycle);
			await worker.DisposeAsync().AsTask().WaitAsync(_timeout);
		}
	}

	private static async Task PrepareAsync(GateWorker worker, string operation)
	{
		if(operation != "Start")
			await worker.StartAsync().WaitAsync(_timeout);
		if(operation == "Resume")
			await worker.PauseAsync().WaitAsync(_timeout);
	}

	private static Task ContinueAsync(GateWorker worker, string operation) => operation switch
	{
		"Start" => worker.StartAsync(),
		"Stop" => worker.StopAsync(),
		"Pause" => worker.PauseAsync(),
		"Resume" => worker.ResumeAsync(),
		_ => throw new ArgumentOutOfRangeException(nameof(operation)),
	};

	private static ICommand CreateObservedCommand(string operation, Action completed) => operation switch
	{
		"Start" => new ObservedStartCommand(completed),
		"Stop" => new ObservedStopCommand(completed),
		"Pause" => new ObservedPauseCommand(completed),
		"Resume" => new ObservedResumeCommand(completed),
		_ => throw new ArgumentOutOfRangeException(nameof(operation)),
	};

	private sealed class ObservedStartCommand(Action completed) : WorkerStartCommand
	{
		protected override void OnStarted(CommandContext context, IWorker worker, TimeSpan timeout) => completed();
	}

	private sealed class ObservedStopCommand(Action completed) : WorkerStopCommand
	{
		protected override void OnStopped(CommandContext context, IWorker worker, TimeSpan timeout) => completed();
	}

	private sealed class ObservedPauseCommand(Action completed) : WorkerPauseCommand
	{
		protected override void OnPaused(CommandContext context, IWorker worker, TimeSpan timeout) => completed();
	}

	private sealed class ObservedResumeCommand(Action completed) : WorkerResumeCommand
	{
		protected override void OnResumed(CommandContext context, IWorker worker, TimeSpan timeout) => completed();
	}

	private sealed class GateWorker : WorkerBase
	{
		private readonly string _operation;

		public GateWorker(string operation)
		{
			_operation = operation;
			this.CanPauseAndContinue = true;
			this.StateChanged += (_, args) =>
			{
				if(args.ActionName == _operation)
				{
					this.Notified.TrySetResult(args);
				}
			};
		}

		public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource<WorkerStateChangedEventArgs> Notified { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public Exception Failure { get; init; }
		public CancellationToken Cancellation { get; private set; }
		public int BlockedCalls;
		public int Starts;
		public int Stops;

		protected override Task OnStartAsync(string[] args, CancellationToken cancellation)
		{
			this.Starts++;
			return this.InvokeAsync("Start", cancellation);
		}

		protected override Task OnStopAsync(string[] args, CancellationToken cancellation)
		{
			this.Stops++;
			return this.InvokeAsync("Stop", cancellation);
		}

		protected override Task OnPauseAsync(CancellationToken cancellation) => this.InvokeAsync("Pause", cancellation);
		protected override Task OnResumeAsync(CancellationToken cancellation) => this.InvokeAsync("Resume", cancellation);

		private async Task InvokeAsync(string operation, CancellationToken cancellation)
		{
			if(operation != _operation)
				return;

			this.BlockedCalls++;
			this.Cancellation = cancellation;
			this.Entered.TrySetResult();
			await this.Release.Task.WaitAsync(cancellation);

			if(this.Failure != null)
				throw this.Failure;
		}
	}

	private static ICommand CreateCommand(string operation) => operation switch
	{
		"Start" => new WorkerStartCommand(),
		"Stop" => new WorkerStopCommand(),
		"Pause" => new WorkerPauseCommand(),
		"Resume" => new WorkerResumeCommand(),
		_ => throw new ArgumentOutOfRangeException(nameof(operation)),
	};

	private static CommandContext CreateContext(ICommand command, IWorker worker, string options = null)
	{
		var executor = new CommandExecutor();
		var parent = executor.Root.Children.Add(new OwnerCommand(worker));
		var node = parent.Children.Add(command);

		return new(executor, CommandLine.Parse($"{command.Name} {options} first second")[0], node, null);
	}

	private static async Task ObserveAsync(Task task)
	{
		using var deadline = new CancellationTokenSource(_timeout);

		try { await task.WaitAsync(deadline.Token); }
		catch(Exception) when(!deadline.IsCancellationRequested) { }
	}

	private sealed class OwnerCommand : WorkerCommandBase
	{
		public OwnerCommand(IWorker worker) : base("Worker") => this.Worker = worker;
	}

	private sealed class ProbeWorker : IWorker
	{
		public event EventHandler<WorkerStateChangedEventArgs> StateChanged;
		public string Name => "Probe";
		public WorkerState State { get; set; }
		public bool Enabled { get; set; } = true;
		public bool CanPauseAndContinue => true;
		public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public Exception Failure { get; init; }
		public string Operation { get; private set; }
		public string[] Arguments { get; private set; }
		public CancellationToken Cancellation { get; private set; }
		public int AsynchronousCalls;

		public Task StartAsync(string[] args, CancellationToken cancellation = default) => this.InvokeAsync("Start", WorkerState.Running, args, cancellation);
		public Task StopAsync(string[] args, CancellationToken cancellation = default) => this.InvokeAsync("Stop", WorkerState.Stopped, args, cancellation);
		public Task PauseAsync(CancellationToken cancellation = default) => this.InvokeAsync("Pause", WorkerState.Paused, null, cancellation);
		public Task ResumeAsync(CancellationToken cancellation = default) => this.InvokeAsync("Resume", WorkerState.Running, null, cancellation);

		private async Task InvokeAsync(string operation, WorkerState state, string[] args, CancellationToken cancellation)
		{
			Interlocked.Increment(ref this.AsynchronousCalls);
			this.Operation = operation;
			this.Arguments = args;
			this.Cancellation = cancellation;
			this.Entered.TrySetResult();
			await this.Release.Task.WaitAsync(cancellation);

			if(this.Failure != null)
				throw this.Failure;

			this.State = state;
			this.StateChanged?.Invoke(this, new(operation, state));
		}
	}
}
