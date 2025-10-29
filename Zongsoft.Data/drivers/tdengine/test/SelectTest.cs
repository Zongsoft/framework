using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace Zongsoft.Data.TDengine.Tests;

public class SelectTest
{
	[Fact]
	public void TestSelect()
	{
		if(!System.Diagnostics.Debugger.IsAttached)
			return;

		using var accessor = DataAccessProvider.Instance.GetAccessor("Test", new DataAccessOptions([Global.ConnectionSettings]));

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
		Assert.Empty(models);

		var model = models.FirstOrDefault();
		Assert.True(model.GatewayId > 0);
		Assert.True(model.Timestamp > DateTime.MinValue);
	}

	[Fact]
	public async Task TestSelectAsync()
	{
		if(!System.Diagnostics.Debugger.IsAttached)
			return;

		using var accessor = DataAccessProvider.Instance.GetAccessor("Test", new DataAccessOptions([Global.ConnectionSettings]));

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
