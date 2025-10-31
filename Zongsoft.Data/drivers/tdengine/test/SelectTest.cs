using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace Zongsoft.Data.TDengine.Tests;

[Collection("Database")]
public class SelectTest(DatabaseFixture database)
{
	private readonly DatabaseFixture _database = database;

	[Fact]
	public void TestSelect()
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;

		Assert.NotNull(accessor);
		Assert.NotNull(Mapping.Entities);
		Assert.NotEmpty(Mapping.Entities);
		Assert.True(Mapping.Entities.Contains("GatewayHistory"));

		accessor.Delete("GatewayHistory", null);
		Assert.Equal(0, accessor.Count("GatewayHistory"));

		var count = accessor.Insert("GatewayHistory", new Models.GatewayHistory(100, 10001, 123.56, null, DateTime.Now));
		Assert.Equal(1, count);

		var models = accessor.Select<Models.GatewayHistory>();
		Assert.NotNull(models);
		Assert.NotEmpty(models);

		var model = models.FirstOrDefault();
		Assert.True(model.GatewayId > 0);
		Assert.True(model.Timestamp > DateTime.MinValue);
	}

	[Fact]
	public async Task TestSelectAsync()
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;

		Assert.NotNull(accessor);
		Assert.NotNull(Mapping.Entities);
		Assert.NotEmpty(Mapping.Entities);
		Assert.True(Mapping.Entities.Contains("GatewayHistory"));

		await accessor.DeleteAsync("GatewayHistory", null);
		Assert.Equal(0, accessor.Count("GatewayHistory"));

		var count = await accessor.InsertAsync("GatewayHistory", new Models.GatewayHistory(100, 10001, 123.56, null, DateTime.Now));
		Assert.Equal(1, count);

		var models = accessor.SelectAsync<Models.GatewayHistory>();
		Assert.NotNull(models);
		Assert.NotEmpty(models);

		await using var enumerator = models.GetAsyncEnumerator();
		if(await enumerator.MoveNextAsync())
		{
			var model = enumerator.Current;
			Assert.True(model.GatewayId > 0);
			Assert.True(model.Timestamp > DateTime.MinValue);
		}
	}
}
