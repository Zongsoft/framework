using System;
using System.Linq;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Data.Common;
using Zongsoft.Data.Tests.Models;

namespace Zongsoft.Data.SQLite.Tests;

[Collection("Database")]
public class TransactionTest(DatabaseFixture database) : IDisposable
{
	private readonly DatabaseFixture _database = database;

	[Fact]
	public async Task CommitSingleTransactionAsync()
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		var target = GetTarget();
		var action = "Single.Commit";

		using(var transaction = new Transaction())
		{
			Assert.Same(transaction, Transaction.Current);

			await InsertLogAsync(accessor, target, action);
			Assert.True(await ExistsLogAsync(accessor, target, action));

			transaction.Commit();
		}

		Assert.Null(Transaction.Current);
		Assert.True(await ExistsLogAsync(accessor, target, action));
	}

	[Fact]
	public async Task RollbackSingleTransactionAsync()
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		var target = GetTarget();
		var action = "Single.Rollback";

		using(var transaction = new Transaction())
		{
			Assert.Same(transaction, Transaction.Current);

			await InsertLogAsync(accessor, target, action);
			Assert.True(await ExistsLogAsync(accessor, target, action));

			transaction.Rollback();
		}

		Assert.Null(Transaction.Current);
		Assert.False(await ExistsLogAsync(accessor, target, action));
	}

	[Fact]
	public async Task CommitNestedTransactionsAsync()
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		var target = GetTarget();

		using(var outer = new Transaction())
		{
			Assert.Same(outer, Transaction.Current);
			await InsertLogAsync(accessor, target, "Nested.Outer");

			using(var middle = new Transaction())
			{
				Assert.Same(middle, Transaction.Current);
				Assert.Same(outer, middle.Information.Parent);
				await InsertLogAsync(accessor, target, "Nested.Middle");

				using(var inner = new Transaction())
				{
					Assert.Same(inner, Transaction.Current);
					Assert.Same(middle, inner.Information.Parent);
					await InsertLogAsync(accessor, target, "Nested.Inner");

					inner.Commit();
				}

				Assert.Same(middle, Transaction.Current);
				middle.Commit();
			}

			Assert.Same(outer, Transaction.Current);
			outer.Commit();
		}

		Assert.Null(Transaction.Current);
		Assert.True(await ExistsLogAsync(accessor, target, "Nested.Outer"));
		Assert.True(await ExistsLogAsync(accessor, target, "Nested.Middle"));
		Assert.True(await ExistsLogAsync(accessor, target, "Nested.Inner"));
	}

	[Fact]
	public async Task MultipleTransactionCallsAsync()
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		var target = GetTarget();

		using(var transaction = new Transaction())
		{
			await InsertLogAsync(accessor, target, "Multiple.First");
			transaction.Commit();
		}

		using(var transaction = new Transaction())
		{
			await InsertLogAsync(accessor, target, "Multiple.Second");
			transaction.Rollback();
		}

		using(var transaction = new Transaction())
		{
			await InsertLogAsync(accessor, target, "Multiple.Third");
			Assert.True(await ExistsLogAsync(accessor, target, "Multiple.Third"));
			transaction.Commit();
		}

		Assert.True(await ExistsLogAsync(accessor, target, "Multiple.First"));
		Assert.False(await ExistsLogAsync(accessor, target, "Multiple.Second"));
		Assert.True(await ExistsLogAsync(accessor, target, "Multiple.Third"));
	}

	[Fact]
	public async Task ConcurrentAmbientTransactionsKeepCurrentAsync()
	{
		const int COUNT = 64;
		const int LOOPS = 32;

		if(!Global.IsTestingEnabled)
			return;

		var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var remaining = COUNT;

		var tasks = Enumerable.Range(0, COUNT).Select(index => Task.Run(async () =>
		{
			using(var transaction = new Transaction())
			{
				if(Interlocked.Decrement(ref remaining) == 0)
					ready.SetResult();

				await ready.Task.WaitAsync(TimeSpan.FromSeconds(10));

				for(int i = 0; i < LOOPS; i++)
				{
					Assert.Same(transaction, Transaction.Current);

					if((index + i) % 3 == 0)
						await Task.Delay(1);
					else
						await Task.Yield();
				}

				if(index % 2 == 0)
					transaction.Commit();
				else
					transaction.Rollback();
			}

			Assert.Null(Transaction.Current);
		}));

		await Task.WhenAll(tasks);

		Assert.Null(Transaction.Current);
	}

	[Fact]
	public async Task ConcurrentTransactionsCommitAndRollbackIndependentlyAsync()
	{
		const int COUNT = 24;

		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		var target = GetTarget();

		var tasks = Enumerable.Range(0, COUNT).Select(index => Task.Run(async () =>
		{
			var action = index % 2 == 0 ? $"Concurrent.Commit.{index}" : $"Concurrent.Rollback.{index}";

			using(var transaction = new Transaction())
			{
				await InsertLogAsync(accessor, target, action);
				Assert.True(await ExistsLogAsync(accessor, target, action));

				if(index % 2 == 0)
					transaction.Commit();
				else
					transaction.Rollback();
			}
		}));

		await Task.WhenAll(tasks);

		for(int i = 0; i < COUNT; i++)
		{
			var action = i % 2 == 0 ? $"Concurrent.Commit.{i}" : $"Concurrent.Rollback.{i}";

			if(i % 2 == 0)
				Assert.True(await ExistsLogAsync(accessor, target, action));
			else
				Assert.False(await ExistsLogAsync(accessor, target, action));
		}
	}

	[Fact]
	public async Task CommitWaitsForOpenReaderToReleaseAsync()
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		var target = GetTarget();
		var action = "Reader.Commit";

		using(var transaction = new Transaction())
		{
			await InsertLogAsync(accessor, target, action);

			var logs = accessor.SelectAsync<Log>(
				Condition.Equal(nameof(Log.Target), target) &
				Condition.Equal(nameof(Log.Action), action));

			await using(var enumerator = logs.GetAsyncEnumerator())
			{
				Assert.True(await enumerator.MoveNextAsync());

				transaction.Commit();
			}
		}

		Assert.True(await ExistsLogAsync(accessor, target, action));
	}

	[Fact]
	public async Task DisposeAfterDeferredCommitPreservesCommitAsync()
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		var target = GetTarget();
		var action = "Reader.Commit.Dispose";

		using(var transaction = new Transaction())
		{
			await InsertLogAsync(accessor, target, action);
			var session = GetSession(transaction);
			var logs = accessor.SelectAsync<Log>(
				Condition.Equal(nameof(Log.Target), target) &
				Condition.Equal(nameof(Log.Action), action));

			await using(var enumerator = logs.GetAsyncEnumerator())
			{
				Assert.True(await enumerator.MoveNextAsync());

				transaction.Commit();
				Assert.True(session.IsCompleted);
				Assert.True(session.IsReading);

				session.Dispose();
			}

			AssertSessionReleased(session);
		}

		Assert.True(await ExistsLogAsync(accessor, target, action));
	}

	[Fact]
	public async Task CommitReleasesTwoOpenReadersAsync()
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		var target = GetTarget();
		var action = "Reader.Multiple";

		using(var transaction = new Transaction())
		{
			await InsertLogAsync(accessor, target, action);
			var session = GetSession(transaction);
			Assert.NotNull(session.Transaction);

			await using var first = accessor.SelectAsync<UserModel>().GetAsyncEnumerator();
			Assert.True(await first.MoveNextAsync());

			await using var second = accessor.SelectAsync<UserModel>().GetAsyncEnumerator();
			Assert.True(await second.MoveNextAsync());
			Assert.True(session.IsReading);

			await second.DisposeAsync();
			await first.DisposeAsync();

			Assert.False(session.IsReading);
			transaction.Commit();
			AssertSessionReleased(session);
		}

		Assert.True(await ExistsLogAsync(accessor, target, action));
	}

	[Fact]
	public async Task CommitReleasesReaderAfterExecutionFailureAsync()
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		var target = GetTarget();
		var action = "Reader.Failure";
		var command = Mapping.Commands.Script(SQLiteDriver.NAME, "SELECT * FROM \"__Zongsoft_Missing_Table__\"");

		using(var transaction = new Transaction())
		{
			await InsertLogAsync(accessor, target, action);
			var session = GetSession(transaction);
			Assert.NotNull(session.Transaction);

			await using var enumerator = accessor.ExecuteAsync<Log>(command.QualifiedName).GetAsyncEnumerator();
			await Assert.ThrowsAnyAsync<DbException>(() => enumerator.MoveNextAsync().AsTask());

			Assert.False(session.IsReading);
			transaction.Commit();
			AssertSessionReleased(session);
		}

		Assert.True(await ExistsLogAsync(accessor, target, action));
	}

	[Fact]
	public async Task CommitReleasesReaderAfterCancellationAsync()
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		var target = GetTarget();
		var action = "Reader.Cancellation";

		using(var transaction = new Transaction())
		{
			await InsertLogAsync(accessor, target, action);
			var session = GetSession(transaction);
			Assert.NotNull(session.Transaction);

			using var cancellation = new CancellationTokenSource();
			cancellation.Cancel();

			await using var enumerator = accessor.SelectAsync<UserModel>().GetAsyncEnumerator(cancellation.Token);
			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => enumerator.MoveNextAsync().AsTask());

			Assert.False(session.IsReading);
			transaction.Commit();
			AssertSessionReleased(session);
		}

		Assert.True(await ExistsLogAsync(accessor, target, action));
	}

	private static async Task InsertLogAsync(DataAccess accessor, string target, string action)
	{
		var log = Model.Build<Log>(log =>
		{
			log.UserId = 1;
			log.Target = target;
			log.Action = action;
			log.TenantId = 1;
			log.BranchId = 0;
			log.Timestamp = DateTime.Now;
		});

		var count = await accessor.InsertAsync(log);

		Assert.Equal(1, count);
		Assert.True(log.LogId > 0);
	}

	private static string GetTarget() => $"{nameof(TransactionTest)}:{Guid.NewGuid():N}-{Environment.TickCount64:X}";
	private static DataSession GetSession(Transaction transaction) => Assert.IsType<DataSession>(transaction.Information.Parameters["Zongsoft.Data:DataSession"]);
	private static ValueTask<bool> ExistsLogAsync(DataAccess accessor, string target, string action) =>
		accessor.ExistsAsync<Log>(
			Condition.Equal(nameof(Log.Target), target) &
			Condition.Equal(nameof(Log.Action), action));

	private static void AssertSessionReleased(DataSession session)
	{
		Assert.True(session.IsCompleted);
		Assert.False(session.IsReading);
		Assert.Null(session.Transaction);
		Assert.Null(session.Connection);
	}

	void IDisposable.Dispose()
	{
		if(!Global.IsTestingEnabled)
			return;

		_database.Accessor.Execute("TruncateLog");
	}
}
