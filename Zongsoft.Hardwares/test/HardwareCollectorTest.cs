using System;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace Zongsoft.Hardwares.Tests;

public class HardwareCollectorTest
{
	[Fact]
	public async Task TestCollect()
	{
		await foreach(var hardware in HardwareCollector.Instance.CollectAsync(TestContext.Current.CancellationToken))
			Assert.NotNull(hardware);
	}

	[Fact]
	public async Task CollectAsync_PreCanceled_PropagatesCancellation()
	{
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();

		var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
		{
			await foreach(var hardware in HardwareCollector.Instance.CollectAsync(cancellation.Token))
				Assert.Fail("A canceled collection must not yield hardware.");
		});

		Assert.Equal(cancellation.Token, exception.CancellationToken);
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
