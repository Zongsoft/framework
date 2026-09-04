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

	[Theory]
	[InlineData(PagingMode.None, true, false)]
	[InlineData(PagingMode.Disabled, true, false)]
	[InlineData(PagingMode.Enabled, false, true)]
	public async Task SelectAsync_NormalHandle_ReturnsBeforePreparationAndDelegatesPageability(PagingMode mode, bool suppressedBeforePreparation, bool suppressedAfterPreparation)
	{
		var paging = mode switch
		{
			PagingMode.Disabled => Paging.Disabled,
			PagingMode.Enabled => Paging.Page(1, 2),
			_ => null,
		};
		var result = new PageableAsyncEnumerable([1, 2, 3], suppressed: suppressedAfterPreparation);
		using var accessor = new TestDataAccess(result, delayProvider: true);
		var selecting = Task.Run(() => Select(accessor, paging));

		await accessor.ProviderEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
		IAsyncEnumerable<int> sequence = null;
		IPageable pageable = null;

		try
		{
			sequence = await selecting.WaitAsync(TimeSpan.FromSeconds(5));
			pageable = Assert.IsAssignableFrom<IPageable>(sequence);
			Assert.Equal(suppressedBeforePreparation, pageable.Suppressed);
			Assert.Equal(1, accessor.ProviderCalls);
			Assert.Equal(0, accessor.SynchronousProviderCalls);
			Assert.Single(accessor.Contexts);
			Assert.Equal(0, accessor.Contexts[0].DisposeCount);
		}
		finally
		{
			accessor.ReleaseProvider();
		}

		Assert.Equal([1, 2, 3], await CollectAsync(sequence));
		Assert.Equal(suppressedAfterPreparation, pageable.Suppressed);
		Assert.Equal(1, accessor.SelectedCalls);
		Assert.Equal(1, accessor.Contexts[0].DisposeCount);
	}

	[Fact]
	public async Task SelectAsync_PreparesOnceForSequentialEnumerations()
	{
		var result = new TestAsyncEnumerable([1, 2]);
		using var accessor = new TestDataAccess(result);
		var sequence = Select(accessor, Paging.Page(1, 2));
		var pageable = Assert.IsAssignableFrom<IPageable>(sequence);

		var first = await CollectAsync(sequence);
		var second = await CollectAsync(sequence);

		Assert.Equal([1, 2], first);
		Assert.Equal([1, 2], second);
		Assert.True(pageable.Suppressed);
		Assert.Equal(1, accessor.ProviderCalls);
		Assert.Equal(0, accessor.SynchronousProviderCalls);
		Assert.Equal(1, accessor.SelectedCalls);
		Assert.Single(accessor.Contexts);
		Assert.Equal(1, accessor.Contexts[0].DisposeCount);
		Assert.Equal(2, result.EnumerationCount);
		Assert.Equal(2, result.EnumeratorDisposeCount);
	}

	[Fact]
	public async Task SelectAsync_PaginationHandlerFailure_UsesHandleAsSenderAndCleansUp()
	{
		var failure = new InvalidOperationException("Expected pagination observer failure.");
		var paging = Paging.Page(2, 5);
		var eventArgs = new PagingEventArgs(_entityName, paging);
		var result = new PageableAsyncEnumerable([1, 2], eventArgs);
		using var accessor = new TestDataAccess(result, delayProvider: true);
		var sequence = Select(accessor, paging);
		var pageable = Assert.IsAssignableFrom<IPageable>(sequence);
		object sender = null;
		PagingEventArgs observed = null;
		pageable.Paginated += (source, args) =>
		{
			sender = source;
			observed = args;
			throw failure;
		};

		accessor.ReleaseProvider();
		var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => CollectAsync(sequence));

		Assert.Same(failure, exception);
		Assert.Same(sequence, sender);
		Assert.Same(eventArgs, observed);
		Assert.Equal(1, result.SubscriptionCount);
		Assert.Equal(1, result.SubscriberCount);
		Assert.Equal(1, result.EnumerationCount);
		Assert.Equal(1, result.EnumeratorDisposeCount);
		Assert.Single(accessor.Contexts);
		Assert.Equal(1, accessor.Contexts[0].DisposeCount);
	}

	[Fact]
	public void SelectAsync_SelectingShortCircuit_PreservesProvidedResult()
	{
		var result = new TestAsyncEnumerable([7, 8]);
		using var cancellation = new CancellationTokenSource();
		using var accessor = new TestDataAccess();
		accessor.Selecting += (_, args) =>
		{
			args.Context.Result = result;
			args.Cancel = true;
		};

		var sequence = Select(accessor, cancellation: cancellation.Token);

		Assert.Same(result, sequence);
		Assert.False(sequence is IPageable);
		Assert.Equal(0, accessor.ProviderCalls);
		Assert.Equal(0, accessor.SynchronousProviderCalls);
		Assert.Equal(0, accessor.SelectedCalls);
		Assert.Single(accessor.Contexts);
		Assert.Equal(1, accessor.Contexts[0].DisposeCount);
	}

	[Fact]
	public void SelectAsync_PreCanceled_SkipsProviderAndDisposesContextOnce()
	{
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		using var accessor = new TestDataAccess(delayProvider: true);

		var exception = Assert.ThrowsAny<OperationCanceledException>(() => Select(accessor, cancellation: cancellation.Token));

		Assert.Equal(cancellation.Token, exception.CancellationToken);
		Assert.Equal(0, accessor.ProviderCalls);
		Assert.Equal(0, accessor.SynchronousProviderCalls);
		Assert.Equal(0, accessor.SelectedCalls);
		Assert.Single(accessor.Contexts);
		Assert.Equal(1, accessor.Contexts[0].DisposeCount);
	}

	[Fact]
	public async Task SelectAsync_PreparationCanceled_PropagatesTokenAndDisposesContextOnce()
	{
		using var cancellation = new CancellationTokenSource();
		using var accessor = new TestDataAccess(delayProvider: true);
		var sequence = Select(accessor, cancellation: cancellation.Token);
		await accessor.ProviderEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));

		try
		{
			cancellation.Cancel();
			var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => CollectAsync(sequence).WaitAsync(TimeSpan.FromSeconds(5)));

			Assert.Equal(cancellation.Token, exception.CancellationToken);
			Assert.Equal(cancellation.Token, accessor.ProviderCancellation);
			Assert.Equal(1, accessor.ProviderCalls);
			Assert.Equal(0, accessor.SelectedCalls);
			Assert.Single(accessor.Contexts);
			Assert.Equal(1, accessor.Contexts[0].DisposeCount);
		}
		finally
		{
			accessor.ReleaseProvider();
		}
	}

	[Fact]
	public async Task SelectAsync_EnumerationCanceled_PropagatesTokenAndDisposesEnumerator()
	{
		var result = new TestAsyncEnumerable([1, 2, 3]);
		using var accessor = new TestDataAccess(result);
		using var cancellation = new CancellationTokenSource();
		var sequence = Select(accessor);
		var enumerator = sequence.GetAsyncEnumerator(cancellation.Token);

		try
		{
			Assert.True(await enumerator.MoveNextAsync());
			Assert.Equal(1, enumerator.Current);
			cancellation.Cancel();

			var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => enumerator.MoveNextAsync().AsTask());
			Assert.Equal(cancellation.Token, exception.CancellationToken);
		}
		finally
		{
			await enumerator.DisposeAsync();
		}

		Assert.Equal(1, result.EnumerationCount);
		Assert.Equal(1, result.EnumeratorDisposeCount);
		Assert.Single(accessor.Contexts);
		Assert.Equal(1, accessor.Contexts[0].DisposeCount);
	}

	[Fact]
	public async Task SelectAsync_EnumeratorCancellation_DoesNotCancelSharedPreparation()
	{
		using var cancellation = new CancellationTokenSource();
		using var accessor = new TestDataAccess(delayProvider: true);
		var sequence = Select(accessor);
		var enumerator = sequence.GetAsyncEnumerator(cancellation.Token);
		var moving = enumerator.MoveNextAsync().AsTask();

		await accessor.ProviderEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
		cancellation.Cancel();

		try
		{
			var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => moving);
			Assert.Equal(cancellation.Token, exception.CancellationToken);
			Assert.Equal(0, accessor.Contexts[0].DisposeCount);
		}
		finally
		{
			await enumerator.DisposeAsync();
			accessor.ReleaseProvider();
		}

		Assert.Equal([1], await CollectAsync(sequence));
		Assert.Equal(1, accessor.ProviderCalls);
		Assert.Equal(1, accessor.SelectedCalls);
		Assert.Equal(1, accessor.Contexts[0].DisposeCount);
	}

	[Fact]
	public void SelectAsync_FilteringFailure_ThrowsBeforeReturnAndDisposesContextOnce()
	{
		var failure = new InvalidOperationException("Expected pre-provider filter failure.");
		using var accessor = new TestDataAccess { FilteringFailure = failure };

		var exception = Assert.Throws<InvalidOperationException>(() => Select(accessor));

		Assert.Same(failure, exception);
		Assert.Equal(0, accessor.ProviderCalls);
		Assert.Equal(0, accessor.SelectedCalls);
		Assert.Single(accessor.Contexts);
		Assert.Equal(1, accessor.Contexts[0].DisposeCount);
	}

	[Fact]
	public async Task SelectAsync_FilteredFailure_PropagatesDuringEnumerationAndDisposesContextOnce()
	{
		var failure = new InvalidOperationException("Expected post-provider filter failure.");
		using var accessor = new TestDataAccess { FilteredFailure = failure };
		var sequence = Select(accessor);

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => CollectAsync(sequence));

		Assert.Same(failure, exception);
		Assert.Equal(1, accessor.ProviderCalls);
		Assert.Equal(0, accessor.SelectedCalls);
		Assert.Single(accessor.Contexts);
		Assert.Equal(1, accessor.Contexts[0].DisposeCount);
	}

	[Fact]
	public async Task SelectAsync_ProviderFailure_PreservesExceptionAndDisposesContextOnce()
	{
		var failure = new InvalidOperationException("Expected provider failure.");
		using var accessor = new TestDataAccess { ProviderFailure = failure };
		var sequence = Select(accessor);

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => CollectAsync(sequence));

		Assert.Same(failure, exception);
		Assert.Equal(1, accessor.ProviderCalls);
		Assert.Equal(0, accessor.SynchronousProviderCalls);
		Assert.Equal(0, accessor.SelectedCalls);
		Assert.Single(accessor.Contexts);
		Assert.Equal(1, accessor.Contexts[0].DisposeCount);
	}

	[Fact]
	public async Task SelectAsync_SelectedCallbackFailure_PreservesExceptionAndDisposesContextOnce()
	{
		var failure = new InvalidOperationException("Expected selected callback failure.");
		var result = new TestAsyncEnumerable([1]);
		using var accessor = new TestDataAccess(result);
		var sequence = Select(accessor, selected: _ => throw failure);

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => CollectAsync(sequence));

		Assert.Same(failure, exception);
		Assert.Equal(1, accessor.ProviderCalls);
		Assert.Equal(0, accessor.SynchronousProviderCalls);
		Assert.Equal(1, accessor.SelectedCalls);
		Assert.Equal(0, result.EnumerationCount);
		Assert.Single(accessor.Contexts);
		Assert.Equal(1, accessor.Contexts[0].DisposeCount);
	}

	private IAsyncEnumerable<int> Select(TestDataAccess accessor, Paging paging = null, Func<DataSelectContextBase, bool> selecting = null, Action<DataSelectContextBase> selected = null, CancellationToken cancellation = default) =>
		accessor.SelectAsync<int>(_entityName, null, (ISchema)null, paging, null, null, selecting, selected, cancellation);

	private static async Task<List<int>> CollectAsync(IAsyncEnumerable<int> sequence)
	{
		var result = new List<int>();

		await foreach(var item in sequence)
			result.Add(item);

		return result;
	}

	private sealed class TestDataAccess : DataAccessBase
	{
		private readonly IEnumerable _result;
		private readonly TaskCompletionSource _providerRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);
		private int _providerCalls;
		private int _synchronousProviderCalls;
		private int _selectedCalls;

		public TestDataAccess(IEnumerable result = null, bool delayProvider = false) : base("P0")
		{
			_result = result ?? new TestAsyncEnumerable([1]);

			if(!delayProvider)
				_providerRelease.TrySetResult();
		}

		public int ProviderCalls => _providerCalls;
		public int SynchronousProviderCalls => _synchronousProviderCalls;
		public int SelectedCalls => _selectedCalls;
		public Exception FilteringFailure { get; set; }
		public Exception FilteredFailure { get; set; }
		public Exception ProviderFailure { get; set; }
		public CancellationToken ProviderCancellation { get; private set; }
		public TaskCompletionSource ProviderEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public List<TestSelectContext> Contexts { get; } = [];

		public void ReleaseProvider() => _providerRelease.TrySetResult();

		protected override DataSelectContextBase CreateSelectContext(string name, Type modelType, ICondition criteria, Grouping grouping, ISchema schema, Paging paging, Sorting[] sortings, IDataSelectOptions options)
		{
			var context = new TestSelectContext(this, name, modelType, grouping, criteria, schema, paging, sortings, options);
			this.Contexts.Add(context);
			return context;
		}

		protected override void OnSelect(DataSelectContextBase context)
		{
			Interlocked.Increment(ref _synchronousProviderCalls);
			throw new InvalidOperationException("The asynchronous query entry must not call the synchronous provider.");
		}

		protected override async ValueTask OnSelectAsync(DataSelectContextBase context, CancellationToken cancellation)
		{
			Interlocked.Increment(ref _providerCalls);
			this.ProviderCancellation = cancellation;
			this.ProviderEntered.TrySetResult();

			await _providerRelease.Task.WaitAsync(cancellation).ConfigureAwait(false);

			if(this.ProviderFailure != null)
				throw this.ProviderFailure;

			context.Result = _result;
		}

		protected override void OnSelected(DataSelectContextBase context)
		{
			Interlocked.Increment(ref _selectedCalls);
			base.OnSelected(context);
		}

		protected override void OnFiltering(IDataAccessContextBase context)
		{
			if(this.FilteringFailure != null)
				throw this.FilteringFailure;

			base.OnFiltering(context);
		}

		protected override void OnFiltered(IDataAccessContextBase context)
		{
			if(this.FilteredFailure != null)
				throw this.FilteredFailure;

			base.OnFiltered(context);
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

	public enum PagingMode
	{
		None,
		Disabled,
		Enabled,
	}

	private class TestAsyncEnumerable(IEnumerable<int> items) : IAsyncEnumerable<int>, IEnumerable<int>
	{
		private readonly IEnumerable<int> _items = items;
		private int _enumerationCount;
		private int _enumeratorDisposeCount;

		public int EnumerationCount => _enumerationCount;
		public int EnumeratorDisposeCount => _enumeratorDisposeCount;

		public IEnumerator<int> GetEnumerator() => _items.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

		public virtual IAsyncEnumerator<int> GetAsyncEnumerator(CancellationToken cancellation = default)
		{
			Interlocked.Increment(ref _enumerationCount);
			return new Enumerator(_items.GetEnumerator(), cancellation, this.OnFirstMove, () => Interlocked.Increment(ref _enumeratorDisposeCount));
		}

		protected virtual void OnFirstMove() { }

		private sealed class Enumerator(IEnumerator<int> source, CancellationToken cancellation, Action firstMove, Action disposed) : IAsyncEnumerator<int>
		{
			private bool _started;
			private int _disposed;

			public int Current => source.Current;

			public ValueTask<bool> MoveNextAsync()
			{
				cancellation.ThrowIfCancellationRequested();

				if(!_started)
				{
					_started = true;
					firstMove();
				}

				return ValueTask.FromResult(source.MoveNext());
			}

			public ValueTask DisposeAsync()
			{
				if(Interlocked.Exchange(ref _disposed, 1) == 0)
				{
					source.Dispose();
					disposed();
				}

				return ValueTask.CompletedTask;
			}
		}
	}

	private sealed class PageableAsyncEnumerable(IEnumerable<int> items, PagingEventArgs eventArgs = null, bool suppressed = false) : TestAsyncEnumerable(items), IPageable
	{
		private readonly PagingEventArgs _eventArgs = eventArgs;
		private EventHandler<PagingEventArgs> _paginated;
		private int _subscriptionCount;
		private int _unsubscriptionCount;

		public event EventHandler<PagingEventArgs> Paginated
		{
			add
			{
				_paginated += value;
				Interlocked.Increment(ref _subscriptionCount);
			}
			remove
			{
				_paginated -= value;
				Interlocked.Increment(ref _unsubscriptionCount);
			}
		}

		public bool Suppressed { get; } = suppressed;
		public int SubscriptionCount => _subscriptionCount;
		public int UnsubscriptionCount => _unsubscriptionCount;
		public int SubscriberCount => _paginated?.GetInvocationList().Length ?? 0;

		protected override void OnFirstMove()
		{
			if(_eventArgs != null)
				_paginated?.Invoke(this, _eventArgs);
		}
	}
}
