using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Data.Metadata;

namespace Zongsoft.Data.Tests;

[Collection(MappingCollection.Name)]
public sealed class DataAccessBaseSelectTest : IDisposable
{
	private readonly string _entityName = $"P0_Select_{Guid.NewGuid():N}";

	public DataAccessBaseSelectTest() => Mapping.Entities.Add(new DataEntity(null, _entityName));
	public void Dispose() => Mapping.Entities.Remove(_entityName);

	[Fact]
	public async Task SelectAsync_FirstMoveNextAwaitsProviderAsynchronously()
	{
		using var accessor = new TestDataAccess();
		await using var enumerator = Select(accessor).GetAsyncEnumerator();
		var moving = enumerator.MoveNextAsync().AsTask();

		await accessor.ProviderEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));

		try
		{
			Assert.False(moving.IsCompleted);
		}
		finally
		{
			accessor.ReleaseProvider();
		}

		Assert.True(await moving.WaitAsync(TimeSpan.FromSeconds(5)));
	}

	[Fact]
	public void SelectAsync_NotEnumerated_DoesNotCreateContextOrCallProvider()
	{
		using var accessor = new TestDataAccess();
		accessor.ReleaseProvider();

		var sequence = Select(accessor);

		Assert.NotNull(sequence);
		Assert.Empty(accessor.Contexts);
		Assert.Equal(0, accessor.ProviderCalls);
	}

	[Fact]
	public async Task SelectAsync_CompleteEnumeration_KeepsContextAliveAndDisposesOnce()
	{
		using var accessor = new TestDataAccess([1, 2, 3]);
		accessor.ReleaseProvider();
		var results = new List<int>();

		await foreach(var item in Select(accessor))
			results.Add(item);

		Assert.Equal([1, 2, 3], results);
		Assert.Single(accessor.Contexts);
		Assert.Equal(1, accessor.Contexts[0].DisposeCount);
	}

	[Fact]
	public async Task SelectAsync_BreakAfterFirstItem_DisposesContextOnce()
	{
		using var accessor = new TestDataAccess([1, 2, 3]);
		accessor.ReleaseProvider();
		var results = new List<int>();

		await foreach(var item in Select(accessor))
		{
			results.Add(item);
			break;
		}

		Assert.Equal([1], results);
		Assert.Single(accessor.Contexts);
		Assert.Equal(1, accessor.Contexts[0].DisposeCount);
	}

	[Fact]
	public async Task SelectAsync_ProviderThrows_DisposesContextAndSkipsSuccessCallbacks()
	{
		var failure = new InvalidOperationException("provider failed");
		using var accessor = new TestDataAccess { ProviderFailure = failure };

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => EnumerateAsync(accessor));

		Assert.Same(failure, exception);
		Assert.Single(accessor.Contexts);
		Assert.Equal(1, accessor.Contexts[0].DisposeCount);
		Assert.Equal(0, accessor.SelectedCalls);
	}

	[Fact]
	public async Task SelectAsync_ResultEnumerationThrows_DisposesContextAndPreservesException()
	{
		var failure = new InvalidOperationException("enumeration failed");
		using var accessor = new TestDataAccess { EnumerationFailure = failure };
		accessor.ReleaseProvider();

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => EnumerateAsync(accessor));

		Assert.Same(failure, exception);
		Assert.Single(accessor.Contexts);
		Assert.Equal(1, accessor.Contexts[0].DisposeCount);
		Assert.Equal(1, accessor.SelectedCalls);
	}

	[Fact]
	public async Task SelectAsync_CanceledProviderWait_PropagatesTokenAndDisposesContext()
	{
		using var accessor = new TestDataAccess();
		using var cancellation = new CancellationTokenSource();
		var enumeration = Task.Run(() => EnumerateAsync(accessor, cancellation.Token));

		await accessor.ProviderEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
		cancellation.Cancel();
		var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => enumeration);

		Assert.Equal(cancellation.Token, accessor.ProviderCancellation);
		Assert.Equal(cancellation.Token, exception.CancellationToken);
		Assert.Single(accessor.Contexts);
		Assert.Equal(1, accessor.Contexts[0].DisposeCount);
	}

	[Fact]
	public async Task SelectAsync_RepeatedEnumeration_CreatesIndependentContexts()
	{
		using var accessor = new TestDataAccess([1, 2]);
		accessor.ReleaseProvider();
		var sequence = Select(accessor);

		var first = await CollectAsync(sequence);
		var second = await CollectAsync(sequence);

		Assert.Equal([1, 2], first);
		Assert.Equal([1, 2], second);
		Assert.Equal(2, accessor.Contexts.Count);
		Assert.All(accessor.Contexts, context => Assert.Equal(1, context.DisposeCount));
		Assert.Equal(2, accessor.ProviderCalls);
	}

	[Fact]
	public async Task SelectAsync_SelectingShortCircuit_EnumeratesResultAndDisposesContext()
	{
		using var accessor = new TestDataAccess();
		var callbackCalls = 0;
		var sequence = accessor.SelectAsync<int>(
			_entityName, null, (ISchema)null, null, null, null,
			context =>
			{
				context.Result = new[] { 7, 8 };
				return true;
			},
			_ => callbackCalls++,
			default);

		var results = await CollectAsync(sequence);

		Assert.Equal([7, 8], results);
		Assert.Equal(0, accessor.ProviderCalls);
		Assert.Equal(0, accessor.SelectedCalls);
		Assert.Equal(0, callbackCalls);
		Assert.Single(accessor.Contexts);
		Assert.Equal(1, accessor.Contexts[0].DisposeCount);
	}

	private IAsyncEnumerable<int> Select(TestDataAccess accessor, CancellationToken cancellation = default) =>
		accessor.SelectAsync<int>(_entityName, null, (ISchema)null, null, null, null, null, null, cancellation);

	private async Task EnumerateAsync(TestDataAccess accessor, CancellationToken cancellation = default)
	{
		await foreach(var _ in Select(accessor, cancellation).WithCancellation(cancellation)) { }
	}

	private static async Task<List<int>> CollectAsync(IAsyncEnumerable<int> sequence)
	{
		var result = new List<int>();

		await foreach(var item in sequence)
			result.Add(item);

		return result;
	}

	private sealed class TestDataAccess(IEnumerable<int> items = null) : DataAccessBase("P0")
	{
		private readonly IEnumerable<int> _items = items ?? [1];
		private readonly TaskCompletionSource _providerRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);
		private int _providerCalls;
		private int _selectedCalls;

		public int ProviderCalls => _providerCalls;
		public int SelectedCalls => _selectedCalls;
		public Exception ProviderFailure { get; set; }
		public Exception EnumerationFailure { get; set; }
		public CancellationToken ProviderCancellation { get; private set; }
		public TaskCompletionSource ProviderEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public List<TestSelectContext> Contexts { get; } = [];

		public void ReleaseProvider() => _providerRelease.TrySetResult();

		protected override DataSelectContextBase CreateSelectContext(string name, Type entityType, ICondition criteria, Grouping grouping, ISchema schema, Paging paging, Sorting[] sortings, IDataSelectOptions options)
		{
			var context = new TestSelectContext(this, name, entityType, grouping, criteria, schema, paging, sortings, options);
			this.Contexts.Add(context);
			return context;
		}

		protected override async ValueTask OnSelectAsync(DataSelectContextBase context, CancellationToken cancellation)
		{
			Interlocked.Increment(ref _providerCalls);
			this.ProviderCancellation = cancellation;
			this.ProviderEntered.TrySetResult();

			if(this.ProviderFailure != null)
				throw this.ProviderFailure;

			await _providerRelease.Task.WaitAsync(cancellation);
			context.Result = new GuardedEnumerable((TestSelectContext)context, _items, this.EnumerationFailure);
		}

		protected override void OnSelected(DataSelectContextBase context)
		{
			Interlocked.Increment(ref _selectedCalls);
			base.OnSelected(context);
		}

		protected override ISchemaParser CreateSchema() => throw new NotSupportedException();
		protected override IDataSequencer CreateSequencer() => throw new NotSupportedException();
		protected override DataExecuteContextBase CreateExecuteContext(string name, bool isScalar, Type resultType, IEnumerable<Parameter> parameters, IDataExecuteOptions options) => throw new NotSupportedException();
		protected override DataExistContextBase CreateExistContext(string name, ICondition criteria, IDataExistsOptions options) => throw new NotSupportedException();
		protected override DataAggregateContextBase CreateAggregateContext(string name, DataAggregate aggregate, ICondition criteria, IDataAggregateOptions options) => throw new NotSupportedException();
		protected override DataImportContextBase CreateImportContext(string name, IEnumerable data, IEnumerable<string> members, IDataImportOptions options) => throw new NotSupportedException();
		protected override DataDeleteContextBase CreateDeleteContext(string name, ICondition criteria, ISchema schema, IDataDeleteOptions options) => throw new NotSupportedException();
		protected override DataInsertContextBase CreateInsertContext(string name, bool isMultiple, object data, ISchema schema, IDataInsertOptions options) => throw new NotSupportedException();
		protected override DataUpsertContextBase CreateUpsertContext(string name, bool isMultiple, object data, ISchema schema, IDataUpsertOptions options) => throw new NotSupportedException();
		protected override DataUpdateContextBase CreateUpdateContext(string name, object data, ICondition criteria, ISchema schema, IDataUpdateOptions options) => throw new NotSupportedException();

		protected override void OnSelect(DataSelectContextBase context) => throw new NotSupportedException();
		protected override void OnExecute(DataExecuteContextBase context) => throw new NotSupportedException();
		protected override ValueTask OnExecuteAsync(DataExecuteContextBase context, CancellationToken cancellation) => throw new NotSupportedException();
		protected override void OnExists(DataExistContextBase context) => throw new NotSupportedException();
		protected override ValueTask OnExistsAsync(DataExistContextBase context, CancellationToken cancellation) => throw new NotSupportedException();
		protected override void OnAggregate(DataAggregateContextBase context) => throw new NotSupportedException();
		protected override ValueTask OnAggregateAsync(DataAggregateContextBase context, CancellationToken cancellation) => throw new NotSupportedException();
		protected override void OnImport(DataImportContextBase context) => throw new NotSupportedException();
		protected override ValueTask OnImportAsync(DataImportContextBase context, CancellationToken cancellation = default) => throw new NotSupportedException();
		protected override void OnDelete(DataDeleteContextBase context) => throw new NotSupportedException();
		protected override ValueTask OnDeleteAsync(DataDeleteContextBase context, CancellationToken cancellation) => throw new NotSupportedException();
		protected override void OnInsert(DataInsertContextBase context) => throw new NotSupportedException();
		protected override ValueTask OnInsertAsync(DataInsertContextBase context, CancellationToken cancellation) => throw new NotSupportedException();
		protected override void OnUpsert(DataUpsertContextBase context) => throw new NotSupportedException();
		protected override ValueTask OnUpsertAsync(DataUpsertContextBase context, CancellationToken cancellation) => throw new NotSupportedException();
		protected override void OnUpdate(DataUpdateContextBase context) => throw new NotSupportedException();
		protected override ValueTask OnUpdateAsync(DataUpdateContextBase context, CancellationToken cancellation) => throw new NotSupportedException();
	}

	private sealed class TestSelectContext : DataSelectContextBase
	{
		private int _disposeCount;

		public TestSelectContext(IDataAccess dataAccess, string name, Type modelType, Grouping grouping, ICondition criteria, ISchema schema, Paging paging, Sorting[] sortings, IDataSelectOptions options) :
			base(dataAccess, name, modelType, grouping, criteria, schema, paging, sortings, options) { }

		public int DisposeCount => _disposeCount;
		public override TFeature GetFeature<TFeature>() => default;

		protected override void Dispose(bool disposing)
		{
			Interlocked.Increment(ref _disposeCount);
			base.Dispose(disposing);
		}
	}

	private sealed class GuardedEnumerable(TestSelectContext context, IEnumerable<int> items, Exception failure = null) : IEnumerable<int>
	{
		public IEnumerator<int> GetEnumerator() => new GuardedEnumerator(context, items.GetEnumerator(), failure);
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

		private sealed class GuardedEnumerator(TestSelectContext context, IEnumerator<int> source, Exception failure) : IEnumerator<int>
		{
			public int Current => source.Current;
			object IEnumerator.Current => this.Current;

			public bool MoveNext()
			{
				ObjectDisposedException.ThrowIf(context.DisposeCount != 0, context);

				if(failure != null)
					throw failure;

				return source.MoveNext();
			}

			public void Reset() => source.Reset();
			public void Dispose() => source.Dispose();
		}
	}
}
