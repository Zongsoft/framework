using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Data.Common;
using Zongsoft.Data.Features;
using Zongsoft.Data.Metadata;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.Tests;

public class OperationReturningTest
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task CallerAndOperationReceiveIndependentProjections(bool callerReturning)
	{
		var options = callerReturning ? DataUpdateOptions.Return(ReturningKind.Older, "Value").Build() : new DataUpdateOptions();
		var original = options.Returning;
		await using var fixture = new Fixture(options);
		fixture.Context.Returning.Columns.Append("Value", ReturningKind.Newer);
		fixture.Context.Returning.Columns.Append("Extra", ReturningKind.Newer);
		Assert.False(fixture.Context.Returning.Columns.Append("value", ReturningKind.Newer));
		Assert.True(DataReturning.TryGet(fixture.Context, out var columns));
		Assert.Equal(callerReturning ? 3 : 2, columns.Count);

		Populate(fixture.Context);
		Assert.Empty(fixture.Context.Returning.Rows);
		Assert.True(original == null || original.Rows.IsEmpty);
		DataReturning.Validate(fixture.Context);
		DataReturning.Complete(fixture.Context);

		Assert.Same(original, options.Returning);
		var operation = Assert.Single(fixture.Context.Returning.Rows);
		Assert.Equal(2, operation.Values.Length);
		Assert.True(operation.TryGetValue("Value", ReturningKind.Newer, out var value));
		Assert.Equal(2, value);
		Assert.True(operation.TryGetValue("Extra", ReturningKind.Newer, out var extra));
		Assert.Null(extra);
		Assert.False(operation.TryGetValue("Value", ReturningKind.Older, out _));

		if(callerReturning)
		{
			Assert.Single(original.Columns);
			var row = Assert.Single(original.Rows);
			Assert.Single(row.Values);
			Assert.True(row.TryGetValue("Value", ReturningKind.Older, out value));
			Assert.Equal(1, value);
			Assert.False(row.TryGetValue("Extra", ReturningKind.Older, out _));
			row.Values[0] = 999;
			Assert.Equal(2, operation[0]);
		}
	}

	[Fact]
	public async Task SharedFeatureAndReusedOptionsDoNotShareOperationResults()
	{
		var options = DataUpdateOptions.Return(ReturningKind.Newer, "Value").Build();
		await using var first = new Fixture(options);
		await using var second = new Fixture(options);
		first.Context.Returning.Columns.Append("Extra", ReturningKind.Newer);
		second.Context.Returning.Columns.Append("Value", ReturningKind.Newer);
		Assert.Same(first.Source.Features.Get<IReturningFeature>(), second.Source.Features.Get<IReturningFeature>());
		Populate(first.Context);
		Populate(second.Context);
		DataReturning.Complete(first.Context);
		DataReturning.Complete(second.Context);
		Assert.Equal(2, options.Returning.Rows.Count);
		Assert.Single(first.Context.Returning.Rows);
		Assert.Single(second.Context.Returning.Rows);
		Assert.False(first.Context.Returning.Rows[0].TryGetValue("Value", ReturningKind.Newer, out _));
		Assert.False(second.Context.Returning.Rows[0].TryGetValue("Extra", ReturningKind.Newer, out _));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task UnsupportedRequiredKindFailsBeforeExecution(bool missingFeature)
	{
		await using var fixture = new Fixture(new());
		fixture.Source.Features.Set<IReturningFeature>(missingFeature ? null : new ReturningFeature(false));
		fixture.Context.Returning.Columns.Append("Value", ReturningKind.Older);
		var provider = new Provider((_, _) => throw new InvalidOperationException("Must not execute."));
		await Assert.ThrowsAsync<NotSupportedException>(() => provider.ExecuteAsync(fixture.Context, default).AsTask());
		Assert.Empty(fixture.Driver.Commands);
		Assert.Empty(fixture.Context.Returning.Rows);
	}

	[Fact]
	public async Task OptionalUnsupportedKindDoesNotBlockRequiredNewerProjection()
	{
		await using var fixture = new Fixture(DataUpdateOptions.Return(ReturningKind.Older, "Value").Build());
		fixture.Source.Features.Set<IReturningFeature>(new ReturningFeature(false));
		fixture.Context.Returning.Columns.Append("Extra", ReturningKind.Newer);
		var statements = new System.Collections.Generic.List<IStatementBase>();

		await foreach(var item in new UpdateStatementBuilder().BuildAsync(fixture.Context))
			statements.Add(item);

		var statement = Assert.Single(statements);
		var update = Assert.IsType<UpdateStatement>(statement);
		Assert.Equal("Extra@Newer", Assert.Single(update.Returning.Members).Field.Alias);
		Assert.Single(fixture.Context.Options.Returning.Columns);
		Assert.Single(fixture.Context.Returning.Columns);
	}

	[Theory]
	[InlineData("MissingColumn")]
	[InlineData("MissingRows")]
	[InlineData("Failure")]
	[InlineData("Cancellation")]
	public async Task FailureDiscardsBothProjectionsBeforeCommit(string stage)
	{
		var options = DataUpdateOptions.Return(ReturningKind.Older, "Value").Build();
		await using var fixture = new Fixture(options);
		Populate(fixture.Context);
		DataReturning.Complete(fixture.Context);
		fixture.Context.Returning.Columns.Append("Extra", ReturningKind.Newer);
		using var cancellation = new CancellationTokenSource();
		var provider = new Provider(async (context, token) =>
		{
			await using var command = context.Session.Build(context, new UpdateStatement(fixture.Context.Entity));
			await command.ExecuteNonQueryAsync(token);
			fixture.Context.Count = 1;

			if(stage != "MissingRows")
				Populate(context, stage == "MissingColumn");

			if(stage == "Failure")
				throw new InvalidOperationException();
			if(stage == "Cancellation")
				cancellation.Cancel();
		});

		var failure = await Record.ExceptionAsync(() => provider.ExecuteAsync(fixture.Context, cancellation.Token).AsTask());
		switch(stage)
		{
			case "MissingColumn":
				Assert.Equal("Extra", Assert.IsType<DataArgumentException>(failure).Name);
				break;
			case "MissingRows":
				Assert.IsType<DataException>(failure);
				break;
			case "Failure":
				Assert.IsType<InvalidOperationException>(failure);
				break;
			case "Cancellation":
				Assert.IsAssignableFrom<OperationCanceledException>(failure);
				break;
		}

		var transaction = Assert.Single(fixture.Driver.Connections).Transaction;
		Assert.Equal(0, transaction.CommitCount);
		Assert.Equal(1, transaction.RollbackCount);
		Assert.Equal(0, fixture.Context.Count);
		Assert.Single(options.Returning.Rows);
		Assert.Empty(fixture.Context.Returning.Rows);
		DataReturning.Complete(fixture.Context);
		Assert.Empty(fixture.Context.Returning.Rows);
	}

	[Fact]
	public async Task ResultsArePublishedOnlyAfterCommitCompletes()
	{
		var options = DataUpdateOptions.Return(ReturningKind.Newer, "Value").Build();
		await using var fixture = new Fixture(options);
		fixture.Context.Returning.Columns.Append("Extra", ReturningKind.Newer);
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		fixture.Driver.Commit = async token => { entered.SetResult(); await release.Task.WaitAsync(token); };
		var provider = new Provider(async (context, token) =>
		{
			await using var command = context.Session.Build(context, new UpdateStatement(fixture.Context.Entity));
			await command.ExecuteNonQueryAsync(token);
			fixture.Context.Count = 1;
			Populate(context);
		});
		var pending = provider.ExecuteAsync(fixture.Context, default).AsTask();

		try
		{
			await entered.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
			Assert.False(pending.IsCompleted);
			Assert.Empty(options.Returning.Rows);
			Assert.Empty(fixture.Context.Returning.Rows);
		}
		finally { release.TrySetResult(); }

		await pending;
		Assert.Single(options.Returning.Rows);
		Assert.Single(fixture.Context.Returning.Rows);
	}

	private static void Populate(IDataAccessContext context, bool missing = false)
	{
		using var table = new DataTable();
		table.Columns.Add("Value@Older", typeof(int));
		table.Columns.Add("Value@Newer", typeof(int));

		if(!missing)
			table.Columns.Add("Extra@Newer", typeof(string));

		table.Rows.Add(missing ? [1, 2] : [1, 2, DBNull.Value]);
		using var reader = table.CreateDataReader();
		Assert.True(reader.Read());
		DataReturning.Populate(context, reader);
	}

	private sealed class Fixture : IAsyncDisposable
	{
		private readonly DataAccess _access = new("OperationReturning");
		public Fixture(DataUpdateOptions options)
		{
			var entity = new DataEntity(null, $"OperationReturning{Guid.NewGuid():N}");
			entity.Properties.Simplex("Value", DataType.Int32, false);
			entity.Properties.Simplex("Extra", DataType.String, true);
			Mapping.Entities.Add(entity);
			this.Source = new(this.Driver);
			this.Source.Features.Set<IReturningFeature>(ReturningFeature.Instance);
			_access.Provider.Multiplexer = new Multiplexer(this.Source);
			this.Context = new(_access, entity.Name, new Row { Value = 2 }, Condition.Equal("Value", 1), SchemaParser.Instance.Parse(entity.Name, "Value", typeof(Row)), options);
		}
		public DataConnectorTest.DataDriverMocker Driver { get; } = new();
		public DataConnectorTest.DataSourceMocker Source { get; }
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
	private sealed class Provider(Func<IDataAccessContext, CancellationToken, ValueTask> execute) : DataProvider("OperationReturning", [])
	{
		protected override ValueTask OnExecuteAsync(IDataAccessContext context, CancellationToken cancellation) => execute(context, cancellation);
	}
}
