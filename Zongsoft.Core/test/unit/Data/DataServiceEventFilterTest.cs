using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Xunit;

using Zongsoft.Data.Metadata;

namespace Zongsoft.Data.Tests;

[Collection(MappingCollection.Name)]
public sealed class DataServiceEventFilterTest
{
	[Theory]
	[InlineData(DataAccessMethod.Select)]
	[InlineData(DataAccessMethod.Execute)]
	[InlineData(DataAccessMethod.Exists)]
	[InlineData(DataAccessMethod.Aggregate)]
	[InlineData((DataAccessMethod)(-1))]
	public void OnlyWriteOperationsCanBeEnabled(DataAccessMethod method)
	{
		Assert.Equal("methods", Assert.Throws<ArgumentOutOfRangeException>(() => new Filter(method)).ParamName);
		Assert.Equal("methods", Assert.Throws<ArgumentOutOfRangeException>(() => new Filter(DataAccessMethod.Insert, method)).ParamName);
	}

	[Fact]
	public void EmptyOperationListsAreRejected()
	{
		Assert.Equal("methods", Assert.Throws<ArgumentException>(() => new Filter()).ParamName);
		Assert.Equal("methods", Assert.Throws<ArgumentException>(() => new Filter([])).ParamName);
	}

	[Fact]
	public async Task NullArgumentsAreRejected()
	{
		Assert.Equal("methods", Assert.Throws<ArgumentNullException>(() => new Filter(null)).ParamName);
		var filter = new Filter(DataAccessMethod.Insert);
		Assert.Equal("context", (await Assert.ThrowsAsync<ArgumentNullException>(() => filter.OnFilteringAsync(null).AsTask())).ParamName);
		Assert.Equal("context", (await Assert.ThrowsAsync<ArgumentNullException>(() => filter.OnFilteredAsync(null).AsTask())).ParamName);
	}

	[Theory]
	[InlineData(DataAccessMethod.Insert)]
	[InlineData(DataAccessMethod.Update)]
	[InlineData(DataAccessMethod.Delete)]
	[InlineData(DataAccessMethod.Upsert)]
	[InlineData(DataAccessMethod.Import)]
	[InlineData(DataAccessMethod.Select)]
	public async Task DisabledOperationsLeaveDataAndReturningUntouched(DataAccessMethod method)
	{
		await using var scope = new Scope();
		var source = new DataAsyncPipelineTest.Sequence<Record>([new()]);
		var input = source.Async;
		var operation = scope.Create(method, input, true);
		var filter = new Filter(method == DataAccessMethod.Insert ? DataAccessMethod.Delete : DataAccessMethod.Insert);
		var context = new Context(operation);
		var originalData = (operation as IDataMutateContextBase)?.Data;
		SetCount(operation, 1);

		Assert.False(await filter.OnFilteringAsync(context));
		await filter.OnFilteredAsync(context);

		Assert.Empty(filter.Publications);
		Assert.Equal(0, source.Enumerations);

		if(operation is IDataMutateContextBase mutation)
		{
			Assert.Same(originalData, mutation.Data);
			Assert.True(mutation.Returning.Columns.IsEmpty);
		}

		if(operation is DataImportContextBase importation)
			Assert.Same(input, importation.Data);
	}

	[Fact]
	public async Task MissingAccessContextIsIgnored()
	{
		var filter = new Filter(DataAccessMethod.Insert);
		var context = new Context(null);
		Assert.False(await filter.OnFilteringAsync(context));
		await filter.OnFilteredAsync(context);
		Assert.Empty(filter.Publications);
	}

	[Theory]
	[InlineData(DataAccessMethod.Insert, false)]
	[InlineData(DataAccessMethod.Insert, true)]
	[InlineData(DataAccessMethod.Upsert, false)]
	[InlineData(DataAccessMethod.Upsert, true)]
	public async Task InsertAndUpsertKeepSingleOrCollectionContract(DataAccessMethod method, bool multiple)
	{
		await using var scope = new Scope();
		var record = new Record { Id = 11, TenantId = 7, Name = "input" };
		var dictionary = DataDictionary.GetDictionary<Record>(record);
		var operation = scope.Create(method, multiple ? new[] { dictionary } : dictionary, multiple);
		var context = new Context(operation);
		var methods = new[] { method, method };
		var filter = new Filter(methods);
		Array.Fill(methods, DataAccessMethod.Select);
		var cancellation = TestContext.Current.CancellationToken;

		Assert.False(await filter.OnFilteringAsync(context, cancellation));
		record.Id = 23;
		SetCount(operation, 1);
		await filter.OnFilteredAsync(context, cancellation);

		var publication = Assert.Single(filter.Publications);
		Assert.Equal(method, publication.Method);
		Assert.Equal(multiple, publication.Multiple);
		Assert.Same(context, publication.Context);
		Assert.Equal(cancellation, publication.Cancellation);
		Assert.Equal(23, Assert.Single(publication.Data).GetValue(item => item.Id));
	}

	[Theory]
	[InlineData(DataAccessMethod.Insert, false)]
	[InlineData(DataAccessMethod.Insert, true)]
	[InlineData(DataAccessMethod.Upsert, false)]
	[InlineData(DataAccessMethod.Upsert, true)]
	[InlineData(DataAccessMethod.Import, false)]
	[InlineData(DataAccessMethod.Import, true)]
	public async Task BatchInputIsConsumedOnceAndRetainsPopulatedValues(DataAccessMethod method, bool asynchronous)
	{
		await using var scope = new Scope();
		var records = new[] { new Record { Id = 11, Name = "first" }, new Record { Id = 12, Name = "second" } };
		var source = new DataAsyncPipelineTest.Sequence<Record>(records);
		var operation = scope.Create(method, asynchronous ? source.Async : source.Sync, true);
		var context = new Context(operation);
		var filter = new Filter(method);
		var cancellation = TestContext.Current.CancellationToken;

		Assert.False(await filter.OnFilteringAsync(context, cancellation));
		Assert.Equal(1, source.Enumerations);
		Assert.Equal(1, source.Disposals);

		if(operation is DataImportContextBase importation)
			Assert.Equal(records, Assert.IsType<Record[]>(importation.Data));
		else
			Assert.Equal(2, Assert.IsType<IDataDictionary<Record>[]>(((IDataMutateContextBase)operation).Data).Length);

		records[1].Id = 42;
		SetCount(operation, 2);
		await filter.OnFilteredAsync(context, cancellation);

		var publication = Assert.Single(filter.Publications);
		Assert.True(publication.Multiple);
		Assert.Equal(new[] { 11, 42 }, publication.Data.Select(item => item.GetValue(record => record.Id)));
		Assert.Equal(1, source.Enumerations);
		Assert.Equal(1, source.Disposals);
	}

	[Theory]
	[InlineData(DataAccessMethod.Insert)]
	[InlineData(DataAccessMethod.Upsert)]
	public async Task AsyncDictionaryInputIsMaterializedBeforeTheWrite(DataAccessMethod method)
	{
		await using var scope = new Scope();
		var dictionary = DataDictionary.GetDictionary<Record>(new Record { Id = 11 });
		var released = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var source = new DataAsyncPipelineTest.Sequence<IDataDictionary<Record>>([dictionary]) { DisposeGate = released.Task };
		var input = source.Async;
		var operation = (IDataMutateContextBase)scope.Create(method, input, true);
		var context = new Context(operation);
		var filter = new Filter(method);
		var pending = filter.OnFilteringAsync(context, TestContext.Current.CancellationToken).AsTask();

		try
		{
			await source.Disposing.Task.WaitAsync(TestContext.Current.CancellationToken);
			Assert.False(pending.IsCompleted);
			Assert.Same(input, operation.Data);
		}
		finally
		{
			released.TrySetResult();
		}

		Assert.False(await pending);
		Assert.Same(dictionary, Assert.Single(Assert.IsType<IDataDictionary<Record>[]>(operation.Data)));
		operation.Count = 1;
		await filter.OnFilteredAsync(context, TestContext.Current.CancellationToken);
		Assert.True(Assert.Single(filter.Publications).Multiple);
		Assert.Equal(1, source.Enumerations);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task InputFailureDisposesEnumeratorAndPreservesOriginalSource(bool asynchronous)
	{
		await using var scope = new Scope();
		var failure = new InvalidOperationException();
		var source = new DataAsyncPipelineTest.Sequence<Record>([new(), new()]) { Failure = failure };
		var input = asynchronous ? (object)source.Async : source.Sync;
		var operation = (IDataMutateContextBase)scope.Create(DataAccessMethod.Insert, input, true);
		var filter = new Filter(DataAccessMethod.Insert);

		Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => filter.OnFilteringAsync(new Context(operation)).AsTask()));
		Assert.Same(input, operation.Data);
		Assert.Equal(1, source.Disposals);
		Assert.Empty(filter.Publications);
	}

	[Theory]
	[InlineData(DataAccessMethod.Delete, "full", true)]
	[InlineData(DataAccessMethod.Delete, "nested", true)]
	[InlineData(DataAccessMethod.Delete, "partial", false)]
	[InlineData(DataAccessMethod.Delete, "none", false)]
	[InlineData(DataAccessMethod.Delete, "in", false)]
	[InlineData(DataAccessMethod.Delete, "or", false)]
	[InlineData(DataAccessMethod.Delete, "range", false)]
	[InlineData(DataAccessMethod.Delete, "null", false)]
	[InlineData(DataAccessMethod.Delete, "conflict", false)]
	[InlineData(DataAccessMethod.Delete, "keyless", false)]
	[InlineData(DataAccessMethod.Update, "full", true)]
	[InlineData(DataAccessMethod.Update, "nested", true)]
	[InlineData(DataAccessMethod.Update, "partial", false)]
	[InlineData(DataAccessMethod.Update, "none", false)]
	[InlineData(DataAccessMethod.Update, "in", false)]
	[InlineData(DataAccessMethod.Update, "or", false)]
	[InlineData(DataAccessMethod.Update, "range", false)]
	[InlineData(DataAccessMethod.Update, "null", false)]
	[InlineData(DataAccessMethod.Update, "conflict", false)]
	[InlineData(DataAccessMethod.Update, "keyless", false)]
	public async Task ReturningUsesCriteriaRatherThanRowCountToSelectContract(DataAccessMethod method, string condition, bool single)
	{
		await using var scope = new Scope(condition != "keyless");
		var tenant = Condition.Equal(nameof(Record.TenantId), 7);
		var id = Condition.Equal(nameof(Record.Id), 11);
		ICondition criteria = condition switch
		{
			"full" or "keyless" => tenant & id,
			"nested" => ConditionCollection.And(tenant, ConditionCollection.And(id)),
			"partial" => tenant,
			"in" => tenant & Condition.In(nameof(Record.Id), new[] { 11 }),
			"or" => tenant | id,
			"range" => tenant & Condition.GreaterThan(nameof(Record.Id), 0),
			"null" => tenant & Condition.Equal(nameof(Record.Id), null),
			"conflict" => tenant & id & Condition.Equal(nameof(Record.Id), 12),
			_ => null,
		};
		var operation = (IDataMutateContextBase)scope.Create(method, new Record { Name = "input" }, criteria: criteria);
		var context = new Context(operation);
		var filter = new Filter(method);
		Assert.False(await filter.OnFilteringAsync(context));
		var kind = method == DataAccessMethod.Delete ? ReturningKind.Older : ReturningKind.Newer;
		Assert.Equal(new[] { nameof(Record.TenantId), nameof(Record.Id), nameof(Record.Name), nameof(Record.Description) }, operation.Returning.Columns.Select(column => column.Name));
		Assert.All(operation.Returning.Columns, column => Assert.Equal(kind, column.Kind));
		Populate(operation.Returning, "database", 1);
		operation.Count = 1;

		await filter.OnFilteredAsync(context);

		var publication = Assert.Single(filter.Publications);
		Assert.Equal(!single, publication.Multiple);
		var data = Assert.Single(publication.Data);
		Assert.Equal("database", data.GetValue(record => record.Name));
		Assert.True(data.ContainsKey(nameof(Record.Description)));
		Assert.Null(data[nameof(Record.Description)]);
		Assert.False(data.ContainsKey("Related"));
	}

	[Theory]
	[InlineData(DataAccessMethod.Delete)]
	[InlineData(DataAccessMethod.Update)]
	public async Task ReturningDoesNotChangeCallerOptionsOrIncludePreviousRows(DataAccessMethod method)
	{
		await using var scope = new Scope();
		IDataMutateOptions options = method == DataAccessMethod.Delete ? DataDeleteOptions.Return(nameof(Record.Name)).Build() : DataUpdateOptions.Return(ReturningKind.Older, nameof(Record.Name)).Build();
		var returning = options.Returning;
		Populate(returning, "previous", 1);
		options.Parameters["origin"] = "caller";
		var parameters = options.Parameters;
		var filter = new Filter(method);

		for(var i = 0; i < 2; i++)
		{
			var operation = (IDataMutateContextBase)scope.Create(method, new Record(), options: options);
			var context = new Context(operation);
			await filter.OnFilteringAsync(context);
			Populate(operation.Returning, "current" + i, 2);
			operation.Count = 2;
			await filter.OnFilteredAsync(context);
		}

		Assert.Same(returning, options.Returning);
		Assert.Single(returning.Columns);
		Assert.Single(returning.Rows);
		Assert.Same(parameters, options.Parameters);
		Assert.Equal("caller", parameters["origin"]);
		Assert.Equal(2, filter.Publications.Count);
		Assert.Equal(new[] { "current0", "current1" }, filter.Publications.Select(item => item.Data[0].GetValue(record => record.Name)));
		Assert.All(filter.Publications, item => Assert.Equal(2, item.Data.Length));
	}

	[Theory]
	[InlineData(DataAccessMethod.Delete, 0)]
	[InlineData(DataAccessMethod.Update, 0)]
	[InlineData(DataAccessMethod.Delete, 1)]
	[InlineData(DataAccessMethod.Update, 1)]
	public async Task EmptyReturningOrZeroAffectedRowsDoNotPublish(DataAccessMethod method, int count)
	{
		await using var scope = new Scope();
		var operation = (IDataMutateContextBase)scope.Create(method, new Record());
		var filter = new Filter(method);
		var context = new Context(operation);
		await filter.OnFilteringAsync(context);
		Assert.Null(operation.Options.Returning);

		if(count == 0)
			Populate(operation.Returning, "ignored", 1);
		operation.Count = count;
		await filter.OnFilteredAsync(context);
		Assert.Empty(filter.Publications);
	}

	[Theory]
	[InlineData(DataAccessMethod.Insert)]
	[InlineData(DataAccessMethod.Upsert)]
	[InlineData(DataAccessMethod.Import)]
	public async Task ZeroAffectedRowsDoNotReadInputOrPublish(DataAccessMethod method)
	{
		await using var scope = new Scope();
		var source = new DataAsyncPipelineTest.Sequence<Record>([new()]) { Failure = new InvalidOperationException() };
		var operation = scope.Create(method, source.Sync, true);
		var filter = new Filter(method);
		await filter.OnFilteredAsync(new Context(operation));
		Assert.Equal(0, source.Enumerations);
		Assert.Empty(filter.Publications);
	}

	[Fact]
	public async Task ReturningPreparationCanBeSpecialized()
	{
		await using var scope = new Scope();
		var operation = (IDataMutateContextBase)scope.Create(DataAccessMethod.Update, new Record());
		var context = new Context(operation);
		var filter = new ProjectionFilter();
		await filter.OnFilteringAsync(context);
		Assert.Equal(nameof(Record.Name), Assert.Single(operation.Returning.Columns).Name);
		Populate(operation.Returning, "projected", 1);
		operation.Count = 1;
		await filter.OnFilteredAsync(context);
		Assert.Single(Assert.Single(Assert.Single(filter.Publications).Data));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task PublicationIsAwaitedAndPropagatesFailureOrCancellation(bool cancel)
	{
		await using var scope = new Scope();
		using var cancellation = new CancellationTokenSource();
		var released = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var filter = new Filter(DataAccessMethod.Insert) { Callback = (_, token) => new ValueTask(released.Task.WaitAsync(token)) };
		var operation = scope.Create(DataAccessMethod.Insert, new Record());
		SetCount(operation, 1);
		var pending = filter.OnFilteredAsync(new Context(operation), cancellation.Token).AsTask();
		Assert.False(pending.IsCompleted);

		try
		{
			if(cancel)
			{
				cancellation.Cancel();
				var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
				Assert.Equal(cancellation.Token, exception.CancellationToken);
			}
			else
			{
				var failure = new InvalidOperationException();
				released.SetException(failure);
				Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => pending));
			}
		}
		finally
		{
			released.TrySetResult();
		}
	}

	[Fact]
	public async Task CanceledOperationDoesNotPrepareOrPublish()
	{
		await using var scope = new Scope();
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		var source = new DataAsyncPipelineTest.Sequence<Record>([new()]);
		var operation = scope.Create(DataAccessMethod.Insert, source.Async, true);
		var context = new Context(operation);
		var filter = new Filter(DataAccessMethod.Insert);
		Assert.Equal(cancellation.Token, (await Assert.ThrowsAnyAsync<OperationCanceledException>(() => filter.OnFilteringAsync(context, cancellation.Token).AsTask())).CancellationToken);
		SetCount(operation, 1);
		Assert.Equal(cancellation.Token, (await Assert.ThrowsAnyAsync<OperationCanceledException>(() => filter.OnFilteredAsync(context, cancellation.Token).AsTask())).CancellationToken);
		Assert.Equal(0, source.Enumerations);
		Assert.Empty(filter.Publications);
	}

	[Fact]
	public async Task OneFilterCanHandleOverlappingOperationsWithoutSharingData()
	{
		await using var scope = new Scope();
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var filter = new Filter(DataAccessMethod.Insert) { Callback = (_, _) => new ValueTask(release.Task) };
		var first = scope.Create(DataAccessMethod.Insert, new Record { Id = 11 });
		var second = scope.Create(DataAccessMethod.Insert, new Record { Id = 22 });
		SetCount(first, 1);
		SetCount(second, 1);
		var firstPending = filter.OnFilteredAsync(new Context(first)).AsTask();
		var secondPending = filter.OnFilteredAsync(new Context(second)).AsTask();

		try
		{
			Assert.False(firstPending.IsCompleted);
			Assert.False(secondPending.IsCompleted);
			Assert.Equal(new[] { 11, 22 }, filter.Publications.Select(item => item.Data.Single().GetValue(record => record.Id)));
		}
		finally
		{
			release.TrySetResult();
			await Task.WhenAll(firstPending, secondPending);
		}
	}

	private static void SetCount(IDataAccessContextBase operation, int count)
	{
		if(operation is IDataMutateContextBase mutation)
			mutation.Count = count;
		else if(operation is DataImportContextBase importation)
			importation.Count = count;
	}

	private static void Populate(Returning returning, string name, int count)
	{
		using var table = new DataTable();
		foreach(var column in returning.Columns)
			table.Columns.Add(column.Alias, typeof(object));

		for(var i = 0; i < count; i++)
		{
			table.Rows.Add(returning.Columns.Select(column => column.Name switch
			{
				nameof(Record.TenantId) => (object)7,
				nameof(Record.Id) => 11 + i,
				nameof(Record.Name) => name,
				_ => DBNull.Value,
			}).ToArray());
		}

		using var reader = table.CreateDataReader();
		while(reader.Read())
			returning.Rows.Populate(reader);
	}

	private sealed class Record
	{
		public int TenantId { get; set; }
		public int Id { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
	}

	private sealed class Context(IDataAccessContextBase operation) : IDataServiceContext<Record>
	{
		public IDataService<Record> Service => null;
		public DataServiceMethod Method => operation?.Method switch
		{
			DataAccessMethod.Insert => DataServiceMethod.Insert(),
			DataAccessMethod.Update => DataServiceMethod.Update(),
			DataAccessMethod.Delete => DataServiceMethod.Delete(),
			DataAccessMethod.Upsert => DataServiceMethod.Upsert(),
			DataAccessMethod.Import => DataServiceMethod.Import(),
			_ => DataServiceMethod.Select(),
		};
		public IDataAccessContextBase AccessContext => operation;
		public object Result { get; set; }
		public object[] Arguments => [];
	}

	private sealed record Publication(DataAccessMethod Method, IDataServiceContext<Record> Context, IDataDictionary<Record>[] Data, bool Multiple, CancellationToken Cancellation);

	private class Filter(params DataAccessMethod[] methods) : DataServiceBase<Record>.EventFilterBase(methods)
	{
		public ConcurrentQueue<Publication> Publications { get; } = new();
		public Func<Publication, CancellationToken, ValueTask> Callback { get; init; }
		private ValueTask Publish(IDataServiceContext<Record> context, IDataDictionary<Record>[] data, bool multiple, CancellationToken cancellation)
		{
			var publication = new Publication(context.AccessContext.Method, context, data, multiple, cancellation);
			this.Publications.Enqueue(publication);
			return this.Callback?.Invoke(publication, cancellation) ?? ValueTask.CompletedTask;
		}
		protected override ValueTask OnInsertedAsync(IDataServiceContext<Record> context, IDataDictionary<Record> argument, CancellationToken cancellation) => this.Publish(context, [argument], false, cancellation);
		protected override ValueTask OnInsertedAsync(IDataServiceContext<Record> context, IEnumerable<IDataDictionary<Record>> argument, CancellationToken cancellation) => this.Publish(context, argument.ToArray(), true, cancellation);
		protected override ValueTask OnUpdatedAsync(IDataServiceContext<Record> context, IDataDictionary<Record> argument, CancellationToken cancellation) => this.Publish(context, [argument], false, cancellation);
		protected override ValueTask OnUpdatedAsync(IDataServiceContext<Record> context, IEnumerable<IDataDictionary<Record>> argument, CancellationToken cancellation) => this.Publish(context, argument.ToArray(), true, cancellation);
		protected override ValueTask OnDeletedAsync(IDataServiceContext<Record> context, IDataDictionary<Record> argument, CancellationToken cancellation) => this.Publish(context, [argument], false, cancellation);
		protected override ValueTask OnDeletedAsync(IDataServiceContext<Record> context, IEnumerable<IDataDictionary<Record>> argument, CancellationToken cancellation) => this.Publish(context, argument.ToArray(), true, cancellation);
		protected override ValueTask OnUpsertedAsync(IDataServiceContext<Record> context, IDataDictionary<Record> argument, CancellationToken cancellation) => this.Publish(context, [argument], false, cancellation);
		protected override ValueTask OnUpsertedAsync(IDataServiceContext<Record> context, IEnumerable<IDataDictionary<Record>> argument, CancellationToken cancellation) => this.Publish(context, argument.ToArray(), true, cancellation);
		protected override ValueTask OnImportedAsync(IDataServiceContext<Record> context, IEnumerable<IDataDictionary<Record>> argument, CancellationToken cancellation) => this.Publish(context, argument.ToArray(), true, cancellation);
	}

	private sealed class ProjectionFilter() : Filter(DataAccessMethod.Update)
	{
		protected override void PrepareReturning(IDataMutateContextBase context, ReturningKind kind) => context.Returning.Columns.Append(nameof(Record.Name), kind);
	}

	private sealed class Scope : IAsyncDisposable
	{
		private readonly DataAsyncPipelineTest.TestDataAccess _accessor = new();
		private readonly List<IDisposable> _operations = [];
		private readonly string _name = $"EventFilter_{Guid.NewGuid():N}";
		public Scope(bool key = true)
		{
			var entity = new DataEntity(null, _name);
			entity.Properties.Simplex(nameof(Record.TenantId), DataType.Int32, false);
			entity.Properties.Simplex(nameof(Record.Id), DataType.Int32, false);
			entity.Properties.Simplex(nameof(Record.Name), DataType.String, true);
			entity.Properties.Simplex(nameof(Record.Description), DataType.String, true);
			entity.Properties.Complex("Related", "Other");

			if(key)
				entity.SetKey(nameof(Record.TenantId), nameof(Record.Id));

			Mapping.Entities.Add(entity);
		}
		public IDataAccessContextBase Create(DataAccessMethod method, object data = null, bool multiple = false, ICondition criteria = null, IDataMutateOptions options = null)
		{
			IDataAccessContextBase operation = method switch
			{
				DataAccessMethod.Insert => new Insertion(_accessor, _name, multiple, data, options as IDataInsertOptions ?? new DataInsertOptions()),
				DataAccessMethod.Upsert => new Upsertion(_accessor, _name, multiple, data, options as IDataUpsertOptions ?? new DataUpsertOptions()),
				DataAccessMethod.Update => new Updation(_accessor, _name, data, criteria, options as IDataUpdateOptions ?? new DataUpdateOptions()),
				DataAccessMethod.Delete => new Deletion(_accessor, _name, criteria, options as IDataDeleteOptions ?? new DataDeleteOptions()),
				DataAccessMethod.Import => new Importation(_accessor, _name, data),
				DataAccessMethod.Select => new DataAsyncPipelineTest.SelectContext(_accessor, _name, typeof(Record), null, null, null, null, null, new DataSelectOptions()),
				_ => throw new ArgumentOutOfRangeException(nameof(method)),
			};
			_operations.Add((IDisposable)operation);
			return operation;
		}
		public async ValueTask DisposeAsync()
		{
			foreach(var operation in _operations)
				operation.Dispose();

			Mapping.Entities.Remove(_name);
			await _accessor.DisposeAsync();
		}
	}

	private sealed class Insertion(IDataAccess access, string name, bool multiple, object data, IDataInsertOptions options) : DataInsertContextBase(access, name, multiple, data, null, options)
	{
		public override T GetFeature<T>() => default;
	}
	private sealed class Upsertion(IDataAccess access, string name, bool multiple, object data, IDataUpsertOptions options) : DataUpsertContextBase(access, name, multiple, data, null, options)
	{
		public override T GetFeature<T>() => default;
	}
	private sealed class Updation(IDataAccess access, string name, object data, ICondition criteria, IDataUpdateOptions options) : DataUpdateContextBase(access, name, data, criteria, null, options)
	{
		public override T GetFeature<T>() => default;
	}
	private sealed class Deletion(IDataAccess access, string name, ICondition criteria, IDataDeleteOptions options) : DataDeleteContextBase(access, name, criteria, null, options)
	{
		public override T GetFeature<T>() => default;
	}
	private sealed class Importation(IDataAccess access, string name, object data) : DataImportContextBase(access, name, data, [])
	{
		public override T GetFeature<T>() => default;
	}
}
