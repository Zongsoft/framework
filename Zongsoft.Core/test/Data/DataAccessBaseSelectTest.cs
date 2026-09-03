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
	public void SelectAsync_PageableResult_ReturnsOriginalInstance()
	{
		var result = new PageableAsyncEnumerable([1, 2, 3]);
		using var accessor = new TestDataAccess(result);

		var sequence = Select(accessor);

		Assert.Same(result, sequence);
		var pageable = Assert.IsAssignableFrom<IPageable>(sequence);
		Assert.Same(result, pageable);
		Assert.False(pageable.Suppressed);
		Assert.Equal(1, accessor.ProviderCalls);
		Assert.Single(accessor.Contexts);
		Assert.Equal(1, accessor.Contexts[0].DisposeCount);
	}

	[Fact]
	public void SelectAsync_NonPageableResult_DoesNotInventPageability()
	{
		var result = new TestAsyncEnumerable([1, 2, 3]);
		using var accessor = new TestDataAccess(result);

		var sequence = Select(accessor, Paging.Page(1, 2));

		Assert.Same(result, sequence);
		Assert.False(sequence is IPageable);
	}

	[Fact]
	public async Task SelectAsync_PreparesOnceBeforeReturnAndRepeatedEnumerationDoesNotRepeatProvider()
	{
		var result = new TestAsyncEnumerable([1, 2]);
		using var accessor = new TestDataAccess(result);
		var sequence = Select(accessor);

		Assert.Equal(1, accessor.ProviderCalls);
		Assert.Equal(1, accessor.SelectedCalls);
		Assert.Single(accessor.Contexts);
		Assert.Equal(1, accessor.Contexts[0].DisposeCount);

		var first = await CollectAsync(sequence);
		var second = await CollectAsync(sequence);

		Assert.Equal([1, 2], first);
		Assert.Equal([1, 2], second);
		Assert.Equal(1, accessor.ProviderCalls);
		Assert.Equal(1, accessor.SelectedCalls);
		Assert.Single(accessor.Contexts);
	}

	[Fact]
	public void SelectAsync_SelectingShortCircuit_PreservesProvidedResult()
	{
		var result = new PageableAsyncEnumerable([7, 8]);
		using var accessor = new TestDataAccess();
		accessor.Selecting += (_, args) =>
		{
			args.Context.Result = result;
			args.Cancel = true;
		};

		var sequence = Select(accessor);

		Assert.Same(result, sequence);
		Assert.IsAssignableFrom<IPageable>(sequence);
		Assert.Equal(0, accessor.ProviderCalls);
		Assert.Equal(0, accessor.SelectedCalls);
		Assert.Single(accessor.Contexts);
	}

	private IAsyncEnumerable<int> Select(TestDataAccess accessor, Paging paging = null, CancellationToken cancellation = default) =>
		accessor.SelectAsync<int>(_entityName, null, (ISchema)null, paging, null, null, null, null, cancellation);

	private static async Task<List<int>> CollectAsync(IAsyncEnumerable<int> sequence)
	{
		var result = new List<int>();

		await foreach(var item in sequence)
			result.Add(item);

		return result;
	}

	private sealed class TestDataAccess(IEnumerable result = null) : DataAccessBase("P0")
	{
		private readonly IEnumerable _result = result ?? new TestAsyncEnumerable([1]);
		private int _providerCalls;
		private int _selectedCalls;

		public int ProviderCalls => _providerCalls;
		public int SelectedCalls => _selectedCalls;
		public List<TestSelectContext> Contexts { get; } = [];

		protected override DataSelectContextBase CreateSelectContext(string name, Type entityType, ICondition criteria, Grouping grouping, ISchema schema, Paging paging, Sorting[] sortings, IDataSelectOptions options)
		{
			var context = new TestSelectContext(this, name, entityType, grouping, criteria, schema, paging, sortings, options);
			this.Contexts.Add(context);
			return context;
		}

		protected override ValueTask OnSelectAsync(DataSelectContextBase context, CancellationToken cancellation)
		{
			Interlocked.Increment(ref _providerCalls);
			context.Result = _result;
			return ValueTask.CompletedTask;
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

	private class TestAsyncEnumerable(IEnumerable<int> items) : IAsyncEnumerable<int>, IEnumerable<int>
	{
		private readonly IEnumerable<int> _items = items;

		public IEnumerator<int> GetEnumerator() => _items.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

		public IAsyncEnumerator<int> GetAsyncEnumerator(CancellationToken cancellation = default) =>
			new Enumerator(_items.GetEnumerator(), cancellation);

		private sealed class Enumerator(IEnumerator<int> source, CancellationToken cancellation) : IAsyncEnumerator<int>
		{
			public int Current => source.Current;

			public ValueTask<bool> MoveNextAsync()
			{
				cancellation.ThrowIfCancellationRequested();
				return ValueTask.FromResult(source.MoveNext());
			}

			public ValueTask DisposeAsync()
			{
				source.Dispose();
				return ValueTask.CompletedTask;
			}
		}
	}

	private sealed class PageableAsyncEnumerable(IEnumerable<int> items, bool suppressed = false) : TestAsyncEnumerable(items), IPageable
	{
		public event EventHandler<PagingEventArgs> Paginated { add { } remove { } }
		public bool Suppressed { get; } = suppressed;
	}
}
