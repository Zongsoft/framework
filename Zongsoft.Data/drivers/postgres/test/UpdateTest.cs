using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Data.Tests.Models;

namespace Zongsoft.Data.PostgreSql.Tests;

[Collection("Database")]
public class UpdateTest(DatabaseFixture database) : IDisposable
{
	private readonly DatabaseFixture _database = database;

	[Fact]
	public async Task UpdateAsync()
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;

		await accessor.InsertAsync(Model.Build<UserModel>(model => {
			model.UserId = 100;
			model.Name = "Popeye";
		}), DataInsertOptions.SuppressSequence().IgnoreConstraint());

		var count = await accessor.UpdateAsync<UserModel>(new
		{
			Name = "Popeye Zhong",
		}, Condition.Equal(nameof(UserModel.UserId), 100));
		Assert.Equal(1, count);

		var result = accessor.SelectAsync<string>(
			Model.Naming.Get<UserModel>(),
			Condition.Equal(nameof(UserModel.UserId), 100),
			nameof(UserModel.Name));

		var enumerator = result.GetAsyncEnumerator();
		Assert.True(await enumerator.MoveNextAsync());
		var name = enumerator.Current;
		await enumerator.DisposeAsync();
		Assert.Equal("Popeye Zhong", name);

		var options = DataUpdateOptions
			.Return(ReturningKind.Newer, nameof(UserModel.Name), nameof(UserModel.Enabled))
			.Return(ReturningKind.Older, nameof(UserModel.Name), nameof(UserModel.Enabled))
			.Build();

		count = await accessor.UpdateAsync<UserModel>(new
		{
			Name = "Popeye",
			Enabled = true,
		}, Condition.Equal(nameof(UserModel.UserId), 100), options);

		Assert.Equal(1, count);
		Assert.True(options.HasReturning(out var returning));
		Assert.Single(returning.Rows);
		Assert.True(returning.Rows[0].TryGetValue(nameof(UserModel.Name), ReturningKind.Newer, out var value));
		Assert.Equal("Popeye", value);
		Assert.True(returning.Rows[0].TryGetValue(nameof(UserModel.Name), ReturningKind.Older, out value));
		Assert.Equal("Popeye Zhong", value);
		Assert.True(returning.Rows[0].TryGetValue(nameof(UserModel.Enabled), ReturningKind.Newer, out value));
		Assert.Equal(true, value);
		Assert.True(returning.Rows[0].TryGetValue(nameof(UserModel.Enabled), ReturningKind.Older, out value));
		Assert.Equal(true, value);
	}

	void IDisposable.Dispose()
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		accessor.Delete<UserModel>(Condition.Equal(nameof(UserModel.UserId), 100));
	}
}
