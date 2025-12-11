using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Zongsoft.Data.Tests;

public sealed class Sequence : Zongsoft.Common.ISequenceBase
{
	private readonly Dictionary<string, long> _dictionary = new();

	public TimeSpan Latency { get; set; }

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

	public ValueTask<long> IncreaseAsync(string key, int interval = 1, int seed = 0, CancellationToken cancellation = default) => ValueTask.FromResult(this.Increase(key, interval, seed));

	public void Reset(string key, int value = 0)
	{
		if(this.Latency > TimeSpan.Zero)
			Thread.Sleep(this.Latency);

		lock(this)
		{
			_dictionary[key] = value;
		}
	}

	public ValueTask ResetAsync(string key, int value = 0, CancellationToken cancellation = default)
	{
		this.Reset(key, value);
		return ValueTask.CompletedTask;
	}
}
