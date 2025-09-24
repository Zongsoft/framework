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

		Parallel.For(0, 1000, _ =>
		{
			mocker.Increase("Integer", 1, 1);
			mocker.Increase("Double", 1d, 1d);
		});

		Assert.Equal(1001, mocker.Increase("Integer", 1));
		Assert.Equal(1001d, mocker.Increase("Double", 1d));

		var sequence = Sequence.Variate(mocker);
		Assert.Equal(1, sequence.Increase("A"));

		Parallel.For(1, 100, i =>
		{
			sequence.Increase("A");
		});

	}

	[Fact]
	public async Task TestIncreaseAsync()
	{
		var mocker = new SequenceMocker();

		Parallel.For(0, 1000, async _ =>
		{
			await mocker.IncreaseAsync("Integer", 1, 1);
			await mocker.IncreaseAsync("Double", 1d, 1d);
		});

		Assert.Equal(1001, await mocker.IncreaseAsync("Integer", 1));
		Assert.Equal(1001d, await mocker.IncreaseAsync("Double", 1d));
	}

	#region 嵌套子类
	public sealed class SequenceMocker : Zongsoft.Common.ISequence
	{
		private readonly Dictionary<string, long> _integerSequences = new();
		private readonly Dictionary<string, double> _doubleSequences = new();

		public long Decrease(string key, int interval = 1, int seed = 0, TimeSpan? expiry = null) => this.Increase(key, -interval, seed, expiry);
		public double Decrease(string key, double interval, double seed = 0, TimeSpan? expiry = null) => this.Increase(key, -interval, seed, expiry);
		public ValueTask<long> DecreaseAsync(string key, int interval = 1, int seed = 0, TimeSpan? expiry = null, CancellationToken cancellation = default) => this.IncreaseAsync(key, -interval, seed, expiry, cancellation);
		public ValueTask<double> DecreaseAsync(string key, double interval, double seed = 0, TimeSpan? expiry = null, CancellationToken cancellation = default) => this.IncreaseAsync(key, -interval, seed, expiry, cancellation);

		public long Increase(string key, int interval = 1, int seed = 0, TimeSpan? expiry = null)
		{
			lock(this)
			{
				if(_integerSequences.TryAdd(key, seed))
					return seed;

				return _integerSequences[key] += interval;
			}
		}

		public double Increase(string key, double interval, double seed = 0, TimeSpan? expiry = null)
		{
			lock(this)
			{
				if(_doubleSequences.TryAdd(key, seed))
					return seed;

				return _doubleSequences[key] += interval;
			}
		}

		public ValueTask<long> IncreaseAsync(string key, int interval = 1, int seed = 0, TimeSpan? expiry = null, CancellationToken cancellation = default) => ValueTask.FromResult(this.Increase(key, interval, seed, expiry));
		public ValueTask<double> IncreaseAsync(string key, double interval, double seed = 0, TimeSpan? expiry = null, CancellationToken cancellation = default) => ValueTask.FromResult(this.Increase(key, interval, seed, expiry));

		public void Reset(string key, int value = 0, TimeSpan? expiry = null)
		{
			lock(this)
			{
				_integerSequences[key] = value;
			}
		}

		public void Reset(string key, double value, TimeSpan? expiry = null)
		{
			lock(this)
			{
				_doubleSequences[key] = value;
			}
		}

		public ValueTask ResetAsync(string key, int value = 0, TimeSpan? expiry = null, CancellationToken cancellation = default)
		{
			lock(this)
			{
				_integerSequences[key] = value;
				return ValueTask.CompletedTask;
			}
		}

		public ValueTask ResetAsync(string key, double value, TimeSpan? expiry = null, CancellationToken cancellation = default)
		{
			lock(this)
			{
				_doubleSequences[key] = value;
				return ValueTask.CompletedTask;
			}
		}
	}
	#endregion
}
