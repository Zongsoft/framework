using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Xunit;

using Zongsoft.Data.Common;
using Zongsoft.Data.Features;
using Zongsoft.Data.Metadata;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.Tests;

public class DataExecutorTest
{
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task AwaitParentBeforeExecutingSlaves(bool continued)
	{
		var executor = new RecordingExecutor();
		var context = new Context(null);
		var entity = new DataEntity(null, "Executor");
		var parent = new UpdateStatement(entity);
		var selection = new SelectStatement(entity);
		var deletion = new DeleteStatement(entity);
		parent.Slaves.Add(selection);
		selection.Slaves.Add(deletion);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		executor.Handler = async (_, statement, cancellation) =>
		{
			if(statement != parent)
				return true;

			await release.Task.WaitAsync(cancellation);
			return continued;
		};
		var operation = executor.ExecuteAsync(context, parent).AsTask();

		try
		{
			Assert.False(operation.IsCompleted);
			Assert.Same(parent, Assert.Single(executor.Statements));
		}
		finally { release.TrySetResult(); }

		await operation.WaitAsync(TimeSpan.FromSeconds(10));
		Assert.Equal(continued ? new IStatementBase[] { parent, selection, deletion } : [parent], executor.Statements);
	}

	[Fact]
	public async Task TypedExecutorCanCompleteItsOwnSlaves()
	{
		var executor = new RecordingExecutor();
		var context = new Context(null);
		var entity = new DataEntity(null, "Executor");
		var parent = new UpdateStatement(entity);
		var child = new DeleteStatement(entity);
		parent.Slaves.Add(child);
		executor.Handler = async (current, statement, cancellation) =>
		{
			if(statement == parent)
				await executor.ExecuteAsync(current, child, cancellation);

			return false;
		};
		await executor.ExecuteAsync(context, parent);
		Assert.Equal(new IStatementBase[] { parent, child }, executor.Statements);
	}

	[Fact]
	public async Task ConcurrentExecutionCreatesTypedExecutorOnce()
	{
		var executor = new RecordingExecutor();
		var context = new Context(null);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var operations = Enumerable.Range(0, 32).Select(_ => Task.Run(async () =>
		{
			await release.Task;
			await executor.ExecuteAsync(context, new SelectStatement());
		})).ToArray();
		release.SetResult();
		await Task.WhenAll(operations).WaitAsync(TimeSpan.FromSeconds(10));
		Assert.Equal(1, executor.Creations);
		Assert.Equal(32, executor.Statements.Count);
	}

	[Fact]
	public async Task ProviderDecorationUsesEachContextsDriver()
	{
		var first = new Driver();
		var second = new Driver();
		var provider = DataProvider.Create("Executor", new SourceProvider());
		var decorator = new Decorator(provider.Executor);
		provider.Executor = decorator;
		var firstContext = new Context(new DataConnectorTest.DataSourceMocker(first), provider);
		var secondContext = new Context(new DataConnectorTest.DataSourceMocker(second), provider);
		var statement = new UpdateStatement(new DataEntity(null, "Executor"));
		await provider.Executor.ExecuteAsync(firstContext, statement);
		await provider.Executor.ExecuteAsync(secondContext, statement);
		Assert.Equal(2, decorator.Count);
		Assert.Same(statement, Assert.Single(first.Recorder.Statements));
		Assert.Same(statement, Assert.Single(second.Recorder.Statements));
	}

	[Fact]
	public async Task CancellationPreventsExecutionAndStopsSlaves()
	{
		var executor = new RecordingExecutor();
		var context = new Context(null);
		using var cancelled = new CancellationTokenSource();
		cancelled.Cancel();
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => executor.ExecuteAsync(context, new SelectStatement(), cancelled.Token).AsTask());
		Assert.Equal(0, executor.Creations);

		using var cancellation = new CancellationTokenSource();
		var parent = new SelectStatement();
		parent.Slaves.Add(new SelectStatement());
		executor.Handler = (_, _, token) =>
		{
			Assert.Equal(cancellation.Token, token);
			cancellation.Cancel();
			return ValueTask.FromResult(true);
		};
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => executor.ExecuteAsync(context, parent, cancellation.Token).AsTask());
		Assert.Same(parent, Assert.Single(executor.Statements));
	}

	[Fact]
	public async Task FailureStopsSlavesAndPreservesException()
	{
		var failure = new InvalidOperationException();
		var executor = new RecordingExecutor { Handler = (_, _, _) => ValueTask.FromException<bool>(failure) };
		var statement = new SelectStatement();
		statement.Slaves.Add(new SelectStatement());
		Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => executor.ExecuteAsync(new Context(null), statement).AsTask()));
		Assert.Same(statement, Assert.Single(executor.Statements));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task ReturningRequiresDeclaredOperationCapabilities(bool supported)
	{
		var driver = new Driver();
		if(supported)
			driver.Features.Set<IReturningFeature>(ReturningFeature.Instance);

		var entity = new DataEntity(null, $"Executor{Guid.NewGuid():N}");
		entity.Properties.Simplex(nameof(Row.Value), DataType.Int32, false);
		Mapping.Entities.Add(entity);
		await using var access = new DataAccess("Executor");
		access.Provider.Multiplexer = new Multiplexer(new DataConnectorTest.DataSourceMocker(driver));

		try
		{
			var schema = SchemaParser.Instance.Parse(entity.Name, nameof(Row.Value), typeof(Row));
			var criteria = Condition.Equal(nameof(Row.Value), 1);
			using var update = new DataUpdateContext(access, entity.Name, new Row { Value = 2 }, criteria, schema,
				DataUpdateOptions.Return(ReturningKind.Older, nameof(Row.Value)).Build());
			await using var updateSession = update.Session;
			var updates = new List<IStatementBase>();

			await foreach(var statement in new UpdateStatementBuilder().BuildAsync(update))
				updates.Add(statement);

			Assert.Equal(supported, Assert.IsType<UpdateStatement>(Assert.Single(updates)).Returning != null);

			using var delete = new DataDeleteContext(access, entity.Name, criteria, schema, DataDeleteOptions.Return(nameof(Row.Value)).Build());
			await using var deleteSession = delete.Session;
			var deletions = new List<IStatementBase>();

			await foreach(var statement in new DeleteStatementBuilder().BuildAsync(delete))
				deletions.Add(statement);

			Assert.Equal(supported, Assert.IsType<DeleteStatement>(Assert.Single(deletions)).Returning != null);
			Assert.Empty(driver.Commands);
		}
		finally { Mapping.Entities.Remove(entity.Name); }
	}

	private sealed class Row
	{
		public int Value { get; set; }
	}

	private sealed class Driver : DataConnectorTest.DataDriverMocker, IDataExecutor<UpdateStatement>, IDataExecutor<DeleteStatement>
	{
		public RecordingExecutor Recorder => (RecordingExecutor)this.Executor;
		protected override IDataExecutor CreateExecutor() => new RecordingExecutor();
		ValueTask<bool> IDataExecutor<UpdateStatement>.ExecuteAsync(IDataAccessContext context, UpdateStatement statement, CancellationToken cancellation) => throw new InvalidOperationException();
		ValueTask<bool> IDataExecutor<DeleteStatement>.ExecuteAsync(IDataAccessContext context, DeleteStatement statement, CancellationToken cancellation) => throw new InvalidOperationException();
	}

	private sealed class RecordingExecutor : DataExecutor
	{
		private int _creations;
		public int Creations => Volatile.Read(ref _creations);
		public ConcurrentQueue<IStatementBase> Statements { get; } = new();
		public Func<IDataAccessContext, IStatementBase, CancellationToken, ValueTask<bool>> Handler { get; set; }
		protected override IDataExecutor<SelectStatement> CreateSelectExecutor() => this.Create<SelectStatement>();
		protected override IDataExecutor<UpdateStatement> CreateUpdateExecutor() => this.Create<UpdateStatement>();
		protected override IDataExecutor<DeleteStatement> CreateDeleteExecutor() => this.Create<DeleteStatement>();
		private IDataExecutor<TStatement> Create<TStatement>() where TStatement : IStatementBase
		{
			Interlocked.Increment(ref _creations);
			return new TypedExecutor<TStatement>(this);
		}
	}

	private sealed class TypedExecutor<TStatement>(RecordingExecutor owner) : IDataExecutor<TStatement> where TStatement : IStatementBase
	{
		public ValueTask<bool> ExecuteAsync(IDataAccessContext context, TStatement statement, CancellationToken cancellation)
		{
			owner.Statements.Enqueue(statement);
			return owner.Handler?.Invoke(context, statement, cancellation) ?? ValueTask.FromResult(true);
		}
	}

	private sealed class Decorator(IDataExecutor inner) : IDataExecutor
	{
		public int Count { get; private set; }
		public ValueTask ExecuteAsync(IDataAccessContext context, IStatementBase statement, CancellationToken cancellation = default)
		{
			this.Count++;
			return inner.ExecuteAsync(context, statement, cancellation);
		}
	}

	private sealed class Context(IDataSource source, IDataProvider provider = null) : IDataAccessContext
	{
		public string Name => "Executor";
		public DataAccessMethod Method => DataAccessMethod.Update;
		public IDataAccess DataAccess => null;
		public ClaimsPrincipal Principal => null;
		public IDataSource Source => source;
		public IDataProvider Provider => provider;
		public DataSession Session => null;
		public TFeature GetFeature<TFeature>() => default;
	}

	private sealed class SourceProvider : IDataSourceProvider
	{
		public IEnumerable<IDataSource> GetSources(string name) => [];
	}

	private sealed class Multiplexer(IDataSource source) : IDataMultiplexer
	{
		public IDataSourceProvider Provider => throw new NotSupportedException();
		public IDataSourceSelector Selector => throw new NotSupportedException();
		public IDataSource GetSource(IDataAccessContextBase context) => source;
	}
}
