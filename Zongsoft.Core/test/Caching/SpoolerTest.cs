using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Xunit;

using Zongsoft.Diagnostics;

namespace Zongsoft.Caching.Tests;

public class SpoolerTest
{
	[Fact]
	public async Task TestClearAsync()
	{
		await using var spooler = new Spooler<string>((_, __) => ValueTask.CompletedTask, TimeSpan.FromHours(1));
		Assert.True(spooler.IsEmpty);

		await spooler.PutAsync("A", TestContext.Current.CancellationToken);
		await spooler.PutAsync("B", TestContext.Current.CancellationToken);
		await spooler.PutAsync("C", TestContext.Current.CancellationToken);
		Assert.Equal(3, spooler.Count);

		spooler.Clear();
		Assert.True(spooler.IsEmpty);
	}

	[Fact]
	public async Task TestFlushAsync()
	{
		const int COUNT = 1000;

		var flusher = new Flusher<string>();
		await using var spooler = new Spooler<string>(flusher.OnFlushAsync, TimeSpan.FromHours(1));
		Assert.True(spooler.IsEmpty);
		Assert.Equal(0, flusher.Count);

		await spooler.FlushAsync(TestContext.Current.CancellationToken);
		Assert.True(spooler.IsEmpty);
		Assert.Equal(0, flusher.Count);

		#if NET8_0_OR_GREATER
		await Parallel.ForAsync(0, COUNT, TestContext.Current.CancellationToken, async (index, cancellation) => await spooler.PutAsync($"Value#{index}", cancellation));
		#else
		for(int i = 0; i < COUNT; i++)
			await spooler.PutAsync($"Value#${i}", TestContext.Current.CancellationToken);
		#endif

		Assert.Equal(COUNT, spooler.Count);

		await spooler.FlushAsync(TestContext.Current.CancellationToken);
		Assert.True(spooler.IsEmpty);
		Assert.Equal(COUNT, flusher.Count);
	}

	[Fact]
	public async Task TestLimitAsync()
	{
		var flusher = new Flusher<string>();
		await using var spooler = new Spooler<string>(flusher.OnFlushAsync, TimeSpan.FromHours(1), 3);
		Assert.True(spooler.IsEmpty);
		Assert.Equal(0, flusher.Count);

		await spooler.PutAsync("A", TestContext.Current.CancellationToken);
		await spooler.PutAsync("B", TestContext.Current.CancellationToken);
		await spooler.PutAsync("C", TestContext.Current.CancellationToken);
		Assert.Equal(3, spooler.Count);
		Assert.Equal(0, flusher.Count);

		//触发数量限制
		await spooler.PutAsync("D", TestContext.Current.CancellationToken);

		Assert.False(spooler.IsEmpty);
		Assert.Equal(1, spooler.Count);
		Assert.Equal(3, flusher.Count);
	}

	[Fact]
	public async Task TestPeriodAsync()
	{
		var clock = new SpoolerTimeProvider();
		var flusher = new Flusher<string>();
		await using var spooler = new Spooler<string>(flusher.OnFlushAsync, TimeSpan.FromHours(1), clock: clock);
		Assert.True(spooler.IsEmpty);
		Assert.Equal(0, flusher.Count);

		await spooler.PutAsync("A", TestContext.Current.CancellationToken);
		await spooler.PutAsync("B", TestContext.Current.CancellationToken);
		await spooler.PutAsync("C", TestContext.Current.CancellationToken);
		Assert.Equal(3, spooler.Count);
		Assert.Equal(0, flusher.Count);

		#if NET8_0_OR_GREATER
		//设置触发周期
		spooler.Period = TimeSpan.FromSeconds(1);
		clock.Advance(spooler.Period);
		//等待周期刷新
		await flusher.WaitAsync(TestContext.Current.CancellationToken);

		Assert.True(spooler.IsEmpty);
		Assert.Equal(3, flusher.Count);
		#endif
	}

	[Fact]
	public async Task TestConcurrentFlushAsync()
	{
		const int COUNT = 256;
		const int CONCURRENCY = 16;

		var flusher = new RecordingFlusher<int>(TimeSpan.FromMilliseconds(10));
		await using var spooler = new Spooler<int>(flusher.OnFlushAsync, TimeSpan.FromHours(1));

		for(int i = 0; i < COUNT; i++)
			await spooler.PutAsync(i, TestContext.Current.CancellationToken);

		var tasks = Enumerable.Range(0, CONCURRENCY).Select(_ => spooler.FlushAsync(TestContext.Current.CancellationToken).AsTask()).ToArray();
		await Task.WhenAll(tasks);

		Assert.True(spooler.IsEmpty);
		Assert.Equal(1, flusher.Calls);
		Assert.Equal(1, flusher.MaximumConcurrency);
		Assert.Equal(COUNT, flusher.Count);
		Assert.Equal(Enumerable.Range(0, COUNT), flusher.Values.OrderBy(value => value));
	}

	[Fact]
	public async Task TestConcurrentLimitAsync()
	{
		const int COUNT = 1024;
		const int LIMIT = 8;

		var flusher = new RecordingFlusher<int>(TimeSpan.FromMilliseconds(1));
		await using var spooler = new Spooler<int>(flusher.OnFlushAsync, TimeSpan.FromHours(1), LIMIT);

		await Parallel.ForAsync(0, COUNT, TestContext.Current.CancellationToken, spooler.PutAsync);
		await spooler.FlushAsync(TestContext.Current.CancellationToken);

		Assert.True(spooler.IsEmpty);
		Assert.Equal(1, flusher.MaximumConcurrency);
		Assert.Equal(COUNT, flusher.Count);
		Assert.Equal(COUNT, flusher.Values.Distinct().Count());
		Assert.Equal(Enumerable.Range(0, COUNT), flusher.Values.OrderBy(value => value));
	}

	[Theory]
	[InlineData(1, 1)]
	[InlineData(3, 1)]
	[InlineData(100, 1)]
	[InlineData(3, 2)]
	[InlineData(3, 3)]
	public async Task PutAsync_RefilledDuringFlush_CompletesWithoutTimer(int limit, int enumerations)
	{
		using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		var flusher = new PausingFlusher(enumerations);
		await using var spooler = new Spooler<int>(flusher.OnFlushAsync, TimeSpan.FromDays(1), limit);

		for(int value = 0; value < limit; value++)
			await spooler.PutAsync(value, cancellation.Token);

		var pending = spooler.PutAsync(limit * 2, cancellation.Token).AsTask();

		try
		{
			await flusher.Entered.WaitAsync(TimeSpan.FromSeconds(5), cancellation.Token);
			for(int value = limit; value < limit * 2; value++)
				await spooler.PutAsync(value, cancellation.Token);

			Assert.Equal(limit, spooler.Count);
			flusher.Resume();

			// No timer or external FlushAsync may rescue this pending write.
			await pending.WaitAsync(TimeSpan.FromSeconds(2), cancellation.Token);
			Assert.Equal(1, spooler.Count);
			Assert.Equal(Enumerable.Range(0, limit * 2), flusher.Values);
			await spooler.FlushAsync(cancellation.Token);
			Assert.True(spooler.IsEmpty);
			Assert.Equal(Enumerable.Range(0, limit * 2 + 1), flusher.Values);
		}
		finally
		{
			flusher.Resume();
			await cancellation.CancelAsync();

			try { await pending.WaitAsync(TimeSpan.FromSeconds(5)); }
			catch(OperationCanceledException) { }
		}
	}

	[Fact]
	public async Task PutAsync_CancelledDuringFlush_DoesNotEnqueue()
	{
		using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		var flusher = new PausingFlusher();
		await using var spooler = new Spooler<int>(flusher.OnFlushAsync, TimeSpan.FromDays(1), 1);
		await spooler.PutAsync(0, cancellation.Token);
		var pending = spooler.PutAsync(1, cancellation.Token).AsTask();

		try
		{
			await flusher.Entered.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
			await cancellation.CancelAsync();
		}
		finally { flusher.Resume(); }

		var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending.WaitAsync(TimeSpan.FromSeconds(5)));
		Assert.Equal(cancellation.Token, exception.CancellationToken);
		Assert.True(spooler.IsEmpty);
		Assert.Equal(new[] { 0 }, flusher.Values);
		await spooler.PutAsync(2, TestContext.Current.CancellationToken);
		await spooler.FlushAsync(TestContext.Current.CancellationToken);
		Assert.Equal(new[] { 0, 2 }, flusher.Values);
	}

	[Fact]
	public async Task PutAsync_DisposedDuringFlush_ThrowsObjectDisposedException()
	{
		var flusher = new PausingFlusher();
		await using var spooler = new Spooler<int>(flusher.OnFlushAsync, TimeSpan.FromDays(1), 1);
		await spooler.PutAsync(0, TestContext.Current.CancellationToken);
		var pending = spooler.PutAsync(1, TestContext.Current.CancellationToken).AsTask();
		Task disposal;

		try
		{
			await flusher.Entered.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
			disposal = spooler.DisposeAsync().AsTask();

			Assert.False(disposal.IsCompleted);
		}
		finally { flusher.Resume(); }

		await Assert.ThrowsAsync<ObjectDisposedException>(() => pending.WaitAsync(TimeSpan.FromSeconds(5)));
		await disposal.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

		Assert.Equal(new[] { 0 }, flusher.Values);
		await Assert.ThrowsAsync<ObjectDisposedException>(() => spooler.PutAsync(2).AsTask());
	}

	[Fact]
	public async Task PutAsync_FlusherThrows_PropagatesAndCanRetry()
	{
		var failure = new InvalidOperationException("Consumer failed.");
		var fails = true;
		var notifications = 0;
		var consumed = new List<int>();
		await using var spooler = new Spooler<int>((values, _) =>
		{
			if(fails)
				throw failure;

			consumed.AddRange(values);
			return ValueTask.CompletedTask;
		}, TimeSpan.FromDays(1), 1);
		spooler.Failed += (_, _) => Interlocked.Increment(ref notifications);
		await spooler.PutAsync(0, TestContext.Current.CancellationToken);

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => spooler.PutAsync(1).AsTask());
		Assert.Same(failure, exception);
		Assert.Equal(1, spooler.Count);
		Assert.Empty(consumed);

		fails = false;
		await spooler.PutAsync(1, TestContext.Current.CancellationToken).AsTask().WaitAsync(TimeSpan.FromSeconds(5));
		await spooler.FlushAsync(TestContext.Current.CancellationToken);
		Assert.True(spooler.IsEmpty);
		Assert.Equal(new[] { 0, 1 }, consumed);
		Assert.Equal(0, notifications);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(3)]
	public async Task PutAsync_FastPath_CompletesSynchronously(int limit)
	{
		var flusher = new Flusher<int>();
		await using var spooler = new Spooler<int>(flusher.OnFlushAsync, TimeSpan.FromDays(1), limit);
		var pending = spooler.PutAsync(42, TestContext.Current.CancellationToken);

		Assert.True(pending.IsCompletedSuccessfully);
		await pending;
		Assert.Equal(1, spooler.Count);

		await spooler.FlushAsync(TestContext.Current.CancellationToken);

		Assert.Equal(1, flusher.Count);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(3)]
	public async Task PutAsync_CancelledBeforeWrite_DoesNotEnqueue(int limit)
	{
		var flusher = new Flusher<int>();
		await using var spooler = new Spooler<int>(flusher.OnFlushAsync, TimeSpan.FromDays(1), limit);
		using var cancellation = new CancellationTokenSource();
		await cancellation.CancelAsync();

		var pending = spooler.PutAsync(42, cancellation.Token);
		var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending.AsTask());

		Assert.Equal(cancellation.Token, exception.CancellationToken);
		Assert.True(spooler.IsEmpty);

		await spooler.FlushAsync(TestContext.Current.CancellationToken);

		Assert.Equal(0, flusher.Count);
	}

	[Fact]
	public async Task PutAsync_Disposed_ReturnsFaultedValueTask()
	{
		var spooler = new Spooler<int>((_, _) => ValueTask.CompletedTask, TimeSpan.FromDays(1), 3);
		await spooler.DisposeAsync();
		// Calling the method itself must not become a synchronous throw after optimization.
		var pending = spooler.PutAsync(1);
		Assert.True(pending.IsFaulted);
		await Assert.ThrowsAsync<ObjectDisposedException>(() => pending.AsTask());
	}

	[Fact]
	public async Task PutAsync_NonConsumingCallback_WaitsWithoutPollingAndResumesAfterClear()
	{
		var calls = 0;
		using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		await using var spooler = new Spooler<int>((_, _) =>
		{
			Interlocked.Increment(ref calls);
			return ValueTask.CompletedTask;
		}, TimeSpan.FromDays(1), 1);
		await spooler.PutAsync(0, cancellation.Token);
		var context = new CountingContext();
		var previous = SynchronizationContext.Current;
		Task pending;

		try
		{
			SynchronizationContext.SetSynchronizationContext(context);
			pending = spooler.PutAsync(1, cancellation.Token).AsTask();
		}
		finally { SynchronizationContext.SetSynchronizationContext(previous); }

		try
		{
			Assert.Equal(0, context.Posts);
			Assert.Equal(1, Volatile.Read(ref calls));
			Assert.False(pending.IsCompleted);
			spooler.Clear();
			await pending.WaitAsync(TimeSpan.FromSeconds(5), cancellation.Token);
			Assert.Equal(new[] { 1 }, spooler.ToArray());
			Assert.True(spooler.IsEmpty);
		}
		finally
		{
			await cancellation.CancelAsync();

			try { await pending.WaitAsync(TimeSpan.FromSeconds(5)); }
			catch(OperationCanceledException) { }
		}
	}

	[Fact]
	public async Task FlushAsync_WaiterCancelled_DoesNotInterruptOwner()
	{
		var flusher = new PausingFlusher(readBeforePause: false);
		await using var spooler = new Spooler<int>(flusher.OnFlushAsync, TimeSpan.FromDays(1), 3);
		using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		await spooler.PutAsync(42, TestContext.Current.CancellationToken);
		var owner = spooler.FlushAsync(TestContext.Current.CancellationToken).AsTask();

		try
		{
			await flusher.Entered.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
			var waiter = spooler.FlushAsync(cancellation.Token).AsTask();
			Assert.False(waiter.IsCompleted);
			await cancellation.CancelAsync();
			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiter.WaitAsync(TimeSpan.FromSeconds(5)));
			Assert.False(owner.IsCompleted);
		}
		finally { flusher.Resume(); }

		await owner.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
		Assert.Equal(new[] { 42 }, flusher.Values);
		Assert.Equal(1, flusher.Calls);
	}

	[Fact]
	public async Task FlushAsync_EmptyDuringCallback_WaitsForCallbackCompletion()
	{
		var flusher = new PausingFlusher();
		await using var spooler = new Spooler<int>(flusher.OnFlushAsync, TimeSpan.FromDays(1), 3);
		await spooler.PutAsync(42, TestContext.Current.CancellationToken);
		var owner = spooler.FlushAsync(TestContext.Current.CancellationToken).AsTask();
		Task emptyFlush;

		try
		{
			await flusher.Entered.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
			Assert.True(spooler.IsEmpty);

			emptyFlush = spooler.FlushAsync(TestContext.Current.CancellationToken).AsTask();

			Assert.False(emptyFlush.IsCompleted);
			Assert.False(owner.IsCompleted);
			Assert.Equal(1, flusher.Calls);
		}
		finally { flusher.Resume(); }

		await Task.WhenAll(owner, emptyFlush).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

		Assert.Equal(1, flusher.Calls);
		Assert.Equal(new[] { 42 }, flusher.Values);
	}

	[Theory]
	[InlineData(3)]
	[InlineData(100)]
	public async Task FlushAsync_RefilledWhileEnumerating_PreservesPerEnumerationLimit(int limit)
	{
		var values = new List<int>();
		var replenish = true;
		var next = limit;
		Spooler<int> spooler = null;

		await using var instance = spooler = new Spooler<int>(async (items, cancellation) =>
		{
			var count = 0;
			foreach(var value in items)
			{
				Assert.True(++count <= limit);
				values.Add(value);

				if(replenish)
				{
					var write = spooler.PutAsync(next++, cancellation);
					Assert.True(write.IsCompletedSuccessfully);
					await write;
				}
			}
		}, TimeSpan.FromDays(1), limit);

		for(int value = 0; value < limit; value++)
			await spooler.PutAsync(value, TestContext.Current.CancellationToken);

		await spooler.FlushAsync(TestContext.Current.CancellationToken);
		Assert.Equal(limit, spooler.Count);
		Assert.Equal(Enumerable.Range(0, limit), values);

		replenish = false;
		await spooler.FlushAsync(TestContext.Current.CancellationToken);
		Assert.True(spooler.IsEmpty);
		Assert.Equal(Enumerable.Range(0, limit * 2), values);
	}

	[Theory]
	[InlineData(1, 4, true)]
	[InlineData(100, 4, true)]
	[InlineData(1000, 16, true)]
	[InlineData(100, 16, false)]
	public async Task PutAsync_ConcurrentBatches_ConsumeEveryRecordOnce(int limit, int producers, bool asynchronous)
	{
		const int COUNT = 4096;
		var seen = new bool[COUNT];
		var active = 0;
		var consumed = 0;

		using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		cancellation.CancelAfter(TimeSpan.FromSeconds(15));

		await using var spooler = new Spooler<int>(async (items, token) =>
		{
			Assert.Equal(1, Interlocked.Increment(ref active));

			try
			{
				if(asynchronous)
					await Task.Yield();

				foreach(var value in items)
				{
					Assert.False(seen[value]);
					seen[value] = true;
					consumed++;
				}
			}
			finally
			{
				Interlocked.Decrement(ref active);
			}
		}, TimeSpan.FromDays(1), limit);

		var tasks = Enumerable.Range(0, producers).Select(producer => Task.Run(async () =>
		{
			for(int value = producer; value < COUNT; value += producers)
				await spooler.PutAsync(value, cancellation.Token);
		}, cancellation.Token)).ToArray();

		await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(20), cancellation.Token);
		while(!spooler.IsEmpty)
			await spooler.FlushAsync(cancellation.Token);

		Assert.Equal(COUNT, consumed);
		Assert.All(seen, Assert.True);
		Assert.Equal(0, active);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task PutAsync_NonConsumingCallback_CanCancelOrDispose(bool dispose)
	{
		using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		await using var spooler = new Spooler<int>((_, _) => ValueTask.CompletedTask, TimeSpan.FromDays(1), 1);
		await spooler.PutAsync(0, cancellation.Token);
		var pending = spooler.PutAsync(1, cancellation.Token).AsTask();
		Assert.False(pending.IsCompleted);

		if(dispose)
		{
			await spooler.DisposeAsync();
			await Assert.ThrowsAsync<ObjectDisposedException>(() => pending.WaitAsync(TimeSpan.FromSeconds(5)));
		}
		else
		{
			await cancellation.CancelAsync();
			var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending.WaitAsync(TimeSpan.FromSeconds(5)));
			Assert.Equal(cancellation.Token, exception.CancellationToken);
			Assert.Equal(new[] { 0 }, spooler.ToArray());
		}
	}

	[Fact]
	public async Task PutAsync_PartialConsumer_PreservesUnconsumedRecords()
	{
		var values = new List<int>();
		await using var spooler = new Spooler<int>((items, _) =>
		{
			using var iterator = items.GetEnumerator();
			Assert.True(iterator.MoveNext());
			values.Add(iterator.Current);
			return ValueTask.CompletedTask;
		}, TimeSpan.FromDays(1), 3);

		for(int value = 0; value < 10; value++)
			await spooler.PutAsync(value, TestContext.Current.CancellationToken).AsTask().WaitAsync(TimeSpan.FromSeconds(5));

		Assert.Equal(3, spooler.Count);
		Assert.Equal(Enumerable.Range(0, 7), values);

		while(!spooler.IsEmpty)
			await spooler.FlushAsync(TestContext.Current.CancellationToken);

		Assert.Equal(Enumerable.Range(0, 10), values);
	}

	[Fact]
	public async Task FlushAsync_AsyncFailure_ReleasesWaitingFlusher()
	{
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var failure = new InvalidOperationException("Asynchronous consumer failure.");
		var calls = 0;
		var notifications = 0;
		var values = new List<int>();

		await using var spooler = new Spooler<int>(async (items, _) =>
		{
			if(++calls == 1)
			{
				entered.SetResult();
				await release.Task;
				throw failure;
			}

			values.AddRange(items);
		}, TimeSpan.FromDays(1), 3);
		spooler.Failed += (_, _) => Interlocked.Increment(ref notifications);

		await spooler.PutAsync(42, TestContext.Current.CancellationToken);
		var owner = spooler.FlushAsync(TestContext.Current.CancellationToken).AsTask();
		Task waiter;

		try
		{
			await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
			waiter = spooler.FlushAsync(TestContext.Current.CancellationToken).AsTask();
			Assert.False(waiter.IsCompleted);
		}
		finally { release.TrySetResult(); }

		Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => owner.WaitAsync(TimeSpan.FromSeconds(5))));
		await waiter.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
		Assert.Equal(2, calls);
		Assert.Equal(new[] { 42 }, values);
		Assert.True(spooler.IsEmpty);
		Assert.Equal(0, notifications);
	}

	[Fact]
	public async Task PutAsync_WaitingForFlush_UsesFreedSpaceWithoutAnotherCallback()
	{
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var calls = 0;
		var values = new List<int>();
		await using var spooler = new Spooler<int>(async (items, _) =>
		{
			if(++calls == 1)
			{
				entered.SetResult();
				await release.Task;
			}

			using var iterator = items.GetEnumerator();
			Assert.True(iterator.MoveNext());
			values.Add(iterator.Current);
		}, TimeSpan.FromDays(1), 3);

		for(int value = 0; value < 3; value++)
			await spooler.PutAsync(value, TestContext.Current.CancellationToken);

		var owner = spooler.FlushAsync(TestContext.Current.CancellationToken).AsTask();
		Task pending;

		try
		{
			await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
			pending = spooler.PutAsync(3, TestContext.Current.CancellationToken).AsTask();
			Assert.False(pending.IsCompleted);
		}
		finally
		{
			release.TrySetResult();
		}

		await Task.WhenAll(owner, pending).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
		Assert.Equal(1, calls);
		Assert.Equal(new[] { 0 }, values);
		Assert.Equal(new[] { 1, 2, 3 }, spooler.ToArray());
	}

	[Fact]
	public async Task FlushAsync_DeferredAfterCallback_ProcessesNewItems()
	{
		using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		cancellation.CancelAfter(TimeSpan.FromSeconds(5));
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var values = new List<int>();
		var calls = 0;
		Task deferred = Task.CompletedTask;
		Spooler<int> spooler = null;

		await using var instance = spooler = new Spooler<int>((items, token) =>
		{
			if(++calls == 1)
			{
				deferred = Task.Run(async () =>
				{
					await release.Task.WaitAsync(token);
					await spooler.FlushAsync(token);
				}, token);
			}

			values.AddRange(items);
			return ValueTask.CompletedTask;
		}, TimeSpan.FromDays(1), 3);
		await spooler.PutAsync(10, cancellation.Token);

		try
		{
			await spooler.FlushAsync(cancellation.Token);
			await spooler.PutAsync(20, cancellation.Token);
		}
		finally { release.TrySetResult(); }

		await deferred.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

		Assert.Equal(2, calls);
		Assert.Equal(new[] { 10, 20 }, values);
		Assert.True(spooler.IsEmpty);
	}

	[Fact]
	public async Task DisposeAsync_ClosesAdmissionAndRejectsWaitersAfterCallbackEnds()
	{
		var flusher = new PausingFlusher(readBeforePause: false);
		await using var spooler = new Spooler<int>(flusher.OnFlushAsync, TimeSpan.FromDays(1), 1);
		await spooler.PutAsync(10, TestContext.Current.CancellationToken);
		var owner = spooler.FlushAsync(TestContext.Current.CancellationToken).AsTask();
		Task disposal;
		Task writer = null;
		Task waiter = null;

		try
		{
			await flusher.Entered.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
			writer = spooler.PutAsync(20, TestContext.Current.CancellationToken).AsTask();
			waiter = spooler.FlushAsync(TestContext.Current.CancellationToken).AsTask();
			Assert.False(writer.IsCompleted);
			Assert.False(waiter.IsCompleted);

			disposal = spooler.DisposeAsync().AsTask();

			Assert.False(disposal.IsCompleted);

			Assert.Throws<ObjectDisposedException>(() => spooler.Period);
			Assert.Throws<ObjectDisposedException>(() => spooler.Period = TimeSpan.FromSeconds(1));
			Assert.Throws<ObjectDisposedException>(() => spooler.Count);
			Assert.Throws<ObjectDisposedException>(spooler.Clear);
			await Assert.ThrowsAsync<ObjectDisposedException>(() => spooler.PutAsync(30).AsTask());
			await Assert.ThrowsAsync<ObjectDisposedException>(() => spooler.FlushAsync().AsTask());
			Assert.False(owner.IsCompleted);
		}
		finally { flusher.Resume(); }

		await Task.WhenAll(owner, disposal).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
		await Assert.ThrowsAsync<ObjectDisposedException>(() => writer.WaitAsync(TimeSpan.FromSeconds(5)));
		await Assert.ThrowsAsync<ObjectDisposedException>(() => waiter.WaitAsync(TimeSpan.FromSeconds(5)));

		Assert.Equal(1, flusher.Calls);
		Assert.Equal(new[] { 10 }, flusher.Values);
	}

	[Fact]
	public async Task FlushAsync_DerivedSpooler_ReceivesOriginalCancellation()
	{
		using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		await using var spooler = new DerivedSpooler(TimeSpan.FromDays(1), 3);
		await spooler.PutAsync(10, cancellation.Token);
		await spooler.PutAsync(20, cancellation.Token);

		await spooler.FlushAsync(cancellation.Token);

		Assert.Equal(cancellation.Token, spooler.Cancellation);
		Assert.Equal(new[] { 10, 20 }, spooler.Values);
		Assert.Equal(3, spooler.Limit);
		Assert.True(spooler.IsEmpty);
	}

	[Fact]
	public async Task PeriodicFlush_DerivedSpooler_UsesProvidedClock()
	{
		var clock = new SpoolerTimeProvider();
		await using var spooler = new DerivedSpooler(TimeSpan.FromMinutes(1), 3, clock);
		await spooler.PutAsync(42, TestContext.Current.CancellationToken);

		Assert.Empty(spooler.Values);
		Assert.False(spooler.Flushed.IsCompleted);

		clock.Advance(spooler.Period);
		await spooler.Flushed.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
		await spooler.DisposeAsync();

		Assert.Equal(new[] { 42 }, spooler.Values);
	}

	[Fact]
	public async Task PeriodicFlush_Failure_NotifiesAndAllowsExplicitProcessing()
	{
		var clock = new SpoolerTimeProvider();
		var failure = new InvalidOperationException("Periodic consumer failure.");
		var notified = new TaskCompletionSource<FailureEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
		var values = new List<int>();
		var calls = 0;
		var notifications = 0;
		object source = null;
		var spooler = new Spooler<int>((items, _) =>
		{
			if(Interlocked.Increment(ref calls) == 1)
				throw failure;

			values.AddRange(items);
			return ValueTask.CompletedTask;
		}, TimeSpan.FromDays(1), 1, clock);

		try
		{
			spooler.Failed += (sender, args) =>
			{
				source = sender;
				Interlocked.Increment(ref notifications);
				notified.TrySetResult(args);
			};
			await spooler.PutAsync(10, TestContext.Current.CancellationToken);

			clock.Advance(spooler.Period);
			var args = await notified.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

			Assert.Same(spooler, source);
			Assert.Same(failure, args.Exception);
			Assert.Null(args.State);
			Assert.Equal(1, spooler.Count);
			Assert.Empty(values);

			await spooler.PutAsync(20, TestContext.Current.CancellationToken);
			await spooler.FlushAsync(TestContext.Current.CancellationToken);

			Assert.Equal(3, calls);
			Assert.Equal(1, notifications);
			Assert.Equal(new[] { 10, 20 }, values);
			Assert.True(spooler.IsEmpty);

			spooler.Period = TimeSpan.FromHours(1);

			Assert.Equal(TimeSpan.FromHours(1), spooler.Period);
			Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => spooler.DisposeAsync().AsTask()));
		}
		finally { await CleanupAsync(spooler); }
	}

	[Fact]
	public async Task PeriodicFlush_NotificationThrows_SkipsLaterSubscribersAndPreservesFailures()
	{
		var clock = new SpoolerTimeProvider();
		var failure = new InvalidOperationException("Periodic consumer failure.");
		var notificationFailure = new InvalidOperationException("Notification failure.");
		var notified = new TaskCompletionSource<FailureEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
		var values = new List<int>();
		var calls = 0;
		var notifications = 0;
		object source = null;
		var spooler = new Spooler<int>((items, _) =>
		{
			if(++calls == 1)
				throw failure;

			values.AddRange(items);
			return ValueTask.CompletedTask;
		}, TimeSpan.FromDays(1), 1, clock);

		try
		{
			spooler.Failed += (sender, args) =>
			{
				source = sender;
				Interlocked.Increment(ref notifications);
				notified.TrySetResult(args);
				throw notificationFailure;
			};
			spooler.Failed += (_, _) => Interlocked.Increment(ref notifications);
			await spooler.PutAsync(10, TestContext.Current.CancellationToken);

			clock.Advance(spooler.Period);
			var args = await notified.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

			Assert.Same(spooler, source);
			Assert.Same(failure, args.Exception);
			Assert.Null(args.State);
			Assert.Equal(1, spooler.Count);
			Assert.Empty(values);

			await spooler.FlushAsync(TestContext.Current.CancellationToken);

			Assert.Equal(new[] { 10 }, values);
			Assert.Equal(2, calls);
			Assert.True(spooler.IsEmpty);

			var exception = await Assert.ThrowsAsync<AggregateException>(() =>
				spooler.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));

			Assert.Equal(1, notifications);
			Assert.Collection(exception.Flatten().InnerExceptions,
				error => Assert.Same(failure, error),
				error => Assert.Same(notificationFailure, error));
		}
		finally { await CleanupAsync(spooler); }
	}

	[Fact]
	public async Task DisposeAsync_BufferedItems_StopsAcceptingWithoutFlushing()
	{
		var calls = 0;
		var values = new List<int>();
		await using var spooler = new Spooler<int>((items, _) =>
		{
			calls++;
			values.AddRange(items);
			return ValueTask.CompletedTask;
		}, TimeSpan.FromDays(1), 3);

		Assert.False(typeof(IDisposable).IsAssignableFrom(spooler.GetType()));
		Assert.Null(spooler.GetType().GetMethod("Dispose", Type.EmptyTypes));
		var disposable = Assert.IsAssignableFrom<IAsyncDisposable>(spooler);

		await spooler.PutAsync(10, TestContext.Current.CancellationToken);
		await spooler.PutAsync(20, TestContext.Current.CancellationToken);

		await disposable.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
		await spooler.DisposeAsync();

		Assert.Equal(0, calls);
		Assert.Empty(values);
		await Assert.ThrowsAsync<ObjectDisposedException>(() => spooler.PutAsync(30).AsTask());
		await Assert.ThrowsAsync<ObjectDisposedException>(() => spooler.FlushAsync().AsTask());
	}

	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(2)]
	public async Task DisposeAsync_ActiveFlush_WaitsWithoutDrainingPendingItems(int trigger)
	{
		var clock = new SpoolerTimeProvider();
		var flusher = new PausingFlusher();
		await using var spooler = new Spooler<int>(flusher.OnFlushAsync, TimeSpan.FromDays(1), 3, clock);

		for(int value = 0; value < 3; value++)
			await spooler.PutAsync(value, TestContext.Current.CancellationToken);

		var owner = trigger switch
		{
			0 => spooler.FlushAsync(TestContext.Current.CancellationToken).AsTask(),
			1 => spooler.PutAsync(3, TestContext.Current.CancellationToken).AsTask(),
			_ => Task.CompletedTask,
		};

		if(trigger == 2)
			clock.Advance(spooler.Period);

		Task disposal = null;
		Task[] others = [];
		Task writer = null;
		Task waiter = null;

		try
		{
			await flusher.Entered.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
			Assert.True(spooler.IsEmpty);

			for(int value = 10; value < 13; value++)
				await spooler.PutAsync(value, TestContext.Current.CancellationToken);

			writer = spooler.PutAsync(13, TestContext.Current.CancellationToken).AsTask();
			waiter = spooler.FlushAsync(TestContext.Current.CancellationToken).AsTask();
			Assert.False(writer.IsCompleted);
			Assert.False(waiter.IsCompleted);

			disposal = spooler.DisposeAsync().AsTask();

			Assert.False(disposal.IsCompleted);
			await Assert.ThrowsAsync<ObjectDisposedException>(() => spooler.PutAsync(20).AsTask());

			var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			var callers = 0;
			others = Enumerable.Range(0, 3).Select(_ => Task.Run(async () =>
			{
				var pending = spooler.DisposeAsync();
				if(Interlocked.Increment(ref callers) == 3)
					entered.TrySetResult();

				await pending;
			}, TestContext.Current.CancellationToken)).ToArray();
			await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

			Assert.False(disposal.IsCompleted);
			Assert.All(others, pending => Assert.False(pending.IsCompleted));
		}
		finally { flusher.Resume(); }

		if(trigger == 1)
			await Assert.ThrowsAsync<ObjectDisposedException>(() => owner.WaitAsync(TimeSpan.FromSeconds(5)));
		else
			await owner.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

		await Task.WhenAll(others.Append(disposal)).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
		await Assert.ThrowsAsync<ObjectDisposedException>(() => writer.WaitAsync(TimeSpan.FromSeconds(5)));
		await Assert.ThrowsAsync<ObjectDisposedException>(() => waiter.WaitAsync(TimeSpan.FromSeconds(5)));
		await spooler.DisposeAsync();

		Assert.Equal(1, flusher.Calls);
		Assert.Equal(new[] { 0, 1, 2 }, flusher.Values);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task DisposeAsync_ExplicitFlushFailure_DoesNotRepeatOperationFailure(bool full)
	{
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var failure = new ApplicationException("Explicit callback failure.");
		var values = new List<int>();
		var notifications = 0;
		await using var spooler = new Spooler<int>(async (items, _) =>
		{
			values.AddRange(items);
			entered.TrySetResult();
			await release.Task;
			throw failure;
		}, TimeSpan.FromDays(1), 1);
		spooler.Failed += (_, _) => Interlocked.Increment(ref notifications);
		await spooler.PutAsync(10, TestContext.Current.CancellationToken);
		var owner = full ?
			spooler.PutAsync(20, TestContext.Current.CancellationToken).AsTask() :
			spooler.FlushAsync(TestContext.Current.CancellationToken).AsTask();
		Task disposal;

		try
		{
			await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
			Assert.True(spooler.IsEmpty);

			disposal = spooler.DisposeAsync().AsTask();

			Assert.False(disposal.IsCompleted);
			Assert.False(owner.IsCompleted);
		}
		finally { release.TrySetResult(); }

		Assert.Same(failure, await Assert.ThrowsAsync<ApplicationException>(() => owner.WaitAsync(TimeSpan.FromSeconds(5))));
		await disposal.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
		await spooler.DisposeAsync();

		Assert.Equal(new[] { 10 }, values);
		Assert.Equal(0, notifications);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task DisposeAsync_PeriodicFailure_WaitsForNotificationAndPreservesFailure(bool notificationThrows)
	{
		var clock = new SpoolerTimeProvider();
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		using var release = new ManualResetEventSlim();
		var cancellation = TestContext.Current.CancellationToken;
		var failure = new ApplicationException("Periodic callback failure.");
		var notificationFailure = new InvalidOperationException("Failure notification failed.");
		var calls = 0;
		var notifications = 0;
		Task ticking = null;
		var spooler = new Spooler<int>((_, _) =>
		{
			Interlocked.Increment(ref calls);
			return ValueTask.FromException(failure);
		}, TimeSpan.FromDays(1), 3, clock);

		try
		{
			spooler.Failed += (_, _) =>
			{
				Interlocked.Increment(ref notifications);
				entered.TrySetResult();

				if(!release.Wait(TimeSpan.FromSeconds(10), cancellation))
					throw new TimeoutException();

				if(notificationThrows)
					throw notificationFailure;
			};
			spooler.Failed += (_, _) => Interlocked.Increment(ref notifications);
			await spooler.PutAsync(10, cancellation);
			ticking = Task.Run(() => clock.Advance(spooler.Period), cancellation);
			Task disposal;

			try
			{
				await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellation);
				disposal = spooler.DisposeAsync().AsTask();

				Assert.False(disposal.IsCompleted);
				Assert.Equal(1, Volatile.Read(ref notifications));
				await Assert.ThrowsAsync<ObjectDisposedException>(() => spooler.PutAsync(20).AsTask());
			}
			finally { release.Set(); }

			var observed = await Record.ExceptionAsync(() => disposal.WaitAsync(TimeSpan.FromSeconds(5), cancellation));

			if(notificationThrows)
			{
				var errors = Assert.IsType<AggregateException>(observed).Flatten().InnerExceptions;
				Assert.Collection(errors,
					error => Assert.Same(failure, error),
					error => Assert.Same(notificationFailure, error));
			}
			else
				Assert.Same(failure, observed);

			var repeated = await Record.ExceptionAsync(() => spooler.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5), cancellation));

			Assert.Same(observed, repeated);
			Assert.Equal(1, calls);
			Assert.Equal(notificationThrows ? 1 : 2, notifications);
		}
		finally
		{
			release.Set();

			try
			{
				if(ticking != null)
					await ticking.WaitAsync(TimeSpan.FromSeconds(5));
			}
			finally { await CleanupAsync(spooler); }
		}
	}

	[Fact]
	public async Task DisposeAsync_PeriodicCallbackThrowsObjectDisposedException_PreservesFailure()
	{
		var clock = new SpoolerTimeProvider();
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var notified = new TaskCompletionSource<FailureEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
		var failure = new ObjectDisposedException("Callback dependency");
		var values = new List<int>();
		var calls = 0;
		var notifications = 0;
		var spooler = new Spooler<int>(async (items, _) =>
		{
			Interlocked.Increment(ref calls);
			values.AddRange(items);
			entered.TrySetResult();
			await release.Task;
			throw failure;
		}, TimeSpan.FromDays(1), 3, clock);

		try
		{
			spooler.Failed += (_, args) =>
			{
				Interlocked.Increment(ref notifications);
				notified.TrySetResult(args);
			};
			await spooler.PutAsync(42, TestContext.Current.CancellationToken);
			clock.Advance(spooler.Period);
			Task disposal;

			try
			{
				await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
				Assert.True(spooler.IsEmpty);

				disposal = spooler.DisposeAsync().AsTask();

				Assert.False(disposal.IsCompleted);
				Assert.Equal(0, Volatile.Read(ref notifications));
			}
			finally { release.TrySetResult(); }

			Assert.Same(failure, await Assert.ThrowsAsync<ObjectDisposedException>(() => disposal.WaitAsync(TimeSpan.FromSeconds(5))));
			var args = await notified.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

			Assert.Same(failure, args.Exception);
			Assert.Null(args.State);
			Assert.Equal(1, notifications);
			Assert.Equal(1, calls);
			Assert.Equal(new[] { 42 }, values);
		}
		finally { await CleanupAsync(spooler); }
	}

	[Fact]
	public async Task DisposeAsync_PendingPeriodicTick_DoesNotNotifyFailure()
	{
		var clock = new SpoolerTimeProvider();
		var flusher = new PausingFlusher();
		var notifications = 0;
		await using var spooler = new Spooler<int>(flusher.OnFlushAsync, TimeSpan.FromDays(1), 3, clock);
		spooler.Failed += (_, _) => Interlocked.Increment(ref notifications);
		await spooler.PutAsync(42, TestContext.Current.CancellationToken);
		var owner = spooler.FlushAsync(TestContext.Current.CancellationToken).AsTask();
		Task disposal;

		try
		{
			await flusher.Entered.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
			await spooler.PutAsync(99, TestContext.Current.CancellationToken);
			clock.Advance(spooler.Period);
			disposal = spooler.DisposeAsync().AsTask();

			Assert.False(disposal.IsCompleted);
			Assert.False(owner.IsCompleted);
			await Assert.ThrowsAsync<ObjectDisposedException>(() => spooler.PutAsync(100).AsTask());
		}
		finally { flusher.Resume(); }

		await Task.WhenAll(owner, disposal).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

		Assert.Equal(0, notifications);
		Assert.Equal(1, flusher.Calls);
		Assert.Equal(new[] { 42 }, flusher.Values);
	}

	[Fact]
	public async Task DisposeAsync_DerivedCleanup_RunsOnceAfterCallbackEnds()
	{
		await using var spooler = new DisposableSpooler();
		await spooler.PutAsync(42, TestContext.Current.CancellationToken);
		var owner = spooler.FlushAsync(TestContext.Current.CancellationToken).AsTask();
		Task disposal;
		Task repeated;

		try
		{
			await spooler.Entered.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

			disposal = spooler.DisposeAsync().AsTask();
			repeated = spooler.DisposeAsync().AsTask();

			Assert.False(disposal.IsCompleted);
			Assert.False(repeated.IsCompleted);
			Assert.False(spooler.CallbackCompleted);
			Assert.Equal(0, spooler.ReleaseCount);
		}
		finally { spooler.Resume(); }

		await Task.WhenAll(owner, disposal, repeated).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
		await spooler.DisposeAsync();

		Assert.True(spooler.CallbackCompleted);
		Assert.False(spooler.ReleasedDuringCallback);
		Assert.Equal(1, spooler.ReleaseCount);
		Assert.Equal(new[] { 42 }, spooler.Values);
	}

	private static async Task CleanupAsync<T>(Spooler<T> spooler) =>
		await Record.ExceptionAsync(() => spooler.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5)));

	private sealed class DisposableSpooler() : Spooler<int>(TimeSpan.FromDays(1), 3)
	{
		private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
		private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
		private int _released;

		public Task Entered => _entered.Task;
		public List<int> Values { get; } = [];
		public bool CallbackCompleted { get; private set; }
		public bool ReleasedDuringCallback { get; private set; }
		public int ReleaseCount { get; private set; }
		public void Resume() => _release.TrySetResult();

		protected override async ValueTask OnFlushAsync(IEnumerable<int> items, CancellationToken cancellation)
		{
			this.Values.AddRange(items);
			_entered.TrySetResult();
			await _release.Task;
			this.CallbackCompleted = true;
		}

		protected override async ValueTask DisposeAsyncCore()
		{
			await base.DisposeAsyncCore();

			if(Interlocked.Exchange(ref _released, 1) == 0)
			{
				this.ReleasedDuringCallback = !this.CallbackCompleted;
				this.ReleaseCount++;
			}
		}
	}

	private sealed class DerivedSpooler(TimeSpan period, int limit, TimeProvider clock = null) : Spooler<int>(period, limit, clock)
	{
		private readonly TaskCompletionSource _flushed = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public List<int> Values { get; } = [];
		public CancellationToken Cancellation { get; private set; }
		public Task Flushed => _flushed.Task;

		protected override ValueTask OnFlushAsync(IEnumerable<int> items, CancellationToken cancellation)
		{
			this.Cancellation = cancellation;
			this.Values.AddRange(items);
			_flushed.TrySetResult();
			return ValueTask.CompletedTask;
		}
	}

	private sealed class CountingContext : SynchronizationContext
	{
		private int _posts;
		public int Posts => Volatile.Read(ref _posts);
		public override void Post(SendOrPostCallback callback, object state)
		{
			Interlocked.Increment(ref _posts);
			ThreadPool.QueueUserWorkItem(_ => callback(state));
		}
	}

	private sealed class PausingFlusher(int enumerations = 1, bool readBeforePause = true)
	{
		private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
		private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
		private int _calls;
		public List<int> Values { get; } = [];
		public Task Entered => _entered.Task;
		public int Calls => _calls;
		public void Resume() => _release.TrySetResult();

		public async ValueTask OnFlushAsync(IEnumerable<int> values, CancellationToken cancellation)
		{
			var first = Interlocked.Increment(ref _calls) == 1;
			if(first && !readBeforePause)
			{
				_entered.TrySetResult();
				await _release.Task;
			}

			for(int index = 1; index < enumerations; index++)
				values.GetEnumerator().Dispose();

			this.Values.AddRange(values);
			if(first && readBeforePause)
			{
				_entered.TrySetResult();
				// Deliberately allow completion after cancellation, as real callbacks may do.
				await _release.Task;
			}
		}
	}

	private class Flusher<T>
	{
		private int _count;
		public int Count => _count;

		private readonly TaskCompletionSource _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public Task WaitAsync(CancellationToken cancellation) => _completion.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellation);

		public ValueTask OnFlushAsync(IEnumerable<T> values, CancellationToken cancellation)
		{
			Interlocked.Add(ref _count, values.Count());
			_completion.TrySetResult();
			return ValueTask.CompletedTask;
		}
	}

	private sealed class RecordingFlusher<T>(TimeSpan delay)
	{
		private int _calls;
		private int _count;
		private int _concurrency;
		private int _maximumConcurrency;
		private readonly ConcurrentBag<T> _values = [];

		public int Calls => _calls;
		public int Count => _count;
		public int MaximumConcurrency => _maximumConcurrency;
		public IEnumerable<T> Values => _values;

		public async ValueTask OnFlushAsync(IEnumerable<T> values, CancellationToken cancellation)
		{
			Interlocked.Increment(ref _calls);
			this.SetMaximum(Interlocked.Increment(ref _concurrency));

			try
			{
				if(delay > TimeSpan.Zero)
					await Task.Delay(delay, cancellation);

				foreach(var value in values)
				{
					_values.Add(value);
					Interlocked.Increment(ref _count);
				}

				if(delay > TimeSpan.Zero)
					await Task.Delay(delay, cancellation);
			}
			finally
			{
				Interlocked.Decrement(ref _concurrency);
			}
		}

		private void SetMaximum(int value)
		{
			int maximum;

			while(value > (maximum = _maximumConcurrency) && Interlocked.CompareExchange(ref _maximumConcurrency, value, maximum) != maximum);
		}
	}
}
