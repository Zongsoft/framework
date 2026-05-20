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
		using var spooler = new Spooler<string>((_, __) => ValueTask.CompletedTask, TimeSpan.FromSeconds(10));
		Assert.True(spooler.IsEmpty);

		await spooler.PutAsync("A");
		await spooler.PutAsync("B");
		await spooler.PutAsync("C");
		Assert.Equal(3, spooler.Count);

		spooler.Clear();
		Assert.True(spooler.IsEmpty);
	}

	[Fact]
	public async Task TestFlushAsync()
	{
		const int COUNT = 1000;

		var flusher = new Flusher<string>();
		using var spooler = new Spooler<string>(flusher.OnFlushAsync, TimeSpan.FromSeconds(10));
		Assert.True(spooler.IsEmpty);
		Assert.Equal(0, flusher.Count);

		await spooler.FlushAsync();
		Assert.True(spooler.IsEmpty);
		Assert.Equal(0, flusher.Count);

		#if NET8_0_OR_GREATER
		await Parallel.ForAsync(0, COUNT, async (index, cancellation) => await spooler.PutAsync($"Value#{index}", cancellation));
		#else
		for(int i = 0; i < COUNT; i++)
			await spooler.PutAsync($"Value#${i}");
		#endif

		Assert.Equal(COUNT, spooler.Count);

		await spooler.FlushAsync();
		Assert.True(spooler.IsEmpty);
		Assert.Equal(COUNT, flusher.Count);
	}

	[Fact]
	public async Task TestLimitAsync()
	{
		var flusher = new Flusher<string>();
		using var spooler = new Spooler<string>(flusher.OnFlushAsync, TimeSpan.FromSeconds(10), 3);
		Assert.True(spooler.IsEmpty);
		Assert.Equal(0, flusher.Count);

		await spooler.PutAsync("A");
		await spooler.PutAsync("B");
		await spooler.PutAsync("C");
		Assert.Equal(3, spooler.Count);
		Assert.Equal(0, flusher.Count);

		//触发数量限制
		await spooler.PutAsync("D");

		Assert.False(spooler.IsEmpty);
		Assert.Equal(1, spooler.Count);
		Assert.Equal(3, flusher.Count);
	}

	[Fact]
	public async Task TestPeriodAsync()
	{
		var flusher = new Flusher<string>();
		using var spooler = new Spooler<string>(flusher.OnFlushAsync, TimeSpan.FromSeconds(10));
		Assert.True(spooler.IsEmpty);
		Assert.Equal(0, flusher.Count);

		await spooler.PutAsync("A");
		await spooler.PutAsync("B");
		await spooler.PutAsync("C");
		Assert.Equal(3, spooler.Count);
		Assert.Equal(0, flusher.Count);

		#if NET8_0_OR_GREATER
		//设置触发周期
		spooler.Period = TimeSpan.FromMilliseconds(1);
		//等待周期刷新
		SpinWait.SpinUntil(() => flusher.Count > 0, 1000);

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
		using var spooler = new Spooler<int>(flusher.OnFlushAsync, TimeSpan.FromSeconds(10));

		for(int i = 0; i < COUNT; i++)
			await spooler.PutAsync(i);

		var tasks = Enumerable.Range(0, CONCURRENCY).Select(_ => spooler.FlushAsync().AsTask()).ToArray();
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
		using var spooler = new Spooler<int>(flusher.OnFlushAsync, TimeSpan.FromSeconds(10), LIMIT);

		await Parallel.ForAsync(0, COUNT, spooler.PutAsync);
		await spooler.FlushAsync();

		Assert.True(spooler.IsEmpty);
		Assert.Equal(1, flusher.MaximumConcurrency);
		Assert.Equal(COUNT, flusher.Count);
		Assert.Equal(COUNT, flusher.Values.Distinct().Count());
		Assert.Equal(Enumerable.Range(0, COUNT), flusher.Values.OrderBy(value => value));
	}

	private class Flusher<T>
	{
		private int _count;
		public int Count => _count;
		public ValueTask OnFlushAsync(IEnumerable<T> values, CancellationToken cancellation)
		{
			Interlocked.Add(ref _count, values.Count());
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
