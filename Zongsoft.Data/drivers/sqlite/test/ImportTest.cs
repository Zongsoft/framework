using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Xunit;

using Zongsoft.Data.Tests.Models;

namespace Zongsoft.Data.SQLite.Tests;

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
