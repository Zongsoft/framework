using System;
using System.Linq;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Data.Tests.Models;

namespace Zongsoft.Data.PostgreSql.Tests;

[Collection("Database")]
public class IncreaseTest(DatabaseFixture database)
{
	private readonly DatabaseFixture _database = database;

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task IncreaseReturnsUpdatedValue(bool asynchronous)
	{
		if(!Global.IsTestingEnabled)
			return;

		const uint TENANT_ID = 900100;
		var accessor = _database.Accessor;
		var criteria = Condition.Equal(nameof(Tenant.TenantId), TENANT_ID);
		await accessor.DeleteAsync<Tenant>(criteria);

		try
		{
			var tenant = Model.Build<Tenant>(model =>
			{
				model.TenantId = TENANT_ID;
				model.TenantNo = $"Increase-{Guid.NewGuid():N}";
				model.Name = "Increase test";
				model.RegisteredCapital = 10;
				model.CreatorId = 1;
			});

			Assert.Equal(1, await accessor.InsertAsync(tenant,
				"TenantId,TenantNo,Name,RegisteredCapital,CreatorId",
				DataInsertOptions.Sequence(DataSequenceBehavior.Never)));

			Assert.Equal(12L, asynchronous ?
				await accessor.IncreaseAsync<Tenant>(nameof(Tenant.RegisteredCapital), criteria, 2) :
				accessor.Increase<Tenant>(nameof(Tenant.RegisteredCapital), criteria, 2));

			Assert.Equal(9L, asynchronous ?
				await accessor.IncreaseAsync("Tenant", nameof(Tenant.RegisteredCapital), criteria, -3) :
				accessor.Increase("Tenant", nameof(Tenant.RegisteredCapital), criteria, -3));

			Assert.Equal((ushort)9, accessor.Select<ushort>("Tenant", criteria, nameof(Tenant.RegisteredCapital)).Single());
			Assert.Equal(0L, asynchronous ?
				await accessor.IncreaseAsync<Tenant>(nameof(Tenant.RegisteredCapital), Condition.Equal(nameof(Tenant.TenantId), TENANT_ID + 1)) :
				accessor.Increase<Tenant>(nameof(Tenant.RegisteredCapital), Condition.Equal(nameof(Tenant.TenantId), TENANT_ID + 1)));
		}
		finally
		{
			await accessor.DeleteAsync<Tenant>(criteria);
		}
	}
}
