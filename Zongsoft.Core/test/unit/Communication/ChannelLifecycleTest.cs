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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Communication.Tests;

public class ChannelLifecycleTest
{
	private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);

	[Fact]
	public async Task ConcurrentCloseAndDisposeShareCleanup()
	{
		var channel = new ProbeChannel();
		var closing = 0;
		var closed = 0;
		Task reentrant = null;
		channel.Closing += (_, _) =>
		{
			Interlocked.Increment(ref closing);
			reentrant = channel.CloseAsync().AsTask();
		};
		channel.Closed += (_, _) => Interlocked.Increment(ref closed);

		var close = channel.CloseAsync().AsTask();
		await channel.Entered.Task.WaitAsync(_timeout);
		var concurrent = Enumerable.Range(0, 16).Select(_ => channel.CloseAsync().AsTask()).ToArray();
		var disposals = Enumerable.Range(0, 16).Select(_ => channel.DisposeAsync().AsTask()).ToArray();

		Assert.False(close.IsCompleted);
		Assert.All(disposals, task => Assert.False(task.IsCompleted));
		Assert.Equal(0, channel.Disposals);

		channel.Release.TrySetResult();
		await Task.WhenAll(concurrent.Concat(disposals).Append(close).Append(reentrant)).WaitAsync(_timeout);
		await channel.CloseAsync();
		await channel.DisposeAsync();

		Assert.True(channel.IsClosed);
		Assert.True(channel.IsDisposed);
		Assert.Equal(1, channel.Closes);
		Assert.Equal(1, channel.Disposals);
		Assert.Equal(1, closing);
		Assert.Equal(1, closed);
	}

	[Fact]
	public async Task CancellationOnlyAbandonsOneWaiter()
	{
		var channel = new ProbeChannel();
		using var cancellation = new CancellationTokenSource();
		var first = channel.CloseAsync(cancellation.Token).AsTask();
		var second = channel.CloseAsync().AsTask();
		cancellation.Cancel();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
		Assert.False(second.IsCompleted);
		Assert.False(channel.Cancellation.CanBeCanceled);

		channel.Release.TrySetResult();
		await second.WaitAsync(_timeout);
		await channel.DisposeAsync();

		Assert.Equal(1, channel.Closes);
	}

	[Fact]
	public async Task PreCanceledCloseDoesNotStartCleanup()
	{
		var channel = new ProbeChannel();
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => channel.CloseAsync(new(true)).AsTask());

		Assert.False(channel.Entered.Task.IsCompleted);
		Assert.False(channel.IsClosed);

		channel.Release.TrySetResult();
		await channel.DisposeAsync();
	}

	[Fact]
	public async Task FailuresCannotSkipCloseOrDisposal()
	{
		var channel = new ProbeChannel { FailClose = true };
		var closed = 0;
		channel.Closing += (_, _) => throw new InvalidOperationException();
		channel.Closed += (_, _) =>
		{
			closed++;
			throw new NotSupportedException();
		};
		channel.Release.TrySetResult();

		await Assert.ThrowsAsync<InvalidOperationException>(() => channel.DisposeAsync().AsTask());
		await Assert.ThrowsAsync<InvalidOperationException>(() => channel.DisposeAsync().AsTask());

		Assert.Equal(1, channel.Closes);
		Assert.Equal(1, channel.Disposals);
		Assert.Equal(1, closed);
		Assert.True(channel.IsClosed);
		Assert.True(channel.IsDisposed);
	}

	private sealed class ProbeChannel : ChannelBase
	{
		public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public int Closes;
		public int Disposals;
		public bool FailClose { get; init; }
		public CancellationToken Cancellation { get; private set; }

		protected override async ValueTask OnCloseAsync(CancellationToken cancellation)
		{
			Interlocked.Increment(ref this.Closes);
			this.Cancellation = cancellation;
			this.Entered.TrySetResult();
			await this.Release.Task;

			if(this.FailClose)
				throw new ApplicationException();
		}

		protected override ValueTask DisposeAsync(bool disposing)
		{
			Assert.True(this.IsClosed);
			Interlocked.Increment(ref this.Disposals);
			return ValueTask.CompletedTask;
		}
	}
}
