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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Components.Tests;

public class WorkerCallbackTest
{
	private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);

	[Fact]
	public async Task ParentWorkerCanStartChildWorker()
	{
		await using var parent = new ProbeWorker();
		await using var child = new ProbeWorker();
		parent.Callback = async state =>
		{
			parent.Callback = null;
			Assert.Equal(WorkerState.Starting, state);

			await child.StartAsync().WaitAsync(_timeout);

			Assert.Equal(WorkerState.Running, child.State);
		};

		child.Callback = state =>
		{
			child.Callback = null;
			Assert.Equal(WorkerState.Starting, state);
			Assert.Equal(WorkerState.Starting, parent.State);
			Assert.Equal(WorkerState.Starting, child.State);

			return Task.CompletedTask;
		};

		await parent.StartAsync().WaitAsync(_timeout);

		Assert.Equal(WorkerState.Running, parent.State);
		Assert.Equal(WorkerState.Running, child.State);
		Assert.Equal(1, parent.Starts);
		Assert.Equal(1, child.Starts);
		Assert.Equal(0, parent.Stops);
		Assert.Equal(0, child.Stops);
	}

	[Fact]
	public async Task BackgroundTaskCanStopAfterStartingOperationCompletes()
	{
		var worker = new ProbeWorker();
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		Task background = null;
		worker.Callback = state =>
		{
			worker.Callback = null;
			background = Task.Run(async () =>
			{
				await release.Task.WaitAsync(_timeout);
				await worker.StopAsync().WaitAsync(_timeout);
			});
			return Task.CompletedTask;
		};

		try
		{
			await worker.StartAsync().WaitAsync(_timeout);

			Assert.Equal(WorkerState.Running, worker.State);
			Assert.False(background.IsCompleted);
			release.TrySetResult();
			await background.WaitAsync(_timeout);

			Assert.Equal(WorkerState.Stopped, worker.State);
			Assert.Equal(1, worker.Stops);
			Assert.False(worker.IsDisposed);
		}
		finally
		{
			release.TrySetResult();
			if(background != null)
				await background.WaitAsync(_timeout);

			await worker.DisposeAsync().AsTask().WaitAsync(_timeout);
		}
	}

	private sealed class ProbeWorker : WorkerBase
	{
		public Func<WorkerState, Task> Callback { get; set; }
		public int Starts;
		public int Stops;

		protected override Task OnStartAsync(string[] args, CancellationToken cancellation)
		{
			Interlocked.Increment(ref this.Starts);
			return this.Callback?.Invoke(WorkerState.Starting) ?? Task.CompletedTask;
		}

		protected override Task OnStopAsync(string[] args, CancellationToken cancellation)
		{
			Interlocked.Increment(ref this.Stops);
			return this.Callback?.Invoke(WorkerState.Stopping) ?? Task.CompletedTask;
		}
	}
}
