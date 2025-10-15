using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Xunit;
using System.Linq;

namespace Zongsoft.Caching.Tests;

public class SpoolerTest
{
	[Fact]
	public void TestClear()
	{
		using var spooler = new Spooler<string>(_ => { }, TimeSpan.FromSeconds(1), 0);
		Assert.True(spooler.IsEmpty);

		spooler.Put("A");
		spooler.Put("B");
		spooler.Put("C");
		Assert.Equal(3, spooler.Count);

		spooler.Clear();
		Assert.True(spooler.IsEmpty);
	}

	[Fact]
	public void TestFlush()
	{
		const int COUNT = 1000;

		var flusher = new Flusher<string>();
		using var spooler = new Spooler<string>(flusher.OnFlush, TimeSpan.FromSeconds(10), 0);
		Assert.True(spooler.IsEmpty);
		Assert.Equal(0, flusher.Count);

		Parallel.For(0, COUNT, index => spooler.Put($"Value#{index}"));
		Assert.Equal(COUNT, spooler.Count);

		spooler.Flush();
		Assert.True(spooler.IsEmpty);
		Assert.Equal(COUNT, flusher.Count);
	}

	[Fact]
	public void TestLimit()
	{
		var flusher = new Flusher<string>();
		using var spooler = new Spooler<string>(flusher.OnFlush, TimeSpan.FromSeconds(10), 0);
		Assert.True(spooler.IsEmpty);
		Assert.Equal(0, flusher.Count);

		spooler.Put("A");
		spooler.Put("B");
		spooler.Put("C");
		Assert.Equal(3, spooler.Count);
		Assert.Equal(0, flusher.Count);

		//设置数量限制
		spooler.Limit = 4;
		//触发数量限制
		spooler.Put("D");

		Assert.True(spooler.IsEmpty);
		Assert.Equal(4, flusher.Count);
	}

	[Fact]
	public void TestPeriod()
	{
		var flusher = new Flusher<string>();
		using var spooler = new Spooler<string>(flusher.OnFlush, TimeSpan.FromSeconds(10), 0);
		Assert.True(spooler.IsEmpty);
		Assert.Equal(0, flusher.Count);

		spooler.Put("A");
		spooler.Put("B");
		spooler.Put("C");
		Assert.Equal(3, spooler.Count);
		Assert.Equal(0, flusher.Count);

		//设置触发周期
		spooler.Period = TimeSpan.FromMilliseconds(1);
		//等待周期刷新
		SpinWait.SpinUntil(() => flusher.Count > 0, 1000);

		Assert.True(spooler.IsEmpty);
		Assert.Equal(3, flusher.Count);
	}

	private class Flusher<T>
	{
		private int _count;
		public int Count => _count;
		public void OnFlush(IEnumerable<T> values) => Interlocked.Add(ref _count, values.Count());
	}
}
