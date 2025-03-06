using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Collections.Tests;

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
		var processor = new Processor<string>();
		using var stash = new Stash<string>(processor.OnProcess, TimeSpan.FromSeconds(10), 0);
		Assert.True(stash.IsEmpty);
		Assert.False(stash.TryTake(out var value));
		Assert.Equal(0, processor.Count);

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
		Assert.Equal(0, processor.Count);
	}

	[Fact]
	public void TestFlush()
	{
		const int COUNT = 1000;

		var processor = new Processor<string>();
		using var stash = new Stash<string>(processor.OnProcess, TimeSpan.FromSeconds(10), 0);
		Assert.True(stash.IsEmpty);
		Assert.False(stash.TryTake(out var value));
		Assert.Equal(0, processor.Count);

		Parallel.For(0, COUNT, index => stash.Put($"Value#{index}"));
		Assert.Equal(COUNT, stash.Count);

		stash.Flush();
		Assert.True(stash.IsEmpty);
		Assert.Equal(COUNT, processor.Count);
	}

	[Fact]
	public void TestLimit()
	{
		var processor = new Processor<string>();
		using var stash = new Stash<string>(processor.OnProcess, TimeSpan.FromSeconds(10), 0);
		Assert.True(stash.IsEmpty);
		Assert.False(stash.TryTake(out var value));
		Assert.Equal(0, processor.Count);

		stash.Put("A");
		stash.Put("B");
		stash.Put("C");
		Assert.Equal(3, stash.Count);

		//设置数量限制
		stash.Limit = 4;
		//触发数量限制
		stash.Put("D");

		Assert.True(stash.IsEmpty);
		Assert.Equal(4, processor.Count);
	}

	[Fact]
	public void TestPeriod()
	{
		var processor = new Processor<string>();
		using var stash = new Stash<string>(processor.OnProcess, TimeSpan.FromSeconds(10), 0);
		Assert.True(stash.IsEmpty);
		Assert.False(stash.TryTake(out var value));
		Assert.Equal(0, processor.Count);

		stash.Put("A");
		stash.Put("B");
		stash.Put("C");
		Assert.Equal(3, stash.Count);

		//设置触发周期
		stash.Period = TimeSpan.FromMilliseconds(1);
		//等待周期刷新
		SpinWait.SpinUntil(() => processor.Count >= 3, 500);

		Assert.True(stash.IsEmpty);
		Assert.Equal(3, processor.Count);
	}

	private class Processor<T>
	{
		private int _count;
		public int Count => _count;

		public void OnProcess(IEnumerable<T> values)
		{
			foreach(var _ in values)
				Interlocked.Increment(ref _count);
		}
	}
}
