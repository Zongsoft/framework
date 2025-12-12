using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Data.Tests.Models;

namespace Zongsoft.Data.PostgreSql.Tests;

[Collection("Database")]
public class UpsertSequenceTest(DatabaseFixture database) : IDisposable
{
	private readonly DatabaseFixture _database = database;

	[Fact]
	public async Task UpsertAsync()
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		var model = Model.Build<UserModel>(model =>
		{
			model.Name = $"${Zongsoft.Common.Randomizer.GenerateString()}";
		});

		var count = await accessor.UpsertAsync(model);
		Assert.Equal(1, count);
		Assert.True(model.UserId > 0);
		Assert.True(await accessor.ExistsAsync<UserModel>(Condition.Equal(nameof(UserModel.UserId), model.UserId)));

		var text = $"${Zongsoft.Common.Randomizer.GenerateString()}";
		count = await accessor.UpsertAsync<UserModel>(new {
			UserId = model.UserId,
			Name = text
		}, DataUpsertOptions.SuppressSequence());
		Assert.Equal(1, count);

		var result = accessor.SelectAsync<string>(
			Model.Naming.Get<UserModel>(),
			Condition.Equal(nameof(UserModel.UserId), model.UserId),
			nameof(UserModel.Name));

		var enumerator = result.GetAsyncEnumerator();
		Assert.True(await enumerator.MoveNextAsync());
		var name = enumerator.Current;
		await enumerator.DisposeAsync();
		Assert.Equal(text, name);
	}

	[Fact]
	public async Task UpsertWithChildrenAsync()
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		var model = Model.Build<RoleModel>(model =>
		{
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

		var count = await accessor.UpsertAsync(model, $"*,{nameof(RoleModel.Children)}{{*}}");
		Assert.Equal(3, count);
		Assert.True(model.RoleId > 0);
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

		var id = model.RoleId;
		var text = $"${Zongsoft.Common.Randomizer.GenerateString()}";

		model = Model.Build<RoleModel>(model =>
		{
			model.RoleId = id;
			model.Name = $"${Guid.NewGuid()}";
			model.Children =
			[
				Model.Build<MemberModel>(member => {
					member.MemberId = 100;
					member.MemberType = MemberType.User;
				}),
				Model.Build<MemberModel>(member => {
					member.MemberId = 444;
					member.MemberType = MemberType.Role;
				}),
			];
		});

		count = await accessor.UpsertAsync(model, $"*,{nameof(RoleModel.Children)}{{*}}");
		Assert.Equal(3, count);
		Assert.NotNull(model.Children);
		Assert.NotEmpty(model.Children);
		foreach(var child in model.Children)
			Assert.Equal(model.RoleId, child.RoleId);

		roles = accessor.SelectAsync<RoleModel>(
			Condition.Equal(nameof(RoleModel.RoleId), model.RoleId),
			$"*,{nameof(RoleModel.Children)}{{*}}");

		await using var enumerator1 = roles.GetAsyncEnumerator(CancellationToken.None);
		Assert.True(await enumerator1.MoveNextAsync());

		role = enumerator1.Current;
		Assert.NotNull(role);
		Assert.Equal(model.RoleId, role.RoleId);
		Assert.Equal(model.Name, role.Name);
		Assert.NotNull(role.Children);

		members = role.Children.OrderBy(member => member.MemberId).ToArray();
		Assert.NotEmpty(members);
		Assert.Equal(3, members.Length);
		Assert.Equal(model.RoleId, members[0].RoleId);
		Assert.Equal(100U, members[0].MemberId);
		Assert.Equal(MemberType.User, members[0].MemberType);
		Assert.Equal(model.RoleId, members[1].RoleId);
		Assert.Equal(404U, members[1].MemberId);
		Assert.Equal(MemberType.Role, members[1].MemberType);
		Assert.Equal(model.RoleId, members[2].RoleId);
		Assert.Equal(444U, members[2].MemberId);
		Assert.Equal(MemberType.Role, members[2].MemberType);
	}

	[Fact]
	public async Task UpsertManyAsync()
	{
		const int COUNT = 100;

		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		var models = Model.Build<UserModel>(COUNT, (model, index) => {
			model.Name = $"${Zongsoft.Common.Randomizer.GenerateString()}#{index}";
		}).ToArray();

		var count = await accessor.UpsertManyAsync(models);
		Assert.Equal(COUNT, count);
		for(int i = 0; i < COUNT; i++)
			Assert.True(models[i].UserId > 0);

		count = await accessor.CountAsync<UserModel>(Condition.In(nameof(UserModel.UserId), models.Select(model => model.UserId)));
		Assert.Equal(COUNT, count);

		count = await accessor.UpsertManyAsync(Model.Build<UserModel>(COUNT, (model, index) =>
		{
			model.UserId = models[index].UserId;
			model.Name = $"$User@{models[index].UserId}";
		}), DataUpsertOptions.SuppressSequence());
		Assert.Equal(COUNT, count);

		count = await accessor.CountAsync<UserModel>(Condition.In(nameof(UserModel.UserId), models.Select(model => model.UserId)));
		Assert.Equal(COUNT, count);

		var users = accessor.SelectAsync<UserModel>(Condition.In(nameof(UserModel.UserId), models.Select(model => model.UserId)));
		await foreach(var user in users)
		{
			Assert.NotNull(user);
			Assert.True(user.UserId > 0);
			Assert.Equal($"$User@{user.UserId}", user.Name);
		}
	}

	[Fact]
	public async Task UpsertManyWithChildrenAsync()
	{
		const int COUNT = 10;

		if(!Global.IsTestingEnabled)
			return;

		int index = 0;
		var accessor = _database.Accessor;
		var models = Model.Build<RoleModel>(COUNT, (model, index) =>
		{
			model.Name = $"${Zongsoft.Common.Randomizer.GenerateString()}#{index}";
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

		var count = await accessor.UpsertManyAsync(models, $"*,{nameof(RoleModel.Children)}{{*}}");
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

		var result = accessor.SelectAsync<RoleModel>(
			Condition.In(nameof(RoleModel.RoleId), models.Select(model => model.RoleId)),
			$"*,{nameof(RoleModel.Children)}{{*}}");

		index = 0;
		await foreach(var role in result)
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

		var roles = Model.Build<RoleModel>(COUNT, (model, index) =>
		{
			model.RoleId = models[index].RoleId;
			model.Name = $"$Role#{models[index].RoleId}";
			model.Children =
			[
				Model.Build<MemberModel>(member => {
					member.MemberId = (uint)(501 + index);
					member.MemberType = MemberType.User;
				}),
				Model.Build<MemberModel>(member => {
					member.MemberId = (uint)(701 + index);
					member.MemberType = MemberType.Role;
				}),
			];
		}).ToArray();

		count = await accessor.UpsertManyAsync(roles, $"*,{nameof(RoleModel.Children)}{{*}}");
		Assert.Equal(3 * COUNT, count);

		for(int i = 0; i < roles.Length; i++)
		{
			var role = roles[i];
			Assert.NotNull(role.Children);
			Assert.NotEmpty(role.Children);

			foreach(var child in role.Children)
				Assert.Equal(role.RoleId, child.RoleId);
		}

		result = accessor.SelectAsync<RoleModel>(
			Condition.In(nameof(RoleModel.RoleId), roles.Select(role => role.RoleId)),
			$"*,{nameof(RoleModel.Children)}{{*}}");

		index = 0;
		await foreach(var role in result)
		{
			Assert.NotNull(role);
			Assert.True(role.RoleId > 0);
			Assert.Equal($"$Role#{role.RoleId}", role.Name);
			Assert.NotNull(role.Children);

			var members = role.Children.OrderBy(member => member.MemberId).ToArray();
			Assert.NotEmpty(members);
			Assert.Equal(3, members.Length);
			Assert.Equal(role.RoleId, members[0].RoleId);
			Assert.Equal((uint)(501 + index), members[0].MemberId);
			Assert.Equal(MemberType.User, members[0].MemberType);
			Assert.Equal(role.RoleId, members[1].RoleId);
			Assert.Equal((uint)(601 + index), members[1].MemberId);
			Assert.Equal(MemberType.Role, members[1].MemberType);
			Assert.Equal(role.RoleId, members[2].RoleId);
			Assert.Equal((uint)(701 + index), members[2].MemberId);
			Assert.Equal(MemberType.Role, members[2].MemberType);

			++index;
		}
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
