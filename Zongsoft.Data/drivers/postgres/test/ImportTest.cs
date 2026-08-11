using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Xunit;

using Zongsoft.Data.Tests.Models;

namespace Zongsoft.Data.PostgreSql.Tests;

[Collection("Database")]
public class ImportTest(DatabaseFixture database) : IDisposable
{
	private const string PREFIX = "$Imported:";
	private readonly DatabaseFixture _database = database;

	[Fact]
	public async Task ImportModelAsync()
	{
		const int COUNT = 100;

		if(!Global.IsTestingEnabled)
			return;

		IDataAccess accessor = _database.Accessor;
		await accessor.DeleteAsync<UserModel>(Condition.GreaterThanEqual(nameof(UserModel.UserId), 1000));

		var users = Model.Build<UserModel>(COUNT, (model, index) =>
		{
			model.UserId = (uint)(1000 + index);
			model.Name = $"{PREFIX}{Zongsoft.Common.Randomizer.GenerateString()}_{index}";
		});

		var count = await accessor.ImportAsync(users);
		Assert.Equal(COUNT, count);

		var models = accessor.SelectAsync<UserModel>(
			Condition.GreaterThan(nameof(UserModel.UserId), 0) &
			Condition.Like(nameof(UserModel.Name), $"{PREFIX}%"));

		count = 0;
		await foreach(var model in models)
		{
			Assert.NotNull(model);
			Assert.True(model.UserId > 0);
			Assert.StartsWith(PREFIX, model.Name);
			++count;
		}

		Assert.Equal(COUNT, count);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task ImportDefaultOptions_EnlistsInAmbientTransaction(bool asynchronous)
	{
		const uint USER_ID = 900001;

		if(!Global.IsTestingEnabled)
			return;

		IDataAccess accessor = _database.Accessor;
		var name = $"{PREFIX}Transaction:{Guid.NewGuid():N}";
		await accessor.DeleteAsync<UserModel>(Condition.Equal(nameof(UserModel.UserId), USER_ID));

		try
		{
			using(var transaction = new Transaction())
			{
				var user = Model.Build<UserModel>(model =>
				{
					model.UserId = USER_ID;
					model.Name = name;
				});

				Assert.Same(transaction, Transaction.Current);
				var count = asynchronous ?
					await accessor.ImportAsync([user]) :
					accessor.Import([user]);

				Assert.Equal(1, count);
				Assert.Same(transaction, Transaction.Current);
				Assert.True(await accessor.ExistsAsync<UserModel>(Condition.Equal(nameof(UserModel.UserId), USER_ID)));
				transaction.Rollback();
			}

			Assert.Null(Transaction.Current);
			Assert.False(await accessor.ExistsAsync<UserModel>(Condition.Equal(nameof(UserModel.UserId), USER_ID)));
		}
		finally
		{
			await accessor.DeleteAsync<UserModel>(Condition.Equal(nameof(UserModel.UserId), USER_ID));
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task ImportTransactionSuppressed_DoesNotEnlistInAmbientTransaction(bool asynchronous)
	{
		const uint USER_ID = 900003;

		if(!Global.IsTestingEnabled)
			return;

		IDataAccess accessor = _database.Accessor;
		var name = $"{PREFIX}TransactionSuppressed:{Guid.NewGuid():N}";
		var condition = Condition.Equal(nameof(UserModel.UserId), USER_ID);
		await accessor.DeleteAsync<UserModel>(condition);

		try
		{
			using(var transaction = new Transaction())
			{
				var user = Model.Build<UserModel>(model =>
				{
					model.UserId = USER_ID;
					model.Name = name;
				});

				Assert.Same(transaction, Transaction.Current);
				var options = DataImportOptions.SuppressTransaction().Build();
				var count = asynchronous ?
					await accessor.ImportAsync([user], options) :
					accessor.Import([user], options);

				Assert.Equal(1, count);
				Assert.Same(transaction, Transaction.Current);
				transaction.Rollback();
			}

			Assert.Null(Transaction.Current);
			Assert.True(await accessor.ExistsAsync<UserModel>(condition));
		}
		finally
		{
			await accessor.DeleteAsync<UserModel>(condition);
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task ImportWithoutAmbientTransaction_Commits(bool asynchronous)
	{
		const uint FIRST_USER_ID = 900010;
		const uint SECOND_USER_ID = 900011;

		if(!Global.IsTestingEnabled)
			return;

		IDataAccess accessor = _database.Accessor;
		var condition = Condition.In(nameof(UserModel.UserId), [FIRST_USER_ID, SECOND_USER_ID]);
		await accessor.DeleteAsync<UserModel>(condition);

		try
		{
			var users = Model.Build<UserModel>(2, (model, index) =>
			{
				model.UserId = index == 0 ? FIRST_USER_ID : SECOND_USER_ID;
				model.Name = $"{PREFIX}LocalCommit:{(asynchronous ? "Async" : "Sync")}:{index}";
			});

			Assert.Null(Transaction.Current);

			var count = asynchronous ?
				await accessor.ImportAsync(users) :
				accessor.Import(users);

			Assert.Equal(2, count);
			Assert.Null(Transaction.Current);
			Assert.Equal(2, await accessor.CountAsync<UserModel>(condition));
		}
		finally
		{
			await accessor.DeleteAsync<UserModel>(condition);
		}
	}

	[Theory]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public async Task ImportFailureWithoutAmbientTransaction_RollsBackEntireBatch(bool asynchronous, bool transactionSuppressed)
	{
		const uint FIRST_USER_ID = 900012;
		const uint SECOND_USER_ID = 900013;

		if(!Global.IsTestingEnabled)
			return;

		IDataAccess accessor = _database.Accessor;
		var condition = Condition.In(nameof(UserModel.UserId), [FIRST_USER_ID, SECOND_USER_ID]);
		await accessor.DeleteAsync<UserModel>(condition);

		try
		{
			var users = Model.Build<UserModel>(3, (model, index) =>
			{
				model.UserId = index == 0 ? FIRST_USER_ID : SECOND_USER_ID;
				model.Name = $"{PREFIX}LocalRollback:{index}";
			});
			var options = transactionSuppressed ? DataImportOptions.SuppressTransaction().Build() : null;

			Assert.Null(Transaction.Current);

			var exception = asynchronous ?
				await Record.ExceptionAsync(async () => await accessor.ImportAsync(users, options)) :
				Record.Exception(() => accessor.Import(users, options));

			Assert.NotNull(exception);
			Assert.Null(Transaction.Current);
			Assert.Equal(0, await accessor.CountAsync<UserModel>(condition));
		}
		finally
		{
			await accessor.DeleteAsync<UserModel>(condition);
		}
	}

	[Fact]
	public async Task ImportModelSequenceAsync()
	{
		const int COUNT = 100;

		if(!Global.IsTestingEnabled)
			return;

		IDataAccess accessor = _database.Accessor;

		var users = Model.Build<UserModel>(COUNT, (model, index) =>
		{
			model.Name = $"{PREFIX}{Zongsoft.Common.Randomizer.GenerateString()}_{index}";
		});

		var count = await accessor.ImportAsync(users);
		Assert.Equal(COUNT, count);

		var models = accessor.SelectAsync<UserModel>(
			Condition.GreaterThan(nameof(UserModel.UserId), 0) &
			Condition.Like(nameof(UserModel.Name), $"{PREFIX}%"));

		count = 0;
		await foreach(var model in models)
		{
			Assert.NotNull(model);
			Assert.True(model.UserId > 0);
			Assert.StartsWith(PREFIX, model.Name);
			++count;
		}

		Assert.Equal(COUNT, count);
	}

	[Fact]
	public async Task ImportStructAsync()
	{
		const int COUNT = 100;

		if(!Global.IsTestingEnabled)
			return;

		IDataAccess accessor = _database.Accessor;
		await accessor.DeleteAsync<User>(Condition.GreaterThanEqual(nameof(User.UserId), 1000));

		var users = new User[COUNT];
		for(int i = 0; i < COUNT; i++)
		{
			users[i] = new((uint)(1000 + i), $"{PREFIX}{Zongsoft.Common.Randomizer.GenerateString()}_{i}", $"{PREFIX}#{i}");
		}

		var count = await accessor.ImportAsync(users);
		Assert.Equal(COUNT, count);

		var models = accessor.SelectAsync<User>(
			Condition.GreaterThan(nameof(User.UserId), 0) &
			Condition.Like(nameof(User.Name), $"{PREFIX}%"));

		count = 0;
		await foreach(var model in models)
		{
			Assert.True(model.UserId > 0);
			Assert.StartsWith(PREFIX, model.Name);
			Assert.NotNull(model.Description);
			Assert.NotEmpty(model.Description);
			Assert.StartsWith(PREFIX, model.Description);

			++count;
		}

		Assert.Equal(COUNT, count);
	}

	[Fact]
	public async Task ImportStructSequenceAsync()
	{
		const int COUNT = 100;

		if(!Global.IsTestingEnabled)
			return;

		IDataAccess accessor = _database.Accessor;
		var users = new User[COUNT];

		for(int i = 0; i < COUNT; i++)
		{
			users[i] = new(0, $"{PREFIX}{Zongsoft.Common.Randomizer.GenerateString()}_{i}", $"{PREFIX}#{i}");
		}

		var count = await accessor.ImportAsync(users);
		Assert.Equal(COUNT, count);

		var models = accessor.SelectAsync<User>(
			Condition.GreaterThan(nameof(User.UserId), 0) &
			Condition.Like(nameof(User.Name), $"{PREFIX}%"));

		count = 0;
		await foreach(var model in models)
		{
			Assert.True(model.UserId > 0);
			Assert.StartsWith(PREFIX, model.Name);
			Assert.NotNull(model.Description);
			Assert.NotEmpty(model.Description);
			Assert.StartsWith(PREFIX, model.Description);

			++count;
		}

		Assert.Equal(COUNT, count);
	}

	[Fact]
	public async Task ImportDictionaryAsync()
	{
		const int COUNT = 100;

		if(!Global.IsTestingEnabled)
			return;

		IDataAccess accessor = _database.Accessor;
		await accessor.DeleteAsync("Security.User", Condition.GreaterThanEqual(nameof(UserModel.UserId), 1000));

		var data = new Dictionary<string, object>[COUNT];
		for(int i = 0; i < COUNT; i++)
		{
			data[i] = new()
			{
				{ nameof(UserModel.UserId), (uint)(1000 + i) },
				{ nameof(UserModel.Name), $"{PREFIX}{Zongsoft.Common.Randomizer.GenerateString()}_{i}" }
			};
		}

		var count = await accessor.ImportAsync("Security.User", data);
		Assert.Equal(COUNT, count);

		var models = accessor.SelectAsync<IDictionary<string, object>>(
			"Security.User",
			Condition.GreaterThan(nameof(UserModel.UserId), 0) &
			Condition.Like(nameof(UserModel.Name), $"{PREFIX}%"));

		count = 0;
		await foreach(var model in models)
		{
			Assert.NotNull(model);
			Assert.NotEmpty(model);

			Assert.True(model.TryGetValue(nameof(UserModel.UserId), out var value));
			Assert.NotNull(value);
			Assert.True((uint)value > 0);

			Assert.True(model.TryGetValue(nameof(UserModel.Name), out value));
			Assert.NotNull(value);
			Assert.StartsWith(PREFIX, (string)value);

			++count;
		}

		Assert.Equal(COUNT, count);
	}

	[Fact]
	public async Task ImportDictionarySequenceAsync()
	{
		const int COUNT = 100;

		if(!Global.IsTestingEnabled)
			return;

		IDataAccess accessor = _database.Accessor;
		await accessor.DeleteAsync("Security.User", Condition.GreaterThanEqual(nameof(UserModel.UserId), 1000));

		var data = new Dictionary<string, object>[COUNT];
		for(int i = 0; i < COUNT; i++)
		{
			data[i] = new()
			{
				{ nameof(UserModel.Name), $"{PREFIX}{Zongsoft.Common.Randomizer.GenerateString()}_{i}" }
			};
		}

		var count = await accessor.ImportAsync("Security.User", data);
		Assert.Equal(COUNT, count);

		var models = accessor.SelectAsync<IDictionary<string, object>>(
			"Security.User",
			Condition.GreaterThan(nameof(UserModel.UserId), 0) &
			Condition.Like(nameof(UserModel.Name), $"{PREFIX}%"));

		count = 0;
		await foreach(var model in models)
		{
			Assert.NotNull(model);
			Assert.NotEmpty(model);

			Assert.True(model.TryGetValue(nameof(UserModel.UserId), out var value));
			Assert.NotNull(value);
			Assert.True((uint)value > 0);

			Assert.True(model.TryGetValue(nameof(UserModel.Name), out value));
			Assert.NotNull(value);
			Assert.StartsWith(PREFIX, (string)value);

			++count;
		}

		Assert.Equal(COUNT, count);
	}

	void IDisposable.Dispose()
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		accessor.Delete<UserModel>(Condition.Like(nameof(UserModel.Name), $"{PREFIX}%"));
	}

	[Model("Security.User")]
	internal struct User(uint userId, string name, string description = null)
	{
		public uint UserId = userId;
		public string Name = name;
		public string Description { get; set; } = description;
	}
}
