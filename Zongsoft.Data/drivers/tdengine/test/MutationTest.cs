using System;
using System.Linq;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Data.Common;
using Zongsoft.Data.TDengine.Tests.Models;

using Zongsoft.Data.Features;

namespace Zongsoft.Data.TDengine.Tests;

[Collection("Database")]
public class MutationTest(DatabaseFixture database)
{
	private const string TESTING_DISABLED_REASON = "TDengine integration tests require a debugger or ZONGSOFT_DATA_TESTS=1.";
	private readonly DatabaseFixture _database = database;

	[Fact]
	[Trait("Category", "Unit")]
	public void TransactionIsSuppressed()
	{
		Assert.True(TDengineDriver.Instance.Features.Get<ITransactionFeature>()?.Suppressed == true);
	}

	[Fact]
	[Trait("Category", "Unit")]
	public async Task UpdateIsExplicitlyUnsupported()
	{
		var model = new GatewayHistory(200, 20001, 1.25, null, new DateTime(2097, 1, 1, 0, 0, 0, DateTimeKind.Local));

		await Assert.ThrowsAsync<NotSupportedException>(() => _database.Accessor.UpdateAsync(model).AsTask());
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	[Trait("Category", "Integration")]
	public async Task MultipleWritesPreserveRowsAsync(bool upsert)
	{
		Assert.SkipUnless(Global.IsTestingEnabled, TESTING_DISABLED_REASON);
		var accessor = _database.Accessor;
		var timestamp = new DateTime(2097, 1, 10, 0, 0, 0, DateTimeKind.Local);
		var criteria = Condition.GreaterThanEqual(nameof(GatewayHistory.Timestamp), timestamp) &
			Condition.LessThan(nameof(GatewayHistory.Timestamp), timestamp.AddMilliseconds(2));
		var models = new[]
		{
			new GatewayHistory(208, 20009, 11.5, null, timestamp),
			new GatewayHistory(208, 20009, 12.5, null, timestamp.AddMilliseconds(1)),
		};

		try
		{
			await accessor.DeleteAsync<GatewayHistory>(criteria);
			Assert.Equal(models.Length, upsert ? await accessor.UpsertManyAsync(models) : await accessor.InsertManyAsync(models));
			var rows = await Global.ToArrayAsync(accessor.SelectAsync<GatewayHistory>(criteria));
			Assert.Equal(models.Length, rows.Length);

			foreach(var model in models)
			{
				var row = Assert.Single(rows, row => row.Timestamp == model.Timestamp);
				Assert.Equal(model.GatewayId, row.GatewayId);
				Assert.Equal(model.MetricId, row.MetricId);
				Assert.Equal(model.Value, row.Value);
			}
		}
		finally
		{
			await accessor.DeleteAsync<GatewayHistory>(criteria);
		}
	}

	[Fact]
	[Trait("Category", "Integration")]
	public async Task TransactionRollbackDoesNotRollbackInsertAsync()
	{
		Assert.SkipUnless(Global.IsTestingEnabled, TESTING_DISABLED_REASON);

		var accessor = _database.Accessor;
		var timestamp = new DateTime(2097, 1, 2, 0, 0, 0, DateTimeKind.Local);
		var criteria = Condition.Equal(nameof(GatewayHistory.Timestamp), timestamp);
		var model = new GatewayHistory(201, 20002, 2.5, null, timestamp);

		try
		{
			await accessor.DeleteAsync<GatewayHistory>(criteria);

			await using(var transaction = new Transaction())
			{
				Assert.Same(transaction, Transaction.Current);
				Assert.Equal(1, await accessor.InsertAsync(model));
				await transaction.RollbackAsync();
			}

			Assert.Null(Transaction.Current);
			Assert.True(await accessor.ExistsAsync<GatewayHistory>(criteria));
		}
		finally
		{
			await accessor.DeleteAsync<GatewayHistory>(criteria);
		}
	}

	[Fact]
	[Trait("Category", "Integration")]
	public async Task UpsertSameTimestampOverwritesAsync()
	{
		Assert.SkipUnless(Global.IsTestingEnabled, TESTING_DISABLED_REASON);

		var accessor = _database.Accessor;
		var timestamp = new DateTime(2097, 1, 3, 0, 0, 0, DateTimeKind.Local);
		var criteria = Condition.Equal(nameof(GatewayHistory.Timestamp), timestamp);
		var original = new GatewayHistory(202, 20003, 3.75, null, timestamp);
		var replacement = new GatewayHistory(202, 20003, 7.5, null, timestamp);

		try
		{
			await accessor.DeleteAsync<GatewayHistory>(criteria);
			Assert.Equal(1, await accessor.InsertAsync(original));
			Assert.Equal(1, await accessor.UpsertAsync(replacement));

			var rows = await Global.ToArrayAsync(accessor.SelectAsync<GatewayHistory>(criteria));
			var row = Assert.Single(rows);
			Assert.Equal(replacement.GatewayId, row.GatewayId);
			Assert.Equal(replacement.MetricId, row.MetricId);
			Assert.Equal(replacement.Value, row.Value);
			Assert.Equal(replacement.Timestamp, row.Timestamp);
		}
		finally
		{
			await accessor.DeleteAsync<GatewayHistory>(criteria);
		}
	}

	[Fact]
	[Trait("Category", "Integration")]
	public async Task ImportThenUpsertUsesSameSubtableAsync()
	{
		Assert.SkipUnless(Global.IsTestingEnabled, TESTING_DISABLED_REASON);

		IDataAccess accessor = _database.Accessor;
		var timestamp = new DateTime(2097, 1, 9, 0, 0, 0, DateTimeKind.Local);
		var criteria = Condition.Equal(nameof(GatewayHistory.Timestamp), timestamp);
		var original = new GatewayHistory(207, 20008, 9.25, null, timestamp);
		var replacement = new GatewayHistory(207, 20008, 10.5, null, timestamp);

		try
		{
			await accessor.DeleteAsync<GatewayHistory>(criteria);
			Assert.Equal(1, await accessor.ImportAsync([original]));
			Assert.Equal(1, await accessor.UpsertAsync(replacement));

			var row = Assert.Single(await Global.ToArrayAsync(accessor.SelectAsync<GatewayHistory>(criteria)));
			Assert.Equal(replacement.Value, row.Value);
		}
		finally
		{
			await accessor.DeleteAsync<GatewayHistory>(criteria);
		}
	}

	[Fact]
	[Trait("Category", "Integration")]
	public async Task DeleteByTimestampAsync()
	{
		Assert.SkipUnless(Global.IsTestingEnabled, TESTING_DISABLED_REASON);

		var accessor = _database.Accessor;
		var timestamp = new DateTime(2097, 1, 4, 0, 0, 0, DateTimeKind.Local);
		var otherTimestamp = new DateTime(2097, 1, 5, 0, 0, 0, DateTimeKind.Local);
		var criteria = Condition.Equal(nameof(GatewayHistory.Timestamp), timestamp);
		var otherCriteria = Condition.Equal(nameof(GatewayHistory.Timestamp), otherTimestamp);

		try
		{
			await accessor.DeleteAsync<GatewayHistory>(criteria);
			await accessor.DeleteAsync<GatewayHistory>(otherCriteria);
			Assert.Equal(1, await accessor.InsertAsync(new GatewayHistory(203, 20004, 4.25, null, timestamp)));
			Assert.Equal(1, await accessor.InsertAsync(new GatewayHistory(203, 20004, 5.25, null, otherTimestamp)));

			await accessor.DeleteAsync<GatewayHistory>(criteria);
			Assert.False(await accessor.ExistsAsync<GatewayHistory>(criteria));
			Assert.True(await accessor.ExistsAsync<GatewayHistory>(otherCriteria));
		}
		finally
		{
			await accessor.DeleteAsync<GatewayHistory>(criteria);
			await accessor.DeleteAsync<GatewayHistory>(otherCriteria);
		}
	}

	[Fact]
	[Trait("Category", "Integration")]
	public async Task DeleteAllAsync()
	{
		Assert.SkipUnless(Global.IsTestingEnabled, TESTING_DISABLED_REASON);

		var accessor = _database.Accessor;

		try
		{
			await accessor.DeleteAsync<GatewayHistory>(null);
			Assert.Equal(1, await accessor.InsertAsync(new GatewayHistory(204, 20005, 6.25, null, new DateTime(2097, 1, 6, 0, 0, 0, DateTimeKind.Local))));
			Assert.Equal(1, await accessor.InsertAsync(new GatewayHistory(205, 20006, 7.25, null, new DateTime(2097, 1, 7, 0, 0, 0, DateTimeKind.Local))));

			Assert.Equal(2, await accessor.DeleteAsync<GatewayHistory>(null));
			Assert.Empty(await Global.ToArrayAsync(accessor.SelectAsync<GatewayHistory>()));
		}
		finally
		{
			await accessor.DeleteAsync<GatewayHistory>(null);
		}
	}

	[Fact]
	[Trait("Category", "Integration")]
	public async Task DeleteByNonTimeFieldIsRejectedAsync()
	{
		Assert.SkipUnless(Global.IsTestingEnabled, TESTING_DISABLED_REASON);

		const double VALUE = 8.25;
		var accessor = _database.Accessor;
		var timestamp = new DateTime(2097, 1, 8, 0, 0, 0, DateTimeKind.Local);
		var timestampCriteria = Condition.Equal(nameof(GatewayHistory.Timestamp), timestamp);
		var invalidCriteria = Condition.Equal(nameof(GatewayHistory.Value), VALUE);

		try
		{
			await accessor.DeleteAsync<GatewayHistory>(timestampCriteria);
			Assert.Equal(1, await accessor.InsertAsync(new GatewayHistory(206, 20007, VALUE, null, timestamp)));

			var exception = await Record.ExceptionAsync(async () => await accessor.DeleteAsync<GatewayHistory>(invalidCriteria));
			Assert.NotNull(exception);
			Assert.True(await accessor.ExistsAsync<GatewayHistory>(timestampCriteria));
		}
		finally
		{
			await accessor.DeleteAsync<GatewayHistory>(timestampCriteria);
		}
	}
}
