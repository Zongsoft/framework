using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Caching.Tests;

public class StashTest
{
	[Fact]
	public void TestDiscard()
	{
		using var stash = new Stash<string>(_ => { }, TimeSpan.FromSeconds(1), 0);
		Assert.True(stash.IsEmpty);

		stash.Put("A");
		stash.Put("B");
		stash.Put("C");
		Assert.Equal(3, stash.Count);

		stash.Discard();
		Assert.True(stash.IsEmpty);
	}

	[Fact]
	public void TestTake()
	{
		using var stash = new Stash<string>(_ => { }, TimeSpan.FromSeconds(10), 0);
		Assert.True(stash.IsEmpty);
		Assert.Equal(0, stash.Count);
		Assert.False(stash.TryTake(out var value));

		stash.Put("A");
		stash.Put("B");
		stash.Put("C");
		Assert.Equal(3, stash.Count);

		Assert.True(stash.TryTake(out value));
		Assert.Equal("A", value);
		Assert.Equal(2, stash.Count);

		Assert.True(stash.TryTake(out value));
		Assert.Equal("B", value);
		Assert.Equal(1, stash.Count);

		Assert.True(stash.TryTake(out value));
		Assert.Equal("C", value);
		Assert.Equal(0, stash.Count);
		Assert.False(stash.TryTake(out _));

		Assert.True(stash.IsEmpty);
		Assert.Equal(0, stash.Count);

		stash.Put("A");
		stash.Put("B");
		stash.Put("C");
		Assert.Equal(3, stash.Count);

		Assert.True(stash.TryTake(-1, out value));
		Assert.Equal("C", value);
		Assert.Equal(2, stash.Count);

		Assert.True(stash.TryTake(-2, out value));
		Assert.Equal("A", value);
		Assert.Equal(1, stash.Count);

		Assert.True(stash.TryTake(-1, out value));
		Assert.Equal("B", value);
		Assert.Equal(0, stash.Count);
		Assert.False(stash.TryTake(out _));

		Assert.True(stash.IsEmpty);
		Assert.Equal(0, stash.Count);
	}

	[Fact]
	public void TestFlush()
	{
		const int COUNT = 1000;

		var flusher = new Flusher<string>();
		using var stash = new Stash<string>(flusher.OnFlush, TimeSpan.FromSeconds(10), 0);
		Assert.True(stash.IsEmpty);
		Assert.False(stash.TryTake(out var value));
		Assert.Equal(0, flusher.Count);

		Parallel.For(0, COUNT, index => stash.Put($"Value#{index}"));
		Assert.Equal(COUNT, stash.Count);

		stash.Flush();
		Assert.True(stash.IsEmpty);
		Assert.Equal(COUNT, flusher.Count);
	}

	[Fact]
	public void TestLimit()
	{
		var flusher = new Flusher<string>();
		using var stash = new Stash<string>(flusher.OnFlush, TimeSpan.FromSeconds(10), 0);
		Assert.True(stash.IsEmpty);
		Assert.False(stash.TryTake(out var value));
		Assert.Equal(0, flusher.Count);

		stash.Put("A");
		stash.Put("B");
		stash.Put("C");
		Assert.Equal(3, stash.Count);

		//设置数量限制
		stash.Limit = 4;
		//触发数量限制
		stash.Put("D");

		Assert.True(stash.IsEmpty);
		Assert.Equal(4, flusher.Count);
	}

	[Fact]
	public void TestPeriod()
	{
		var flusher = new Flusher<string>();
		using var stash = new Stash<string>(flusher.OnFlush, TimeSpan.FromSeconds(10), 0);
		Assert.True(stash.IsEmpty);
		Assert.False(stash.TryTake(out var value));
		Assert.Equal(0, flusher.Count);

		stash.Put("A");
		stash.Put("B");
		stash.Put("C");
		Assert.Equal(3, stash.Count);

		//设置触发周期
		stash.Period = TimeSpan.FromMilliseconds(1);
		//等待周期刷新
		SpinWait.SpinUntil(() => flusher.Count <= 0, 500);

		Assert.True(stash.IsEmpty);
		Assert.Equal(3, flusher.Count);
	}

	private class Flusher<T>
	{
		private int _count;
		public int Count => _count;
		public void OnFlush(IReadOnlyList<T> values) => Interlocked.Add(ref _count, values.Count);
	}
}
