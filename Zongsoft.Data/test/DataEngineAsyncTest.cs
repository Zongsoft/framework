using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Xunit;

using Zongsoft.Data.Common;
using Zongsoft.Data.Common.Expressions;
using Zongsoft.Data.Metadata;

namespace Zongsoft.Data.Tests;

[Collection(nameof(DataConnectorCollection))]
public sealed class DataEngineAsyncTest
{
	private static TimeSpan Timeout => TimeSpan.FromSeconds(5);

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task BatchAsync_ConsumesWholeSourceInOneSessionAndCommitsOnce(bool upsert)
	{
		await using var scope = new Scope();
		using var cancellation = new CancellationTokenSource();
		var source = new Input<Row>(Enumerable.Range(1, 513).Select(value => new Row { Value = value }).ToArray(), 512);
		var commit = new Gate();
		var executed = new Gate();
		var calls = new List<string>();
		IDataAccessContext captured = null;
		scope.Driver.Commit = async token =>
		{
			calls.Add("commit");
			await commit.WaitAsync(token);
		};
		scope.Provider.Executing += (_, args, token) =>
		{
			Assert.Equal(cancellation.Token, token);
			Assert.Same(source, ((IDataMutateContextBase)args.Context).Data);
			captured = args.Context;
			calls.Add("executing");
			return ValueTask.CompletedTask;
		};
		scope.Provider.Executed += async (_, args, token) =>
		{
			Assert.Same(captured, args.Context);
			Assert.Same(source, ((IDataMutateContextBase)args.Context).Data);
			calls.Add("executed");
			await executed.WaitAsync(token);
		};
		scope.Driver.Batch = (context, batch, token) =>
		{
			Assert.Same(captured, context);
			Assert.Equal(cancellation.Token, token);
			Assert.Equal(typeof(Row), context is DataInsertContext insertion ? insertion.ModelType : ((DataUpsertContext)context).ModelType);
			calls.Add($"batch:{batch.Count}");
		};
		var operation = upsert ?
			scope.Access.UpsertManyAsync(scope.Name, source, (ISchema)null, null, cancellation.Token).AsTask() :
			scope.Access.InsertManyAsync(scope.Name, source, (ISchema)null, null, cancellation.Token).AsTask();

		try
		{
			await source.Paused.Task.WaitAsync(Timeout);
			Assert.False(operation.IsCompleted);
			Assert.Equal(["executing", "batch:256", "batch:256"], calls);
			Assert.Equal(cancellation.Token, source.Token);
			var connection = Assert.Single(scope.Driver.Connections);
			Assert.Equal(1, connection.BeginCount);
			Assert.Equal(0, connection.Transaction.CommitCount);
			Assert.Equal(0, connection.DisposeCount);
			source.Release.TrySetResult();
			await commit.Entered.Task.WaitAsync(Timeout);
			Assert.False(operation.IsCompleted);
			Assert.Equal(["executing", "batch:256", "batch:256", "batch:1", "commit"], calls);
			commit.Release.TrySetResult();
			await executed.Entered.Task.WaitAsync(Timeout);
			Assert.False(operation.IsCompleted);
			executed.Release.TrySetResult();
			Assert.Equal(513, await operation.WaitAsync(Timeout));
			Assert.Equal(Enumerable.Range(1, 513), scope.Driver.Rows.Select(row => ((Row)row).Value));
			Assert.Equal(1, source.Enumerations);
			Assert.Equal(1, source.Disposals);
			Assert.Equal(1, connection.Transaction.CommitCount);
			Assert.Equal(0, connection.Transaction.RollbackCount);
			Assert.Equal(1, connection.Transaction.DisposeCount);
			Assert.Equal(1, connection.DisposeCount);
			Assert.Equal(3, scope.Driver.Commands.Length);
			Assert.All(scope.Driver.Commands, command => Assert.Equal(1, command.DisposeCount));
			Assert.Equal(["executing", "batch:256", "batch:256", "batch:1", "commit", "executed"], calls);
		}
		finally
		{
			source.Release.TrySetResult();
			commit.Release.TrySetResult();
			executed.Release.TrySetResult();
		}
	}

	[Fact]
	public async Task BatchAsync_CancelledInputDisposesSourceAndRollsBackWithoutCompletionEvent()
	{
		await using var scope = new Scope();
		using var cancellation = new CancellationTokenSource();
		var source = new Input<Row>(Enumerable.Range(1, 300).Select(value => new Row { Value = value }).ToArray(), 256);
		var completed = 0;
		var errors = 0;
		scope.Provider.Executed += (_, _, _) => { completed++; return ValueTask.CompletedTask; };
		scope.Provider.Error += (_, args, token) =>
		{
			Assert.Equal(CancellationToken.None, token);
			Assert.IsAssignableFrom<OperationCanceledException>(args.Exception);
			errors++;
			return ValueTask.CompletedTask;
		};
		var operation = scope.Access.InsertManyAsync(scope.Name, source, (ISchema)null, null, cancellation.Token).AsTask();

		try
		{
			await source.Paused.Task.WaitAsync(Timeout);
			cancellation.Cancel();
			var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation.WaitAsync(Timeout));
			Assert.Equal(cancellation.Token, error.CancellationToken);
			Assert.Equal(256, scope.Driver.Rows.Count);
			Assert.Equal(1, source.Disposals);
			Assert.Equal(0, completed);
			Assert.Equal(1, errors);
			var connection = Assert.Single(scope.Driver.Connections);
			Assert.Equal(0, connection.Transaction.CommitCount);
			Assert.Equal(1, connection.Transaction.RollbackCount);
			Assert.Equal(1, connection.DisposeCount);
			Assert.Equal(CancellationToken.None, scope.Driver.ErrorToken);
		}
		finally { source.Release.TrySetResult(); }
	}

	[Fact]
	public async Task ImportAsync_StructSourcePreservesElementTypeAndAwaitsConsumption()
	{
		await using var scope = new Scope();
		using var cancellation = new CancellationTokenSource();
		var source = new Input<Row>([new() { Value = 17 }, new() { Value = 23 }], 1);
		var imported = 0;
		scope.Access.Imported += (_, args, token) =>
		{
			Assert.Same(source, args.Context.Data);
			Assert.Equal(typeof(Row), args.Context.ModelType);
			Assert.Equal(cancellation.Token, token);
			imported++;
			return ValueTask.CompletedTask;
		};
		var operation = scope.Access.ImportAsync(scope.Name, source, [nameof(Row.Value)], null, cancellation.Token).AsTask();

		try
		{
			await source.Paused.Task.WaitAsync(Timeout);
			Assert.False(operation.IsCompleted);
			Assert.Equal([17], scope.Driver.Rows.Cast<int>());
			Assert.Equal(0, imported);
			source.Release.TrySetResult();
			Assert.Equal(2, await operation.WaitAsync(Timeout));
			Assert.Equal([17, 23], scope.Driver.Rows.Cast<int>());
			Assert.Equal(cancellation.Token, source.Token);
			Assert.Equal(1, source.Disposals);
			Assert.Equal(1, imported);
			Assert.Equal(1, Assert.Single(scope.Driver.Connections).Transaction.CommitCount);
		}
		finally { source.Release.TrySetResult(); }
	}

	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(2)]
	[InlineData(3)]
	public async Task SelectAsync_DelaysCommandsAndAwaitsReaderThenCommandCleanup(int exit)
	{
		await using var scope = new Scope();
		using var cancellation = new CancellationTokenSource();
		using var context = new DataSelectContext(scope.Access, scope.Name, typeof(int), null, null, null, exit == 3 ? new Paging(1, 20) : null, null);
		await using var session = context.Session;
		var executor = new DataSelectExecutor();
		await executor.ExecuteAsync(context, new SelectStatement(scope.Entity), cancellation.Token);
		var result = Assert.IsAssignableFrom<IAsyncEnumerable<int>>(context.Result);
		Assert.Empty(scope.Driver.Commands);
		var readCleanup = new Gate();
		var commandCleanup = new Gate();
		var failure = new InvalidOperationException();
		var paginated = new Gate();
		scope.Driver.ReaderDispose = () => readCleanup.WaitAsync(CancellationToken.None).AsTask();
		scope.Driver.CommandDispose = () => commandCleanup.WaitAsync(CancellationToken.None).AsTask();

		if(exit == 2)
			scope.Driver.Read = _ => throw failure;

		if(exit == 3)
		{
			Assert.IsAssignableFrom<IPageable>(result).Paginated += async (_, args, token) =>
			{
				Assert.Equal(1, args.Paging.Total);
				Assert.Equal(cancellation.Token, token);
				await paginated.WaitAsync(token);
				throw failure;
			};
		}

		var values = new List<int>();
		var operation = ConsumeAsync();

		try
		{
			if(exit == 3)
			{
				await paginated.Entered.Task.WaitAsync(Timeout);
				Assert.False(operation.IsCompleted);
				Assert.Equal(0, Assert.Single(scope.Driver.Commands).Reader.DisposeCount);
				paginated.Release.TrySetResult();
			}

			await readCleanup.Entered.Task.WaitAsync(Timeout);
			Assert.False(operation.IsCompleted);
			var command = Assert.Single(scope.Driver.Commands);
			Assert.Equal(0, command.DisposeCount);
			readCleanup.Release.TrySetResult();
			await commandCleanup.Entered.Task.WaitAsync(Timeout);
			Assert.False(operation.IsCompleted);
			commandCleanup.Release.TrySetResult();

			if(exit >= 2)
				Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => operation.WaitAsync(Timeout)));
			else
			{
				await operation.WaitAsync(Timeout);
				Assert.Equal([1], values);
				values.Clear();
				await ConsumeAsync().WaitAsync(Timeout);
				Assert.Equal([1], values);
				Assert.Equal(2, scope.Driver.Commands.Length);
			}

			Assert.All(scope.Driver.Commands, item =>
			{
				Assert.Equal(1, item.DisposeCount);
				Assert.Equal(1, item.Reader.DisposeCount);
			});
		}
		finally
		{
			paginated.Release.TrySetResult();
			readCleanup.Release.TrySetResult();
			commandCleanup.Release.TrySetResult();
		}

		async Task ConsumeAsync()
		{
			await foreach(var item in result.WithCancellation(cancellation.Token))
			{
				values.Add(item);
				if(exit == 1)
					break;
			}
		}
	}

	[Fact]
	public async Task SelectAsync_CancelledAfterFirstRowRejectsBufferedRowsAndReleasesResources()
	{
		await using var scope = new Scope();
		using var context = new DataSelectContext(scope.Access, scope.Name, typeof(int), null, null, null, null, null);
		await using var session = context.Session;
		await new DataSelectExecutor().ExecuteAsync(context, new SelectStatement(scope.Entity), CancellationToken.None);
		await AssertBufferedResultCancellationAsync(scope, Assert.IsAssignableFrom<IAsyncEnumerable<int>>(context.Result));
	}

	[Fact]
	public async Task ExecuteAsync_CancelledAfterFirstRowRejectsBufferedRowsAndReleasesResources()
	{
		await using var scope = new Scope();
		var metadata = new DataCommand(null, scope.Name);
		Mapping.Commands.Add(metadata);

		try
		{
			using var context = new DataExecuteContext(scope.Access, metadata.Name, false, typeof(int), null);
			await using var session = context.Session;
			await new DataExecuteExecutor().ExecuteAsync(context, new ExecutionStatement(metadata), CancellationToken.None);
			await AssertBufferedResultCancellationAsync(scope, Assert.IsAssignableFrom<IAsyncEnumerable<int>>(context.Result));
		}
		finally { Mapping.Commands.Remove(metadata.QualifiedName); }
	}

	private static async Task AssertBufferedResultCancellationAsync(Scope scope, IAsyncEnumerable<int> result)
	{
		using var cancellation = new CancellationTokenSource();
		var reads = 0;
		var readerCleanup = new Gate();
		var commandCleanup = new Gate();
		scope.Driver.ReaderDispose = () => readerCleanup.WaitAsync(CancellationToken.None).AsTask();
		scope.Driver.CommandDispose = () => commandCleanup.WaitAsync(CancellationToken.None).AsTask();
		scope.Driver.Read = token =>
		{
			Assert.Equal(cancellation.Token, token);
			// A provider may return already buffered rows even when the token is cancelled.
			return Task.FromResult(++reads <= 2);
		};
		await using var iterator = result.GetAsyncEnumerator(cancellation.Token);
		Assert.True(await iterator.MoveNextAsync());
		Assert.Equal(1, iterator.Current);
		cancellation.Cancel();
		var operation = iterator.MoveNextAsync().AsTask();

		try
		{
			Assert.False(operation.IsCompleted);
			await readerCleanup.Entered.Task.WaitAsync(Timeout);
			Assert.False(operation.IsCompleted);
			var command = Assert.Single(scope.Driver.Commands);
			Assert.Equal(0, command.DisposeCount);
			readerCleanup.Release.TrySetResult();
			await commandCleanup.Entered.Task.WaitAsync(Timeout);
			Assert.False(operation.IsCompleted);
			commandCleanup.Release.TrySetResult();
			var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation.WaitAsync(Timeout));
			Assert.Equal(cancellation.Token, error.CancellationToken);
			Assert.Equal(1, reads);
			Assert.Equal(1, command.Reader.DisposeCount);
			Assert.Equal(1, command.DisposeCount);
		}
		finally
		{
			readerCleanup.Release.TrySetResult();
			commandCleanup.Release.TrySetResult();
			await Record.ExceptionAsync(() => operation.WaitAsync(Timeout));
		}
	}

	[Fact]
	public async Task ImportAsync_ReferenceSourcePreservesModelAndRecordIdentity()
	{
		await using var scope = new Scope();
		var rows = new[] { new Parent { Value = 17 }, new Parent { Value = 23 } };
		var source = new Input<Parent>(rows, -1);
		scope.Access.Imported += (_, args, _) =>
		{
			Assert.Equal(typeof(Parent), args.Context.ModelType);
			Assert.Same(source, args.Context.Data);
			return ValueTask.CompletedTask;
		};
		Assert.Equal(2, await scope.Access.ImportAsync(scope.Name, source, [nameof(Parent.Value)], null, CancellationToken.None));
		Assert.Equal([17, 23], scope.Driver.Rows.Cast<int>());
		Assert.Collection(scope.Driver.ImportTargets,
			target => Assert.Same(rows[0], target),
			target => Assert.Same(rows[1], target));
		Assert.Equal(1, source.Enumerations);
		Assert.Equal(1, source.Disposals);
	}

	[Fact]
	public async Task ImportAsync_GeneratedSequenceReturnsUpdatedStructTarget()
	{
		await using var scope = new Scope();
		var property = (IDataEntitySimplexProperty)scope.Entity.Properties[nameof(Row.Value)];
		property.Sequence = DataEntityPropertySequence.Create(property, new DataPropertySequence("#", 101, 7));
		scope.Access.Sequencer.Sequence = new SequenceMocker();
		var rows = new[] { new Row { Value = 0 } };
		var source = new Input<Row>(rows, -1);
		Assert.Equal(1, await scope.Access.ImportAsync(scope.Name, source, [nameof(Row.Value)], null, CancellationToken.None));
		Assert.Equal(101, Assert.IsType<int>(Assert.Single(scope.Driver.Rows)));
		Assert.Equal(101, Assert.IsType<Row>(Assert.Single(scope.Driver.ImportTargets)).Value);
		Assert.Equal(0, rows[0].Value);
	}

	private struct Row
	{
		public int Value;
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task UpdateManyAsync_StreamsRecordsAndCommitsOnlyAfterInputCompletes(bool cancel)
	{
		await using var scope = new Scope();
		using var cancellation = new CancellationTokenSource();
		var source = new Input<Row>([new() { Value = 17 }, new() { Value = 23 }], 1);
		var operation = scope.Access.UpdateManyAsync(scope.Name, source, cancellation.Token).AsTask();

		try
		{
			await source.Paused.Task.WaitAsync(Timeout);
			Assert.False(operation.IsCompleted);
			Assert.Equal(17, Assert.IsType<Row>(Assert.Single(scope.Driver.Rows)).Value);
			var connection = Assert.Single(scope.Driver.Connections);
			Assert.Equal(0, connection.Transaction.CommitCount);

			if(cancel)
			{
				cancellation.Cancel();
				var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation.WaitAsync(Timeout));
				Assert.Equal(cancellation.Token, error.CancellationToken);
				Assert.Equal(0, connection.Transaction.CommitCount);
				Assert.Equal(1, connection.Transaction.RollbackCount);
				Assert.Single(scope.Driver.Rows);
			}
			else
			{
				source.Release.TrySetResult();
				Assert.Equal(2, await operation.WaitAsync(Timeout));
				Assert.Equal([17, 23], scope.Driver.Rows.Cast<Row>().Select(row => row.Value));
				Assert.Equal(1, connection.Transaction.CommitCount);
				Assert.Equal(0, connection.Transaction.RollbackCount);
			}

			Assert.Equal(1, connection.BeginCount);
			Assert.Equal(1, connection.Transaction.DisposeCount);
			Assert.Equal(1, connection.DisposeCount);
			Assert.Equal(cancellation.Token, source.Token);
			Assert.Equal(1, source.Disposals);
		}
		finally { source.Release.TrySetResult(); }
	}

	[Fact]
	public async Task ProviderAsync_FailureAwaitsRollbackAndErrorNotifications()
	{
		await using var scope = new Scope();
		using var cancellation = new CancellationTokenSource();
		var failure = new InvalidOperationException();
		var rollback = new Gate();
		var driverError = new Gate();
		var observerError = new Gate();
		var calls = new List<string>();
		var source = new Input<Row>([new() { Value = 17 }], -1);
		scope.Driver.Execute = _ => throw failure;
		scope.Driver.Rollback = async token =>
		{
			Assert.Equal(CancellationToken.None, token);
			calls.Add("rollback");
			await rollback.WaitAsync(token);
		};
		scope.Driver.Error = async (_, error, token) =>
		{
			Assert.Same(failure, error);
			Assert.Equal(CancellationToken.None, token);
			calls.Add("driver-error");
			await driverError.WaitAsync(token);
			return error;
		};
		scope.Provider.Error += async (_, args, token) =>
		{
			Assert.Same(failure, args.Exception);
			Assert.Equal(CancellationToken.None, token);
			calls.Add("observer-error");
			await observerError.WaitAsync(token);
		};
		scope.Provider.Executed += (_, _, _) => throw new Xunit.Sdk.XunitException("Failed execution must not publish completion.");
		var operation = scope.Access.InsertManyAsync(scope.Name, source, (ISchema)null, null, cancellation.Token).AsTask();

		try
		{
			await rollback.Entered.Task.WaitAsync(Timeout);
			Assert.False(operation.IsCompleted);
			Assert.Equal(["rollback"], calls);
			Assert.Equal(1, Assert.Single(scope.Driver.Commands).DisposeCount);
			rollback.Release.TrySetResult();
			await driverError.Entered.Task.WaitAsync(Timeout);
			Assert.False(operation.IsCompleted);
			Assert.Equal(["rollback", "driver-error"], calls);
			driverError.Release.TrySetResult();
			await observerError.Entered.Task.WaitAsync(Timeout);
			Assert.False(operation.IsCompleted);
			observerError.Release.TrySetResult();
			Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => operation.WaitAsync(Timeout)));
			Assert.Equal(["rollback", "driver-error", "observer-error"], calls);
			Assert.Equal(1, source.Disposals);
		}
		finally
		{
			rollback.Release.TrySetResult();
			driverError.Release.TrySetResult();
			observerError.Release.TrySetResult();
		}
	}

	[Fact]
	public async Task ProviderAsync_CompletionFailureDoesNotRollBackCommittedData()
	{
		await using var scope = new Scope();
		var failure = new InvalidOperationException();
		var errorNotified = false;
		scope.Provider.Error += (_, _, _) => { errorNotified = true; return ValueTask.CompletedTask; };
		scope.Provider.Executed += (_, _, _) => throw failure;
		var source = new Input<Row>([new() { Value = 17 }], -1);
		var operation = scope.Access.InsertManyAsync(scope.Name, source, (ISchema)null, null, CancellationToken.None).AsTask();
		Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => operation.WaitAsync(Timeout)));
		var connection = Assert.Single(scope.Driver.Connections);
		Assert.Equal(1, connection.Transaction.CommitCount);
		Assert.Equal(0, connection.Transaction.RollbackCount);
		Assert.Equal(1, connection.DisposeCount);
		Assert.False(errorNotified);
	}

	[Fact]
	public async Task SelectAsync_AssociatedReadFailureDisposesBothReadersAndCommands()
	{
		await using var scope = new Scope();
		var child = new DataEntity(null, $"Child{Guid.NewGuid():N}");
		child.Properties.Simplex(nameof(Parent.Value), DataType.Int32, false);
		Mapping.Entities.Add(child);
		scope.Entity.Properties.Complex(nameof(Parent.Children), child.Name, false, DataAssociationMultiplicity.Many);
		var failure = new InvalidOperationException();
		var entered = new Gate();
		var reads = 0;
		scope.Driver.Read = async token =>
		{
			if(Interlocked.Increment(ref reads) == 1)
				return true;
			await entered.WaitAsync(token);
			throw failure;
		};

		try
		{
			var schema = SchemaParser.Instance.Parse(scope.Name, "Value,Children{Value}", typeof(Parent));
			using var context = new DataSelectContext(scope.Access, scope.Name, typeof(Parent), null, null, schema, null, null);
			await using var session = context.Session;
			var statement = new SelectStatement(scope.Entity);
			statement.Slaves.Add(new SelectStatement(child, nameof(Parent.Children)));
			await new DataSelectExecutor().ExecuteAsync(context, statement, CancellationToken.None);
			var result = Assert.IsAssignableFrom<IAsyncEnumerable<Parent>>(context.Result);
			await using var iterator = result.GetAsyncEnumerator();
			var operation = iterator.MoveNextAsync().AsTask();

			try
			{
				await entered.Entered.Task.WaitAsync(Timeout);
				Assert.False(operation.IsCompleted);
				Assert.Equal(2, scope.Driver.Commands.Length);
				Assert.All(scope.Driver.Commands, command => Assert.Equal(0, command.Reader.DisposeCount));
			}
			finally { entered.Release.TrySetResult(); }

			Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => operation.WaitAsync(Timeout)));
			Assert.All(scope.Driver.Commands, command =>
			{
				Assert.Equal(1, command.Reader.DisposeCount);
				Assert.Equal(1, command.DisposeCount);
			});
		}
		finally
		{
			entered.Release.TrySetResult();
			Mapping.Entities.Remove(child.Name);
		}
	}

	public sealed class Parent
	{
		public int Value { get; set; }
		public List<Parent> Children { get; set; }
	}

	private sealed class Scope : IAsyncDisposable
	{
		public Scope()
		{
			this.Entity = new DataEntity(null, $"Engine{Guid.NewGuid():N}");
			this.Entity.Properties.Simplex(nameof(Row.Value), DataType.Int32, false);
			Mapping.Entities.Add(this.Entity);
			this.Access = new DataAccess("Engine");
			this.Provider = (DataProvider)this.Access.Provider;
			this.Provider.Multiplexer = new Multiplexer(new DataConnectorTest.DataSourceMocker(this.Driver));
			this.Provider.Executor = new Executor(this.Driver);
		}
		public string Name => this.Entity.Name;
		public DataEntity Entity { get; }
		public DataAccess Access { get; }
		public DataProvider Provider { get; }
		public Driver Driver { get; } = new();
		public async ValueTask DisposeAsync()
		{
			try { await this.Access.DisposeAsync(); }
			finally { Mapping.Entities.Remove(this.Entity.Name); }
		}
	}

	private sealed class Multiplexer(IDataSource source) : IDataMultiplexer
	{
		public IDataSourceProvider Provider => throw new NotSupportedException();
		public IDataSourceSelector Selector => throw new NotSupportedException();
		public IDataSource GetSource(IDataAccessContextBase context) => source;
	}

	private sealed class Driver : DataConnectorTest.DataDriverMocker
	{
		public List<object> Rows { get; } = [];
		public List<object> ImportTargets { get; } = [];
		public Action<IDataAccessContext, IList, CancellationToken> Batch { get; set; }
		public CancellationToken ErrorToken { get; private set; }
		public Func<IDataAccessContext, Exception, CancellationToken, ValueTask<Exception>> Error { get; set; }
		public override IStatementBuilder Builder => new Builder(this);
		protected override IDataImporter CreateImporter() => new Importer();
		public override ValueTask<Exception> OnErrorAsync(IDataAccessContext context, Exception exception, CancellationToken cancellation = default)
		{
			this.ErrorToken = cancellation;
			return this.Error == null ? ValueTask.FromResult(exception) : this.Error(context, exception, cancellation);
		}
	}

	private sealed class Builder(Driver driver) : IStatementBuilder
	{
		public async IAsyncEnumerable<IStatementBase> BuildAsync(IDataAccessContext context, [EnumeratorCancellation] CancellationToken cancellation = default)
		{
			cancellation.ThrowIfCancellationRequested();
			var batch = context is DataUpdateContext update ? new object[] { update.Data } : Assert.IsAssignableFrom<IList>(((IDataMutateContextBase)context).Data);
			driver.Batch?.Invoke(context, batch, cancellation);
			driver.Rows.AddRange(batch.Cast<object>());
			yield return new Statement();
			await Task.CompletedTask;
		}
	}

	private sealed class Statement : StatementBase { }

	private sealed class Executor(Driver driver) : IDataExecutor
	{
		public async ValueTask ExecuteAsync(IDataAccessContext context, IStatementBase statement, CancellationToken cancellation = default)
		{
			await using var command = context.Session.Build(context, statement);
			await command.ExecuteNonQueryAsync(cancellation);
			var count = context is DataUpdateContext ? 1 : ((IList)((IDataMutateContextBase)context).Data).Count;

			if(context is DataInsertContext insertion)
				insertion.Count += count;
			else if(context is DataUpsertContext upsertion)
				upsertion.Count += count;
			else if(context is DataUpdateContext update)
				update.Count += count;
			Assert.Single(driver.Connections);
		}
	}

	private sealed class Importer : DataImporterBase
	{
		protected override async ValueTask OnImportAsync(DataImportContext context, MemberCollection members, CancellationToken cancellation = default)
		{
			var driver = Assert.IsType<Driver>(context.Source.Driver);
			await using var command = context.Session.Build(context, new Statement());
			await command.ExecuteNonQueryAsync(cancellation);
			var member = Assert.Single(members);

			await foreach(var row in GetRecordsAsync(context).WithCancellation(cancellation))
			{
				var result = await member.GetValueAsync(row, cancellation);
				Assert.IsType(context.ModelType, result.Target);
				driver.ImportTargets.Add(result.Target);
				driver.Rows.Add(result.Value);
				context.Count++;
			}
		}
	}

	private sealed class Gate
	{
		public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public async ValueTask WaitAsync(CancellationToken cancellation)
		{
			this.Entered.TrySetResult();
			await this.Release.Task.WaitAsync(cancellation);
		}
	}

	private sealed class Input<T>(T[] items, int pause) : IAsyncEnumerable<T>
	{
		public TaskCompletionSource Paused { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public int Enumerations { get; private set; }
		public int Disposals { get; private set; }
		public CancellationToken Token { get; private set; }
		public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellation = default)
		{
			this.Enumerations++;
			this.Token = cancellation;

			try
			{
				for(var index = 0; index < items.Length; index++)
				{
					if(index == pause)
					{
						this.Paused.TrySetResult();
						await this.Release.Task.WaitAsync(cancellation);
					}

					cancellation.ThrowIfCancellationRequested();
					yield return items[index];
				}
			}
			finally { this.Disposals++; }
		}
	}
}
