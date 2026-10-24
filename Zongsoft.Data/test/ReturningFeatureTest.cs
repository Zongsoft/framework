using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Data.Common;
using Zongsoft.Data.Features;
using Zongsoft.Data.Metadata;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.Tests;

public class ReturningFeatureTest
{
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task DefaultExecutionAwaitsWriteAndPreservesResult(bool continued)
	{
		await using var fixture = new Fixture();
		var statement = new UpdateStatement(fixture.Context.Entity);
		var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var count = 0;
		using var cancellation = new CancellationTokenSource();
		var operation = ReturningFeature.Instance.ExecuteAsync(fixture.Context, statement,
			token =>
			{
				Assert.Equal(cancellation.Token, token);
				Interlocked.Increment(ref count);
				return new ValueTask<bool>(release.Task);
			}, (_, _) => throw new InvalidOperationException(), cancellation.Token);

		Assert.False(operation.IsCompleted);
		Assert.Equal(1, count);

		release.SetResult(continued);

		Assert.Equal(continued, await operation);
		Assert.Equal(1, count);
		Assert.Empty(fixture.Driver.Commands);
	}

	[Fact]
	public async Task CancellationAndFailureDoNotRetryWrite()
	{
		await using var fixture = new Fixture();
		var statement = new UpdateStatement(fixture.Context.Entity);
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ReturningFeature.Instance.ExecuteAsync(fixture.Context, statement,
			_ => throw new InvalidOperationException(), (_, _) => throw new InvalidOperationException(), cancellation.Token).AsTask());

		var failure = new InvalidOperationException();
		var count = 0;
		var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => ReturningFeature.Instance.ExecuteAsync(fixture.Context, statement,
			_ => { count++; return ValueTask.FromException<bool>(failure); },
			(_, _) => throw new InvalidOperationException()).AsTask());

		Assert.Same(failure, exception);
		Assert.Equal(1, count);
	}

	[Theory]
	[InlineData(DataAccessMethod.Insert)]
	[InlineData(DataAccessMethod.Update)]
	[InlineData(DataAccessMethod.Upsert)]
	[InlineData(DataAccessMethod.Delete)]
	public async Task SourceFeatureInterceptsActualStatementWithoutFallback(DataAccessMethod method)
	{
		await using var fixture = new Fixture();
		var feature = new InterceptingFeature();
		fixture.Driver.Features.Set<IReturningFeature>(ReturningFeature.Instance);
		fixture.Source.Features.Set<IReturningFeature>(feature);
		IMutateStatement statement = method switch
		{
			DataAccessMethod.Insert => new InsertStatement(fixture.Context.Entity, null),
			DataAccessMethod.Upsert => new UpsertStatement(fixture.Context.Entity, null),
			DataAccessMethod.Delete => new DeleteStatement(fixture.Context.Entity),
			_ => new UpdateStatement(fixture.Context.Entity),
		};
		statement.Returning = new();
		var result = statement switch
		{
			InsertStatement insertion => await new DataInsertExecutor().ExecuteAsync(fixture.Context, insertion, default),
			UpsertStatement upsertion => await new DataUpsertExecutor().ExecuteAsync(fixture.Context, upsertion, default),
			DeleteStatement deletion => await new DataDeleteExecutor().ExecuteAsync(fixture.Context, deletion, default),
			_ => await new DataUpdateExecutor().ExecuteAsync(fixture.Context, (UpdateStatement)statement, default),
		};

		Assert.False(result);
		Assert.Same(statement, feature.Statement);
		Assert.Equal(1, feature.Count);
		Assert.Equal(DataAccessMethod.Update, fixture.Context.Method);
		Assert.Empty(fixture.Driver.Commands);
	}

	[Fact]
	public async Task OrdinaryMutationBypassesReturningFeature()
	{
		await using var fixture = new Fixture();
		var feature = new InterceptingFeature();
		fixture.Source.Features.Set<IReturningFeature>(feature);
		await new DataUpdateExecutor().ExecuteAsync(fixture.Context, new UpdateStatement(fixture.Context.Entity), default);

		Assert.Equal(0, feature.Count);
		Assert.Single(fixture.Driver.Commands);
		Assert.Equal(1, fixture.Context.Count);
	}

	[Fact]
	public async Task SimulatedReaderUsesExistingPopulationAndPublication()
	{
		await using var fixture = new Fixture();
		var reader = new DataConnectorTest.DbReaderMocker(fixture.Driver);
		fixture.Source.Features.Set<IReturningFeature>(new ReaderFeature(reader));
		var statement = new UpdateStatement(fixture.Context.Entity) { Returning = new() };
		var continued = await new DataUpdateExecutor().ExecuteAsync(fixture.Context, statement, default);

		Assert.True(continued);
		Assert.Empty(fixture.Options.Returning.Rows);
		Assert.Equal(0, fixture.Context.Count);
		Assert.Equal(1, reader.DisposeCount);
		Assert.Empty(fixture.Driver.Commands);

		DataReturning.Complete(fixture.Context);

		var row = Assert.Single(fixture.Options.Returning.Rows);
		Assert.True(row.TryGetValue(nameof(Row.Value), ReturningKind.Newer, out var value));
		Assert.Equal(1, value);
	}

	private sealed class InterceptingFeature : ReturningFeature
	{
		public int Count { get; private set; }
		public IMutateStatement Statement { get; private set; }
		protected override ValueTask<bool> OnExecuteAsync(IDataMutateContext context, IMutateStatement statement,
			Func<CancellationToken, ValueTask<bool>> execute,
			Func<DbDataReader, CancellationToken, ValueTask<bool>> populate, CancellationToken cancellation)
		{
			this.Count++;
			this.Statement = statement;
			return ValueTask.FromResult(false);
		}
	}

	private sealed class ReaderFeature(DbDataReader reader) : ReturningFeature
	{
		protected override async ValueTask<bool> OnExecuteAsync(IDataMutateContext context, IMutateStatement statement,
			Func<CancellationToken, ValueTask<bool>> execute,
			Func<DbDataReader, CancellationToken, ValueTask<bool>> populate, CancellationToken cancellation)
		{
			await using(reader)
				return await populate(reader, cancellation);
		}
	}

	private sealed class Fixture : IAsyncDisposable
	{
		private readonly DataAccess _access;
		public Fixture()
		{
			var entity = new DataEntity(null, $"ReturningFeature{Guid.NewGuid():N}");
			entity.Properties.Simplex(nameof(Row.Value), DataType.Int32, false);
			Mapping.Entities.Add(entity);
			this.Driver = new DataConnectorTest.DataDriverMocker();
			this.Source = new DataConnectorTest.DataSourceMocker(this.Driver);
			_access = new DataAccess("ReturningFeature");
			_access.Provider.Multiplexer = new Multiplexer(this.Source);
			this.Options = DataUpdateOptions.Return(ReturningKind.Newer, nameof(Row.Value)).Build();
			this.Context = new DataUpdateContext(_access, entity.Name, new Row { Value = 2 }, Condition.Equal(nameof(Row.Value), 1),
				SchemaParser.Instance.Parse(entity.Name, nameof(Row.Value), typeof(Row)), this.Options);
		}

		public DataConnectorTest.DataDriverMocker Driver { get; }
		public DataConnectorTest.DataSourceMocker Source { get; }
		public DataUpdateOptions Options { get; }
		public DataUpdateContext Context { get; }
		public async ValueTask DisposeAsync()
		{
			DataReturning.Discard(this.Context);
			await this.Context.Session.DisposeAsync();
			this.Context.Dispose();
			await _access.DisposeAsync();
			Mapping.Entities.Remove(this.Context.Name);
		}
	}

	private sealed class Row
	{
		public int Value { get; set; }
	}

	private sealed class Multiplexer(IDataSource source) : IDataMultiplexer
	{
		public IDataSourceProvider Provider => null;
		public IDataSourceSelector Selector => null;
		public IDataSource GetSource(IDataAccessContextBase context) => source;
	}
}
