using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Data.TDengine.Tests;

[Collection("Database")]
public class ImporterTest(DatabaseFixture database)
{
	private const string PREFIX = "$Imported:";
	private readonly DatabaseFixture _database = database;

	[Fact]
	public void TestImportModel()
	{
		const int COUNT = 100;

		if(!Global.IsTestingEnabled)
			return;

		var timestamp = DateTime.Now;
		IDataAccess accessor = _database.Accessor;

		//执行数据导入操作
		var count = accessor.Import(GenerateModels(COUNT));
		Assert.Equal(COUNT, count);

		var models = accessor.Select<Models.GatewayHistory>(
			Condition.GreaterThanEqual(nameof(Models.GatewayHistory.Timestamp), timestamp) &
			Condition.Like(nameof(Models.GatewayHistory.Text), $"{PREFIX}%"));

		count = 0;
		foreach(var model in models)
		{
			Assert.True(model.GatewayId > 0);
			Assert.StartsWith(PREFIX, model.Text);
			++count;
		}

		Assert.Equal(COUNT, count);
	}

	[Fact]
	public async Task TestImportModelAsync()
	{
		const int COUNT = 100;

		if(!Global.IsTestingEnabled)
			return;

		var timestamp = DateTime.Now;
		IDataAccess accessor = _database.Accessor;

		//执行数据导入操作
		var count = await accessor.ImportAsync(GenerateModels(COUNT));
		Assert.Equal(COUNT, count);

		var models = accessor.SelectAsync<Models.GatewayHistory>(
			Condition.GreaterThanEqual(nameof(Models.GatewayHistory.Timestamp), timestamp) &
			Condition.Like(nameof(Models.GatewayHistory.Text), $"{PREFIX}%"));

		count = 0;
		await foreach(var model in models)
		{
			Assert.True(model.GatewayId > 0);
			Assert.StartsWith(PREFIX, model.Text);
			++count;
		}

		Assert.Equal(COUNT, count);
	}

	[Fact]
	public async Task TestImportDictionaryAsync()
	{
		const int COUNT = 100;

		if(!Global.IsTestingEnabled)
			return;

		var timestamp = DateTime.Now;
		IDataAccess accessor = _database.Accessor;

		//执行数据导入操作
		var count = await accessor.ImportAsync(nameof(Models.GatewayHistory), GenerateDictionaries(COUNT));
		Assert.Equal(COUNT, count);

		var models = accessor.SelectAsync<IDictionary<string, object>>(
			nameof(Models.GatewayHistory),
			Condition.GreaterThanEqual(nameof(Models.GatewayHistory.Timestamp), timestamp) &
			Condition.Like(nameof(Models.GatewayHistory.Text), $"{PREFIX}%"));

		count = 0;
		await foreach(var model in models)
		{
			Assert.NotNull(model);
			Assert.NotEmpty(model);

			Assert.True(model.TryGetValue(nameof(Models.GatewayHistory.GatewayId), out var value));
			Assert.NotNull(value);
			Assert.True((uint)value > 0);

			Assert.True(model.TryGetValue(nameof(Models.GatewayHistory.Text), out value));
			Assert.NotNull(value);
			Assert.StartsWith(PREFIX, (string)value);

			++count;
		}

		Assert.Equal(COUNT, count);
	}

	#region 私有方法
	private static IEnumerable<Models.GatewayHistory> GenerateModels(int count = 100)
	{
		var timestamp = DateTime.Now;

		for(int i = 0; i < count; i++)
		{
			var failureCode = i > 0 && i % 50 == 0 ? Random.Shared.Next(1, 10) : 0;
			var failureMessage = failureCode > 0 ? $"Message #{Math.Abs(Random.Shared.Next()):X}" : null;

			yield return new Models.GatewayHistory()
			{
				Timestamp = timestamp.AddMilliseconds(1),
				GatewayId = (uint)(i + 1),
				MetricId = (ulong)Random.Shared.NextInt64(),
				Value = Random.Shared.NextDouble(),
				Text = $"{PREFIX}{Random.Shared.Next()}",
				FailureCode = failureCode,
				FailureMessage = failureMessage,
			};
		}
	}

	private static IEnumerable<Dictionary<string, object>> GenerateDictionaries(int count = 100)
	{
		var timestamp = DateTime.Now;

		for(int i = 0; i < count; i++)
		{
			var failureCode = i > 0 && i % 50 == 0 ? Random.Shared.Next(1, 10) : 0;
			var failureMessage = failureCode > 0 ? $"Message #{Math.Abs(Random.Shared.Next()):X}" : null;

			yield return new Dictionary<string, object>()
			{
				{ nameof(Models.GatewayHistory.Timestamp), timestamp.AddMilliseconds(1) },
				{ nameof(Models.GatewayHistory.GatewayId), (uint)(i + 1) },
				{ nameof(Models.GatewayHistory.MetricId), (ulong)Random.Shared.NextInt64() },
				{ nameof(Models.GatewayHistory.Value), Random.Shared.NextDouble() },
				{ nameof(Models.GatewayHistory.Text), $"{PREFIX}{Random.Shared.Next()}" },
				{ nameof(Models.GatewayHistory.FailureCode), failureCode },
				{ nameof(Models.GatewayHistory.FailureMessage), failureMessage },
			};
		}
	}
	#endregion
}
