using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Data.MySql.Tests.Models;

namespace Zongsoft.Data.MySql.Tests;

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
	}
}
