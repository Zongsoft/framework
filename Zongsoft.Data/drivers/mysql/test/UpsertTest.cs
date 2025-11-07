using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Data.MySql.Tests.Models;

namespace Zongsoft.Data.MySql.Tests;

[Collection("Database")]
public class UpsertTest(DatabaseFixture database) : IDisposable
{
	private readonly DatabaseFixture _database = database;

	[Fact]
	public async Task UpsertAsync()
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;

		await accessor.DeleteAsync<UserModel>(Condition.Equal(nameof(UserModel.UserId), 100));

		var count = await accessor.UpsertAsync(Model.Build<UserModel>(model => {
			model.UserId = 100;
			model.Name = "Popeye";
		}), DataUpsertOptions.SuppressSequence());
		Assert.Equal(1, count);

		count = await accessor.UpsertAsync<UserModel>(new {
			UserId = 100,
			Name = "Popeye Zhong"
		});
		Assert.True(count > 0);

		var result = accessor.SelectAsync<string>(
			Model.Naming.Get<UserModel>(),
			Condition.Equal(nameof(UserModel.UserId), 100),
			nameof(UserModel.Name));

		var enumerator = result.GetAsyncEnumerator();
		Assert.True(await enumerator.MoveNextAsync());
		var name = enumerator.Current;
		await enumerator.DisposeAsync();

		Assert.Equal("Popeye Zhong", name);
	}

	public void Dispose()
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		accessor.Delete<UserModel>(Condition.Equal(nameof(UserModel.UserId), 100));
	}
}
