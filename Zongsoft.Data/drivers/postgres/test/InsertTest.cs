using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Data.PostgreSql.Tests.Models;

namespace Zongsoft.Data.PostgreSql.Tests;

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
			model.Name = $"${Zongsoft.Common.Randomizer.GenerateString()}";
		}), DataInsertOptions.SuppressSequence());
		Assert.Equal(COUNT, count);

		count = await accessor.DeleteAsync<UserModel>(Condition.Like(nameof(UserModel.Name), "$%"));
		Assert.Equal(COUNT, count);
	}

	public void Dispose()
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		accessor.Delete<UserModel>(Condition.Equal(nameof(UserModel.UserId), 100));
		accessor.Delete<UserModel>(Condition.Like(nameof(UserModel.Name), "$%"));
	}
}
