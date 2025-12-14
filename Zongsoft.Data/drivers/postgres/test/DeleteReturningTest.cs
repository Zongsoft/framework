using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Data.Tests.Models;

namespace Zongsoft.Data.PostgreSql.Tests;

[Collection("Database")]
public class DeleteReturningTest(DatabaseFixture database)
{
	private readonly DatabaseFixture _database = database;

	[Fact]
	public async Task DeleteAsync()
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		await accessor.InsertAsync(Model.Build<UserModel>(model => {
			model.UserId = 100;
			model.Name = "Popeye";
		}), DataInsertOptions.SuppressSequence().IgnoreConstraint());

		var options = DataDeleteOptions.Return(nameof(UserModel.UserId), nameof(UserModel.Name)).Build();
		var count = await accessor.DeleteAsync<UserModel>(Condition.Equal(nameof(UserModel.UserId), 100), options);

		Assert.Equal(1, count);
		Assert.Single(options.Returning.Rows);
		Assert.True(options.Returning.Rows[0].TryGetValue(nameof(UserModel.UserId), out var value));
		Assert.Equal(100, Convert.ChangeType(value, typeof(int)));
		Assert.True(options.Returning.Rows[0].TryGetValue(nameof(UserModel.Name), out value));
		Assert.Equal("Popeye", value);
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

		var options = DataDeleteOptions.Return(nameof(RoleModel.RoleId), nameof(RoleModel.Name)).Build();
		var count = await accessor.DeleteAsync<RoleModel>(
			Condition.Equal(nameof(RoleModel.RoleId), 100),
			nameof(RoleModel.Children),
			options);

		Assert.Equal(2, count);
		Assert.Single(options.Returning.Rows);
		Assert.True(options.Returning.Rows[0].TryGetValue(nameof(RoleModel.RoleId), out var value));
		Assert.Equal(100, Convert.ChangeType(value, typeof(int)));
		Assert.True(options.Returning.Rows[0].TryGetValue(nameof(RoleModel.Name), out value));
		Assert.Equal("Guests", value);
	}
}
