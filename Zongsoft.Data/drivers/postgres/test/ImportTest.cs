using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Data.Tests.Models;

namespace Zongsoft.Data.PostgreSql.Tests;

[Collection("Database")]
public class ImportTest(DatabaseFixture database) : IDisposable
{
	private readonly DatabaseFixture _database = database;

	[Fact]
	public async Task ImportAsync()
	{
		const int COUNT = 1000;

		if(!Global.IsTestingEnabled)
			return;

		IDataAccess accessor = _database.Accessor;

		await accessor.DeleteAsync<UserModel>(Condition.GreaterThanEqual(nameof(UserModel.UserId), 1000));

		var users = Model.Build<UserModel>(COUNT, (model, index) =>
		{
			model.UserId = (uint)(1000 + index);
			model.Name = $"${Zongsoft.Common.Randomizer.GenerateString()}_{index}";
		});

		var count = await accessor.ImportAsync(users);
		Assert.Equal(COUNT, count);

		count = await accessor.DeleteAsync<UserModel>(Condition.Between(nameof(UserModel.UserId), 1000, 1000 + COUNT));
		Assert.Equal(COUNT, count);
	}

	void IDisposable.Dispose()
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		accessor.Delete<UserModel>(Condition.Like(nameof(UserModel.Name), "$%"));
	}
}
