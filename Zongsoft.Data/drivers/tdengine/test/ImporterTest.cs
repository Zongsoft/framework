using System;
using System.Threading.Tasks;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Data.TDengine.Tests;

[Collection("Database")]
[Trait("Category", "Integration")]
public class ImporterTest(DatabaseFixture database)
{
	private const string PREFIX = "$Imported:";
	private const string TESTING_DISABLED_REASON = "TDengine integration tests require a debugger or ZONGSOFT_DATA_TESTS=1.";
	private readonly DatabaseFixture _database = database;

	[Fact]
	public void TestImportModel()
	{
		const int COUNT = 100;
		const uint GATEWAY_ID = 201;
		const ulong METRIC_ID = 20001;

		Assert.SkipUnless(Global.IsTestingEnabled, TESTING_DISABLED_REASON);

		var timestamp = new DateTime(2096, 1, 3, 0, 0, 0, DateTimeKind.Local);
		var criteria = GetCriteria(timestamp, COUNT, GATEWAY_ID, METRIC_ID);
		IDataAccess accessor = _database.Accessor;

		try
		{
			accessor.Delete<Models.GatewayHistory>(GetTimeCriteria(timestamp, COUNT));

			var count = accessor.Import(GenerateModels(COUNT, timestamp, GATEWAY_ID, METRIC_ID));
			Assert.Equal(COUNT, count);

			var models = accessor.Select<Models.GatewayHistory>(criteria);

			count = 0;
			foreach(var model in models)
			{
				Assert.Equal(GATEWAY_ID, model.GatewayId);
				Assert.Equal(METRIC_ID, model.MetricId);
				Assert.StartsWith(PREFIX, model.Text);
				++count;
			}

			Assert.Equal(COUNT, count);
		}
		finally
		{
			accessor.Delete<Models.GatewayHistory>(GetTimeCriteria(timestamp, COUNT));
		}
	}

	[Fact]
	public async Task TestImportModelAsync()
	{
		const int COUNT = 100;
		const uint GATEWAY_ID = 202;
		const ulong METRIC_ID = 20002;

		Assert.SkipUnless(Global.IsTestingEnabled, TESTING_DISABLED_REASON);

		var timestamp = new DateTime(2096, 1, 4, 0, 0, 0, DateTimeKind.Local);
		var criteria = GetCriteria(timestamp, COUNT, GATEWAY_ID, METRIC_ID);
		IDataAccess accessor = _database.Accessor;

		try
		{
			await accessor.DeleteAsync<Models.GatewayHistory>(GetTimeCriteria(timestamp, COUNT));

			var count = await accessor.ImportAsync(GenerateModels(COUNT, timestamp, GATEWAY_ID, METRIC_ID));
			Assert.Equal(COUNT, count);

			var models = accessor.SelectAsync<Models.GatewayHistory>(criteria);

			count = 0;
			await foreach(var model in models)
			{
				Assert.Equal(GATEWAY_ID, model.GatewayId);
				Assert.Equal(METRIC_ID, model.MetricId);
				Assert.StartsWith(PREFIX, model.Text);
				++count;
			}

			Assert.Equal(COUNT, count);
		}
		finally
		{
			await accessor.DeleteAsync<Models.GatewayHistory>(GetTimeCriteria(timestamp, COUNT));
		}
	}

	[Fact]
	public async Task TestImportDictionaryAsync()
	{
		const int COUNT = 100;
		const uint GATEWAY_ID = 203;
		const ulong METRIC_ID = 20003;

		Assert.SkipUnless(Global.IsTestingEnabled, TESTING_DISABLED_REASON);

		var timestamp = new DateTime(2096, 1, 5, 0, 0, 0, DateTimeKind.Local);
		var criteria = GetCriteria(timestamp, COUNT, GATEWAY_ID, METRIC_ID);
		IDataAccess accessor = _database.Accessor;

		try
		{
			await accessor.DeleteAsync<Models.GatewayHistory>(GetTimeCriteria(timestamp, COUNT));

			var count = await accessor.ImportAsync(
				nameof(Models.GatewayHistory),
				GenerateDictionaries(COUNT, timestamp, GATEWAY_ID, METRIC_ID));
			Assert.Equal(COUNT, count);

			var models = accessor.SelectAsync<IDictionary<string, object>>(nameof(Models.GatewayHistory), criteria);

			count = 0;
			await foreach(var model in models)
			{
				Assert.NotNull(model);
				Assert.NotEmpty(model);

				Assert.True(model.TryGetValue(nameof(Models.GatewayHistory.GatewayId), out var value));
				Assert.Equal(GATEWAY_ID, Assert.IsType<uint>(value));

				Assert.True(model.TryGetValue(nameof(Models.GatewayHistory.Text), out value));
				Assert.StartsWith(PREFIX, Assert.IsType<string>(value));

				++count;
			}

			Assert.Equal(COUNT, count);
		}
		finally
		{
			await accessor.DeleteAsync<Models.GatewayHistory>(GetTimeCriteria(timestamp, COUNT));
		}
	}

	#region 私有方法
	private static ICondition GetCriteria(DateTime timestamp, int count, uint gatewayId, ulong metricId) =>
		GetTimeCriteria(timestamp, count) &
		Condition.Equal(nameof(Models.GatewayHistory.GatewayId), gatewayId) &
		Condition.Equal(nameof(Models.GatewayHistory.MetricId), metricId) &
		Condition.Like(nameof(Models.GatewayHistory.Text), $"{PREFIX}%");

	private static ICondition GetTimeCriteria(DateTime timestamp, int count) =>
		Condition.GreaterThan(nameof(Models.GatewayHistory.Timestamp), timestamp) &
		Condition.LessThanEqual(nameof(Models.GatewayHistory.Timestamp), timestamp.AddMilliseconds(count));

	private static IEnumerable<Models.GatewayHistory> GenerateModels(
		int count,
		DateTime timestamp,
		uint gatewayId,
		ulong metricId)
	{
		for(int i = 0; i < count; i++)
		{
			var failureCode = i > 0 && i % 50 == 0 ? i / 50 : 0;
			var failureMessage = failureCode > 0 ? $"Failure #{i:D4}" : null;

			yield return new Models.GatewayHistory()
			{
				Timestamp = timestamp.AddMilliseconds(i + 1),
				GatewayId = gatewayId,
				MetricId = metricId,
				Value = i + 0.5,
				Text = $"{PREFIX}{i:D4}",
				FailureCode = failureCode,
				FailureMessage = failureMessage,
			};
		}
	}

	private static IEnumerable<Dictionary<string, object>> GenerateDictionaries(
		int count,
		DateTime timestamp,
		uint gatewayId,
		ulong metricId)
	{
		for(int i = 0; i < count; i++)
		{
			var failureCode = i > 0 && i % 50 == 0 ? i / 50 : 0;
			var failureMessage = failureCode > 0 ? $"Failure #{i:D4}" : null;

			yield return new Dictionary<string, object>()
			{
				{ nameof(Models.GatewayHistory.Timestamp), timestamp.AddMilliseconds(i + 1) },
				{ nameof(Models.GatewayHistory.GatewayId), gatewayId },
				{ nameof(Models.GatewayHistory.MetricId), metricId },
				{ nameof(Models.GatewayHistory.Value), i + 0.5 },
				{ nameof(Models.GatewayHistory.Text), $"{PREFIX}{i:D4}" },
				{ nameof(Models.GatewayHistory.FailureCode), failureCode },
				{ nameof(Models.GatewayHistory.FailureMessage), failureMessage },
			};
		}
	}
	#endregion
}
