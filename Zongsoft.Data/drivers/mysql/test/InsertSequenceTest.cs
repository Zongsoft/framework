using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Data.Tests.Models;

namespace Zongsoft.Data.MySql.Tests;

[Collection("Database")]
public class InsertSequenceTest(DatabaseFixture database) : IDisposable
{
	private readonly DatabaseFixture _database = database;

	[Fact]
	public async Task InsertAsync()
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		var model = Model.Build<UserModel>(model =>
		{
			model.Name = $"${Zongsoft.Common.Randomizer.GenerateString()}";
		});

		var count = await accessor.InsertAsync(model);
		Assert.Equal(1, count);
		Assert.True(model.UserId > 0);
		Assert.True(await accessor.ExistsAsync<UserModel>(Condition.Equal(nameof(UserModel.UserId), model.UserId)));
	}

	[Fact]
	public async Task InsertWithChildrenAsync()
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		var model = Model.Build<RoleModel>(model => {
			model.Name = $"${Zongsoft.Common.Randomizer.GenerateString()}";
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

		var count = await accessor.InsertAsync(model, $"*,{nameof(RoleModel.Children)}{{*}}");
		Assert.Equal(3, count);
		Assert.NotNull(model.Children);
		Assert.NotEmpty(model.Children);
		foreach(var child in model.Children)
			Assert.Equal(model.RoleId, child.RoleId);

		var roles = accessor.SelectAsync<RoleModel>(
			Condition.Equal(nameof(RoleModel.RoleId), model.RoleId),
			$"*,{nameof(RoleModel.Children)}{{*}}");

		await using var enumerator = roles.GetAsyncEnumerator(CancellationToken.None);
		Assert.True(await enumerator.MoveNextAsync());

		var role = enumerator.Current;
		Assert.NotNull(role);
		Assert.Equal(model.RoleId, role.RoleId);
		Assert.Equal(model.Name, role.Name);
		Assert.NotNull(role.Children);

		var members = role.Children.OrderBy(member => member.MemberId).ToArray();
		Assert.NotEmpty(members);
		Assert.Equal(2, members.Length);
		Assert.Equal(model.RoleId, members[0].RoleId);
		Assert.Equal(100U, members[0].MemberId);
		Assert.Equal(MemberType.User, members[0].MemberType);
		Assert.Equal(model.RoleId, members[1].RoleId);
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
		var models = Model.Build<UserModel>(COUNT, (model, index) =>
		{
			model.Name = $"${Zongsoft.Common.Randomizer.GenerateString()}_{index}";
		}).ToArray();

		var count = await accessor.InsertManyAsync(models);
		Assert.Equal(COUNT, count);
		foreach(var model in models)
			Assert.True(model.UserId > 0);

		count = accessor.Count<UserModel>(Condition.In(nameof(UserModel.UserId), models.Select(model => model.UserId)));
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
			Assert.True(model.RoleId > 0);
			Assert.NotNull(model.Children);
			Assert.NotEmpty(model.Children);

			foreach(var child in model.Children)
				Assert.Equal(model.RoleId, child.RoleId);
		}

		var roles = accessor.SelectAsync<RoleModel>(
			Condition.In(nameof(RoleModel.RoleId), models.Select(model => model.RoleId)),
			$"*,{nameof(RoleModel.Children)}{{*}}");

		int index = 0;

		await foreach(var role in roles)
		{
			Assert.NotNull(role);
			Assert.Equal(models[index].RoleId, role.RoleId);
			Assert.Equal(models[index].Name, role.Name);
			Assert.NotNull(role.Children);

			var members = role.Children.OrderBy(member => member.MemberId).ToArray();
			Assert.NotEmpty(members);
			Assert.Equal(2, members.Length);
			Assert.Equal(role.RoleId, members[0].RoleId);
			Assert.Equal((uint)(501 + index), members[0].MemberId);
			Assert.Equal(MemberType.User, members[0].MemberType);
			Assert.Equal(role.RoleId, members[1].RoleId);
			Assert.Equal((uint)(601 + index), members[1].MemberId);
			Assert.Equal(MemberType.Role, members[1].MemberType);

			++index;
		}

		await accessor.DeleteAsync<RoleModel>(Condition.In(nameof(RoleModel.RoleId), models.Select(model => model.RoleId)));
		await accessor.DeleteAsync<MemberModel>(Condition.In(nameof(RoleModel.RoleId), models.Select(model => model.RoleId)));
	}

	void IDisposable.Dispose()
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		accessor.Delete<MemberModel>(Condition.Like($"{nameof(MemberModel.Role)}.{nameof(RoleModel.Name)}", "$%"));
		accessor.Delete<RoleModel>(Condition.Like(nameof(RoleModel.Name), "$%"));
		accessor.Delete<UserModel>(Condition.Like(nameof(UserModel.Name), "$%"));
	}
}
