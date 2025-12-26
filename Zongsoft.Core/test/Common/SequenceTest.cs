using System;
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

		mocker.Latency = TimeSpan.FromMilliseconds(100);
		var sequence = mocker.Variate();
		Assert.Equal(1, sequence.Increase("Var"));

		var statistics = sequence.GetStatistics("Var");
		Assert.NotNull(statistics);
		Assert.Equal(statistics.Threshold, mocker.GetValue("Var"));

		Parallel.For(1, 1000, _ => sequence.Increase("Var"));
		Assert.Equal(1001, sequence.Increase("Var"));
		Assert.Equal(statistics.Threshold, mocker.GetValue("Var"));

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

		mocker.Latency = TimeSpan.FromMilliseconds(100);
		var sequence = mocker.Variate();
		Assert.Equal(1, await sequence.IncreaseAsync("Var"));

		var statistics = sequence.GetStatistics("Var");
		Assert.NotNull(statistics);
		Assert.Equal(statistics.Threshold, mocker.GetValue("Var"));

		#if NET8_0_OR_GREATER
		await Parallel.ForAsync(1, 1000, async (_, cancellation) => await sequence.IncreaseAsync("Var", cancellation: cancellation));
		Assert.Equal(1001, await sequence.IncreaseAsync("Var"));
		Assert.Equal(statistics.Threshold, mocker.GetValue("Var"));
		#endif

		sequence.Reset("Var");
		Assert.Equal(1, sequence.Increase("Var"));
		Assert.Equal(2, sequence.Increase("Var"));
		Assert.Equal(3, sequence.Increase("Var"));
	}

	#region 嵌套子类
	public sealed class SequenceMocker(TimeSpan? latency = null) : Zongsoft.Common.ISequenceBase
	{
		private readonly Dictionary<string, long> _dictionary = new();
		public TimeSpan Latency { get; set; } = latency ?? TimeSpan.Zero;

		public long GetValue(string key) => _dictionary.TryGetValue(key, out var value) ? value : 0;
		public long Decrease(string key, int interval = 1, int seed = 0) => this.Increase(key, -interval, seed);
		public ValueTask<long> DecreaseAsync(string key, int interval = 1, int seed = 0, CancellationToken cancellation = default) => this.IncreaseAsync(key, -interval, seed, cancellation);

		public long Increase(string key, int interval = 1, int seed = 0)
		{
			if(this.Latency > TimeSpan.Zero)
				Thread.Sleep(this.Latency);

			lock(this)
			{
				if(_dictionary.TryAdd(key, seed))
					return seed;

				return _dictionary[key] += interval;
			}
		}

		public async ValueTask<long> IncreaseAsync(string key, int interval = 1, int seed = 0, CancellationToken cancellation = default)
		{
			if(this.Latency > TimeSpan.Zero)
				await Task.Delay(this.Latency, cancellation);

			lock(this)
			{
				if(_dictionary.TryAdd(key, seed))
					return seed;

				return _dictionary[key] += interval;
			}
		}

		public void Reset(string key, int value = 0)
		{
			if(this.Latency > TimeSpan.Zero)
				Thread.Sleep(this.Latency);

			lock(this)
			{
				_dictionary[key] = value;
			}
		}

		public async ValueTask ResetAsync(string key, int value = 0, CancellationToken cancellation = default)
		{
			if(this.Latency > TimeSpan.Zero)
				await Task.Delay(this.Latency, cancellation);

			lock(this)
			{
				_dictionary[key] = value;
			}
		}
	}
	#endregion
}
