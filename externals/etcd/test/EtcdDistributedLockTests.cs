using System;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Services.Distributing;

namespace Zongsoft.Externals.Etcd.Tests;

[Collection(EtcdIntegrationCollection.Name)]
public class EtcdDistributedLockTests
{
	[Fact]
	public async Task CompetitionOwnershipAndFencingTokensMatchContract()
	{
		RequireEtcd();
		using var service = CreateService();
		var manager = (IDistributedLockManager)service;

		await using var owner = await service.AcquireAsync("resource", TimeSpan.FromSeconds(3));
		await using var contender = await service.AcquireAsync("resource", TimeSpan.FromSeconds(3));
		Assert.True(owner.IsLocked);
		Assert.True(owner.FencingToken > 0);
		Assert.True(contender.IsUnheld);
		Assert.Equal(0, contender.FencingToken);
		Assert.False(await service.ReleaseAsync("resource", [1, 2, 3]));
		Assert.True((await manager.GetExpiryAsync("resource")) > TimeSpan.Zero);

		var firstFence = owner.FencingToken;
		await owner.DisposeAsync();
		await contender.EnterAsync();
		Assert.True(contender.IsLocked);
		Assert.True(contender.FencingToken > firstFence);
	}

	[Fact]
	public async Task ManualAndAutomaticRenewalKeepOwnershipAlive()
	{
		RequireEtcd();
		using var service = CreateService();

		await using(var manual = await service.AcquireAsync("manual", TimeSpan.FromSeconds(2)))
		{
			await Task.Delay(1200);
			Assert.True(await manual.RenewAsync());
			await Task.Delay(1200);
			await using var contender = await service.AcquireAsync("manual", TimeSpan.FromSeconds(2));
			Assert.True(contender.IsUnheld);
		}

		var options = new DistributedLockOptions(TimeSpan.FromSeconds(2)) { RenewalInterval = TimeSpan.FromMilliseconds(500) };
		await using(var automatic = await service.AcquireAsync("automatic", options))
		{
			await Task.Delay(TimeSpan.FromSeconds(4.5));
			await using var contender = await service.AcquireAsync("automatic", TimeSpan.FromSeconds(2));
			Assert.True(automatic.IsLocked);
			Assert.True(contender.IsUnheld);
		}

		await using var successor = await service.AcquireAsync("automatic", TimeSpan.FromSeconds(2));
		Assert.True(successor.IsHeld);
	}

	[Fact]
	public async Task EnterAsync_ObservesCancellationWhileContended()
	{
		RequireEtcd();
		using var service = CreateService();
		await using var owner = await service.AcquireAsync("cancel", TimeSpan.FromSeconds(3));
		await using var contender = await service.AcquireAsync("cancel", TimeSpan.FromSeconds(3));
		using var source = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => contender.EnterAsync(source.Token).AsTask());
		Assert.True(contender.IsUnheld);
		Assert.True(owner.IsLocked);
	}

	private static EtcdService CreateService() => new("test", Global.ConnectionString) { Namespace = "locks:" + Guid.NewGuid().ToString("N") };
	private static void RequireEtcd()
	{
		Assert.SkipUnless(Global.IsTestingEnabled, "Set ZONGSOFT_ETCD_TESTS=1 to run etcd integration tests.");
		Assert.SkipUnless(Global.IsAvailable(), $"etcd is unavailable at {Global.Server}.");
	}
}
