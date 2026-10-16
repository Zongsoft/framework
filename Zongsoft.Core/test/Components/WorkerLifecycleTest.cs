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
		Assert.Equal(0, worker.Disposals);

		worker.StopRelease.TrySetResult();
		await Task.WhenAll(stops.Concat(disposals).Append(stop)).WaitAsync(_timeout);

		Assert.Equal(WorkerState.Stopped, worker.State);
		Assert.True(worker.IsDisposed);
		Assert.Equal(1, worker.Stops);
		Assert.Equal(1, worker.Disposals);
		await Assert.ThrowsAsync<ObjectDisposedException>(() => worker.StartAsync());
	}

	[Fact]
	public async Task StartFailureCleansPartialResourcesAndPropagates()
	{
		await using var worker = new ProbeWorker { FailStart = true };
		worker.StartRelease.TrySetResult();
		worker.StopRelease.TrySetResult();

		await Assert.ThrowsAsync<ApplicationException>(() => worker.StartAsync());

		Assert.Equal(1, worker.Stops);
		Assert.Equal(WorkerState.Stopped, worker.State);
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
		worker.FailStop = true;

		await Assert.ThrowsAsync<ApplicationException>(() => worker.StopAsync());
		await Assert.ThrowsAsync<InvalidOperationException>(() => worker.StartAsync());
		Assert.Equal(WorkerState.Stopping, worker.State);

		worker.FailStop = false;
		await worker.StopAsync();
		Assert.Equal(WorkerState.Stopped, worker.State);
	}

	[Fact]
	public async Task PauseAndResumeUseTheSameLifecycleCoordinator()
	{
		await using var worker = new ProbeWorker();
		worker.StartRelease.TrySetResult();
		worker.StopRelease.TrySetResult();
		await worker.StartAsync();
		await worker.PauseAsync();
		Assert.Equal(WorkerState.Paused, worker.State);

		await worker.ResumeAsync();
		Assert.Equal(WorkerState.Running, worker.State);
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
		public int Disposals;
		public bool FailStart { get; init; }
		public bool FailStop { get; set; }
		public CancellationToken StopCancellation { get; private set; }

		protected override async Task OnStartAsync(string[] args, CancellationToken cancellation)
		{
			Interlocked.Increment(ref this.Starts);
			this.Started.TrySetResult();
			await this.StartRelease.Task.WaitAsync(cancellation);

			if(this.FailStart)
				throw new ApplicationException();
		}

		protected override async Task OnStopAsync(string[] args, CancellationToken cancellation)
		{
			Interlocked.Increment(ref this.Stops);
			this.StopCancellation = cancellation;
			this.Stopping.TrySetResult();
			await this.StopRelease.Task;

			if(this.FailStop)
				throw new ApplicationException();
		}

		protected override void Dispose(bool disposing)
		{
			Assert.Equal(WorkerState.Stopped, this.State);
			Interlocked.Increment(ref this.Disposals);
		}
	}
}
