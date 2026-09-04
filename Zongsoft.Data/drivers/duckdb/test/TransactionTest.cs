using System;
using System.Data;
using System.Linq;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Data.Common;
using Zongsoft.Data.Metadata;
using Zongsoft.Data.Tests.Models;

namespace Zongsoft.Data.DuckDB.Tests;

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

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task SessionCommandScalarAndNonQuery_EnlistInAmbientTransaction(bool asynchronous)
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		var target = GetTarget();
		var action = $"Command.{(asynchronous ? "Async" : "Sync")}";
		var commandName = $"{nameof(SessionCommandScalarAndNonQuery_EnlistInAmbientTransaction)}.{Guid.NewGuid():N}";
		var insert = Mapping.Commands.Add($"{commandName}.Insert", DataCommandMutability.Insert).Script(DuckDBDriver.NAME,
			"INSERT INTO \"Log\" (\"UserId\", \"Target\", \"Action\", \"TenantId\", \"BranchId\", \"Severity\", \"Timestamp\") VALUES ($UserId, $Target, $Action, $TenantId, $BranchId, $Severity, $Timestamp)")
			.Parameter("@UserId", DataType.UInt32)
			.Parameter("@Target", DataType.AnsiString, 100)
			.Parameter("@Action", DataType.AnsiString, 100)
			.Parameter("@TenantId", DataType.UInt32)
			.Parameter("@BranchId", DataType.UInt32)
			.Parameter("@Severity", DataType.Byte)
			.Parameter("@Timestamp", DataType.DateTime);
		var count = Mapping.Commands.Add($"{commandName}.Count").Script(DuckDBDriver.NAME,
			"SELECT COUNT(*) FROM \"Log\" WHERE \"Target\"=$Target AND \"Action\"=$Action")
			.Parameter("@Target", DataType.AnsiString, 100)
			.Parameter("@Action", DataType.AnsiString, 100);
		var parameters = new Parameter[]
		{
			new("@UserId", 1U),
			new("@Target", target),
			new("@Action", action),
			new("@TenantId", 1U),
			new("@BranchId", 0U),
			new("@Severity", LogSeverity.Info),
			new("@Timestamp", DateTime.Now),
		};
		var criteria = new Parameter[]
		{
			new("@Target", target),
			new("@Action", action),
		};

		using(var transaction = new Transaction())
		{
			var affected = asynchronous ?
				await accessor.ExecuteAsync(insert.QualifiedName, parameters) :
				accessor.Execute(insert.QualifiedName, parameters);
			Assert.Equal(1, affected);

			var scalar = asynchronous ?
				await accessor.ExecuteScalarAsync(count.QualifiedName, criteria) :
				accessor.ExecuteScalar(count.QualifiedName, criteria);
			Assert.Equal(1L, Convert.ToInt64(scalar));
			Assert.NotNull(GetSession(transaction).Transaction);

			transaction.Rollback();
		}

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
				Assert.Same(outer.Context, middle.Context.Parent);
				await InsertLogAsync(accessor, target, "Nested.Middle");

				using(var inner = new Transaction())
				{
					Assert.Same(inner, Transaction.Current);
					Assert.Same(middle.Context, inner.Context.Parent);
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
	public async Task NestedRollbackMakesRootTransactionAbortOnlyAsync()
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		var target = GetTarget();

		using(var outer = new Transaction())
		{
			await InsertLogAsync(accessor, target, "Nested.Abort.Outer");

			using(var inner = new Transaction())
			{
				await InsertLogAsync(accessor, target, "Nested.Abort.Inner");
				inner.Rollback();
			}

			outer.Commit();
		}

		Assert.False(await ExistsLogAsync(accessor, target, "Nested.Abort.Outer"));
		Assert.False(await ExistsLogAsync(accessor, target, "Nested.Abort.Inner"));
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
			var session = GetSession(transaction);

			var logs = accessor.SelectAsync<Log>(
				Condition.Equal(nameof(Log.Target), target) &
				Condition.Equal(nameof(Log.Action), action));

			await using(var enumerator = logs.GetAsyncEnumerator())
			{
				Assert.True(await enumerator.MoveNextAsync());

				//提交操作在另一线程执行，并阻塞等待当前读取器释放
				var committing = Task.Run(transaction.Commit);

				//等待提交线程进入完成流程（真实提交被延迟）
				SpinWait.SpinUntil(() => session.IsCompleted, TimeSpan.FromSeconds(5));
				Assert.True(session.IsCompleted);

				//读取器未释放前，提交尚未完成（真实提交被延迟）
				Assert.False(committing.IsCompleted);

				//释放读取器后提交完成
				await enumerator.DisposeAsync();
				await committing;
				AssertSessionReleased(session);
			}
		}

		Assert.True(await ExistsLogAsync(accessor, target, action));
	}

	[Fact]
	public async Task CommitAsyncWaitsForOpenReaderToReleaseAsync()
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		var target = GetTarget();
		var action = "Reader.CommitAsync";

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

				//异步提交：不阻塞当前线程，但不会在真实提交完成前返回
				var committing = transaction.CommitAsync();
				Assert.False(committing.IsCompleted);

				//读取器未释放前，提交尚未完成（真实提交被延迟）
				Assert.False(committing.IsCompleted);

				//释放读取器后提交完成
				await enumerator.DisposeAsync();
				await committing;
				AssertSessionReleased(session);
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
			var connection = Assert.IsAssignableFrom<DbConnection>(session.Connection);
			var logs = accessor.SelectAsync<Log>(
				Condition.Equal(nameof(Log.Target), target) &
				Condition.Equal(nameof(Log.Action), action));

			await using(var enumerator = logs.GetAsyncEnumerator())
			{
				Assert.True(await enumerator.MoveNextAsync());

				var committing = Task.Run(transaction.Commit);
				SpinWait.SpinUntil(() => session.IsCompleted, TimeSpan.FromSeconds(5));

				//释放读取器后提交完成
				await enumerator.DisposeAsync();
				await committing;

				//提交完成后会话已释放，连接已关闭
				AssertSessionReleased(session);
				Assert.Equal(ConnectionState.Closed, connection.State);

				//提交后再处置会话不应影响已提交的结果
				session.Dispose();
			}

			AssertSessionReleased(session);
			Assert.Equal(ConnectionState.Closed, connection.State);
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

			await second.DisposeAsync();
			await first.DisposeAsync();

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
		var command = Mapping.Commands.Script(DuckDBDriver.NAME, "SELECT * FROM \"__Zongsoft_Missing_Table__\"");

		using(var transaction = new Transaction())
		{
			await InsertLogAsync(accessor, target, action);
			var session = GetSession(transaction);
			Assert.NotNull(session.Transaction);

			await using var enumerator = accessor.ExecuteAsync<Log>(command.QualifiedName).GetAsyncEnumerator();
			await Assert.ThrowsAnyAsync<DbException>(() => enumerator.MoveNextAsync().AsTask());

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

			transaction.Commit();
			AssertSessionReleased(session);
		}

		Assert.True(await ExistsLogAsync(accessor, target, action));
	}

	[Fact]
	public async Task SelectResultCancellationAfterFirstRowReleasesReaderAsync()
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		using var transaction = new Transaction();
		using var cancellation = new CancellationTokenSource();
		var enumerator = accessor.SelectAsync<UserModel>().GetAsyncEnumerator(cancellation.Token);

		Assert.True(await enumerator.MoveNextAsync());
		cancellation.Cancel();

		var exception = await Record.ExceptionAsync(() => enumerator.MoveNextAsync().AsTask());
		await enumerator.DisposeAsync();

		var session = GetSession(transaction);
		RollbackAndAssertSessionReleased(transaction, session);

		Assert.IsAssignableFrom<OperationCanceledException>(exception);
	}

	[Fact]
	public async Task ExecuteResultEarlyBreakReleasesReaderAsync()
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		var command = Mapping.Commands.Script(DuckDBDriver.NAME, "SELECT 1 UNION ALL SELECT 2");

		using var transaction = new Transaction();
		var results = accessor.ExecuteAsync<int>(command.QualifiedName);

		await foreach(var result in results)
		{
			Assert.Equal(1, result);
			break;
		}

		var session = GetSession(transaction);
		RollbackAndAssertSessionReleased(transaction, session);
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
	private static DataSession GetSession(Transaction transaction) => Assert.IsType<DataSession>(transaction.Context.Parameters["Zongsoft.Data:DataSession"]);
	private static void RollbackAndAssertSessionReleased(Transaction transaction, DataSession session)
	{
		transaction.Rollback();

		try
		{
			AssertSessionReleased(session);
		}
		finally
		{
			session.Connection?.Close();
		}
	}

	private static ValueTask<bool> ExistsLogAsync(DataAccess accessor, string target, string action) =>
		accessor.ExistsAsync<Log>(
			Condition.Equal(nameof(Log.Target), target) &
			Condition.Equal(nameof(Log.Action), action));

	private static void AssertSessionReleased(DataSession session)
	{
		AssertSessionCompleted(session);
		Assert.Null(session.Transaction);
		Assert.Null(session.Connection);
	}

	private static void AssertSessionCompleted(DataSession session)
	{
		Assert.True(session.IsCompleted);
		Assert.Throws<DataException>(() => session.AcquireLease());
	}

	void IDisposable.Dispose()
	{
		if(!Global.IsTestingEnabled)
			return;

		_database.Accessor.Execute("TruncateLog");
	}
}
