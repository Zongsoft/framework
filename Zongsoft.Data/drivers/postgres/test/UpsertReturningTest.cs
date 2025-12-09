using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Data.Tests.Models;

namespace Zongsoft.Data.PostgreSql.Tests;

[Collection("Database")]
public class UpsertReturningTest(DatabaseFixture database) : IDisposable
{
	private readonly DatabaseFixture _database = database;

	[Fact]
	public async Task UpsertAsync()
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		await accessor.DeleteAsync<UserModel>(Condition.Equal(nameof(UserModel.UserId), 100));

		var options = DataUpsertOptions
			.Return(ReturningKind.Newer, nameof(UserModel.UserId), nameof(UserModel.Name), nameof(UserModel.Enabled))
			.Return(ReturningKind.Older, nameof(UserModel.UserId), nameof(UserModel.Name), nameof(UserModel.Enabled))
			.SuppressSequence().Build();

		var count = await accessor.UpsertAsync(Model.Build<UserModel>(model => {
			model.UserId = 100;
			model.Name = "Popeye";
		}), options);

		Assert.Equal(1, count);
		Assert.Single(options.Returning.Rows);

		Assert.True(options.Returning.Rows[0].TryGetValue(nameof(UserModel.UserId), ReturningKind.Newer, out var value));
		Assert.Equal(100, Convert.ChangeType(value, typeof(int)));
		Assert.True(options.Returning.Rows[0].TryGetValue(nameof(UserModel.Name), ReturningKind.Newer, out value));
		Assert.Equal("Popeye", value);
		Assert.True(options.Returning.Rows[0].TryGetValue(nameof(UserModel.Enabled), ReturningKind.Newer, out value));
		Assert.Equal(true, value);
		Assert.True(options.Returning.Rows[0].TryGetValue(nameof(UserModel.UserId), ReturningKind.Older, out value));
		Assert.Null(value);
		Assert.True(options.Returning.Rows[0].TryGetValue(nameof(UserModel.Name), ReturningKind.Older, out value));
		Assert.Null(value);
		Assert.True(options.Returning.Rows[0].TryGetValue(nameof(UserModel.Enabled), ReturningKind.Older, out value));
		Assert.Null(value);

		options = DataUpsertOptions
			.Return(ReturningKind.Newer, nameof(UserModel.UserId), nameof(UserModel.Name), nameof(UserModel.Enabled))
			.Return(ReturningKind.Older, nameof(UserModel.UserId), nameof(UserModel.Name), nameof(UserModel.Enabled))
			.SuppressSequence().Build();

		count = await accessor.UpsertAsync<UserModel>(new {
			UserId = 100,
			Name = "Popeye Zhong"
		}, options);

		Assert.True(count > 0);
		Assert.Single(options.Returning.Rows);

		Assert.True(options.Returning.Rows[0].TryGetValue(nameof(UserModel.UserId), ReturningKind.Newer, out value));
		Assert.Equal(100, Convert.ChangeType(value, typeof(int)));
		Assert.True(options.Returning.Rows[0].TryGetValue(nameof(UserModel.Name), ReturningKind.Newer, out value));
		Assert.Equal("Popeye Zhong", value);
		Assert.True(options.Returning.Rows[0].TryGetValue(nameof(UserModel.Enabled), ReturningKind.Newer, out value));
		Assert.Equal(true, value);
		Assert.True(options.Returning.Rows[0].TryGetValue(nameof(UserModel.UserId), ReturningKind.Older, out value));
		Assert.Equal(100, Convert.ChangeType(value, typeof(int)));
		Assert.True(options.Returning.Rows[0].TryGetValue(nameof(UserModel.Name), ReturningKind.Older, out value));
		Assert.Equal("Popeye", value);
		Assert.True(options.Returning.Rows[0].TryGetValue(nameof(UserModel.Enabled), ReturningKind.Older, out value));
		Assert.Equal(true, value);
	}

	[Fact]
	public async Task UpsertWithChildrenAsync()
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

		var options = DataUpsertOptions
			.Return(ReturningKind.Newer, nameof(RoleModel.RoleId), nameof(RoleModel.Name), nameof(RoleModel.Enabled))
			.Return(ReturningKind.Older, nameof(RoleModel.RoleId), nameof(RoleModel.Name), nameof(RoleModel.Enabled))
			.SuppressSequence().Build();

		var count = await accessor.UpsertAsync(model, $"*,{nameof(RoleModel.Children)}{{*}}", options);
		Assert.Equal(3, count);
		Assert.NotNull(model.Children);
		Assert.NotEmpty(model.Children);
		foreach(var child in model.Children)
			Assert.Equal(10U, child.RoleId);

		Assert.Single(options.Returning.Rows);
		Assert.True(options.Returning.Rows[0].TryGetValue(nameof(RoleModel.RoleId), ReturningKind.Newer, out var value));
		Assert.Equal(model.RoleId, Convert.ChangeType(value, typeof(uint)));
		Assert.True(options.Returning.Rows[0].TryGetValue(nameof(RoleModel.Name), ReturningKind.Newer, out value));
		Assert.Equal(model.Name, value);
		Assert.True(options.Returning.Rows[0].TryGetValue(nameof(RoleModel.Enabled), ReturningKind.Newer, out value));
		Assert.Equal(true, value);
		Assert.True(options.Returning.Rows[0].TryGetValue(nameof(RoleModel.RoleId), ReturningKind.Older, out value));
		Assert.Null(value);
		Assert.True(options.Returning.Rows[0].TryGetValue(nameof(RoleModel.Name), ReturningKind.Older, out value));
		Assert.Null(value);
		Assert.True(options.Returning.Rows[0].TryGetValue(nameof(RoleModel.Enabled), ReturningKind.Older, out value));
		Assert.Null(value);

		model = Model.Build<RoleModel>(model =>
		{
			model.RoleId = 10;
			model.Name = "Masters";
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

		options = DataUpsertOptions
			.Return(ReturningKind.Newer, nameof(RoleModel.RoleId), nameof(RoleModel.Name), nameof(RoleModel.Enabled))
			.Return(ReturningKind.Older, nameof(RoleModel.RoleId), nameof(RoleModel.Name), nameof(RoleModel.Enabled))
			.SuppressSequence().Build();

		count = await accessor.UpsertAsync(model, $"*,{nameof(RoleModel.Children)}{{*}}", options);
		Assert.True(count > 0);
		Assert.NotNull(model.Children);
		Assert.NotEmpty(model.Children);
		foreach(var child in model.Children)
			Assert.Equal(10U, child.RoleId);

		Assert.Single(options.Returning.Rows);
		Assert.True(options.Returning.Rows[0].TryGetValue(nameof(RoleModel.RoleId), ReturningKind.Newer, out value));
		Assert.Equal(model.RoleId, Convert.ChangeType(value, typeof(uint)));
		Assert.True(options.Returning.Rows[0].TryGetValue(nameof(RoleModel.Name), ReturningKind.Newer, out value));
		Assert.Equal(model.Name, value);
		Assert.True(options.Returning.Rows[0].TryGetValue(nameof(RoleModel.Enabled), ReturningKind.Newer, out value));
		Assert.Equal(true, value);
		Assert.True(options.Returning.Rows[0].TryGetValue(nameof(RoleModel.RoleId), ReturningKind.Older, out value));
		Assert.Equal(model.RoleId, Convert.ChangeType(value, typeof(uint)));
		Assert.True(options.Returning.Rows[0].TryGetValue(nameof(RoleModel.Name), ReturningKind.Older, out value));
		Assert.Equal("Managers", value);
		Assert.True(options.Returning.Rows[0].TryGetValue(nameof(RoleModel.Enabled), ReturningKind.Older, out value));
		Assert.Equal(true, value);
	}

	[Fact]
	public async Task UpsertManyAsync()
	{
		const int COUNT = 100;
		const int OFFSET = 10;

		const string OLDER_PREFIX = "#Unnamed@";
		const string NEWER_PREFIX = "#User@";

		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		await accessor.DeleteAsync<UserModel>(Condition.Between(nameof(UserModel.UserId), OFFSET, OFFSET + COUNT));

		var models = Model.Build<UserModel>(COUNT, (model, index) =>
		{
			model.UserId = (uint)(OFFSET + index);
			model.Name = $"{OLDER_PREFIX}{OFFSET + index}";
		}).ToArray();

		var options = DataUpsertOptions
			.Return(ReturningKind.Newer, nameof(UserModel.UserId), nameof(UserModel.Name), nameof(UserModel.Enabled))
			.Return(ReturningKind.Older, nameof(UserModel.UserId), nameof(UserModel.Name), nameof(UserModel.Enabled))
			.SuppressSequence().Build();

		var count = await accessor.UpsertManyAsync(models, options);
		Assert.Equal(COUNT, count);
		Assert.Equal(COUNT, options.Returning.Rows.Count);

		for(int i = 0; i < COUNT; i++)
		{
			var model = models[i];
			var row = options.Returning.Rows[i];

			Assert.True(row.TryGetValue(nameof(UserModel.UserId), ReturningKind.Newer, out var value));
			Assert.Equal(model.UserId, Convert.ChangeType(value, typeof(uint)));
			Assert.True(row.TryGetValue(nameof(UserModel.Name), ReturningKind.Newer, out value));
			Assert.Equal(model.Name, value);
			Assert.True(row.TryGetValue(nameof(UserModel.Enabled), ReturningKind.Newer, out value));
			Assert.Equal(true, value);
			Assert.True(row.TryGetValue(nameof(UserModel.UserId), ReturningKind.Older, out value));
			Assert.Null(value);
			Assert.True(row.TryGetValue(nameof(UserModel.Name), ReturningKind.Older, out value));
			Assert.Null(value);
			Assert.True(row.TryGetValue(nameof(UserModel.Enabled), ReturningKind.Older, out value));
			Assert.Null(value);
		}

		models = Model.Build<UserModel>(COUNT, (model, index) =>
		{
			model.UserId = (uint)(OFFSET + index);
			model.Name = $"{NEWER_PREFIX}{OFFSET + index}";
		}).ToArray();

		options = DataUpsertOptions
			.Return(ReturningKind.Newer, nameof(UserModel.UserId), nameof(UserModel.Name), nameof(UserModel.Enabled))
			.Return(ReturningKind.Older, nameof(UserModel.UserId), nameof(UserModel.Name), nameof(UserModel.Enabled))
			.SuppressSequence().Build();

		count = await accessor.UpsertManyAsync(models, options);
		Assert.Equal(COUNT, count);
		Assert.Equal(COUNT, options.Returning.Rows.Count);

		for(int i = 0; i < COUNT; i++)
		{
			var model = models[i];
			var row = options.Returning.Rows[i];

			Assert.True(row.TryGetValue(nameof(UserModel.UserId), ReturningKind.Newer, out var value));
			Assert.Equal(model.UserId, Convert.ChangeType(value, typeof(uint)));
			Assert.True(row.TryGetValue(nameof(UserModel.Name), ReturningKind.Newer, out value));
			Assert.Equal(model.Name, value);
			Assert.True(row.TryGetValue(nameof(UserModel.Enabled), ReturningKind.Newer, out value));
			Assert.Equal(true, value);
			Assert.True(row.TryGetValue(nameof(UserModel.UserId), ReturningKind.Older, out value));
			Assert.Equal(model.UserId, Convert.ChangeType(value, typeof(uint)));
			Assert.True(row.TryGetValue(nameof(UserModel.Name), ReturningKind.Older, out value));
			Assert.Equal($"{OLDER_PREFIX}{OFFSET + i}", value);
			Assert.True(row.TryGetValue(nameof(UserModel.Enabled), ReturningKind.Older, out value));
			Assert.Equal(true, value);
		}
	}

	[Fact]
	public async Task UpsertManyWithChildrenAsync()
	{
		const int COUNT = 10;
		const int OFFSET = 1000;

		const string OLDER_PREFIX = "$Role#";
		const string NEWER_PREFIX = "$New Role#";

		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		var models = Model.Build<RoleModel>(COUNT, (model, index) =>
		{
			model.RoleId = (uint)(OFFSET + index);
			model.Name = $"{OLDER_PREFIX}{(OFFSET + index)}";
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

		var options = DataUpsertOptions
			.Return(ReturningKind.Newer, nameof(RoleModel.RoleId), nameof(RoleModel.Name), nameof(RoleModel.Enabled))
			.Return(ReturningKind.Older, nameof(RoleModel.RoleId), nameof(RoleModel.Name), nameof(RoleModel.Enabled))
			.SuppressSequence().Build();

		var count = await accessor.UpsertManyAsync(models, $"*,{nameof(RoleModel.Children)}{{*}}", options);
		Assert.Equal(3 * COUNT, count);
		Assert.Equal(COUNT, options.Returning.Rows.Count);

		for(int i = 0; i < models.Length; i++)
		{
			var model = models[i];
			var row = options.Returning.Rows[i];

			Assert.NotNull(model.Children);
			Assert.NotEmpty(model.Children);
			foreach(var child in model.Children)
				Assert.Equal((uint)(OFFSET + i), child.RoleId);

			Assert.True(row.TryGetValue(nameof(RoleModel.RoleId), ReturningKind.Newer, out var value));
			Assert.Equal(model.RoleId, Convert.ChangeType(value, typeof(uint)));
			Assert.True(row.TryGetValue(nameof(RoleModel.Name), ReturningKind.Newer, out value));
			Assert.Equal(model.Name, value);
			Assert.True(row.TryGetValue(nameof(RoleModel.Enabled), ReturningKind.Newer, out value));
			Assert.Equal(true, value);
			Assert.True(row.TryGetValue(nameof(RoleModel.RoleId), ReturningKind.Older, out value));
			Assert.Null(value);
			Assert.True(row.TryGetValue(nameof(RoleModel.Name), ReturningKind.Older, out value));
			Assert.Null(value);
			Assert.True(row.TryGetValue(nameof(RoleModel.Enabled), ReturningKind.Older, out value));
			Assert.Null(value);
		}

		models = Model.Build<RoleModel>(COUNT, (model, index) =>
		{
			model.RoleId = (uint)(OFFSET + index);
			model.Name = $"{NEWER_PREFIX}{(OFFSET + index)}";
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

		options = DataUpsertOptions
			.Return(ReturningKind.Newer, nameof(RoleModel.RoleId), nameof(RoleModel.Name), nameof(RoleModel.Enabled))
			.Return(ReturningKind.Older, nameof(RoleModel.RoleId), nameof(RoleModel.Name), nameof(RoleModel.Enabled))
			.SuppressSequence().Build();

		count = await accessor.UpsertManyAsync(models, $"*,{nameof(RoleModel.Children)}{{*}}", options);
		Assert.Equal(3 * COUNT, count);
		Assert.Equal(COUNT, options.Returning.Rows.Count);

		for(int i = 0; i < models.Length; i++)
		{
			var model = models[i];
			var row = options.Returning.Rows[i];

			Assert.NotNull(model.Children);
			Assert.NotEmpty(model.Children);
			foreach(var child in model.Children)
				Assert.Equal((uint)(OFFSET + i), child.RoleId);

			Assert.True(row.TryGetValue(nameof(RoleModel.RoleId), ReturningKind.Newer, out var value));
			Assert.Equal(model.RoleId, Convert.ChangeType(value, typeof(uint)));
			Assert.True(row.TryGetValue(nameof(RoleModel.Name), ReturningKind.Newer, out value));
			Assert.Equal(model.Name, value);
			Assert.True(row.TryGetValue(nameof(RoleModel.Enabled), ReturningKind.Newer, out value));
			Assert.Equal(true, value);
			Assert.True(row.TryGetValue(nameof(RoleModel.RoleId), ReturningKind.Older, out value));
			Assert.Equal(model.RoleId, Convert.ChangeType(value, typeof(uint)));
			Assert.True(row.TryGetValue(nameof(RoleModel.Name), ReturningKind.Older, out value));
			Assert.Equal($"{OLDER_PREFIX}{(OFFSET + i)}", value);
			Assert.True(row.TryGetValue(nameof(RoleModel.Enabled), ReturningKind.Older, out value));
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
		accessor.Delete<UserModel>(Condition.Like(nameof(UserModel.Name), "#%"));
		accessor.Delete<RoleModel>(Condition.Equal(nameof(RoleModel.RoleId), 10));
		accessor.Delete<MemberModel>(Condition.Equal(nameof(MemberModel.RoleId), 10));
	}
}
