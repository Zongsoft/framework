using System;
using System.Linq;
using System.Threading.Tasks;

using Xunit;

namespace Zongsoft.Data.TDengine.Tests;

[Collection("Database")]
[Trait("Category", "Integration")]
public class SelectTest(DatabaseFixture database)
{
	private const string TESTING_DISABLED_REASON = "TDengine integration tests require a debugger or ZONGSOFT_DATA_TESTS=1.";
	private readonly DatabaseFixture _database = database;

	[Fact]
	public void TestSelect()
	{
		Assert.SkipUnless(Global.IsTestingEnabled, TESTING_DISABLED_REASON);

		var accessor = _database.Accessor;
		var timestamp = new DateTime(2096, 1, 1, 0, 0, 0, DateTimeKind.Local);
		var criteria = Condition.Equal(nameof(Models.GatewayHistory.Timestamp), timestamp);

		Assert.NotNull(accessor);
		Assert.NotNull(Mapping.Entities);
		Assert.NotEmpty(Mapping.Entities);
		Assert.True(Mapping.Entities.Contains("GatewayHistory"));

		try
		{
			accessor.Delete<Models.GatewayHistory>(criteria);

			var count = accessor.Insert("GatewayHistory", new Models.GatewayHistory(100, 10001, 123.56, null, timestamp));
			Assert.Equal(1, count);

			var models = accessor.Select<Models.GatewayHistory>(criteria).ToArray();
			var model = Assert.Single(models);
			Assert.Equal((uint)100, model.GatewayId);
			Assert.Equal((ulong)10001, model.MetricId);
			Assert.Equal(timestamp, model.Timestamp);
		}
		finally
		{
			accessor.Delete<Models.GatewayHistory>(criteria);
		}
	}

	[Fact]
	public async Task TestSelectAsync()
	{
		Assert.SkipUnless(Global.IsTestingEnabled, TESTING_DISABLED_REASON);

		var accessor = _database.Accessor;
		var timestamp = new DateTime(2096, 1, 2, 0, 0, 0, DateTimeKind.Local);
		var criteria = Condition.Equal(nameof(Models.GatewayHistory.Timestamp), timestamp);

		Assert.NotNull(accessor);
		Assert.NotNull(Mapping.Entities);
		Assert.NotEmpty(Mapping.Entities);
		Assert.True(Mapping.Entities.Contains("GatewayHistory"));

		try
		{
			await accessor.DeleteAsync<Models.GatewayHistory>(criteria);

			var count = await accessor.InsertAsync("GatewayHistory", new Models.GatewayHistory(101, 10002, 123.56, null, timestamp));
			Assert.Equal(1, count);

			var models = accessor.SelectAsync<Models.GatewayHistory>(criteria);
			Assert.NotNull(models);

			await using var enumerator = models.GetAsyncEnumerator();
			Assert.True(await enumerator.MoveNextAsync());

			var model = enumerator.Current;
			Assert.Equal((uint)101, model.GatewayId);
			Assert.Equal((ulong)10002, model.MetricId);
			Assert.Equal(timestamp, model.Timestamp);
			Assert.False(await enumerator.MoveNextAsync());
		}
		finally
		{
			await accessor.DeleteAsync<Models.GatewayHistory>(criteria);
		}
	}
}
