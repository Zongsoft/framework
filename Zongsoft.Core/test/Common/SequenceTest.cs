﻿using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Common.Tests;

public class SequenceTest
{
	[Fact]
	public void TestIncrease()
	{
		var mocker = new SequenceMocker();

		Parallel.For(0, 1000, _ => mocker.Increase("Integer", 1, 1));
		Assert.Equal(1001, mocker.Increase("Integer", 1));

		var sequence = Sequence.Variate(mocker);
		Assert.Equal(1, sequence.Increase("Var"));

		Parallel.For(1, 10000, _ => sequence.Increase("Var"));
		Assert.Equal(10001, sequence.Increase("Var"));

		sequence.Reset("Var");
		Assert.Equal(1, sequence.Increase("Var"));
		Assert.Equal(2, sequence.Increase("Var"));
		Assert.Equal(3, sequence.Increase("Var"));
	}

	[Fact]
	public async Task TestIncreaseAsync()
	{
		var mocker = new SequenceMocker();

		Parallel.For(0, 1000, async _ => await mocker.IncreaseAsync("Integer", 1, 1));
		Assert.Equal(1001, await mocker.IncreaseAsync("Integer", 1));

		var sequence = Sequence.Variate(mocker);
		Assert.Equal(1, await sequence.IncreaseAsync("Var"));

		#if NET8_0_OR_GREATER
		await Parallel.ForAsync(1, 10000, async (_, cancellation) => await sequence.IncreaseAsync("Var", cancellation: cancellation));
		Assert.Equal(10001, await sequence.IncreaseAsync("Var"));
		#endif

		sequence.Reset("Var");
		Assert.Equal(1, sequence.Increase("Var"));
		Assert.Equal(2, sequence.Increase("Var"));
		Assert.Equal(3, sequence.Increase("Var"));
	}

	#region 嵌套子类
	public sealed class SequenceMocker : Zongsoft.Common.ISequenceBase
	{
		private readonly Dictionary<string, long> _dictionary = new();

		public long Decrease(string key, int interval = 1, int seed = 0) => this.Increase(key, -interval, seed);
		public ValueTask<long> DecreaseAsync(string key, int interval = 1, int seed = 0, CancellationToken cancellation = default) => this.IncreaseAsync(key, -interval, seed, cancellation);

		public long Increase(string key, int interval = 1, int seed = 0)
		{
			lock(this)
			{
				if(_dictionary.TryAdd(key, seed))
					return seed;

				return _dictionary[key] += interval;
			}
		}

		public ValueTask<long> IncreaseAsync(string key, int interval = 1, int seed = 0, CancellationToken cancellation = default) => ValueTask.FromResult(this.Increase(key, interval, seed));

		public void Reset(string key, int value = 0)
		{
			lock(this)
			{
				_dictionary[key] = value;
			}
		}

		public ValueTask ResetAsync(string key, int value = 0, CancellationToken cancellation = default)
		{
			lock(this)
			{
				_dictionary[key] = value;
				return ValueTask.CompletedTask;
			}
		}
	}
	#endregion
}
