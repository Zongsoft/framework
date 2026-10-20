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
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Components.Tests;

public class WorkerEnabledTest
{
	private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);

	[Fact]
	public async Task DisabledWorkerSkipsStartUntilEnabled()
	{
		await using var worker = new ProbeWorker { Enabled = false };
		await worker.StartAsync().WaitAsync(_timeout);

		Assert.Equal(WorkerState.Stopped, worker.State);
		Assert.Equal(0, worker.Starts);
		Assert.Equal(0, worker.Stops);

		worker.Enabled = true;
		await worker.StartAsync().WaitAsync(_timeout);

		Assert.Equal(WorkerState.Running, worker.State);
		Assert.Equal(1, worker.Starts);
	}

	[Fact]
	public async Task DisablingDuringStartDoesNotInterruptAcceptedStart()
	{
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var worker = new ProbeWorker
		{
			Starting = async () =>
			{
				entered.TrySetResult();
				await release.Task;
			},
		};
		Task start = null;
		Task assignment = null;

		try
		{
			start = worker.StartAsync();
			await entered.Task.WaitAsync(_timeout);
			assignment = Task.Run(() => worker.Enabled = false);
			await assignment.WaitAsync(_timeout);

			Assert.False(start.IsCompleted);
			Assert.False(worker.Enabled);
			Assert.Equal(WorkerState.Starting, worker.State);
			Assert.Equal(0, worker.Stops);

			release.TrySetResult();
			await start.WaitAsync(_timeout);

			Assert.Equal(WorkerState.Running, worker.State);
			Assert.Equal(1, worker.Starts);
			Assert.Equal(0, worker.Stops);
		}
		finally
		{
			release.TrySetResult();
			if(start != null)
				await start.WaitAsync(_timeout);

			if(assignment != null)
				await assignment.WaitAsync(_timeout);

			await worker.DisposeAsync().AsTask().WaitAsync(_timeout);
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task DisablingStartedWorkerOnlyAffectsNextStart(bool paused)
	{
		await using var worker = new ProbeWorker();
		await worker.StartAsync().WaitAsync(_timeout);
		if(paused)
			await worker.PauseAsync().WaitAsync(_timeout);

		worker.Enabled = false;

		Assert.Equal(paused ? WorkerState.Paused : WorkerState.Running, worker.State);
		Assert.Equal(0, worker.Stops);

		if(!paused)
			await worker.PauseAsync().WaitAsync(_timeout);

		Assert.Equal(WorkerState.Paused, worker.State);
		await worker.ResumeAsync().WaitAsync(_timeout);
		Assert.Equal(WorkerState.Running, worker.State);
		Assert.Equal(1, worker.Pauses);
		Assert.Equal(1, worker.Resumes);

		await worker.StopAsync().WaitAsync(_timeout);
		await worker.StartAsync().WaitAsync(_timeout);

		Assert.Equal(WorkerState.Stopped, worker.State);
		Assert.Equal(1, worker.Starts);
		Assert.Equal(1, worker.Stops);

		worker.Enabled = true;
		await worker.StartAsync().WaitAsync(_timeout);
		worker.Enabled = false;
		await worker.DisposeAsync().AsTask().WaitAsync(_timeout);

		Assert.True(worker.IsDisposed);
		Assert.Equal(WorkerState.Stopped, worker.State);
		Assert.Equal(2, worker.Starts);
		Assert.Equal(2, worker.Stops);
	}

	[Fact]
	public async Task CallbacksCanChangeEnabledWithoutChangingCurrentOperation()
	{
		var worker = new ProbeWorker();
		var callbacks = new List<WorkerState>();
		var notifications = new List<WorkerState>();
		worker.Callback = state =>
		{
			worker.Enabled = false;
			callbacks.Add(state);
		};
		EventHandler<WorkerStateChangedEventArgs> handler = (_, args) =>
		{
			worker.Enabled = true;
			worker.Enabled = false;
			notifications.Add(args.State);
		};
		worker.StateChanged += handler;

		try
		{
			await worker.StartAsync().WaitAsync(_timeout);
			await worker.PauseAsync().WaitAsync(_timeout);
			await worker.ResumeAsync().WaitAsync(_timeout);
			await worker.DisposeAsync().AsTask().WaitAsync(_timeout);

			Assert.Equal(new[] { WorkerState.Starting, WorkerState.Pausing, WorkerState.Resuming, WorkerState.Stopping }, callbacks);
			Assert.Equal(new[] { WorkerState.Running, WorkerState.Paused, WorkerState.Running, WorkerState.Stopped }, notifications);
			Assert.False(worker.Enabled);
			Assert.True(worker.IsDisposed);
			Assert.Equal(1, worker.Starts);
			Assert.Equal(1, worker.Stops);
		}
		finally
		{
			worker.Callback = null;
			worker.StateChanged -= handler;
			await worker.DisposeAsync().AsTask().WaitAsync(_timeout);
		}
	}

	private sealed class ProbeWorker : WorkerBase
	{
		public ProbeWorker() => this.CanPauseAndContinue = true;
		public Func<Task> Starting { get; init; }
		public Action<WorkerState> Callback { get; set; }
		public int Starts;
		public int Stops;
		public int Pauses;
		public int Resumes;

		protected override Task OnStartAsync(string[] args, CancellationToken cancellation)
		{
			this.Starts++;
			this.Callback?.Invoke(WorkerState.Starting);
			return this.Starting?.Invoke() ?? Task.CompletedTask;
		}

		protected override Task OnStopAsync(string[] args, CancellationToken cancellation)
		{
			this.Stops++;
			this.Callback?.Invoke(WorkerState.Stopping);
			return Task.CompletedTask;
		}

		protected override Task OnPauseAsync(CancellationToken cancellation)
		{
			this.Pauses++;
			this.Callback?.Invoke(WorkerState.Pausing);
			return Task.CompletedTask;
		}

		protected override Task OnResumeAsync(CancellationToken cancellation)
		{
			this.Resumes++;
			this.Callback?.Invoke(WorkerState.Resuming);
			return Task.CompletedTask;
		}
	}
}
