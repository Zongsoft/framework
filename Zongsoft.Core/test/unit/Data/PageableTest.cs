using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace Zongsoft.Data.Tests;

public class PageableTest
{
	[Fact]
	public void Filter_NonGeneric_UsesActualCurrentItem()
	{
		var source = new PageableEnumerable<int>([1, 2, 3], CreatePagingEventArgs());
		var result = Pageable.Filter((IEnumerable)source, value => value is int number && number > 1);

		Assert.Equal([2, 3], Collect(result));
	}

	[Fact]
	public void Filter_Generic_PreservesReplacement()
	{
		var source = new PageableEnumerable<int>([1, 2, 3], CreatePagingEventArgs());
		var result = Pageable.Filter<int>(source, (ref int value) =>
		{
			value *= 10;
			return value >= 20;
		});

		Assert.Equal([20, 30], result);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task FilterAsync_PageableAndPlain_AdvancesUntilMatchWithoutSkippingFirst(bool pageable)
	{
		var inspected = new List<int>();
		IAsyncEnumerable<int> source = pageable ?
			new PageableAsyncEnumerable<int>([1, 3, 4], CreatePagingEventArgs()) :
			new AsyncEnumerable<int>([1, 3, 4]);
		var result = Pageable.Filter<int>(source, (ref int value) =>
		{
			inspected.Add(value);
			return value % 2 == 0;
		});

		Assert.Equal([4], await CollectAsync(result));
		Assert.Equal([1, 3, 4], inspected);
		Assert.Equal(pageable, result is IPageable);
	}

	[Fact]
	public void Map_RepeatedCurrent_InvokesMapperOnce()
	{
		var invocations = 0;
		var source = new PageableEnumerable<int>([5], CreatePagingEventArgs());
		var result = Pageable.Map<int, int>(source, value =>
		{
			invocations++;
			return value * 2;
		});

		using var iterator = result.GetEnumerator();
		Assert.True(iterator.MoveNext());
		Assert.Equal(10, iterator.Current);
		Assert.Equal(10, iterator.Current);
		Assert.Equal(1, invocations);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task MapAsync_RepeatedCurrent_InvokesMapperOnce(bool pageable)
	{
		var invocations = 0;
		IAsyncEnumerable<int> source = pageable ?
			new PageableAsyncEnumerable<int>([5], CreatePagingEventArgs()) :
			new AsyncEnumerable<int>([5]);
		var result = Pageable.Map<int, int>(source, value =>
		{
			invocations++;
			return value * 2;
		});
		var iterator = result.GetAsyncEnumerator();

		try
		{
			Assert.True(await iterator.MoveNextAsync());
			Assert.Equal(10, iterator.Current);
			Assert.Equal(10, iterator.Current);
			Assert.Equal(1, invocations);
		}
		finally
		{
			await iterator.DisposeAsync();
		}
	}

	[Theory]
	[InlineData((int)TransformKind.SyncFilter)]
	[InlineData((int)TransformKind.SyncMap)]
	[InlineData((int)TransformKind.AsyncFilter)]
	[InlineData((int)TransformKind.AsyncMap)]
	public async Task PagedTransforms_RepeatedEnumeration_ForwardsSameEventFromOuterResult(int kindValue)
	{
		var eventArgs = CreatePagingEventArgs();
		object result;
		Func<Task> enumerate;
		Action<bool> setSuppressed;
		Func<int> getSubscriberCount;

		switch((TransformKind)kindValue)
		{
			case TransformKind.SyncFilter:
				var filteredSource = new PageableEnumerable<int>([1, 2], eventArgs);
				var filtered = Pageable.Filter<int>(filteredSource, (ref int value) => true);
				result = filtered;
				setSuppressed = value => filteredSource.Suppressed = value;
				getSubscriberCount = () => filteredSource.SubscriberCount;
				enumerate = () =>
				{
					Assert.Equal([1, 2], filtered);
					Assert.Equal([1, 2], filtered);
					return Task.CompletedTask;
				};
				break;
			case TransformKind.SyncMap:
				var mappedSource = new PageableEnumerable<int>([1, 2], eventArgs);
				var mapped = Pageable.Map<int, int>(mappedSource, value => value);
				result = mapped;
				setSuppressed = value => mappedSource.Suppressed = value;
				getSubscriberCount = () => mappedSource.SubscriberCount;
				enumerate = () =>
				{
					Assert.Equal([1, 2], mapped);
					Assert.Equal([1, 2], mapped);
					return Task.CompletedTask;
				};
				break;
			case TransformKind.AsyncFilter:
				var asyncFilteredSource = new PageableAsyncEnumerable<int>([1, 2], eventArgs);
				var asyncFiltered = Pageable.Filter<int>(asyncFilteredSource, (ref int value) => true);
				result = asyncFiltered;
				setSuppressed = value => asyncFilteredSource.Suppressed = value;
				getSubscriberCount = () => asyncFilteredSource.SubscriberCount;
				enumerate = async () =>
				{
					Assert.Equal([1, 2], await CollectAsync(asyncFiltered));
					Assert.Equal([1, 2], await CollectAsync(asyncFiltered));
				};
				break;
			case TransformKind.AsyncMap:
				var asyncMappedSource = new PageableAsyncEnumerable<int>([1, 2], eventArgs);
				var asyncMapped = Pageable.Map<int, int>(asyncMappedSource, value => value);
				result = asyncMapped;
				setSuppressed = value => asyncMappedSource.Suppressed = value;
				getSubscriberCount = () => asyncMappedSource.SubscriberCount;
				enumerate = async () =>
				{
					Assert.Equal([1, 2], await CollectAsync(asyncMapped));
					Assert.Equal([1, 2], await CollectAsync(asyncMapped));
				};
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(kindValue));
		}

		var senders = new List<object>();
		var observed = new List<PagingEventArgs>();
		var pageable = Assert.IsAssignableFrom<IPageable>(result);
		EventHandler<PagingEventArgs> handler = (sender, args) =>
		{
			senders.Add(sender);
			observed.Add(args);
		};
		pageable.Paginated += handler;
		Assert.Equal(1, getSubscriberCount());
		Assert.False(pageable.Suppressed);
		setSuppressed(true);
		Assert.True(pageable.Suppressed);

		await enumerate();

		Assert.Equal(2, senders.Count);
		Assert.All(senders, sender => Assert.Same(result, sender));
		Assert.All(observed, args => Assert.Same(eventArgs, args));

		pageable.Paginated -= handler;
		Assert.Equal(0, getSubscriberCount());
		await enumerate();
		Assert.Equal(2, senders.Count);
	}

	[Fact]
	public async Task FilterAsync_Cancellation_DisposesSource()
	{
		using var cancellation = new CancellationTokenSource();
		var source = new TrackingAsyncEnumerable<int>([1, 2]);
		var result = Pageable.Filter<int>(source, (ref int value) => true);
		var iterator = result.GetAsyncEnumerator(cancellation.Token);

		try
		{
			Assert.True(await iterator.MoveNextAsync());
			cancellation.Cancel();
			var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await iterator.MoveNextAsync());
			Assert.Equal(cancellation.Token, exception.CancellationToken);
		}
		finally
		{
			await iterator.DisposeAsync();
		}

		Assert.Equal(cancellation.Token, source.Cancellation);
		Assert.Equal(1, source.DisposalCount);
	}

	[Fact]
	public async Task Transforms_EarlyExit_DisposesSource()
	{
		var syncSource = new TrackingEnumerable<int>([1, 2]);
		var filtered = Pageable.Filter<int>(syncSource, (ref int value) => true);
		var syncIterator = filtered.GetEnumerator();
		Assert.True(syncIterator.MoveNext());
		syncIterator.Dispose();

		var asyncSource = new TrackingAsyncEnumerable<int>([1, 2]);
		var mapped = Pageable.Map<int, int>(asyncSource, value => value);
		var asyncIterator = mapped.GetAsyncEnumerator();
		Assert.True(await asyncIterator.MoveNextAsync());
		await asyncIterator.DisposeAsync();

		Assert.Equal(1, syncSource.DisposalCount);
		Assert.Equal(1, asyncSource.DisposalCount);
	}

	[Fact]
	public async Task PagedTransform_ConcurrentEnumerations_ForwardEveryEvent()
	{
		var eventArgs = CreatePagingEventArgs();
		var source = new ConcurrentPageableAsyncEnumerable<int>(1, eventArgs);
		var result = Pageable.Map<int, int>(source, value => value);
		var pageable = Assert.IsAssignableFrom<IPageable>(result);
		var senders = new List<object>();
		var observed = new List<PagingEventArgs>();
		pageable.Paginated += (sender, args) =>
		{
			lock(senders)
			{
				senders.Add(sender);
				observed.Add(args);
			}
		};
		Assert.Equal(1, source.SubscriberCount);

		var results = await Task.WhenAll(CollectAsync(result), CollectAsync(result));

		Assert.All(results, items => Assert.Equal([1], items));
		Assert.Equal(2, senders.Count);
		Assert.All(senders, sender => Assert.Same(result, sender));
		Assert.All(observed, args => Assert.Same(eventArgs, args));
	}

	[Fact]
	public void NonPageableTransforms_DoNotInventPageability()
	{
		var syncFilter = Pageable.Filter<int>([1, 2], (ref int value) => true);
		var syncMap = Pageable.Map<int, int>([1, 2], value => value);
		var asyncFilter = Pageable.Filter<int>(new AsyncEnumerable<int>([1, 2]), (ref int value) => true);
		var asyncMap = Pageable.Map<int, int>(new AsyncEnumerable<int>([1, 2]), value => value);

		Assert.False(syncFilter is IPageable);
		Assert.False(syncMap is IPageable);
		Assert.False(asyncFilter is IPageable);
		Assert.False(asyncMap is IPageable);
	}

	private static PagingEventArgs CreatePagingEventArgs() => new("PageableTest", Paging.Page(1, 10));

	private static List<int> Collect(IEnumerable source)
	{
		var result = new List<int>();

		foreach(var item in source)
			result.Add((int)item);

		return result;
	}

	private static async Task<List<T>> CollectAsync<T>(IAsyncEnumerable<T> source)
	{
		var result = new List<T>();

		await foreach(var item in source)
			result.Add(item);

		return result;
	}

	private sealed class AsyncEnumerable<T>(IEnumerable<T> items) : IAsyncEnumerable<T>
	{
		public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellation = default)
		{
			foreach(var item in items)
			{
				cancellation.ThrowIfCancellationRequested();
				yield return item;
				await Task.CompletedTask;
			}
		}
	}

	private sealed class PageableEnumerable<T>(IEnumerable<T> items, PagingEventArgs eventArgs) : IEnumerable<T>, IPageable
	{
		private EventHandler<PagingEventArgs> _paginated;

		public event EventHandler<PagingEventArgs> Paginated
		{
			add => _paginated += value;
			remove => _paginated -= value;
		}

		public int SubscriberCount => _paginated?.GetInvocationList().Length ?? 0;
		public bool Suppressed { get; set; }

		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		public IEnumerator<T> GetEnumerator() => this.Enumerate().GetEnumerator();

		private IEnumerable<T> Enumerate()
		{
			var notified = false;

			foreach(var item in items)
			{
				if(!notified)
				{
					notified = true;
					_paginated?.Invoke(this, eventArgs);
				}

				yield return item;
			}
		}
	}

	private sealed class PageableAsyncEnumerable<T>(IEnumerable<T> items, PagingEventArgs eventArgs) : IAsyncEnumerable<T>, IPageable
	{
		private EventHandler<PagingEventArgs> _paginated;

		public event EventHandler<PagingEventArgs> Paginated
		{
			add => _paginated += value;
			remove => _paginated -= value;
		}

		public int SubscriberCount => _paginated?.GetInvocationList().Length ?? 0;
		public bool Suppressed { get; set; }

		public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellation = default)
		{
			var notified = false;

			foreach(var item in items)
			{
				cancellation.ThrowIfCancellationRequested();

				if(!notified)
				{
					notified = true;
					_paginated?.Invoke(this, eventArgs);
				}

				yield return item;
				await Task.CompletedTask;
			}
		}
	}

	private sealed class TrackingEnumerable<T>(IReadOnlyList<T> items) : IEnumerable<T>
	{
		public int DisposalCount { get; private set; }

		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		public IEnumerator<T> GetEnumerator() => new Enumerator(items, () => this.DisposalCount++);

		private sealed class Enumerator(IReadOnlyList<T> items, Action dispose) : IEnumerator<T>
		{
			private int _index = -1;
			private bool _disposed;

			object IEnumerator.Current => this.Current;
			public T Current => items[_index];
			public bool MoveNext() => ++_index < items.Count;
			public void Reset() => _index = -1;

			public void Dispose()
			{
				if(!_disposed)
				{
					_disposed = true;
					dispose();
				}
			}
		}
	}

	private sealed class TrackingAsyncEnumerable<T>(IReadOnlyList<T> items) : IAsyncEnumerable<T>
	{
		public CancellationToken Cancellation { get; private set; }
		public int DisposalCount { get; private set; }

		public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellation = default)
		{
			this.Cancellation = cancellation;
			return new Enumerator(items, cancellation, () => this.DisposalCount++);
		}

		private sealed class Enumerator(IReadOnlyList<T> items, CancellationToken cancellation, Action dispose) : IAsyncEnumerator<T>
		{
			private int _index = -1;
			private bool _disposed;

			public T Current => items[_index];

			public ValueTask<bool> MoveNextAsync()
			{
				cancellation.ThrowIfCancellationRequested();
				return ValueTask.FromResult(++_index < items.Count);
			}

			public ValueTask DisposeAsync()
			{
				if(!_disposed)
				{
					_disposed = true;
					dispose();
				}

				return ValueTask.CompletedTask;
			}
		}
	}

	private sealed class ConcurrentPageableAsyncEnumerable<T>(T item, PagingEventArgs eventArgs) : IAsyncEnumerable<T>, IPageable
	{
		private int _enumerators;
		private EventHandler<PagingEventArgs> _paginated;
		private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public event EventHandler<PagingEventArgs> Paginated
		{
			add => _paginated += value;
			remove => _paginated -= value;
		}

		public int SubscriberCount => _paginated?.GetInvocationList().Length ?? 0;
		public bool Suppressed => false;

		public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellation = default)
		{
			if(Interlocked.Increment(ref _enumerators) == 2)
				_ready.TrySetResult();

			await _ready.Task.WaitAsync(cancellation);
			_paginated?.Invoke(this, eventArgs);
			yield return item;
		}
	}

	private enum TransformKind
	{
		SyncFilter,
		SyncMap,
		AsyncFilter,
		AsyncMap,
	}
}
