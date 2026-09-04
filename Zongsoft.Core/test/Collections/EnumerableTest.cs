using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Data;

namespace Zongsoft.Collections.Tests;

public class EnumerableTest
{
	[Fact]
	public void TestEmpty()
	{
		var objects = Enumerable.Empty(typeof(object));
		var strings = Enumerable.Empty(typeof(string));
		var integers = Enumerable.Empty(typeof(int));
		var currency = Enumerable.Empty(typeof(decimal));

		Assert.NotNull(objects);
		Assert.NotNull(strings);
		Assert.NotNull(integers);
		Assert.NotNull(currency);

		Assert.Empty(objects);
		Assert.Empty(strings);
		Assert.Empty(integers);
		Assert.Empty(currency);

		Assert.IsAssignableFrom<IEnumerable<object>>(objects);
		Assert.IsAssignableFrom<IEnumerable<string>>(strings);
		Assert.IsAssignableFrom<IEnumerable<int>>(integers);
		Assert.IsAssignableFrom<IEnumerable<decimal>>(currency);
	}

	[Fact]
	public void TestEnumerate()
	{
		const string FIRST = "First";

		var objects = Enumerable.Enumerate(new object(), typeof(object));
		var strings = Enumerable.Enumerate(FIRST, typeof(string));
		var integers = Enumerable.Enumerate(100, typeof(int));
		var currency = Enumerable.Enumerate(100, typeof(decimal));
		var dates = Enumerable.Enumerate(new[] { DateTime.MinValue, DateTime.MaxValue }, typeof(DateTime));
		var items = Enumerable.Enumerate(new Items(), typeof(Zongsoft.Tests.IPerson));

		Assert.NotNull(objects);
		Assert.NotNull(strings);
		Assert.NotNull(integers);
		Assert.NotNull(currency);
		Assert.NotNull(dates);
		Assert.NotNull(items);

		Assert.NotEmpty(objects);
		Assert.NotEmpty(strings);
		Assert.NotEmpty(integers);
		Assert.NotEmpty(currency);
		Assert.NotEmpty(dates);
		Assert.NotEmpty(items);

		Assert.IsAssignableFrom<IEnumerable<object>>(objects);
		Assert.IsAssignableFrom<IEnumerable<string>>(strings);
		Assert.IsAssignableFrom<IEnumerable<int>>(integers);
		Assert.IsAssignableFrom<IEnumerable<decimal>>(currency);
		Assert.IsAssignableFrom<IEnumerable<DateTime>>(dates);
		Assert.IsAssignableFrom<IEnumerable<Zongsoft.Tests.IPerson>>(items);

		var objectsIterator = objects.GetEnumerator();
		var stringsIterator = strings.GetEnumerator();
		var integersIterator = integers.GetEnumerator();
		var currencyIterator = currency.GetEnumerator();
		var datesIterator = dates.GetEnumerator();
		var itemsIterator = items.GetEnumerator();

		Assert.Throws<InvalidOperationException>(() => objectsIterator.Current);
		Assert.Throws<InvalidOperationException>(() => stringsIterator.Current);
		Assert.Throws<InvalidOperationException>(() => integersIterator.Current);
		Assert.Throws<InvalidOperationException>(() => currencyIterator.Current);
		Assert.Throws<InvalidOperationException>(() => datesIterator.Current);
		Assert.Throws<InvalidOperationException>(() => itemsIterator.Current);

		Assert.True(objectsIterator.MoveNext());
		Assert.True(stringsIterator.MoveNext());
		Assert.True(integersIterator.MoveNext());
		Assert.True(currencyIterator.MoveNext());

		Assert.NotNull(objectsIterator.Current);
		Assert.Equal(FIRST, stringsIterator.Current);
		Assert.Equal(100, integersIterator.Current);
		Assert.Equal(100m, currencyIterator.Current);

		Assert.True(datesIterator.MoveNext());
		Assert.Equal(DateTime.MinValue, datesIterator.Current);
		Assert.True(datesIterator.MoveNext());
		Assert.Equal(DateTime.MaxValue, datesIterator.Current);

		Assert.True(itemsIterator.MoveNext());
		Assert.IsAssignableFrom<Zongsoft.Tests.IEmployee>(itemsIterator.Current);
		Assert.True(itemsIterator.MoveNext());
		Assert.IsAssignableFrom<Zongsoft.Tests.ICustomer>(itemsIterator.Current);

		Assert.False(objectsIterator.MoveNext());
		Assert.False(stringsIterator.MoveNext());
		Assert.False(integersIterator.MoveNext());
		Assert.False(currencyIterator.MoveNext());
		Assert.False(datesIterator.MoveNext());
		Assert.False(itemsIterator.MoveNext());

		Assert.Throws<InvalidOperationException>(() => objectsIterator.Current);
		Assert.Throws<InvalidOperationException>(() => stringsIterator.Current);
		Assert.Throws<InvalidOperationException>(() => integersIterator.Current);
		Assert.Throws<InvalidOperationException>(() => currencyIterator.Current);
		Assert.Throws<InvalidOperationException>(() => datesIterator.Current);
		Assert.Throws<InvalidOperationException>(() => itemsIterator.Current);
	}

	[Fact]
	public async Task CastAsync_PublicEntries_PreserveConversionAndPagingSemantics()
	{
		var paging = Paging.Page(1, 2);
		var firstArgs = new PagingEventArgs("First", paging);
		var secondArgs = new PagingEventArgs("Second", paging);
		var firstSource = new PageableAsyncEnumerable<string>(["First", null, "Last"], firstArgs, suppressed: false);
		var secondSource = new PageableAsyncEnumerable<object>([3, null, "Ignored", 4], secondArgs, suppressed: true);
		var objects = Enumerable.Cast(firstSource);
		var integers = Enumerable.Cast<object, int>(secondSource);
		var firstPageable = Assert.IsAssignableFrom<IPageable>(objects);
		var secondPageable = Assert.IsAssignableFrom<IPageable>(integers);
		object firstSender = null;
		object secondSender = null;
		PagingEventArgs firstObserved = null;
		PagingEventArgs secondObserved = null;
		firstPageable.Paginated += (sender, args) => (firstSender, firstObserved) = (sender, args);
		secondPageable.Paginated += (sender, args) => (secondSender, secondObserved) = (sender, args);

		var objectValues = await CollectAsync(objects);
		var integerValues = await CollectAsync(integers);

		Assert.False(firstPageable.Suppressed);
		Assert.True(secondPageable.Suppressed);
		Assert.Same(objects, firstSender);
		Assert.Same(integers, secondSender);
		Assert.Same(firstArgs, firstObserved);
		Assert.Same(secondArgs, secondObserved);
		Assert.Collection(objectValues,
			value => Assert.Equal("First", value),
			Assert.Null,
			value => Assert.Equal("Last", value));
		Assert.Equal([3, 4], integerValues);
	}

	[Theory]
	[InlineData((int)CancellationScope.Method)]
	[InlineData((int)CancellationScope.Enumerator)]
	public async Task CastAsync_CancellationFromEitherScope_StopsAndDisposesSource(int scopeValue)
	{
		var scope = (CancellationScope)scopeValue;
		using var methodCancellation = new CancellationTokenSource();
		using var enumeratorCancellation = new CancellationTokenSource();
		var source = new TrackingAsyncEnumerable<int>([1, 2]);
		var result = Enumerable.Cast<int, int>(source, methodCancellation.Token);
		var iterator = result.GetAsyncEnumerator(enumeratorCancellation.Token);

		try
		{
			Assert.True(await iterator.MoveNextAsync());
			Assert.Equal(1, iterator.Current);
			Assert.True(source.Cancellation.CanBeCanceled);
			Assert.NotEqual(methodCancellation.Token, source.Cancellation);
			Assert.NotEqual(enumeratorCancellation.Token, source.Cancellation);

			if(scope == CancellationScope.Method)
				methodCancellation.Cancel();
			else
				enumeratorCancellation.Cancel();

			var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await iterator.MoveNextAsync());
			Assert.Equal(source.Cancellation, exception.CancellationToken);
		}
		finally
		{
			await iterator.DisposeAsync();
		}

		Assert.Equal(1, source.DisposalCount);
	}

	private static async Task<List<T>> CollectAsync<T>(IAsyncEnumerable<T> source)
	{
		var result = new List<T>();

		await foreach(var item in source)
			result.Add(item);

		return result;
	}

	private class Items : IEnumerable
	{
		private readonly object[] _items =
		[
			new Zongsoft.Tests.Employee(100, "Popeye"),
			new Zongsoft.Tests.Customer("Sophia"),
		];

		public IEnumerator GetEnumerator()
		{
			return new ItemEnumerator(_items);
		}

		private class ItemEnumerator : IEnumerator
		{
			private int _index;
			private object[] _items;

			public ItemEnumerator(object[] items)
			{
				_index = -1;
				_items = items;
			}

			public object Current
			{
				get
				{
					var index = _index;

					if(index >= 0 && index < _items.Length)
						return _items[index];

					throw new InvalidOperationException();
				}
			}

			public bool MoveNext()
			{
				var index = System.Threading.Interlocked.Increment(ref _index);

				if(index < _items.Length)
					return true;

				System.Threading.Interlocked.Exchange(ref _index, _items.Length);
				return false;
			}

			public void Reset()
			{
				_index = -1;
			}
		}
	}

	private sealed class PageableAsyncEnumerable<T>(IEnumerable<T> items, PagingEventArgs eventArgs, bool suppressed) : IAsyncEnumerable<T>, IPageable
	{
		public event EventHandler<PagingEventArgs> Paginated;
		public bool Suppressed { get; } = suppressed;

		public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellation = default)
		{
			var notified = false;

			foreach(var item in items)
			{
				cancellation.ThrowIfCancellationRequested();

				if(!notified)
				{
					notified = true;
					this.Paginated?.Invoke(this, eventArgs);
				}

				yield return item;
				await Task.CompletedTask;
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

	private enum CancellationScope
	{
		Method,
		Enumerator,
	}
}
