using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Data.Tests.Models;

namespace Zongsoft.Data.PostgreSql.Tests;

[Collection("Database")]
public class SelectTest(DatabaseFixture database)
{
	private readonly DatabaseFixture _database = database;

	[Fact]
	public async Task SelectAsync()
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		var users = accessor.SelectAsync<UserModel>();
		Assert.NotEmpty(users);

		var enumerator = users.GetAsyncEnumerator();
		Assert.True(await enumerator.MoveNextAsync());
		var user = enumerator.Current;
		await enumerator.DisposeAsync();

		Assert.NotNull(user);
		Assert.Equal("Administrator", user.Name);
		Assert.Null(user.Roles);
	}

	[Fact]
	public async Task SelectWithPageableAsync()
	{
		const int COUNT = 100;

		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		var models = Model.Build<RoleModel>(COUNT, (model, index) =>
		{
			model.RoleId = (uint)(100 + index);
			model.Name = $"$Role_{model.RoleId}";
		});

		var count = await accessor.InsertManyAsync(models, DataInsertOptions.SuppressSequence());
		Assert.Equal(COUNT, count);

		var page = Paging.Page(1, 20);
		var roles = accessor.SelectAsync<RoleModel>(
			Condition.GreaterThanEqual(nameof(RoleModel.RoleId), 100),
			page);

		await foreach(var role in roles)
		{
			Assert.NotNull(role);
		}

		Assert.Equal(COUNT, page.Total);
		Assert.Equal(1, page.Index);
		Assert.Equal(5, page.Count);

		await accessor.DeleteAsync<RoleModel>(Condition.GreaterThanEqual(nameof(RoleModel.RoleId), 100));
	}

	[Fact]
	public async Task SelectAsync_WithOneToOne()
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		var model = Model.Build<RoleModel>(model =>
		{
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

		var members = accessor.SelectAsync<MemberModel>(
			Condition.Equal($"{nameof(MemberModel.Role)}.{nameof(RoleModel.Name)}", model.Name),
			$"*, {nameof(MemberModel.Role)}{{*}}",
			[
				Sorting.Ascending(nameof(MemberModel.RoleId)),
				Sorting.Ascending(nameof(MemberModel.MemberId))
			]).ToBlockingEnumerable().ToArray();

		Assert.Equal(2, members.Length);

		Assert.Equal(model.RoleId, members[0].RoleId);
		Assert.Equal(100U, members[0].MemberId);
		Assert.Equal(MemberType.User, members[0].MemberType);
		Assert.NotNull(members[0].Role);
		Assert.Equal(model.RoleId, members[0].Role.RoleId);
		Assert.Equal(model.Name, members[0].Role.Name);

		Assert.Equal(model.RoleId, members[1].RoleId);
		Assert.Equal(404U, members[1].MemberId);
		Assert.Equal(MemberType.Role, members[1].MemberType);
		Assert.NotNull(members[1].Role);
		Assert.Equal(model.RoleId, members[1].Role.RoleId);
		Assert.Equal(model.Name, members[1].Role.Name);

		count = await accessor.DeleteAsync<RoleModel>(Condition.Equal(nameof(RoleModel.RoleId), 10), nameof(RoleModel.Children));
		Assert.Equal(3, count);
	}

	[Fact]
	public async Task SelectAsync_WithOneToMany1()
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		await accessor.InsertAsync(Model.Build<UserModel>(model => {
			model.UserId = 100;
			model.Name = "Popeye";
			model.Email = new Email("Popeye", "zongsoft.com");
		}), DataInsertOptions.SuppressSequence().IgnoreConstraint());

		await accessor.InsertAsync(Model.Build<MemberModel>(model =>
		{
			model.RoleId = 1;
			model.MemberId = 100;
			model.MemberType = MemberType.User;
		}), DataInsertOptions.IgnoreConstraint());

		var users = accessor.SelectAsync<UserModel>(
			Condition.Equal(nameof(UserModel.UserId), 100),
			$$"""*, {{nameof(UserModel.Parents)}}{*}""");
		Assert.NotNull(users);

		var enumerator = users.GetAsyncEnumerator();
		Assert.True(await enumerator.MoveNextAsync());
		var user = enumerator.Current;

		Assert.NotNull(user);
		Assert.Equal("Popeye", user.Name);
		Assert.Equal("popeye@zongsoft.com", user.Email);
		Assert.NotNull(user.Parents);

		var parent = user.Parents.FirstOrDefault();
		Assert.NotNull(parent);
		Assert.Equal(1U, parent.RoleId);
		Assert.Equal(100U, parent.MemberId);
		Assert.Equal(MemberType.User, parent.MemberType);
		await enumerator.DisposeAsync();

		await accessor.DeleteAsync<UserModel>(Condition.Equal(nameof(UserModel.UserId), 100));
		await accessor.DeleteAsync<MemberModel>(Condition.Equal(nameof(MemberModel.RoleId), 1));
	}

	[Fact]
	public async Task SelectAsync_WithOneToMany2()
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		await accessor.InsertAsync(Model.Build<UserModel>(model => {
			model.UserId = 100;
			model.Name = "Popeye";
			model.Email = new Email("Popeye", "zongsoft.com");
		}), DataInsertOptions.SuppressSequence().IgnoreConstraint());

		await accessor.InsertAsync(Model.Build<MemberModel>(model =>
		{
			model.RoleId = 1;
			model.MemberId = 100;
			model.MemberType = MemberType.User;
		}), DataInsertOptions.IgnoreConstraint());

		var users = accessor.SelectAsync<UserModel>(
			Condition.Equal(nameof(UserModel.UserId), 100),
			$$"""*, {{nameof(UserModel.Parents)}}{*, {{nameof(MemberModel.Role)}}{*} }""");
		Assert.NotNull(users);

		var enumerator = users.GetAsyncEnumerator();
		Assert.True(await enumerator.MoveNextAsync());
		var user = enumerator.Current;

		Assert.NotNull(user);
		Assert.Equal("Popeye", user.Name);
		Assert.Equal("popeye@zongsoft.com", user.Email);
		Assert.NotNull(user.Parents);

		var parent = user.Parents.FirstOrDefault();
		Assert.NotNull(parent);
		Assert.Equal(1U, parent.RoleId);
		Assert.Equal(100U, parent.MemberId);
		Assert.Equal(MemberType.User, parent.MemberType);
		Assert.NotNull(parent.Role);
		Assert.Equal("Administrators", parent.Role.Name);
		await enumerator.DisposeAsync();

		await accessor.DeleteAsync<UserModel>(Condition.Equal(nameof(UserModel.UserId), 100));
		await accessor.DeleteAsync<MemberModel>(Condition.Equal(nameof(MemberModel.RoleId), 1));
	}

	[Fact]
	public async Task SelectAsync_WithOneToMany3()
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		await accessor.InsertAsync(Model.Build<UserModel>(model => {
			model.UserId = 100;
			model.Name = "Popeye";
			model.Email = new Email("Popeye", "zongsoft.com");
		}), DataInsertOptions.SuppressSequence().IgnoreConstraint());

		await accessor.InsertAsync(Model.Build<MemberModel>(model =>
		{
			model.RoleId = 1;
			model.MemberId = 100;
			model.MemberType = MemberType.User;
		}), DataInsertOptions.IgnoreConstraint());

		var users = accessor.SelectAsync<UserModel>(
			Condition.Equal(nameof(UserModel.UserId), 100),
			$$"""*, {{nameof(UserModel.Roles)}}{*}""");
		Assert.NotNull(users);

		var enumerator = users.GetAsyncEnumerator();
		Assert.True(await enumerator.MoveNextAsync());
		var user = enumerator.Current;

		Assert.NotNull(user);
		Assert.Equal("Popeye", user.Name);
		Assert.Equal("popeye@zongsoft.com", user.Email);
		Assert.NotNull(user.Roles);

		var role = user.Roles.FirstOrDefault();
		Assert.NotNull(role);
		Assert.Equal("Administrators", role.Name);
		await enumerator.DisposeAsync();

		await accessor.DeleteAsync<UserModel>(Condition.Equal(nameof(UserModel.UserId), 100));
		await accessor.DeleteAsync<MemberModel>(Condition.Equal(nameof(MemberModel.RoleId), 1));
	}

	[Fact]
	public async Task SelectAsync_WithOneToMany4()
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		var user = Model.Build<UserModel>(model =>
		{
			model.UserId = 100;
			model.Name = "Popeye";
			model.Email = new("popeye", "zongsoft.com");
		});

		var role = Model.Build<RoleModel>(model =>
		{
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

		var count = await accessor.InsertAsync(user, DataInsertOptions.SuppressSequence());
		Assert.Equal(1, count);

		count = await accessor.InsertAsync(role, $"*,{nameof(RoleModel.Children)}{{*}}", DataInsertOptions.SuppressSequence());
		Assert.Equal(3, count);

		var result = accessor.SelectAsync<RoleModel>(
			Condition.Equal(nameof(RoleModel.RoleId), role.RoleId),
			$"*,{nameof(RoleModel.Children)}{{*," +
				$"{nameof(MemberModel.MemberRole)}{{*}}," +
				$"{nameof(MemberModel.MemberUser)}{{*}}," +
			$"}}").ToBlockingEnumerable().FirstOrDefault();

		Assert.NotNull(result);
		Assert.Equal(role.RoleId, result.RoleId);
		Assert.Equal(role.Name, result.Name);
		Assert.NotNull(result.Children);

		var members = result.Children.OrderBy(child => child.MemberId).ToArray();
		Assert.NotEmpty(members);
		Assert.Equal(2, members.Length);

		Assert.NotNull(members[0]);
		Assert.Equal(role.RoleId, members[0].RoleId);
		Assert.Equal(user.UserId, members[0].MemberId);
		Assert.Equal(MemberType.User, members[0].MemberType);
		Assert.Null(members[0].MemberRole);
		Assert.NotNull(members[0].Member);
		Assert.NotNull(members[0].MemberUser);
		Assert.Same(members[0].Member, members[0].MemberUser);
		Assert.Equal(user.UserId, members[0].MemberUser.UserId);
		Assert.Equal(user.Name, members[0].MemberUser.Name);
		Assert.Equal(user.Email, members[0].MemberUser.Email);

		Assert.NotNull(members[1]);
		Assert.Equal(role.RoleId, members[1].RoleId);
		Assert.Equal(404U, members[1].MemberId);
		Assert.Equal(MemberType.Role, members[1].MemberType);
		Assert.Null(members[1].Member);
		Assert.Null(members[1].MemberUser);
		Assert.Null(members[1].MemberRole);

		await accessor.DeleteAsync<UserModel>(Condition.Equal(nameof(UserModel.UserId), user.UserId));
		await accessor.DeleteAsync<RoleModel>(Condition.Equal(nameof(RoleModel.RoleId), role.RoleId), nameof(RoleModel.Children));
	}
}
