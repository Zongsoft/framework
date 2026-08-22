using System;
using System.Linq;
using System.Threading.Tasks;

using Xunit;

namespace Zongsoft.Externals.Etcd.Tests;

[Collection(EtcdIntegrationCollection.Name)]
public class EtcdServiceTests
{
	[Fact]
	public async Task BasicKv_RoundTripsFindsCountsAndDeletes()
	{
		RequireEtcd();
		using var service = CreateService();

		await service.SetValueAsync("one", "壹");
		await service.SetValueAsync("订单:two", "贰");

		Assert.True(await service.ExistsAsync("one"));
		Assert.Equal("壹", await service.GetValueAsync("one"));
		Assert.Null(await service.GetValueAsync("missing"));
		Assert.Equal(2, await service.CountAsync());
		var values = await service.FindAsync();
		Assert.Equal(2, values.Count);
		Assert.Equal("贰", values["订单:two"]);
		Assert.Single(await service.FindAsync("订单"));
		Assert.Throws<InvalidOperationException>(() => service.Namespace = "changed");
		Assert.True(await service.RemoveAsync("one"));
		Assert.False(await service.RemoveAsync("one"));
		Assert.Equal(1, await service.CountAsync());
	}

	[Fact]
	public async Task Sequence_IntegerDoubleExpiryAndConcurrencyMatchContract()
	{
		RequireEtcd();
		using var service = CreateService();

		Assert.Equal(12, await service.IncreaseAsync("integer", 2, 10));
		Assert.Equal(15, await service.IncreaseAsync("integer", 3, 100));
		Assert.Equal(14, await service.DecreaseAsync("integer"));
		Assert.Equal(14, await service.IncreaseAsync("integer", 0));
		await service.ResetAsync("integer", 7);
		Assert.Equal(7, await service.IncreaseAsync("integer", 0));
		var basic = (Zongsoft.Common.ISequenceBase)service;
		Assert.Equal(9, await basic.IncreaseAsync("integer", 2));

		Assert.Equal(1.75, await service.IncreaseAsync("double", 0.25, 1.5), 10);
		Assert.Equal(1.5, await service.DecreaseAsync("double", 0.25), 10);

		const int count = 32;
		var tasks = Enumerable.Range(0, count).Select(_ => service.IncreaseAsync("concurrent").AsTask()).ToArray();
		var results = await Task.WhenAll(tasks);
		Assert.Equal(Enumerable.Range(1, count).Select(value => (long)value), results.Order());

		Assert.Equal(6, await service.IncreaseAsync("expiring", 1, 5, TimeSpan.FromSeconds(2)));
		await EventuallyAsync(async () => !await service.ExistsAsync("expiring"), TimeSpan.FromSeconds(5));
		Assert.Equal(21, await service.IncreaseAsync("expiring", 1, 20));
	}

	private static EtcdService CreateService() => new("test", Global.ConnectionString) { Namespace = "tests:" + Guid.NewGuid().ToString("N") };
	private static void RequireEtcd()
	{
		Assert.SkipUnless(Global.IsTestingEnabled, "Set ZONGSOFT_ETCD_TESTS=1 to run etcd integration tests.");
		Assert.SkipUnless(Global.IsAvailable(), $"etcd is unavailable at {Global.Server}.");
	}

	internal static async Task EventuallyAsync(Func<Task<bool>> condition, TimeSpan timeout)
	{
		var deadline = DateTime.UtcNow + timeout;
		while(DateTime.UtcNow < deadline)
		{
			if(await condition())
				return;
			await Task.Delay(100);
		}
		Assert.True(await condition(), "The expected etcd state was not observed before timeout.");
	}
}
