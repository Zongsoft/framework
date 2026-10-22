using System;
using System.Linq;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Data.Common;
using Zongsoft.Data.Tests.Models;

namespace Zongsoft.Data.MySql.Tests;

[Collection("Database")]
public class TransactionTest(DatabaseFixture database) : IAsyncDisposable
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

		await using(var transaction = new Transaction())
		{
			Assert.Same(transaction, Transaction.Current);

			await InsertLogAsync(accessor, target, action);
			Assert.True(await ExistsLogAsync(accessor, target, action));

			await transaction.CommitAsync();
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

		await using(var transaction = new Transaction())
		{
			Assert.Same(transaction, Transaction.Current);

			await InsertLogAsync(accessor, target, action);
			Assert.True(await ExistsLogAsync(accessor, target, action));

			await transaction.RollbackAsync();
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

		await using(var outer = new Transaction())
		{
			Assert.Same(outer, Transaction.Current);
			await InsertLogAsync(accessor, target, "Nested.Outer");

			await using(var middle = new Transaction())
			{
				Assert.Same(middle, Transaction.Current);
				Assert.Same(outer.Context, middle.Context.Parent);
				await InsertLogAsync(accessor, target, "Nested.Middle");

				await using(var inner = new Transaction())
				{
					Assert.Same(inner, Transaction.Current);
					Assert.Same(middle.Context, inner.Context.Parent);
					await InsertLogAsync(accessor, target, "Nested.Inner");

					await inner.CommitAsync();
				}

				Assert.Same(middle, Transaction.Current);
				await middle.CommitAsync();
			}

			Assert.Same(outer, Transaction.Current);
			await outer.CommitAsync();
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

		await using(var outer = new Transaction())
		{
			await InsertLogAsync(accessor, target, "Nested.Abort.Outer");

			await using(var inner = new Transaction())
			{
				await InsertLogAsync(accessor, target, "Nested.Abort.Inner");
				await inner.RollbackAsync();
			}

			await outer.CommitAsync();
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

		await using(var transaction = new Transaction())
		{
			await InsertLogAsync(accessor, target, "Multiple.First");
			await transaction.CommitAsync();
		}

		await using(var transaction = new Transaction())
		{
			await InsertLogAsync(accessor, target, "Multiple.Second");
			await transaction.RollbackAsync();
		}

		await using(var transaction = new Transaction())
		{
			await InsertLogAsync(accessor, target, "Multiple.Third");
			Assert.True(await ExistsLogAsync(accessor, target, "Multiple.Third"));
			await transaction.CommitAsync();
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
			await using(var transaction = new Transaction())
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
					await transaction.CommitAsync();
				else
					await transaction.RollbackAsync();
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

			await using(var transaction = new Transaction())
			{
				await InsertLogAsync(accessor, target, action);
				Assert.True(await ExistsLogAsync(accessor, target, action));

				if(index % 2 == 0)
					await transaction.CommitAsync();
				else
					await transaction.RollbackAsync();
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

		await using(var transaction = new Transaction())
		{
			await InsertLogAsync(accessor, target, action);
			var session = GetSession(transaction);

			var logs = accessor.SelectAsync<Log>(
				Condition.Equal(nameof(Log.Target), target) &
				Condition.Equal(nameof(Log.Action), action));

			await using(var enumerator = logs.GetAsyncEnumerator())
			{
				Assert.True(await enumerator.MoveNextAsync());

				//在另一线程开始异步提交，并等待读取器释放
				var committing = Task.Run(() => transaction.CommitAsync());

				//等待会话进入终结流程（真实提交被延迟）
				await WaitForCompletionAsync(session);
				Assert.True(session.IsCompleted);

				//读取器未释放前，提交尚未完成（真实提交被延迟）
				Assert.False(committing.IsCompleted);

				//释放读取器后提交完成
				await enumerator.DisposeAsync();
				await committing;
				await AssertSessionReleasedAsync(session);
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

		await using(var transaction = new Transaction())
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
				await AssertSessionReleasedAsync(session);
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

		await using(var transaction = new Transaction())
		{
			await InsertLogAsync(accessor, target, action);
			var session = GetSession(transaction);
			var logs = accessor.SelectAsync<Log>(
				Condition.Equal(nameof(Log.Target), target) &
				Condition.Equal(nameof(Log.Action), action));

			await using(var enumerator = logs.GetAsyncEnumerator())
			{
				Assert.True(await enumerator.MoveNextAsync());

				var committing = Task.Run(() => transaction.CommitAsync());
				await WaitForCompletionAsync(session);

				//释放读取器后提交完成
				await enumerator.DisposeAsync();
				await committing;

				await AssertSessionReleasedAsync(session);
				await session.DisposeAsync();
			}

			await AssertSessionReleasedAsync(session);
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

		await using(var transaction = new Transaction())
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

			await transaction.CommitAsync();
			await AssertSessionReleasedAsync(session);
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
		var command = Mapping.Commands.Script(MySqlDriver.NAME, "SELECT * FROM `__Zongsoft_Missing_Table__`");

		await using(var transaction = new Transaction())
		{
			await InsertLogAsync(accessor, target, action);
			var session = GetSession(transaction);
			Assert.NotNull(session.Transaction);

			await using var enumerator = accessor.ExecuteAsync<Log>(command.QualifiedName).GetAsyncEnumerator();
			await Assert.ThrowsAnyAsync<DbException>(() => enumerator.MoveNextAsync().AsTask());

			await transaction.CommitAsync();
			await AssertSessionReleasedAsync(session);
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

		await using(var transaction = new Transaction())
		{
			await InsertLogAsync(accessor, target, action);
			var session = GetSession(transaction);
			Assert.NotNull(session.Transaction);

			using var cancellation = new CancellationTokenSource();
			cancellation.Cancel();

			await using var enumerator = accessor.SelectAsync<UserModel>().GetAsyncEnumerator(cancellation.Token);
			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => enumerator.MoveNextAsync().AsTask());

			await transaction.CommitAsync();
			await AssertSessionReleasedAsync(session);
		}

		Assert.True(await ExistsLogAsync(accessor, target, action));
	}

	[Fact]
	public async Task SelectResultCancellationAfterFirstRowReleasesReaderAsync()
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		await using var transaction = new Transaction();
		using var cancellation = new CancellationTokenSource();
		await using var enumerator = accessor.SelectAsync<UserModel>().GetAsyncEnumerator(cancellation.Token);

		Assert.True(await enumerator.MoveNextAsync());
		cancellation.Cancel();

		var exception = await Record.ExceptionAsync(() => enumerator.MoveNextAsync().AsTask());
		await enumerator.DisposeAsync();

		var session = GetSession(transaction);
		await RollbackAndAssertSessionReleasedAsync(transaction, session);

		Assert.IsAssignableFrom<OperationCanceledException>(exception);
	}

	[Fact]
	public async Task ExecuteResultEarlyBreakReleasesReaderAsync()
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		var command = Mapping.Commands.Script(MySqlDriver.NAME, "SELECT 1 UNION ALL SELECT 2");

		await using var transaction = new Transaction();
		var results = accessor.ExecuteAsync<int>(command.QualifiedName);

		await foreach(var result in results)
		{
			Assert.Equal(1, result);
			break;
		}

		var session = GetSession(transaction);
		await RollbackAndAssertSessionReleasedAsync(transaction, session);
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

	private static async Task WaitForCompletionAsync(DataSession session)
	{
		using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));

		while(!session.IsCompleted)
			await Task.Delay(1, cancellation.Token);
	}

	private static string GetTarget() => $"{nameof(TransactionTest)}:{Guid.NewGuid():N}-{Environment.TickCount64:X}";
	private static DataSession GetSession(Transaction transaction) => Assert.IsType<DataSession>(transaction.Context.Parameters["Zongsoft.Data:DataSession"]);
	private static async Task RollbackAndAssertSessionReleasedAsync(Transaction transaction, DataSession session)
	{
		await transaction.RollbackAsync();

		try
		{
			await AssertSessionReleasedAsync(session);
		}
		finally
		{
			if(session.Connection != null)
				await Assert.IsAssignableFrom<DbConnection>(session.Connection).CloseAsync();
		}
	}

	private static ValueTask<bool> ExistsLogAsync(DataAccess accessor, string target, string action) =>
		accessor.ExistsAsync<Log>(
			Condition.Equal(nameof(Log.Target), target) &
			Condition.Equal(nameof(Log.Action), action));

	private static async Task AssertSessionReleasedAsync(DataSession session)
	{
		await AssertSessionCompletedAsync(session);
		Assert.Null(session.Transaction);
		Assert.Null(session.Connection);
	}

	private static async Task AssertSessionCompletedAsync(DataSession session)
	{
		Assert.True(session.IsCompleted);
		await Assert.ThrowsAsync<DataException>(() => session.AcquireLeaseAsync().AsTask());
	}

	async ValueTask IAsyncDisposable.DisposeAsync()
	{
		if(!Global.IsTestingEnabled)
			return;

		await _database.Accessor.ExecuteAsync("TruncateLog");
	}
}
