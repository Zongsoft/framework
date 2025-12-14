using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Data.Tests.Models;

namespace Zongsoft.Data.MySql.Tests;

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
		Assert.False(await accessor.ExistsAsync<UserModel>(Condition.Equal(nameof(UserModel.UserId), 100)));
	}

	[Fact]
	public async Task DeleteAsync_Cascading1()
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
		Assert.False(await accessor.ExistsAsync<RoleModel>(Condition.Equal(nameof(RoleModel.RoleId), 100)));
		Assert.False(await accessor.ExistsAsync<MemberModel>(Condition.Equal(nameof(MemberModel.RoleId), 100)));
	}

	[Fact]
	public async Task DeleteAsync_Cascading2()
	{
		const uint TenantId = 1U;
		const uint BranchId = 0x01_00_00_00;

		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		await accessor.InsertAsync(Model.Build<Branch>(model => {
			model.TenantId = TenantId;
			model.BranchId = BranchId;
			model.BranchNo = "B01";
			model.Name = "Branch#1";
			model.CreatedTime = DateTime.UtcNow;
		}), DataInsertOptions.SuppressSequence().IgnoreConstraint());

		await accessor.InsertAsync(Model.Build<Department>(model =>
		{
			model.TenantId = TenantId;
			model.BranchId = BranchId;
			model.DepartmentId = 1;
			model.DepartmentNo = "D01";
			model.Name = "Department#1";
		}), DataInsertOptions.SuppressSequence().IgnoreConstraint());

		await accessor.InsertAsync(Model.Build<Team>(model =>
		{
			model.TenantId = TenantId;
			model.BranchId = BranchId;
			model.TeamId = 1;
			model.TeamNo = "T01";
			model.Name = "Team#1";
		}), DataInsertOptions.SuppressSequence().IgnoreConstraint());

		var count = await accessor.DeleteAsync<Branch>(
			Condition.Equal(nameof(Branch.TenantId), TenantId) &
			Condition.Equal(nameof(Branch.BranchId), BranchId),
			$"{nameof(Branch.Departments)},{nameof(Branch.Teams)}");

		Assert.Equal(3, count);
		Assert.False(await accessor.ExistsAsync<Branch>(Condition.Equal(nameof(Branch.TenantId), TenantId) & Condition.Equal(nameof(Branch.BranchId), BranchId)));
		Assert.False(await accessor.ExistsAsync<Team>(Condition.Equal(nameof(Team.TenantId), TenantId) & Condition.Equal(nameof(Team.BranchId), BranchId)));
		Assert.False(await accessor.ExistsAsync<Department>(Condition.Equal(nameof(Department.TenantId), TenantId) & Condition.Equal(nameof(Department.BranchId), BranchId)));
	}
}
