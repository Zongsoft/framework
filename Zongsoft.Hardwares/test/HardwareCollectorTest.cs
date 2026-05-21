using System.Linq;
using System.Threading.Tasks;

using Xunit;

namespace Zongsoft.Hardwares.Tests;

public class HardwareCollectorTest
{
	[Fact]
	public void TestCollect()
	{
		var hardwares = HardwareCollector.Instance.Collect();

		Assert.NotNull(hardwares);
		Assert.DoesNotContain(hardwares, hardware => hardware == null);
	}

	[Fact]
	public async Task TestCollectAsync()
	{
		var count = 0;

		await foreach(var hardware in HardwareCollector.Instance.CollectAsync())
		{
			Assert.NotNull(hardware);
			count++;
		}

		Assert.True(count >= 0);
	}
}
