using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

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
}
