using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
	[InlineData(false)]
	[InlineData(true)]
	public async Task SequencerAsync_DefaultIntervalAndTokenReachProvider(bool byProperty)
	{
		var property = new DataEntitySimplexProperty("Id", System.Data.DbType.Int64, false);
		Mapping.Entities[_entityName].Properties.Add(property);
		await using var accessor = new TestDataAccess();
		using var cancellation = new CancellationTokenSource();
		var sequencer = accessor.Sequencer;

		Assert.Equal(11, byProperty ? await sequencer.IncreaseAsync(property) : await sequencer.IncreaseAsync(_entityName, "Id"));
		Assert.Equal(11, byProperty ? await sequencer.IncreaseAsync(property, CancellationToken.None) : await sequencer.IncreaseAsync(_entityName, "Id", CancellationToken.None));
		Assert.Equal(11, byProperty ? await sequencer.IncreaseAsync(property, cancellation.Token) : await sequencer.IncreaseAsync(_entityName, "Id", cancellation.Token));
		Assert.Equal(11, byProperty ? await sequencer.IncreaseAsync(property, 0, cancellation.Token) : await sequencer.IncreaseAsync(_entityName, "Id", 0, cancellation.Token));

		Assert.Equal(4, accessor.SequenceCalls.Count);
		Assert.All(accessor.SequenceCalls, call => Assert.Same(property, call.Property));
		Assert.Equal([1, 1, 1, 0], accessor.SequenceCalls.ConvertAll(call => call.Interval));
		Assert.Equal(CancellationToken.None, accessor.SequenceCalls[0].Cancellation);
		Assert.Equal(CancellationToken.None, accessor.SequenceCalls[1].Cancellation);
		Assert.Equal(cancellation.Token, accessor.SequenceCalls[2].Cancellation);
		Assert.Equal(cancellation.Token, accessor.SequenceCalls[3].Cancellation);
	}

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
		await using var accessor = new TestDataAccess(result, delayProvider: true);
		var selecting = Task.Run(() => this.Select(accessor, paging));

		await accessor.ProviderEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
		IAsyncEnumerable<int> sequence = null;
		IPageable pageable = null;

		try
		{
			sequence = await selecting.WaitAsync(TimeSpan.FromSeconds(5));
			pageable = Assert.IsAssignableFrom<IPageable>(sequence);
			Assert.Equal(suppressedBeforePreparation, pageable.Suppressed);
			Assert.Equal(1, accessor.ProviderCalls);
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
		await using var accessor = new TestDataAccess(result);
		var sequence = this.Select(accessor, Paging.Page(1, 2));
		var pageable = Assert.IsAssignableFrom<IPageable>(sequence);

		var first = await CollectAsync(sequence);
		var second = await CollectAsync(sequence);

		Assert.Equal([1, 2], first);
		Assert.Equal([1, 2], second);
		Assert.True(pageable.Suppressed);
		Assert.Equal(1, accessor.ProviderCalls);
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
		await using var accessor = new TestDataAccess(result, delayProvider: true);
		var sequence = this.Select(accessor, paging);
		var pageable = Assert.IsAssignableFrom<IPageable>(sequence);
		object sender = null;
		PagingEventArgs observed = null;
		pageable.Paginated += (source, args, _) =>
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
	public async Task SelectAsync_SelectingShortCircuit_PreservesProvidedResult()
	{
		var result = new TestAsyncEnumerable([7, 8]);
		using var cancellation = new CancellationTokenSource();
		await using var accessor = new TestDataAccess();
		accessor.Selecting += (_, args, _) =>
		{
			args.Context.Result = result;
			args.Cancel = true;
			return ValueTask.CompletedTask;
		};

		var sequence = this.Select(accessor, cancellation: cancellation.Token);

		Assert.Same(result, sequence);
		Assert.False(sequence is IPageable);
		Assert.Equal(0, accessor.ProviderCalls);
		Assert.Equal(0, accessor.SelectedCalls);
		Assert.Single(accessor.Contexts);
		Assert.Equal(1, accessor.Contexts[0].DisposeCount);
	}

	[Fact]
	public async Task SelectAsync_PreCanceled_SkipsProviderAndDisposesContextOnce()
	{
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		await using var accessor = new TestDataAccess(delayProvider: true);

		var exception = Assert.ThrowsAny<OperationCanceledException>(() => this.Select(accessor, cancellation: cancellation.Token));

		Assert.Equal(cancellation.Token, exception.CancellationToken);
		Assert.Equal(0, accessor.ProviderCalls);
		Assert.Equal(0, accessor.SelectedCalls);
		Assert.Single(accessor.Contexts);
		Assert.Equal(1, accessor.Contexts[0].DisposeCount);
	}

	[Fact]
	public async Task SelectAsync_PreparationCanceled_PropagatesTokenAndDisposesContextOnce()
	{
		using var cancellation = new CancellationTokenSource();
		await using var accessor = new TestDataAccess(delayProvider: true);
		var sequence = this.Select(accessor, cancellation: cancellation.Token);
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
		await using var accessor = new TestDataAccess(result);
		using var cancellation = new CancellationTokenSource();
		var sequence = this.Select(accessor);
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
		await using var accessor = new TestDataAccess(delayProvider: true);
		var sequence = this.Select(accessor);
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
	public async Task SelectAsync_FilteringFailure_ThrowsBeforeReturnAndDisposesContextOnce()
	{
		var failure = new InvalidOperationException("Expected pre-provider filter failure.");
		await using var accessor = new TestDataAccess { FilteringFailure = failure };

		var exception = Assert.Throws<InvalidOperationException>(() => this.Select(accessor));

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
		await using var accessor = new TestDataAccess { FilteredFailure = failure };
		var sequence = this.Select(accessor);

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
		await using var accessor = new TestDataAccess { ProviderFailure = failure };
		var sequence = this.Select(accessor);

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => CollectAsync(sequence));

		Assert.Same(failure, exception);
		Assert.Equal(1, accessor.ProviderCalls);
		Assert.Equal(0, accessor.SelectedCalls);
		Assert.Single(accessor.Contexts);
		Assert.Equal(1, accessor.Contexts[0].DisposeCount);
	}

	[Fact]
	public async Task SelectAsync_SelectedCallbackFailure_PreservesExceptionAndDisposesContextOnce()
	{
		var failure = new InvalidOperationException("Expected selected callback failure.");
		var result = new TestAsyncEnumerable([1]);
		await using var accessor = new TestDataAccess(result);
		var sequence = this.Select(accessor, selected: (_, _) => throw failure);

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => CollectAsync(sequence));

		Assert.Same(failure, exception);
		Assert.Equal(1, accessor.ProviderCalls);
		Assert.Equal(1, accessor.SelectedCalls);
		Assert.Equal(0, result.EnumerationCount);
		Assert.Single(accessor.Contexts);
		Assert.Equal(1, accessor.Contexts[0].DisposeCount);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task SelectAsync_PendingPreHook_PreparesOnce(bool shortCircuit)
	{
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var result = new TestAsyncEnumerable([7, 8]);
		await using var accessor = new TestDataAccess(result);
		using var cancellation = new CancellationTokenSource();
		var calls = 0;
		var sequence = this.Select(accessor, selecting: async (context, token) =>
		{
			calls++;
			Assert.Equal(cancellation.Token, token);
			await release.Task.WaitAsync(token);

			if(shortCircuit)
				context.Result = result;

			return shortCircuit;
		}, cancellation: cancellation.Token);
		var first = CollectAsync(sequence);
		var second = CollectAsync(sequence);

		try
		{
			Assert.False(first.IsCompleted);
			Assert.False(second.IsCompleted);
			Assert.Equal(1, calls);
			Assert.Equal(0, accessor.ProviderCalls);
			Assert.Equal(0, result.EnumerationCount);
			release.SetResult();
			Assert.All(await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(5)), values => Assert.Equal([7, 8], values));
			Assert.Equal(1, calls);
			Assert.Equal(shortCircuit ? 0 : 1, accessor.ProviderCalls);
			Assert.Equal(shortCircuit ? 0 : 1, accessor.SelectedCalls);
			Assert.Equal(1, Assert.Single(accessor.Contexts).DisposeCount);
			Assert.Equal(2, result.EnumerationCount);
			Assert.Equal(2, result.EnumeratorDisposeCount);
		}
		finally
		{
			release.TrySetResult();
			await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(5));
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task SelectAsync_UnenumeratedPreparation_DisposesContext(bool fails)
	{
		var result = new TestAsyncEnumerable([1, 2]);
		await using var accessor = new TestDataAccess(result, delayProvider: true)
		{
			ProviderFailure = fails ? new InvalidOperationException("Expected unenumerated preparation failure.") : null,
		};
		var sequence = this.Select(accessor);
		var context = Assert.Single(accessor.Contexts);

		try
		{
			await accessor.ProviderEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.Equal(0, context.DisposeCount);
			accessor.ReleaseProvider();
			await context.Disposed.Task.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.Equal(1, context.DisposeCount);
			Assert.Equal(1, accessor.ProviderCalls);
			Assert.Equal(0, result.EnumerationCount);
			Assert.Equal(0, result.EnumeratorDisposeCount);
			Assert.Equal(fails ? 0 : 1, accessor.SelectedCalls);
			GC.KeepAlive(sequence);
		}
		finally
		{
			accessor.ReleaseProvider();
		}
	}
	private IAsyncEnumerable<int> Select(TestDataAccess accessor, Paging paging = null, Func<DataSelectContextBase, CancellationToken, ValueTask<bool>> selecting = null, Func<DataSelectContextBase, CancellationToken, ValueTask> selected = null, CancellationToken cancellation = default) =>
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
		private int _selectedCalls;

		public TestDataAccess(IEnumerable result = null, bool delayProvider = false) : base("P0")
		{
			_result = result ?? new TestAsyncEnumerable([1]);

			if(!delayProvider)
				_providerRelease.TrySetResult();
		}

		public int ProviderCalls => _providerCalls;
		public int SelectedCalls => _selectedCalls;
		public Exception FilteringFailure { get; set; }
		public Exception FilteredFailure { get; set; }
		public Exception ProviderFailure { get; set; }
		public CancellationToken ProviderCancellation { get; private set; }
		public TaskCompletionSource ProviderEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public List<TestSelectContext> Contexts { get; } = [];
		public List<(IDataEntitySimplexProperty Property, int Interval, CancellationToken Cancellation)> SequenceCalls { get; } = [];

		public void ReleaseProvider() => _providerRelease.TrySetResult();

		protected override DataSelectContextBase CreateSelectContext(string name, Type modelType, ICondition criteria, Grouping grouping, ISchema schema, Paging paging, Sorting[] sortings, IDataSelectOptions options)
		{
			var context = new TestSelectContext(this, name, modelType, grouping, criteria, schema, paging, sortings, options);
			this.Contexts.Add(context);
			return context;
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

		protected override ValueTask OnSelectedAsync(DataSelectContextBase context, CancellationToken cancellation)
		{
			Interlocked.Increment(ref _selectedCalls);
			return base.OnSelectedAsync(context, cancellation);
		}

		protected override ValueTask OnFilteringAsync(IDataAccessContextBase context, CancellationToken cancellation)
		{
			if(this.FilteringFailure != null)
				throw this.FilteringFailure;

			return base.OnFilteringAsync(context, cancellation);
		}

		protected override ValueTask OnFilteredAsync(IDataAccessContextBase context, CancellationToken cancellation)
		{
			if(this.FilteredFailure != null)
				throw this.FilteredFailure;

			return base.OnFilteredAsync(context, cancellation);
		}

		protected override ISchemaParser CreateSchema() => throw new NotSupportedException();
		protected override IDataSequencer CreateSequencer() => new TestSequencer(this);
		protected override DataExecuteContextBase CreateExecuteContext(string name, bool isScalar, Type resultType, IEnumerable<Parameter> parameters, IDataExecuteOptions options) => throw new NotSupportedException();
		protected override DataExistContextBase CreateExistContext(string name, ICondition criteria, IDataExistsOptions options) => throw new NotSupportedException();
		protected override DataAggregateContextBase CreateAggregateContext(string name, DataAggregate aggregate, ICondition criteria, IDataAggregateOptions options) => throw new NotSupportedException();
		protected override DataImportContextBase CreateImportContext(string name, object data, IEnumerable<string> members, IDataImportOptions options) => throw new NotSupportedException();
		protected override DataDeleteContextBase CreateDeleteContext(string name, ICondition criteria, ISchema schema, IDataDeleteOptions options) => throw new NotSupportedException();
		protected override DataInsertContextBase CreateInsertContext(string name, bool isMultiple, object data, ISchema schema, IDataInsertOptions options) => throw new NotSupportedException();
		protected override DataUpsertContextBase CreateUpsertContext(string name, bool isMultiple, object data, ISchema schema, IDataUpsertOptions options) => throw new NotSupportedException();
		protected override DataUpdateContextBase CreateUpdateContext(string name, object data, ICondition criteria, ISchema schema, IDataUpdateOptions options) => throw new NotSupportedException();

		protected override ValueTask OnExecuteAsync(DataExecuteContextBase context, CancellationToken cancellation) => throw new NotSupportedException();
		protected override ValueTask OnExistsAsync(DataExistContextBase context, CancellationToken cancellation) => throw new NotSupportedException();
		protected override ValueTask OnAggregateAsync(DataAggregateContextBase context, CancellationToken cancellation) => throw new NotSupportedException();
		protected override ValueTask OnImportAsync(DataImportContextBase context, CancellationToken cancellation = default) => throw new NotSupportedException();
		protected override ValueTask OnDeleteAsync(DataDeleteContextBase context, CancellationToken cancellation) => throw new NotSupportedException();
		protected override ValueTask OnInsertAsync(DataInsertContextBase context, CancellationToken cancellation) => throw new NotSupportedException();
		protected override ValueTask OnUpsertAsync(DataUpsertContextBase context, CancellationToken cancellation) => throw new NotSupportedException();
		protected override ValueTask OnUpdateAsync(DataUpdateContextBase context, CancellationToken cancellation) => throw new NotSupportedException();
		private sealed class TestSequencer(DataAccessBase accessor) : DataSequencerBase(accessor)
		{
			public override ValueTask<long> IncreaseAsync(IDataEntitySimplexProperty property, int interval, CancellationToken cancellation = default)
			{
				((TestDataAccess)this.Accessor).SequenceCalls.Add((property, interval, cancellation));
				return ValueTask.FromResult(11L);
			}
		}
	}

	private sealed class TestSelectContext : DataSelectContextBase
	{
		private int _disposeCount;

		public TestSelectContext(IDataAccess dataAccess, string name, Type modelType, Grouping grouping, ICondition criteria, ISchema schema, Paging paging, Sorting[] sortings, IDataSelectOptions options) :
			base(dataAccess, name, modelType, grouping, criteria, schema, paging, sortings, options) { }

		public int DisposeCount => _disposeCount;
		public TaskCompletionSource Disposed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public override TFeature GetFeature<TFeature>() => default;

		protected override void Dispose(bool disposing)
		{
			Interlocked.Increment(ref _disposeCount);
			base.Dispose(disposing);
			this.Disposed.TrySetResult();
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
			return new Enumerator(_items.GetEnumerator(), cancellation, this.OnFirstMoveAsync, () => Interlocked.Increment(ref _enumeratorDisposeCount));
		}

		protected virtual ValueTask OnFirstMoveAsync(CancellationToken cancellation) => ValueTask.CompletedTask;

		private sealed class Enumerator(IEnumerator<int> source, CancellationToken cancellation, Func<CancellationToken, ValueTask> firstMove, Action disposed) : IAsyncEnumerator<int>
		{
			private bool _started;
			private int _disposed;

			public int Current => source.Current;

			public async ValueTask<bool> MoveNextAsync()
			{
				cancellation.ThrowIfCancellationRequested();

				if(!_started)
				{
					_started = true;
					await firstMove(cancellation);
				}

				return source.MoveNext();
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
		private Func<object, PagingEventArgs, CancellationToken, ValueTask> _paginated;
		private int _subscriptionCount;
		private int _unsubscriptionCount;

		public event Func<object, PagingEventArgs, CancellationToken, ValueTask> Paginated
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

		protected override async ValueTask OnFirstMoveAsync(CancellationToken cancellation)
		{
			if(_eventArgs != null && _paginated != null)
			{
				foreach(var handler in _paginated.GetInvocationList().Cast<Func<object, PagingEventArgs, CancellationToken, ValueTask>>())
					await handler(this, _eventArgs, cancellation);
			}
		}
	}
}
