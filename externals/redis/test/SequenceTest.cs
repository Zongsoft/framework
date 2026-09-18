using System;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace Zongsoft.Externals.Redis.Tests;

public class SequenceTest
{
	private RedisService _redis;

	public SequenceTest()
	{
		_redis = new RedisService("test", "server=127.0.0.1;password=;");
	}

	//[Fact]
	public void TestIncrementByOne()
	{
		var key = GetKey();

		Assert.False(_redis.Exists(key));

		Assert.Equal(1, _redis.Increase(key));
		Assert.Equal(2, _redis.Increase(key));
		Assert.Equal(3, _redis.Increase(key));
		Assert.Equal(4, _redis.Increase(key));
		Assert.Equal(5, _redis.Increase(key));

		Assert.Equal(4, _redis.Decrease(key));
		Assert.Equal(3, _redis.Decrease(key));
		Assert.Equal(2, _redis.Decrease(key));
		Assert.Equal(1, _redis.Decrease(key));
		Assert.Equal(0, _redis.Decrease(key));

		((Zongsoft.Common.ISequence)_redis).Reset(key);
		Assert.Equal(0, (int)_redis.GetValue(key));

		Assert.True(_redis.Remove(key));
		Assert.False(_redis.Exists(key));
	}

	//[Fact]
	public void TestIncrementByInterval()
	{
		var key = GetKey();

		const int ROUND = 100;
		const int INTERVAL = 10;

		Assert.False(_redis.Exists(key));

		for(int i = 1; i <= ROUND; i++)
		{
			Assert.Equal(i * INTERVAL, _redis.Increase(key, INTERVAL));
		}

		for(int i = 1; i <= ROUND; i++)
		{
			Assert.Equal((ROUND - i) * INTERVAL, _redis.Decrease(key, INTERVAL));
		}

		Assert.True(_redis.Remove(key));
		Assert.False(_redis.Exists(key));
	}

	//[Fact]
	public void TestIncrementWithSeed()
	{
		var key = GetKey();

		const int ROUND = 100;
		const int INTERVAL = 10;
		const int SEED = 10000;

		Assert.False(_redis.Exists(key));

		for(int i = 1; i <= ROUND; i++)
		{
			Assert.Equal((i * INTERVAL) + SEED, _redis.Increase(key, INTERVAL, SEED));
		}

		for(int i = 1; i <= ROUND; i++)
		{
			Assert.Equal(((ROUND - i) * INTERVAL) + SEED, _redis.Decrease(key, INTERVAL, SEED));
		}

		((Zongsoft.Common.ISequence)_redis).Reset(key, SEED);
		Assert.Equal(SEED, (int)_redis.GetValue(key));

		Assert.True(_redis.Remove(key));
		Assert.False(_redis.Exists(key));
	}

	#region 私有方法
	private static string GetKey()
	{
		return "Test:SequenceId." + Zongsoft.Common.Randomizer.GenerateString() + "-" + Environment.TickCount64.ToString();
	}
	#endregion
}
