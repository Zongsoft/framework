using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Data.Tests.Models;

namespace Zongsoft.Data.MySql.Tests;

[Collection("Database")]
public class InsertTest(DatabaseFixture database) : IDisposable
{
	private readonly DatabaseFixture _database = database;

	[Fact]
	public async Task InsertAsync()
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;

		await accessor.DeleteAsync<UserModel>(Condition.Equal(nameof(UserModel.UserId), 100));

		var count = await accessor.InsertAsync(Model.Build<UserModel>(model => {
			model.UserId = 100;
			model.Name = "Popeye";
		}), DataInsertOptions.SuppressSequence());
		Assert.Equal(1, count);

		count = await accessor.InsertAsync(Model.Build<UserModel>(model => {
			model.UserId = 100;
			model.Name = "Popeye Zhong";
		}), DataInsertOptions.SuppressSequence().IgnoreConstraint());
		Assert.Equal(0, count);
	}

	[Fact]
	public async Task InsertWithChildrenAsync()
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		var model = Model.Build<RoleModel>(model => {
			model.RoleId = 10;
			model.Name = "Managers";
			model.Children =
			[
				Model.Build<MemberModel>(member => {
					member.MemberId = 100;
					member.MemberType = MemberType.User;
				}),
				Model.Build<MemberModel>(member => {
					member.MemberId = 404;
					member.MemberType = MemberType.Role;
				}),
			];
		});

		var count = await accessor.InsertAsync(model, $"*,{nameof(RoleModel.Children)}{{*}}", DataInsertOptions.SuppressSequence());
		Assert.Equal(3, count);
		Assert.NotNull(model.Children);
		Assert.NotEmpty(model.Children);
		foreach(var child in model.Children)
			Assert.Equal(10U, child.RoleId);

		var roles = accessor.SelectAsync<RoleModel>(
			Condition.Equal(nameof(RoleModel.RoleId), 10),
			$"*,{nameof(RoleModel.Children)}{{*}}");

		await using var enumerator = roles.GetAsyncEnumerator(CancellationToken.None);
		Assert.True(await enumerator.MoveNextAsync());

		var role = enumerator.Current;
		Assert.NotNull(role);
		Assert.Equal(10U, role.RoleId);
		Assert.Equal("Managers", role.Name);
		Assert.NotNull(role.Children);

		var members = role.Children.OrderBy(member => member.MemberId).ToArray();
		Assert.NotEmpty(members);
		Assert.Equal(2, members.Length);
		Assert.Equal(10U, members[0].RoleId);
		Assert.Equal(100U, members[0].MemberId);
		Assert.Equal(MemberType.User, members[0].MemberType);
		Assert.Equal(10U, members[1].RoleId);
		Assert.Equal(404U, members[1].MemberId);
		Assert.Equal(MemberType.Role, members[1].MemberType);
	}

	[Fact]
	public async Task InsertManyAsync()
	{
		const int COUNT = 100;

		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;

		var count = await accessor.InsertManyAsync(Model.Build<UserModel>(COUNT, (model, index) => {
			model.UserId = (uint)(200 + index);
			model.Name = $"${Zongsoft.Common.Randomizer.GenerateString()}_{index}";
		}), DataInsertOptions.SuppressSequence());
		Assert.Equal(COUNT, count);
	}

	[Fact]
	public async Task InsertManyWithChildrenAsync()
	{
		const int COUNT = 10;
		const int OFFSET = 1000;

		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		var models = Model.Build<RoleModel>(COUNT, (model, index) => {
			model.RoleId = (uint)(OFFSET + index);
			model.Name = $"$Role#{(OFFSET + index)}";
			model.Children =
			[
				Model.Build<MemberModel>(member => {
					member.MemberId = (uint)(501 + index);
					member.MemberType = MemberType.User;
				}),
				Model.Build<MemberModel>(member => {
					member.MemberId = (uint)(601 + index);
					member.MemberType = MemberType.Role;
				}),
			];
		}).ToArray();

		var count = await accessor.InsertManyAsync(models, $"*,{nameof(RoleModel.Children)}{{*}}", DataInsertOptions.SuppressSequence());
		Assert.Equal(3 * COUNT, count);

		for(int i = 0; i < models.Length; i++)
		{
			var model = models[i];
			Assert.NotNull(model.Children);
			Assert.NotEmpty(model.Children);

			foreach(var child in model.Children)
				Assert.Equal((uint)(OFFSET + i), child.RoleId);
		}

		var roles = accessor.SelectAsync<RoleModel>(
			Condition.Between(nameof(RoleModel.RoleId), OFFSET, OFFSET + COUNT),
			$"*,{nameof(RoleModel.Children)}{{*}}");

		int index = 0;

		await foreach(var role in roles)
		{
			var id = (uint)(OFFSET + index);

			Assert.NotNull(role);
			Assert.Equal(id, role.RoleId);
			Assert.Equal($"$Role#{id}", role.Name);
			Assert.NotNull(role.Children);

			var members = role.Children.OrderBy(member => member.MemberId).ToArray();
			Assert.NotEmpty(members);
			Assert.Equal(2, members.Length);
			Assert.Equal(id, members[0].RoleId);
			Assert.Equal((uint)(501 + index), members[0].MemberId);
			Assert.Equal(MemberType.User, members[0].MemberType);
			Assert.Equal(id, members[1].RoleId);
			Assert.Equal((uint)(601 + index), members[1].MemberId);
			Assert.Equal(MemberType.Role, members[1].MemberType);

			++index;
		}

		await accessor.DeleteAsync<RoleModel>(Condition.Between(nameof(RoleModel.RoleId), OFFSET, OFFSET + COUNT));
		await accessor.DeleteAsync<MemberModel>(Condition.Between(nameof(MemberModel.RoleId), OFFSET, OFFSET + COUNT));
	}

	public void Dispose()
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		accessor.Delete<UserModel>(Condition.Equal(nameof(UserModel.UserId), 100));
		accessor.Delete<UserModel>(Condition.Like(nameof(UserModel.Name), "$%"));
		accessor.Delete<RoleModel>(Condition.Equal(nameof(RoleModel.RoleId), 10));
		accessor.Delete<MemberModel>(Condition.Equal(nameof(MemberModel.RoleId), 10));
	}
}
