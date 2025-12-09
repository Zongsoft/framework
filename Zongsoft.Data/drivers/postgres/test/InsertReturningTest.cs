using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Data.Tests.Models;

namespace Zongsoft.Data.PostgreSql.Tests;

[Collection("Database")]
public class InsertReturningTest(DatabaseFixture database) : IDisposable
{
	private readonly DatabaseFixture _database = database;

	[Fact]
	public async Task InsertAsync()
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		await accessor.DeleteAsync<UserModel>(Condition.Equal(nameof(UserModel.UserId), 100));

		var options = DataInsertOptions.Return(
			nameof(UserModel.UserId),
			nameof(UserModel.Name),
			nameof(UserModel.Enabled)
		).SuppressSequence().Build();

		Assert.True(options.HasReturning(out var returning));
		Assert.Equal(3, returning.Columns.Count);
		Assert.Equal(nameof(UserModel.UserId), returning.Columns[0].Name);
		Assert.Equal(ReturningKind.Newer, returning.Columns[0].Kind);
		Assert.Equal(nameof(UserModel.Name), returning.Columns[1].Name);
		Assert.Equal(ReturningKind.Newer, returning.Columns[1].Kind);
		Assert.Equal(nameof(UserModel.Enabled), returning.Columns[2].Name);
		Assert.Equal(ReturningKind.Newer, returning.Columns[2].Kind);

		var count = await accessor.InsertAsync(Model.Build<UserModel>(model => {
			model.UserId = 100;
			model.Name = "Popeye";
		}), options);
		Assert.Equal(1, count);
		Assert.Single(returning.Rows);
		Assert.True(returning.Rows[0].TryGetValue(nameof(UserModel.UserId), out var value));
		Assert.Equal(100, Convert.ChangeType(value, typeof(int)));
		Assert.True(returning.Rows[0].TryGetValue(nameof(UserModel.Name), out value));
		Assert.Equal("Popeye", value);
		Assert.True(returning.Rows[0].TryGetValue(nameof(UserModel.Enabled), out value));
		Assert.Equal(true, value);

		options = DataInsertOptions.Return(
			nameof(UserModel.UserId),
			nameof(UserModel.Name),
			nameof(UserModel.Enabled)
		).SuppressSequence().IgnoreConstraint().Build();

		count = await accessor.InsertAsync(Model.Build<UserModel>(model => {
			model.UserId = 100;
			model.Name = "Popeye Zhong";
		}), options);
		Assert.Equal(0, count);
		Assert.Empty(options.Returning.Rows);
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

		var options = DataInsertOptions.Return(
			nameof(RoleModel.RoleId),
			nameof(RoleModel.Name),
			nameof(RoleModel.Enabled)
		).SuppressSequence().Build();

		var count = await accessor.InsertAsync(model, $"*,{nameof(RoleModel.Children)}{{*}}", options);
		Assert.Equal(3, count);
		Assert.NotNull(model.Children);
		Assert.NotEmpty(model.Children);
		foreach(var child in model.Children)
			Assert.Equal(10U, child.RoleId);

		Assert.Single(options.Returning.Rows);
		Assert.True(options.Returning.Rows[0].TryGetValue(nameof(RoleModel.RoleId), out var value));
		Assert.Equal(model.RoleId, Convert.ChangeType(value, typeof(uint)));
		Assert.True(options.Returning.Rows[0].TryGetValue(nameof(RoleModel.Name), out value));
		Assert.Equal(model.Name, value);
		Assert.True(options.Returning.Rows[0].TryGetValue(nameof(RoleModel.Enabled), out value));
		Assert.Equal(true, value);
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
			model.UserId = (uint)(200 + index);
			model.Name = $"${Zongsoft.Common.Randomizer.GenerateString()}_{index}";
		}).ToArray();

		var options = DataInsertOptions.Return(
			nameof(UserModel.UserId),
			nameof(UserModel.Name),
			nameof(UserModel.Enabled)
		).SuppressSequence().Build();

		var count = await accessor.InsertManyAsync(models, options);
		Assert.Equal(COUNT, count);
		Assert.Equal(COUNT, options.Returning.Rows.Count);

		for(int i = 0; i < COUNT; i++)
		{
			Assert.True(options.Returning.Rows[i].TryGetValue(nameof(UserModel.UserId), out var value));
			Assert.Equal(models[i].UserId, Convert.ChangeType(value, typeof(uint)));
			Assert.True(options.Returning.Rows[i].TryGetValue(nameof(UserModel.Name), out value));
			Assert.Equal(models[i].Name, value);
			Assert.True(options.Returning.Rows[i].TryGetValue(nameof(UserModel.Enabled), out value));
			Assert.Equal(true, value);
		}
	}

	[Fact]
	public async Task InsertManyWithChildrenAsync()
	{
		const int COUNT = 10;
		const int OFFSET = 1000;

		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		var models = Model.Build<RoleModel>(COUNT, (model, index) =>
		{
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

		var options = DataInsertOptions.Return(
			nameof(RoleModel.RoleId),
			nameof(RoleModel.Name),
			nameof(RoleModel.Enabled)
		).SuppressSequence().Build();

		var count = await accessor.InsertManyAsync(models, $"*,{nameof(RoleModel.Children)}{{*}}", options);
		Assert.Equal(3 * COUNT, count);
		Assert.Equal(COUNT, options.Returning.Rows.Count);

		for(int i = 0; i < models.Length; i++)
		{
			var model = models[i];
			Assert.NotNull(model.Children);
			Assert.NotEmpty(model.Children);

			foreach(var child in model.Children)
				Assert.Equal((uint)(OFFSET + i), child.RoleId);

			Assert.True(options.Returning.Rows[i].TryGetValue(nameof(RoleModel.RoleId), out var value));
			Assert.Equal(model.RoleId, Convert.ChangeType(value, typeof(uint)));
			Assert.True(options.Returning.Rows[i].TryGetValue(nameof(RoleModel.Name), out value));
			Assert.Equal(model.Name, value);
			Assert.True(options.Returning.Rows[i].TryGetValue(nameof(RoleModel.Enabled), out value));
			Assert.Equal(true, value);
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
