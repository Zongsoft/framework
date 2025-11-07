using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Data.PostgreSql.Tests.Models;

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
	}

	public void Dispose()
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		accessor.Delete<UserModel>(Condition.Equal(nameof(UserModel.UserId), 100));
	}
}
