using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Xunit;

namespace Zongsoft.Caching.Tests;

public class SpoolerTest
{
	[Fact]
	public async Task TestClearAsync()
	{
		using var spooler = new Spooler<string>((_, __) => ValueTask.CompletedTask, TimeSpan.FromHours(1));
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
		using var spooler = new Spooler<string>(flusher.OnFlushAsync, TimeSpan.FromHours(1));
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
		using var spooler = new Spooler<string>(flusher.OnFlushAsync, TimeSpan.FromHours(1), 3);
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
		var flusher = new Flusher<string>();
		using var spooler = new Spooler<string>(flusher.OnFlushAsync, TimeSpan.FromHours(1));
		Assert.True(spooler.IsEmpty);
		Assert.Equal(0, flusher.Count);

		await spooler.PutAsync("A", TestContext.Current.CancellationToken);
		await spooler.PutAsync("B", TestContext.Current.CancellationToken);
		await spooler.PutAsync("C", TestContext.Current.CancellationToken);
		Assert.Equal(3, spooler.Count);
		Assert.Equal(0, flusher.Count);

		#if NET8_0_OR_GREATER
		//设置触发周期
		spooler.Period = TimeSpan.FromMilliseconds(1);
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
		using var spooler = new Spooler<int>(flusher.OnFlushAsync, TimeSpan.FromHours(1));

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
		using var spooler = new Spooler<int>(flusher.OnFlushAsync, TimeSpan.FromHours(1), LIMIT);

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
		using var spooler = new Spooler<int>(flusher.OnFlushAsync, TimeSpan.FromDays(1), limit);

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
		using var spooler = new Spooler<int>(flusher.OnFlushAsync, TimeSpan.FromDays(1), 1);
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
		using var spooler = new Spooler<int>(flusher.OnFlushAsync, TimeSpan.FromDays(1), 1);
		await spooler.PutAsync(0, TestContext.Current.CancellationToken);
		var pending = spooler.PutAsync(1, TestContext.Current.CancellationToken).AsTask();
		try
		{
			await flusher.Entered.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
			spooler.Dispose();
		}
		finally { flusher.Resume(); }

		await Assert.ThrowsAsync<ObjectDisposedException>(() => pending.WaitAsync(TimeSpan.FromSeconds(5)));
		Assert.Equal(new[] { 0 }, flusher.Values);
		await Assert.ThrowsAsync<ObjectDisposedException>(() => spooler.PutAsync(2).AsTask());
	}

	[Fact]
	public async Task PutAsync_FlusherThrows_PropagatesAndCanRetry()
	{
		var failure = new InvalidOperationException("Consumer failed.");
		var fails = true;
		var consumed = new List<int>();
		using var spooler = new Spooler<int>((values, _) =>
		{
			if(fails)
				throw failure;
			consumed.AddRange(values);
			return ValueTask.CompletedTask;
		}, TimeSpan.FromDays(1), 1);
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
	}

	[Theory]
	[InlineData(0)]
	[InlineData(3)]
	public async Task PutAsync_FastPath_PreservesSynchronousCompletionAndCancellation(int limit)
	{
		var flusher = new Flusher<int>();
		using var spooler = new Spooler<int>(flusher.OnFlushAsync, TimeSpan.FromDays(1), limit);
		using var cancellation = new CancellationTokenSource();
		await cancellation.CancelAsync();
		var pending = spooler.PutAsync(42, cancellation.Token);
		Assert.True(pending.IsCompletedSuccessfully);
		await pending;
		Assert.Equal(1, spooler.Count);
		await spooler.FlushAsync(TestContext.Current.CancellationToken);
		Assert.Equal(1, flusher.Count);
	}

	[Fact]
	public async Task PutAsync_Disposed_ReturnsFaultedValueTask()
	{
		var spooler = new Spooler<int>((_, _) => ValueTask.CompletedTask, TimeSpan.FromDays(1), 3);
		spooler.Dispose();
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
		using var spooler = new Spooler<int>((_, _) =>
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
		using var spooler = new Spooler<int>(flusher.OnFlushAsync, TimeSpan.FromDays(1), 3);
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
	public async Task FlushAsync_EmptyDuringCallback_DoesNotStartAnotherCallback()
	{
		var flusher = new PausingFlusher();
		using var spooler = new Spooler<int>(flusher.OnFlushAsync, TimeSpan.FromDays(1), 3);
		await spooler.PutAsync(42, TestContext.Current.CancellationToken);
		var owner = spooler.FlushAsync(TestContext.Current.CancellationToken).AsTask();
		try
		{
			await flusher.Entered.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
			var emptyFlush = spooler.FlushAsync(TestContext.Current.CancellationToken);
			Assert.True(emptyFlush.IsCompletedSuccessfully);
			await emptyFlush;
			Assert.False(owner.IsCompleted);
			Assert.Equal(1, flusher.Calls);
		}
		finally { flusher.Resume(); }
		await owner.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
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

		using var instance = spooler = new Spooler<int>((items, cancellation) =>
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
					write.GetAwaiter().GetResult();
				}
			}
			return ValueTask.CompletedTask;
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

		using var spooler = new Spooler<int>(async (items, token) =>
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
		using var spooler = new Spooler<int>((_, _) => ValueTask.CompletedTask, TimeSpan.FromDays(1), 1);
		await spooler.PutAsync(0, cancellation.Token);
		var pending = spooler.PutAsync(1, cancellation.Token).AsTask();
		Assert.False(pending.IsCompleted);
		if(dispose)
		{
			spooler.Dispose();
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
		using var spooler = new Spooler<int>((items, _) =>
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
		var values = new List<int>();

		using var spooler = new Spooler<int>(async (items, _) =>
		{
			if(++calls == 1)
			{
				entered.SetResult();
				await release.Task;
				throw failure;
			}
			values.AddRange(items);
		}, TimeSpan.FromDays(1), 3);

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
	}

	[Fact]
	public async Task PutAsync_WaitingForFlush_UsesFreedSpaceWithoutAnotherCallback()
	{
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var calls = 0;
		var values = new List<int>();
		using var spooler = new Spooler<int>(async (items, _) =>
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
