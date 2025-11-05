using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Data.PostgreSql.Tests.Models;

namespace Zongsoft.Data.PostgreSql.Tests;

[Collection("Database")]
public class DeleteTest(DatabaseFixture database)
{
	private readonly DatabaseFixture _database = database;

	[Fact]
	public async Task DeleteAsync()
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		var count = await accessor.DeleteAsync<UserModel>(Condition.Equal(nameof(UserModel.UserId), 404));
		Assert.Equal(0, count);

		await accessor.InsertAsync(Model.Build<UserModel>(model => {
			model.UserId = 100;
			model.Name = "Popeye";
		}), DataInsertOptions.SuppressSequence().IgnoreConstraint());

		count = await accessor.DeleteAsync<UserModel>(Condition.Equal(nameof(UserModel.UserId), 100));
		Assert.Equal(1, count);
	}

	[Fact]
	public async Task DeleteAsync_Cascading()
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;

		await accessor.InsertAsync(Model.Build<RoleModel>(model => {
			model.RoleId = 100;
			model.Name = "Guests";
		}), DataInsertOptions.SuppressSequence().IgnoreConstraint());

		await accessor.InsertAsync(Model.Build<MemberModel>(model =>
		{
			model.RoleId = 100;
			model.MemberId = 2;
			model.MemberType = MemberType.User;
		}), DataInsertOptions.IgnoreConstraint());

		var count = await accessor.DeleteAsync<RoleModel>(Condition.Equal(nameof(RoleModel.RoleId), 100), nameof(RoleModel.Children));
		Assert.Equal(2, count);
	}
}
