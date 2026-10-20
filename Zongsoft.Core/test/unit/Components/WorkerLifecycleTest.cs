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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Components.Tests;

public class WorkerLifecycleTest
{
	private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);

	[Fact]
	public async Task ConcurrentStartStopAndDisposeWaitForActualCompletion()
	{
		var worker = new ProbeWorker();
		var starts = Enumerable.Range(0, 16).Select(_ => worker.StartAsync()).ToArray();
		await worker.Started.Task.WaitAsync(_timeout);
		var resource = worker.Resource;
		var stop = worker.StopAsync();

		Assert.False(stop.IsCompleted);
		Assert.Equal(1, worker.Starts);

		worker.StartRelease.TrySetResult();
		await Task.WhenAll(starts).WaitAsync(_timeout);
		await worker.Stopping.Task.WaitAsync(_timeout);
		var stops = Enumerable.Range(0, 16).Select(_ => worker.StopAsync()).ToArray();
		var disposals = Enumerable.Range(0, 16).Select(_ => worker.DisposeAsync().AsTask()).ToArray();

		Assert.All(stops, task => Assert.False(task.IsCompleted));
		Assert.All(disposals, task => Assert.False(task.IsCompleted));
		Assert.Equal(0, resource.Disposals);

		worker.StopRelease.TrySetResult();
		await Task.WhenAll(stops.Concat(disposals).Append(stop)).WaitAsync(_timeout);

		Assert.Equal(WorkerState.Stopped, worker.State);
		Assert.True(worker.IsDisposed);
		Assert.Equal(1, worker.Stops);
		Assert.Equal(1, resource.Disposals);
		await Assert.ThrowsAsync<ObjectDisposedException>(() => worker.StartAsync());
	}

	[Fact]
	public async Task StartFailureCleansPartialResourcesAndPropagates()
	{
		await using var worker = new ProbeWorker { StartFailure = new ApplicationException() };
		var starting = worker.StartAsync();
		await worker.Started.Task.WaitAsync(_timeout);
		var resource = worker.Resource;
		worker.StartRelease.TrySetResult();
		worker.StopRelease.TrySetResult();

		await Assert.ThrowsAsync<ApplicationException>(() => starting.WaitAsync(_timeout));

		Assert.Equal(1, worker.Stops);
		Assert.Equal(WorkerState.Stopped, worker.State);
		Assert.Null(worker.Resource);
		Assert.Equal(1, resource.Disposals);
	}

	[Fact]
	public async Task CancelStopWaiterDoesNotCancelCleanup()
	{
		await using var worker = new ProbeWorker();
		worker.StartRelease.TrySetResult();
		await worker.StartAsync();

		using var cancellation = new CancellationTokenSource();
		var first = worker.StopAsync(cancellation: cancellation.Token);
		var second = worker.StopAsync();
		cancellation.Cancel();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
		Assert.False(second.IsCompleted);
		Assert.False(worker.StopCancellation.CanBeCanceled);

		worker.StopRelease.TrySetResult();
		await second.WaitAsync(_timeout);
		Assert.Equal(WorkerState.Stopped, worker.State);
	}

	[Fact]
	public async Task NotificationFailureDoesNotRollbackRunningState()
	{
		await using var worker = new ProbeWorker();
		worker.StartRelease.TrySetResult();
		worker.StopRelease.TrySetResult();
		worker.StateChanged += (_, args) =>
		{
			if(args.State == WorkerState.Running)
				throw new ApplicationException();
		};

		await Assert.ThrowsAsync<ApplicationException>(() => worker.StartAsync());

		Assert.Equal(WorkerState.Running, worker.State);
		await worker.StopAsync();
		Assert.Equal(WorkerState.Stopped, worker.State);
	}

	[Fact]
	public async Task StopFailureBlocksRestartUntilCleanupSucceeds()
	{
		await using var worker = new ProbeWorker();
		worker.StartRelease.TrySetResult();
		worker.StopRelease.TrySetResult();
		await worker.StartAsync();
		var resource = worker.Resource;
		worker.StopFailure = new ApplicationException();

		try
		{
			await Assert.ThrowsAsync<ApplicationException>(() => worker.StopAsync());
			await Assert.ThrowsAsync<InvalidOperationException>(() => worker.StartAsync());
			await Assert.ThrowsAsync<InvalidOperationException>(() => worker.PauseAsync());
			await Assert.ThrowsAsync<InvalidOperationException>(() => worker.ResumeAsync());

			Assert.Equal(WorkerState.Stopping, worker.State);
			Assert.Same(resource, worker.Resource);
			Assert.Equal(0, resource.Disposals);
			Assert.Equal(1, worker.Starts);
			Assert.Equal(1, worker.Stops);

			worker.StopFailure = null;
			await worker.StopAsync();

			Assert.Equal(WorkerState.Stopped, worker.State);
			Assert.Null(worker.Resource);
			Assert.Equal(1, resource.Disposals);
			Assert.Equal(2, worker.Stops);
		}
		finally { worker.StopFailure = null; }
	}

	[Fact]
	public async Task StartDuringStopDoesNotScheduleRestart()
	{
		await using var worker = new ProbeWorker();
		worker.StartRelease.TrySetResult();
		await worker.StartAsync();
		var stopping = worker.StopAsync();

		try
		{
			await worker.Stopping.Task.WaitAsync(_timeout);
			var starting = worker.StartAsync();

			Assert.True(starting.IsCompletedSuccessfully);
			await starting;
			Assert.False(stopping.IsCompleted);
			Assert.Equal(WorkerState.Stopping, worker.State);
			Assert.Equal(1, worker.Starts);
		}
		finally { worker.StopRelease.TrySetResult(); }

		await stopping.WaitAsync(_timeout);

		Assert.Equal(WorkerState.Stopped, worker.State);
		Assert.Equal(1, worker.Starts);
		Assert.Equal(1, worker.Stops);
	}

	[Fact]
	public async Task PauseDuringStartDoesNotSchedulePause()
	{
		await using var worker = new ProbeWorker();
		var notifications = new List<WorkerStateChangedEventArgs>();
		worker.StateChanged += (_, args) => notifications.Add(args);
		worker.StopRelease.TrySetResult();
		var starting = worker.StartAsync();

		try
		{
			await worker.Started.Task.WaitAsync(_timeout);
			var pausing = worker.PauseAsync();

			Assert.True(pausing.IsCompletedSuccessfully);
			await pausing;
			Assert.False(starting.IsCompleted);
			Assert.Equal(WorkerState.Starting, worker.State);
		}
		finally { worker.StartRelease.TrySetResult(); }

		await starting.WaitAsync(_timeout);

		Assert.Equal(WorkerState.Running, worker.State);
		Assert.Equal("Start", Assert.Single(notifications).ActionName);
		Assert.Equal(1, worker.Starts);
	}

	[Fact]
	public async Task PauseAndResumePreserveResourcesUntilStopAndRestart()
	{
		await using var worker = new ProbeWorker();
		var notifications = new List<WorkerStateChangedEventArgs>();
		worker.StateChanged += (_, args) => notifications.Add(args);
		worker.StartRelease.TrySetResult();
		worker.StopRelease.TrySetResult();
		await worker.StartAsync();
		var resource = worker.Resource;
		resource.Position = 37;
		await worker.PauseAsync();

		Assert.Equal(WorkerState.Paused, worker.State);
		Assert.Same(resource, worker.Resource);
		Assert.True(resource.Paused);
		Assert.Equal(37, resource.Position);
		Assert.Equal(0, resource.Disposals);
		Assert.Equal(0, worker.Stops);

		await worker.ResumeAsync();

		Assert.Equal(WorkerState.Running, worker.State);
		Assert.Same(resource, worker.Resource);
		Assert.False(resource.Paused);
		Assert.Equal(37, resource.Position);
		Assert.Equal(0, resource.Disposals);
		Assert.Equal(0, worker.Stops);

		await worker.StopAsync();

		Assert.Equal(WorkerState.Stopped, worker.State);
		Assert.Null(worker.Resource);
		Assert.Equal(1, resource.Disposals);
		Assert.Equal(
			[("Start", WorkerState.Running), ("Pause", WorkerState.Paused), ("Resume", WorkerState.Running), ("Stop", WorkerState.Stopped)],
			notifications.Select(args => (args.ActionName, args.State)));

		await worker.StartAsync();

		Assert.Equal(WorkerState.Running, worker.State);
		Assert.NotNull(worker.Resource);
		Assert.NotSame(resource, worker.Resource);
		Assert.Equal(0, worker.Resource.Position);
		Assert.False(worker.Resource.Paused);
		Assert.Equal(2, worker.Starts);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task PauseAndResumeFailuresRestoreStateAfterImplementationRollback(bool resuming)
	{
		await using var worker = new ProbeWorker();
		worker.StartRelease.TrySetResult();
		worker.StopRelease.TrySetResult();
		await worker.StartAsync().WaitAsync(_timeout);
		var resource = worker.Resource;
		resource.Position = 37;

		if(resuming)
			await worker.PauseAsync().WaitAsync(_timeout);

		var failure = new ApplicationException();
		if(resuming)
			worker.ResumeFailure = failure;
		else
			worker.PauseFailure = failure;

		var operation = resuming ? worker.ResumeAsync() : worker.PauseAsync();
		var error = await Assert.ThrowsAsync<ApplicationException>(() => operation.WaitAsync(_timeout));

		Assert.Same(failure, error);
		Assert.Equal(resuming ? WorkerState.Paused : WorkerState.Running, worker.State);
		Assert.Same(resource, worker.Resource);
		Assert.Equal(resuming, resource.Paused);
		Assert.Equal(37, resource.Position);
		Assert.Equal(0, resource.Disposals);
		Assert.Equal(1, worker.Starts);
		Assert.Equal(0, worker.Stops);

		worker.PauseFailure = null;
		worker.ResumeFailure = null;
		await (resuming ? worker.ResumeAsync() : worker.PauseAsync()).WaitAsync(_timeout);

		Assert.Equal(resuming ? WorkerState.Running : WorkerState.Paused, worker.State);
		Assert.Same(resource, worker.Resource);
		Assert.Equal(!resuming, resource.Paused);
		Assert.Equal(37, resource.Position);
	}

	[Fact]
	public async Task StartupAndCleanupFailuresRequireExplicitStopRetry()
	{
		var startFailure = new ApplicationException();
		var cleanupFailure = new InvalidOperationException();
		await using var worker = new ProbeWorker { StartFailure = startFailure, StopFailure = cleanupFailure };
		worker.StartRelease.TrySetResult();
		worker.StopRelease.TrySetResult();

		try
		{
			var failure = await Assert.ThrowsAsync<AggregateException>(() => worker.StartAsync().WaitAsync(_timeout));
			var resource = worker.Resource;

			Assert.Collection(failure.InnerExceptions, error => Assert.Same(startFailure, error), error => Assert.Same(cleanupFailure, error));
			Assert.Equal(WorkerState.Stopping, worker.State);
			Assert.Equal(1, worker.Starts);
			Assert.Equal(1, worker.Stops);
			Assert.NotNull(resource);
			Assert.Equal(0, resource.Disposals);

			await Assert.ThrowsAsync<InvalidOperationException>(() => worker.StartAsync().WaitAsync(_timeout));

			Assert.Equal(1, worker.Starts);
			Assert.Equal(1, worker.Stops);
			Assert.Same(resource, worker.Resource);

			worker.StopFailure = null;
			await worker.StopAsync().WaitAsync(_timeout);

			Assert.Equal(WorkerState.Stopped, worker.State);
			Assert.Equal(2, worker.Stops);
			Assert.Null(worker.Resource);
			Assert.Equal(1, resource.Disposals);

			worker.StartFailure = null;
			await worker.StartAsync().WaitAsync(_timeout);

			Assert.Equal(WorkerState.Running, worker.State);
			Assert.Equal(2, worker.Starts);
			Assert.NotNull(worker.Resource);
			Assert.NotSame(resource, worker.Resource);
		}
		finally
		{
			worker.StartFailure = null;
			worker.StopFailure = null;
		}
	}

	private sealed class ProbeWorker : WorkerBase
	{
		public ProbeWorker() => this.CanPauseAndContinue = true;
		public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource Stopping { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource StartRelease { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource StopRelease { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public int Starts;
		public int Stops;
		public Exception StartFailure { get; set; }
		public Exception StopFailure { get; set; }
		public Exception PauseFailure { get; set; }
		public Exception ResumeFailure { get; set; }
		public CancellationToken StopCancellation { get; private set; }
		public OwnedResource Resource { get; private set; }

		protected override async Task OnStartAsync(string[] args, CancellationToken cancellation)
		{
			Interlocked.Increment(ref this.Starts);
			this.Resource = new();
			this.Started.TrySetResult();
			await this.StartRelease.Task.WaitAsync(cancellation);

			if(this.StartFailure != null)
				throw this.StartFailure;
		}

		protected override async Task OnStopAsync(string[] args, CancellationToken cancellation)
		{
			Interlocked.Increment(ref this.Stops);
			this.StopCancellation = cancellation;
			this.Stopping.TrySetResult();
			await this.StopRelease.Task;

			if(this.StopFailure != null)
				throw this.StopFailure;

			this.Resource?.Dispose();
			this.Resource = null;
		}

		protected override Task OnPauseAsync(CancellationToken cancellation)
		{
			this.Resource.Paused = true;

			if(this.PauseFailure != null)
			{
				this.Resource.Paused = false;
				throw this.PauseFailure;
			}

			return Task.CompletedTask;
		}

		protected override Task OnResumeAsync(CancellationToken cancellation)
		{
			this.Resource.Paused = false;

			if(this.ResumeFailure != null)
			{
				this.Resource.Paused = true;
				throw this.ResumeFailure;
			}

			return Task.CompletedTask;
		}
	}

	private sealed class OwnedResource : IDisposable
	{
		public int Position;
		public bool Paused;
		public int Disposals;
		public void Dispose() => Interlocked.Increment(ref this.Disposals);
	}
}
