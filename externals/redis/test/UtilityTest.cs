using System;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Externals.Redis.Messaging;

namespace Zongsoft.Externals.Redis.Tests
{
	public class UtilityTest
	{
		[Fact]
		public void TestIncreaseId()
		{
			string id;

			//空值的情况
			id = RedisQueueUtility.IncreaseId(null);
			Assert.Equal("0-1", id);
			id = RedisQueueUtility.IncreaseId(string.Empty);
			Assert.Equal("0-1", id);

			//零值的情况
			id = RedisQueueUtility.IncreaseId("-");
			Assert.Equal("0-1", id);
			id = RedisQueueUtility.IncreaseId("0");
			Assert.Equal("0-1", id);
			id = RedisQueueUtility.IncreaseId("0-0");
			Assert.Equal("0-1", id);

			//无分隔符的情况
			id = RedisQueueUtility.IncreaseId("1");
			Assert.Equal("1-1", id);
			id = RedisQueueUtility.IncreaseId("9");
			Assert.Equal("9-1", id);
			id = RedisQueueUtility.IncreaseId("10");
			Assert.Equal("10-1", id);
			id = RedisQueueUtility.IncreaseId("99");
			Assert.Equal("99-1", id);
			id = RedisQueueUtility.IncreaseId("100");
			Assert.Equal("100-1", id);
			id = RedisQueueUtility.IncreaseId("999");
			Assert.Equal("999-1", id);

			//分隔符打头的情况
			id = RedisQueueUtility.IncreaseId("-0");
			Assert.Equal("0-1", id);
			id = RedisQueueUtility.IncreaseId("-1");
			Assert.Equal("0-2", id);
			id = RedisQueueUtility.IncreaseId("-9");
			Assert.Equal("0-10", id);
			id = RedisQueueUtility.IncreaseId("-10");
			Assert.Equal("0-11", id);
			id = RedisQueueUtility.IncreaseId("-99");
			Assert.Equal("0-100", id);
			id = RedisQueueUtility.IncreaseId("-100");
			Assert.Equal("0-101", id);
			id = RedisQueueUtility.IncreaseId("-999");
			Assert.Equal("0-1000", id);

			//分隔符结尾的情况
			id = RedisQueueUtility.IncreaseId("0-");
			Assert.Equal("0-1", id);
			id = RedisQueueUtility.IncreaseId("1-");
			Assert.Equal("1-1", id);
			id = RedisQueueUtility.IncreaseId("9-");
			Assert.Equal("9-1", id);
			id = RedisQueueUtility.IncreaseId("10-");
			Assert.Equal("10-1", id);
			id = RedisQueueUtility.IncreaseId("99-");
			Assert.Equal("99-1", id);
			id = RedisQueueUtility.IncreaseId("100-");
			Assert.Equal("100-1", id);
			id = RedisQueueUtility.IncreaseId("999-");
			Assert.Equal("999-1", id);

			//分隔符居中的情况
			id = RedisQueueUtility.IncreaseId("0-1");
			Assert.Equal("0-2", id);
			id = RedisQueueUtility.IncreaseId("0-9");
			Assert.Equal("0-10", id);
			id = RedisQueueUtility.IncreaseId("0-10");
			Assert.Equal("0-11", id);
			id = RedisQueueUtility.IncreaseId("0-99");
			Assert.Equal("0-100", id);
			id = RedisQueueUtility.IncreaseId("0-100");
			Assert.Equal("0-101", id);
			id = RedisQueueUtility.IncreaseId("0-999");
			Assert.Equal("0-1000", id);

			id = RedisQueueUtility.IncreaseId("1-0");
			Assert.Equal("1-1", id);
			id = RedisQueueUtility.IncreaseId("1-1");
			Assert.Equal("1-2", id);
			id = RedisQueueUtility.IncreaseId("1-9");
			Assert.Equal("1-10", id);
			id = RedisQueueUtility.IncreaseId("1-10");
			Assert.Equal("1-11", id);
			id = RedisQueueUtility.IncreaseId("1-99");
			Assert.Equal("1-100", id);
			id = RedisQueueUtility.IncreaseId("1-100");
			Assert.Equal("1-101", id);
			id = RedisQueueUtility.IncreaseId("1-999");
			Assert.Equal("1-1000", id);
		}

		[Fact]
		public void TestDecreaseId()
		{
			string id;

			//空值的情况
			id = RedisQueueUtility.DecreaseId(null);
			Assert.Equal("0", id);
			id = RedisQueueUtility.DecreaseId(string.Empty);
			Assert.Equal("0", id);

			//零值的情况
			id = RedisQueueUtility.DecreaseId("-");
			Assert.Equal("0", id);
			id = RedisQueueUtility.DecreaseId("0");
			Assert.Equal("0", id);
			id = RedisQueueUtility.DecreaseId("0-0");
			Assert.Equal("0", id);

			//无分隔符的情况
			id = RedisQueueUtility.DecreaseId("1");
			Assert.Equal("0-9223372036854775807", id);
			id = RedisQueueUtility.DecreaseId("9");
			Assert.Equal("8-9223372036854775807", id);
			id = RedisQueueUtility.DecreaseId("10");
			Assert.Equal("9-9223372036854775807", id);
			id = RedisQueueUtility.DecreaseId("99");
			Assert.Equal("98-9223372036854775807", id);
			id = RedisQueueUtility.DecreaseId("100");
			Assert.Equal("99-9223372036854775807", id);
			id = RedisQueueUtility.DecreaseId("999");
			Assert.Equal("998-9223372036854775807", id);

			//分隔符打头的情况
			id = RedisQueueUtility.DecreaseId("-0");
			Assert.Equal("0", id);
			id = RedisQueueUtility.DecreaseId("-1");
			Assert.Equal("0", id);
			id = RedisQueueUtility.DecreaseId("-9");
			Assert.Equal("0-8", id);
			id = RedisQueueUtility.DecreaseId("-10");
			Assert.Equal("0-9", id);
			id = RedisQueueUtility.DecreaseId("-99");
			Assert.Equal("0-98", id);
			id = RedisQueueUtility.DecreaseId("-100");
			Assert.Equal("0-99", id);
			id = RedisQueueUtility.DecreaseId("-999");
			Assert.Equal("0-998", id);

			//分隔符结尾的情况
			id = RedisQueueUtility.DecreaseId("0-");
			Assert.Equal("0", id);
			id = RedisQueueUtility.DecreaseId("1-");
			Assert.Equal("0-9223372036854775807", id);
			id = RedisQueueUtility.DecreaseId("9-");
			Assert.Equal("8-9223372036854775807", id);
			id = RedisQueueUtility.DecreaseId("10-");
			Assert.Equal("9-9223372036854775807", id);
			id = RedisQueueUtility.DecreaseId("99-");
			Assert.Equal("98-9223372036854775807", id);
			id = RedisQueueUtility.DecreaseId("100-");
			Assert.Equal("99-9223372036854775807", id);
			id = RedisQueueUtility.DecreaseId("999-");
			Assert.Equal("998-9223372036854775807", id);

			//分隔符居中的情况
			id = RedisQueueUtility.DecreaseId("0-1");
			Assert.Equal("0-0", id);
			id = RedisQueueUtility.DecreaseId("0-9");
			Assert.Equal("0-8", id);
			id = RedisQueueUtility.DecreaseId("0-10");
			Assert.Equal("0-9", id);
			id = RedisQueueUtility.DecreaseId("0-99");
			Assert.Equal("0-98", id);
			id = RedisQueueUtility.DecreaseId("0-100");
			Assert.Equal("0-99", id);
			id = RedisQueueUtility.DecreaseId("0-999");
			Assert.Equal("0-998", id);

			id = RedisQueueUtility.DecreaseId("1-0");
			Assert.Equal("0-9223372036854775807", id);
			id = RedisQueueUtility.DecreaseId("1-1");
			Assert.Equal("1-0", id);
			id = RedisQueueUtility.DecreaseId("1-9");
			Assert.Equal("1-8", id);
			id = RedisQueueUtility.DecreaseId("1-10");
			Assert.Equal("1-9", id);
			id = RedisQueueUtility.DecreaseId("1-99");
			Assert.Equal("1-98", id);
			id = RedisQueueUtility.DecreaseId("1-100");
			Assert.Equal("1-99", id);
			id = RedisQueueUtility.DecreaseId("1-999");
			Assert.Equal("1-998", id);
		}
	}
}
