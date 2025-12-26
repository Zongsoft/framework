using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Zongsoft.Data.Tests;

public sealed class SequenceMocker(TimeSpan? latency = null) : Zongsoft.Common.ISequenceBase
{
	private readonly Dictionary<string, long> _dictionary = new();
	public TimeSpan Latency { get; set; } = latency ?? TimeSpan.Zero;

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
