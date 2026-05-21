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

	[Fact]
	public void TestProfileNetworks()
	{
		var network = new Zongsoft.IO.Hardwares.Hardware("Network Adapter", "network0", "network", "network/adapter");
		var storage = new Zongsoft.IO.Hardwares.Hardware("Disk", "disk0", "disk", "storage/disk");
		var device = new Zongsoft.IO.Hardwares.Hardware("Device", "device0", "device", "device");
		var profile = new Zongsoft.IO.Hardwares.HardwareProfile([network, storage, device]);

		Assert.Same(network, Assert.Single(profile.Networks));
		Assert.Same(storage, Assert.Single(profile.Storages));
		Assert.Same(device, Assert.Single(profile.Devices));
	}
}
