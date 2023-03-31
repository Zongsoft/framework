using System;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace Zongsoft.Externals.Redis.Tests
{
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
			var KEY = GetKey();

			Assert.False(_redis.Exists(KEY));

			Assert.Equal(1, _redis.Increase(KEY));
			Assert.Equal(2, _redis.Increase(KEY));
			Assert.Equal(3, _redis.Increase(KEY));
			Assert.Equal(4, _redis.Increase(KEY));
			Assert.Equal(5, _redis.Increase(KEY));

			Assert.Equal(4, _redis.Decrease(KEY));
			Assert.Equal(3, _redis.Decrease(KEY));
			Assert.Equal(2, _redis.Decrease(KEY));
			Assert.Equal(1, _redis.Decrease(KEY));
			Assert.Equal(0, _redis.Decrease(KEY));

			((Zongsoft.Common.ISequence)_redis).Reset(KEY);
			Assert.Equal(0, (int)_redis.GetValue(KEY));

			Assert.True(_redis.Remove(KEY));
			Assert.False(_redis.Exists(KEY));
		}

		//[Fact]
		public void TestIncrementByInterval()
		{
			var KEY = GetKey();

			const int ROUND = 100;
			const int INTERVAL = 10;

			Assert.False(_redis.Exists(KEY));

			for(int i = 1; i <= ROUND; i++)
			{
				Assert.Equal(i * INTERVAL, _redis.Increase(KEY, INTERVAL));
			}

			for(int i = 1; i <= ROUND; i++)
			{
				Assert.Equal((ROUND - i) * INTERVAL, _redis.Decrease(KEY, INTERVAL));
			}

			Assert.True(_redis.Remove(KEY));
			Assert.False(_redis.Exists(KEY));
		}

		//[Fact]
		public void TestIncrementWithSeed()
		{
			var KEY = GetKey();

			const int ROUND = 100;
			const int INTERVAL = 10;
			const int SEED = 10000;

			Assert.False(_redis.Exists(KEY));

			for(int i = 1; i <= ROUND; i++)
			{
				Assert.Equal((i * INTERVAL) + SEED, _redis.Increase(KEY, INTERVAL, SEED));
			}

			for(int i = 1; i <= ROUND; i++)
			{
				Assert.Equal(((ROUND - i) * INTERVAL) + SEED, _redis.Decrease(KEY, INTERVAL, SEED));
			}

			((Zongsoft.Common.ISequence)_redis).Reset(KEY, SEED);
			Assert.Equal(SEED, (int)_redis.GetValue(KEY));

			Assert.True(_redis.Remove(KEY));
			Assert.False(_redis.Exists(KEY));
		}

		#region 私有方法
		private static string GetKey()
		{
			return "Test:SequenceId." + Zongsoft.Common.Randomizer.GenerateString() + "-" + Environment.TickCount64.ToString();
		}
		#endregion
	}
}
