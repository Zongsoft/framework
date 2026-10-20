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

public class WorkerDisposalTest
{
	private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);

	[Fact]
	public async Task DisposalBeforeStartCleansOwnedResources()
	{
		var worker = new ProbeWorker();
		var resource = worker.Resource;
		Task disposal = null;

		try
		{
			disposal = worker.DisposeAsync().AsTask();
			await worker.Stopping.Task.WaitAsync(_timeout);

			Assert.False(disposal.IsCompleted);
			Assert.False(worker.IsDisposed);
			Assert.Equal(0, worker.Starts);
			Assert.Equal(1, worker.Stops);
			Assert.Equal(0, resource.Disposals);
			await AssertClosedAsync(worker);

			worker.StopRelease.TrySetResult();
			await disposal.WaitAsync(_timeout);
			await worker.DisposeAsync().AsTask().WaitAsync(_timeout);

			Assert.True(worker.IsDisposed);
			Assert.Equal(WorkerState.Stopped, worker.State);
			Assert.Null(worker.Resource);
			Assert.Equal(1, resource.Disposals);
			Assert.Equal(1, worker.Stops);
			await AssertClosedAsync(worker);
		}
		finally { await CleanupAsync(worker, disposal); }
	}

	[Fact]
	public async Task FailedDisposalRequiresExplicitRetryAndKeepsLifecycleClosed()
	{
		var failure = new ApplicationException();
		var worker = new ProbeWorker { StopFailure = failure };
		var resource = worker.Resource;
		worker.Release();

		try
		{
			await worker.StartAsync().WaitAsync(_timeout);
			var error = await Assert.ThrowsAsync<ApplicationException>(() => worker.DisposeAsync().AsTask().WaitAsync(_timeout));

			Assert.Same(failure, error);
			Assert.False(worker.IsDisposed);
			Assert.Equal(WorkerState.Stopping, worker.State);
			Assert.Equal(1, worker.Stops);
			Assert.Same(resource, worker.Resource);
			Assert.Equal(0, resource.Disposals);
			await AssertClosedAsync(worker);
			Assert.Equal(1, worker.Stops);

			worker.StopFailure = null;
			await worker.DisposeAsync().AsTask().WaitAsync(_timeout);
			await worker.DisposeAsync().AsTask().WaitAsync(_timeout);

			Assert.True(worker.IsDisposed);
			Assert.Equal(WorkerState.Stopped, worker.State);
			Assert.Equal(2, worker.Stops);
			Assert.Null(worker.Resource);
			Assert.Equal(1, resource.Disposals);
			await AssertClosedAsync(worker);
		}
		finally { await CleanupAsync(worker); }
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task ConcurrentDisposalsShareOneAttempt(bool failStop)
	{
		var failure = failStop ? new ApplicationException() : null;
		var worker = new ProbeWorker { StopFailure = failure };
		var resource = worker.Resource;
		worker.StartRelease.TrySetResult();
		Task[] disposals = [];
		var launch = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var entered = Enumerable.Range(0, 8).Select(_ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)).ToArray();

		try
		{
			await worker.StartAsync().WaitAsync(_timeout);
			disposals = entered.Select(signal => Task.Run(async () =>
			{
				await launch.Task;
				var disposal = worker.DisposeAsync().AsTask();
				signal.TrySetResult();
				await disposal;
			})).ToArray();
			launch.TrySetResult();
			await Task.WhenAll(entered.Select(signal => signal.Task)).WaitAsync(_timeout);
			await worker.Stopping.Task.WaitAsync(_timeout);

			Assert.All(disposals, task => Assert.False(task.IsCompleted));
			Assert.Equal(1, worker.Stops);
			Assert.Equal(0, resource.Disposals);
			Assert.False(worker.StopCancellation.CanBeCanceled);

			worker.StopRelease.TrySetResult();
			foreach(var disposal in disposals)
				Assert.Same(failure, await Record.ExceptionAsync(() => disposal.WaitAsync(_timeout)));

			Assert.Equal(1, worker.Stops);
			Assert.Equal(!failStop, worker.IsDisposed);
			Assert.Equal(failStop ? WorkerState.Stopping : WorkerState.Stopped, worker.State);
			Assert.Equal(failStop ? 0 : 1, resource.Disposals);
			await AssertClosedAsync(worker);
		}
		finally
		{
			launch.TrySetResult();
			await CleanupAsync(worker, disposals);
		}
	}

	[Fact]
	public async Task DisposalWaitsForPendingStartAndPreservesAcceptedStopWaiters()
	{
		var worker = new ProbeWorker();
		var resource = worker.Resource;
		using var cancellation = new CancellationTokenSource();
		Task start = null;
		Task canceled = null;
		Task stop = null;
		Task disposal = null;

		try
		{
			start = worker.StartAsync();
			await worker.Starting.Task.WaitAsync(_timeout);
			canceled = worker.StopAsync(cancellation.Token);
			stop = worker.StopAsync();
			disposal = worker.DisposeAsync().AsTask();

			Assert.False(start.IsCompleted);
			Assert.False(stop.IsCompleted);
			Assert.False(disposal.IsCompleted);
			Assert.Equal(0, worker.Stops);
			await AssertClosedAsync(worker);

			cancellation.Cancel();
			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceled.WaitAsync(_timeout));

			Assert.False(stop.IsCompleted);
			Assert.False(disposal.IsCompleted);

			worker.StartRelease.TrySetResult();
			await start.WaitAsync(_timeout);
			await worker.Stopping.Task.WaitAsync(_timeout);

			Assert.Equal(1, worker.Stops);
			Assert.False(worker.StopCancellation.CanBeCanceled);
			Assert.False(stop.IsCompleted);
			Assert.False(disposal.IsCompleted);
			Assert.Equal(0, resource.Disposals);

			worker.StopRelease.TrySetResult();
			await Task.WhenAll(stop, disposal).WaitAsync(_timeout);

			Assert.True(worker.IsDisposed);
			Assert.Equal(WorkerState.Stopped, worker.State);
			Assert.Equal(1, worker.Starts);
			Assert.Equal(1, worker.Stops);
			Assert.Null(worker.Resource);
			Assert.Equal(1, resource.Disposals);
		}
		finally { await CleanupAsync(worker, start, canceled, stop, disposal); }
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task DisposalJoinsPendingStopWithoutRetryingItsFailure(bool failStop)
	{
		var failure = failStop ? new ApplicationException() : null;
		var worker = new ProbeWorker { StopFailure = failure };
		var resource = worker.Resource;
		worker.StartRelease.TrySetResult();
		Task stop = null;
		Task disposal = null;

		try
		{
			await worker.StartAsync().WaitAsync(_timeout);
			stop = worker.StopAsync();
			await worker.Stopping.Task.WaitAsync(_timeout);
			disposal = worker.DisposeAsync().AsTask();

			Assert.False(stop.IsCompleted);
			Assert.False(disposal.IsCompleted);
			Assert.Equal(1, worker.Stops);
			await AssertClosedAsync(worker);

			worker.StopRelease.TrySetResult();
			Assert.Same(failure, await Record.ExceptionAsync(() => stop.WaitAsync(_timeout)));
			Assert.Same(failure, await Record.ExceptionAsync(() => disposal.WaitAsync(_timeout)));

			Assert.Equal(1, worker.Stops);
			Assert.Equal(!failStop, worker.IsDisposed);
			Assert.Equal(failStop ? 0 : 1, resource.Disposals);

			worker.StopFailure = null;
			await worker.DisposeAsync().AsTask().WaitAsync(_timeout);

			Assert.True(worker.IsDisposed);
			Assert.Equal(failStop ? 2 : 1, worker.Stops);
			Assert.Equal(1, resource.Disposals);
		}
		finally { await CleanupAsync(worker, stop, disposal); }
	}

	[Fact]
	public async Task PreCanceledStopDoesNotStartCleanup()
	{
		var worker = new ProbeWorker();
		var resource = worker.Resource;
		worker.StartRelease.TrySetResult();
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();

		try
		{
			await worker.StartAsync().WaitAsync(_timeout);
			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => worker.StopAsync(cancellation.Token).WaitAsync(_timeout));

			Assert.Equal(WorkerState.Running, worker.State);
			Assert.Equal(0, worker.Stops);
			Assert.Equal(0, resource.Disposals);

			worker.StopRelease.TrySetResult();
			await worker.DisposeAsync().AsTask().WaitAsync(_timeout);

			Assert.True(worker.IsDisposed);
			Assert.Equal(1, worker.Stops);
			Assert.Equal(1, resource.Disposals);
		}
		finally { await CleanupAsync(worker); }
	}

	[Fact]
	public async Task CanceledCleanupPreservesCancellationAndAllowsDisposalRetry()
	{
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		var worker = new ProbeWorker { StopFailure = new OperationCanceledException(cancellation.Token) };
		var resource = worker.Resource;
		worker.StartRelease.TrySetResult();
		Task stop = null;
		Task disposal = null;

		try
		{
			await worker.StartAsync().WaitAsync(_timeout);
			stop = worker.StopAsync();
			await worker.Stopping.Task.WaitAsync(_timeout);
			disposal = worker.DisposeAsync().AsTask();
			worker.StopRelease.TrySetResult();

			var stopped = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => stop.WaitAsync(_timeout));
			var disposed = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => disposal.WaitAsync(_timeout));

			Assert.Equal(cancellation.Token, stopped.CancellationToken);
			Assert.Equal(cancellation.Token, disposed.CancellationToken);
			Assert.True(stop.IsCanceled);
			Assert.True(disposal.IsFaulted);
			Assert.False(worker.IsDisposed);
			Assert.Equal(WorkerState.Stopping, worker.State);
			Assert.Equal(1, worker.Stops);
			Assert.Equal(0, resource.Disposals);

			worker.StopFailure = null;
			await worker.DisposeAsync().AsTask().WaitAsync(_timeout);

			Assert.True(worker.IsDisposed);
			Assert.Equal(2, worker.Stops);
			Assert.Equal(1, resource.Disposals);
		}
		finally { await CleanupAsync(worker, stop, disposal); }
	}

	[Fact]
	public async Task DisposalNotificationFailureDoesNotReopenOrRepeatCleanup()
	{
		var failure = new ApplicationException();
		var worker = new ProbeWorker();
		var resource = worker.Resource;
		worker.Release();
		EventHandler<WorkerStateChangedEventArgs> handler = (_, args) =>
		{
			if(args.State == WorkerState.Stopped)
				throw failure;
		};
		worker.StateChanged += handler;

		try
		{
			await worker.StartAsync().WaitAsync(_timeout);
			var error = await Assert.ThrowsAsync<ApplicationException>(() => worker.DisposeAsync().AsTask().WaitAsync(_timeout));

			Assert.Same(failure, error);
			Assert.True(worker.IsDisposed);
			Assert.Equal(WorkerState.Stopped, worker.State);
			Assert.Null(worker.Resource);
			Assert.Equal(1, worker.Stops);
			Assert.Equal(1, resource.Disposals);
			await AssertClosedAsync(worker);

			await worker.DisposeAsync().AsTask().WaitAsync(_timeout);

			Assert.Equal(1, worker.Stops);
			Assert.Equal(1, resource.Disposals);
		}
		finally
		{
			worker.StateChanged -= handler;
			await CleanupAsync(worker);
		}
	}

	private static async Task AssertClosedAsync(ProbeWorker worker)
	{
		await Assert.ThrowsAsync<ObjectDisposedException>(() => worker.StartAsync().WaitAsync(_timeout));
		await Assert.ThrowsAsync<ObjectDisposedException>(() => worker.StopAsync().WaitAsync(_timeout));
		await Assert.ThrowsAsync<ObjectDisposedException>(() => worker.PauseAsync().WaitAsync(_timeout));
		await Assert.ThrowsAsync<ObjectDisposedException>(() => worker.ResumeAsync().WaitAsync(_timeout));
		Assert.True(worker.CanPauseAndContinue);
		_ = worker.Enabled;
		Assert.Contains(worker.Name, worker.ToString());
	}

	private static async Task CleanupAsync(ProbeWorker worker, params Task[] tasks)
	{
		worker.StopFailure = null;
		worker.Release();

		foreach(var task in tasks)
		{
			if(task == null)
				continue;

			try { await task.WaitAsync(_timeout); }
			catch(Exception exception) when(exception is not TimeoutException) { }
		}

		await worker.DisposeAsync().AsTask().WaitAsync(_timeout);
	}

	private sealed class ProbeWorker : WorkerBase
	{
		private OwnedResource _resource = new();

		public ProbeWorker() => this.CanPauseAndContinue = true;
		public TaskCompletionSource Starting { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource Stopping { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource StartRelease { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource StopRelease { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public Exception StopFailure { get; set; }
		public CancellationToken StopCancellation { get; private set; }
		public OwnedResource Resource => _resource;
		public int Starts;
		public int Stops;

		public void Release()
		{
			this.StartRelease.TrySetResult();
			this.StopRelease.TrySetResult();
		}

		protected override async Task OnStartAsync(string[] args, CancellationToken cancellation)
		{
			Interlocked.Increment(ref this.Starts);
			this.Starting.TrySetResult();
			await this.StartRelease.Task.WaitAsync(cancellation);
		}

		protected override async Task OnStopAsync(string[] args, CancellationToken cancellation)
		{
			Interlocked.Increment(ref this.Stops);
			this.StopCancellation = cancellation;
			this.Stopping.TrySetResult();
			await this.StopRelease.Task;

			if(this.StopFailure != null)
				throw this.StopFailure;

			Interlocked.Exchange(ref _resource, null)?.Dispose();
		}
	}

	private sealed class OwnedResource : IDisposable
	{
		public int Disposals;
		public void Dispose() => Interlocked.Increment(ref this.Disposals);
	}
}
