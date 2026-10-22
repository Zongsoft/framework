using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Xunit;

namespace Zongsoft.Data.Tests;

public sealed class DataDictionaryAsyncTest
{
	private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(5);

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task GetDictionariesAsync_PublicOverloadsPreserveConvenientCalls(bool explicitToken)
	{
		using var cancellation = new CancellationTokenSource();
		var expected = explicitToken ? cancellation.Token : CancellationToken.None;
		var handled = 0;
		var first = new AsyncSource<Row>([new() { Value = 17 }]);
		var second = new AsyncSource<Row>([new() { Value = 17 }]);
		IAsyncEnumerable<IDataDictionary<Row>>[] sequences = explicitToken ?
		[
			DataDictionary.GetDictionariesAsync<Row>(new[] { new Row { Value = 17 } }, cancellation.Token),
			DataDictionary.GetDictionariesAsync<Row>(new[] { new Row { Value = 17 } }, Handle, cancellation.Token),
			DataDictionary.GetDictionariesAsync<Row, Row>(first, cancellation.Token),
			DataDictionary.GetDictionariesAsync<Row, Row>(second, Handle, cancellation.Token),
		] :
		[
			DataDictionary.GetDictionariesAsync<Row>(new[] { new Row { Value = 17 } }),
			DataDictionary.GetDictionariesAsync<Row>(new[] { new Row { Value = 17 } }, Handle),
			DataDictionary.GetDictionariesAsync<Row, Row>(first),
			DataDictionary.GetDictionariesAsync<Row, Row>(second, Handle),
		];

		Assert.Equal(0, handled);
		Assert.Equal(0, first.Enumerations);
		Assert.Equal(0, second.Enumerations);
		var values = new List<int>();

		foreach(var sequence in sequences)
			values.Add(Assert.Single(await CollectAsync(sequence)).GetValue<int>("Value", 0));

		Assert.Equal([17, 117, 17, 117], values);
		Assert.Equal(2, handled);
		Assert.Equal(expected, first.Cancellation);
		Assert.Equal(expected, second.Cancellation);
		Assert.Equal(1, first.Disposals);
		Assert.Equal(1, second.Disposals);

		ValueTask Handle(IDataDictionary<Row> dictionary, CancellationToken token)
		{
			Assert.Equal(expected, token);
			handled++;
			dictionary.SetValue("Value", dictionary.GetValue<int>("Value", 0) + 100);
			return ValueTask.CompletedTask;
		}
	}

	[Fact]
	public async Task GetDictionariesAsync_MemorySourcesPreserveValuesAndIdentity()
	{
		var row = new Row { Value = 1 };
		var classic = new Hashtable { ["Value"] = 2 };
		var generic = new Dictionary<string, object> { ["Value"] = 3 };
		var model = Model.Build<IValueModel>();
		model.Value = 4;
		Assert.IsAssignableFrom<IModel>(model);
		var existing = DataDictionary.GetDictionary<Row>(new Row { Value = 5 });
		var source = new MemorySource([null, row, classic, null, generic, model, existing, null]);
		var sequence = DataDictionary.GetDictionariesAsync<Row>(source, null);

		Assert.Equal(0, source.Enumerations);
		var dictionaries = await CollectAsync(sequence);
		Assert.Equal([1, 2, 3, 4, 5], dictionaries.Select(dictionary => dictionary.GetValue<int>("Value", 0)));
		Assert.Same(row, dictionaries[0].Data);
		Assert.Same(classic, dictionaries[1].Data);
		Assert.Same(generic, dictionaries[2].Data);
		Assert.Same(model, dictionaries[3].Data);
		Assert.Same(existing, dictionaries[4]);
		Assert.Equal(1, source.Enumerations);
		Assert.Equal(1, source.Disposals);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public Task GetDictionariesAsync_AsyncSourcesSupportDifferentAndValueTypes(bool structs) => structs ?
		VerifySourceAsync<ValueRow>([new() { Value = 17 }, new() { Value = 23 }]) :
		VerifySourceAsync<SourceRow>([null, new() { Value = 17 }, null, new() { Value = 23 }, null]);

	private static async Task VerifySourceAsync<TSource>(TSource[] items)
	{
		var source = new AsyncSource<TSource>(items, suspendRead: true);
		var sequence = DataDictionary.GetDictionariesAsync<Row, TSource>(source, null);
		Assert.Equal(0, source.Enumerations);
		var iterator = sequence.GetAsyncEnumerator();
		var moving = iterator.MoveNextAsync().AsTask();

		try
		{
			await source.Reading.Task.WaitAsync(_timeout);
			Assert.False(moving.IsCompleted);
			source.ReadRelease.TrySetResult();
			Assert.True(await moving.WaitAsync(_timeout));
			Assert.Equal(17, iterator.Current.GetValue<int>("Value", 0));
			Assert.IsType<TSource>(iterator.Current.Data);
			Assert.True(await iterator.MoveNextAsync());
			Assert.Equal(23, iterator.Current.GetValue<int>("Value", 0));
			Assert.IsType<TSource>(iterator.Current.Data);
			Assert.False(await iterator.MoveNextAsync());
			Assert.Equal(1, source.Enumerations);
			Assert.Equal(1, source.Disposals);
		}
		finally
		{
			source.ReadRelease.TrySetResult();
			await Record.ExceptionAsync(() => moving.WaitAsync(_timeout));
			await iterator.DisposeAsync();
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task GetDictionariesAsync_HandleWaitsInOrderAndMutatesBeforeYield(bool asynchronous)
	{
		Row[] items = [new() { Value = 17 }, null, new() { Value = 23 }];
		var memory = new MemorySource(items);
		var source = new AsyncSource<Row>(items);
		var entered = new[] { Signal(), Signal() };
		var releases = new[] { Signal(), Signal() };
		var handled = new List<IDataDictionary<Row>>();
		var order = new List<string>();
		var sequence = asynchronous ? DataDictionary.GetDictionariesAsync<Row, Row>(source, Handle) :
			DataDictionary.GetDictionariesAsync<Row>(memory, Handle);
		var iterator = sequence.GetAsyncEnumerator();
		Assert.Empty(handled);
		Assert.Equal(0, memory.Enumerations + source.Enumerations);
		Task<bool> moving = null;

		try
		{
			for(var index = 0; index < 2; index++)
			{
				moving = iterator.MoveNextAsync().AsTask();
				await entered[index].Task.WaitAsync(_timeout);
				Assert.False(moving.IsCompleted);
				Assert.Equal(index + 1, handled.Count);
				Assert.Equal(index == 0 ? 1 : 3, asynchronous ? source.Reads : memory.Reads);
				releases[index].TrySetResult();
				Assert.True(await moving.WaitAsync(_timeout));
				Assert.Same(handled[index], iterator.Current);
				Assert.Equal(index == 0 ? 117 : 123, iterator.Current.GetValue<int>("Value", 0));
				order.Add($"yield:{iterator.Current.GetValue<int>("Value", 0)}");
			}

			Assert.False(await iterator.MoveNextAsync());
			Assert.Equal(["start:17", "finish:117", "yield:117", "start:23", "finish:123", "yield:123"], order);
			Assert.Equal(1, memory.Disposals + source.Disposals);
		}
		finally
		{
			foreach(var release in releases)
				release.TrySetResult();

			if(moving != null)
				await Record.ExceptionAsync(() => moving.WaitAsync(_timeout));

			await iterator.DisposeAsync();
		}

		async ValueTask Handle(IDataDictionary<Row> dictionary, CancellationToken token)
		{
			var index = handled.Count;
			handled.Add(dictionary);
			order.Add($"start:{dictionary.GetValue<int>("Value", 0)}");
			entered[index].TrySetResult();
			await releases[index].Task.WaitAsync(token);
			dictionary.SetValue("Value", dictionary.GetValue<int>("Value", 0) + 100);
			order.Add($"finish:{dictionary.GetValue<int>("Value", 0)}");
		}
	}

	[Theory]
	[InlineData(false, 1)]
	[InlineData(false, 2)]
	[InlineData(false, 3)]
	[InlineData(false, 4)]
	[InlineData(true, 1)]
	[InlineData(true, 2)]
	[InlineData(true, 3)]
	[InlineData(true, 4)]
	public async Task GetDictionariesAsync_CancellationReachesSourceAndHandle(bool asynchronous, int mode)
	{
		using var methodCancellation = new CancellationTokenSource();
		using var enumerationCancellation = new CancellationTokenSource();
		var methodToken = mode == 2 ? CancellationToken.None : methodCancellation.Token;
		var enumerationToken = mode == 1 ? CancellationToken.None : enumerationCancellation.Token;
		var memory = new MemorySource([new Row { Value = 17 }, new Row { Value = 23 }]);
		var source = new AsyncSource<Row>([new() { Value = 17 }, new() { Value = 23 }], suspendDispose: true);
		var entered = Signal();
		var release = Signal();
		var observed = CancellationToken.None;
		var handled = 0;
		var sequence = asynchronous ? DataDictionary.GetDictionariesAsync<Row, Row>(source, Handle, methodToken) :
			DataDictionary.GetDictionariesAsync<Row>(memory, Handle, methodToken);
		var iterator = sequence.GetAsyncEnumerator(enumerationToken);
		var moving = iterator.MoveNextAsync().AsTask();

		try
		{
			await entered.Task.WaitAsync(_timeout);
			Assert.False(moving.IsCompleted);
			Assert.True(observed.CanBeCanceled);

			if(mode < 3)
				Assert.Equal(mode == 1 ? methodToken : enumerationToken, observed);

			if(asynchronous)
				Assert.Equal(observed, source.Cancellation);

			if(mode == 2 || mode == 4)
				enumerationCancellation.Cancel();
			else
				methodCancellation.Cancel();

			if(asynchronous)
			{
				await source.Disposing.Task.WaitAsync(_timeout);
				Assert.False(moving.IsCompleted);
				source.DisposeRelease.TrySetResult();
			}

			var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => moving.WaitAsync(_timeout));
			Assert.Equal(observed, error.CancellationToken);
			Assert.True(observed.IsCancellationRequested);
			Assert.Equal(1, handled);
			Assert.Equal(1, asynchronous ? source.Reads : memory.Reads);
			Assert.Equal(1, memory.Disposals + source.Disposals);
		}
		finally
		{
			release.TrySetResult();
			source.DisposeRelease.TrySetResult();
			await Record.ExceptionAsync(() => moving.WaitAsync(_timeout));
			await iterator.DisposeAsync();
		}

		async ValueTask Handle(IDataDictionary<Row> dictionary, CancellationToken token)
		{
			handled++;
			observed = token;
			entered.TrySetResult();
			await release.Task.WaitAsync(token);
		}
	}

	[Fact]
	public async Task GetDictionariesAsync_PreCancellationSkipsHandleAndDisposesMemorySource()
	{
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		var source = new MemorySource([new Row { Value = 17 }]);
		var handled = 0;
		var sequence = DataDictionary.GetDictionariesAsync<Row>(source, (_, _) =>
		{
			handled++;
			return ValueTask.CompletedTask;
		}, cancellation.Token);
		await using var iterator = sequence.GetAsyncEnumerator();
		var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => iterator.MoveNextAsync().AsTask());

		Assert.Equal(cancellation.Token, error.CancellationToken);
		Assert.Equal(0, handled);
		Assert.Equal(1, source.Disposals);

		await using var unhandled = DataDictionary.GetDictionariesAsync<Row>(source, cancellation.Token).GetAsyncEnumerator();
		var unhandledError = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => unhandled.MoveNextAsync().AsTask());

		Assert.Equal(cancellation.Token, unhandledError.CancellationToken);
		Assert.Equal(2, source.Enumerations);
		Assert.Equal(2, source.Disposals);
	}

	[Theory]
	[InlineData("source")]
	[InlineData("handle")]
	[InlineData("early")]
	public async Task GetDictionariesAsync_FailureOrEarlyStopAwaitsSourceDisposal(string reason)
	{
		var failure = new InvalidOperationException("test failure");
		var source = new AsyncSource<Row>([new() { Value = 17 }, new() { Value = 23 }], suspendDispose: true)
		{
			ReadFailure = reason == "source" ? failure : null,
		};
		var handled = 0;
		var iterator = DataDictionary.GetDictionariesAsync<Row, Row>(source, Handle).GetAsyncEnumerator();
		var moving = iterator.MoveNextAsync().AsTask();
		Task completion = moving;

		try
		{
			if(reason == "early")
			{
				Assert.True(await moving.WaitAsync(_timeout));
				Assert.Equal(17, iterator.Current.GetValue<int>("Value", 0));
				completion = iterator.DisposeAsync().AsTask();
			}

			await source.Disposing.Task.WaitAsync(_timeout);
			Assert.False(completion.IsCompleted);
			source.DisposeRelease.TrySetResult();

			if(reason == "early")
				await completion.WaitAsync(_timeout);
			else
				Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => completion.WaitAsync(_timeout)));

			Assert.Equal(reason == "source" ? 0 : 1, handled);
			Assert.Equal(reason == "source" ? 0 : 1, source.Reads);
			Assert.Equal(1, source.Disposals);
		}
		finally
		{
			source.DisposeRelease.TrySetResult();
			await Record.ExceptionAsync(() => completion.WaitAsync(_timeout));
			await iterator.DisposeAsync();
		}

		ValueTask Handle(IDataDictionary<Row> dictionary, CancellationToken token)
		{
			handled++;
			return reason == "handle" ? ValueTask.FromException(failure) : ValueTask.CompletedTask;
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task GetDictionariesAsync_DisposalFailureIsPropagated(bool early)
	{
		var failure = new InvalidOperationException("test disposal failure");
		var source = new AsyncSource<Row>([new() { Value = 17 }], suspendDispose: true) { DisposeFailure = failure };
		var iterator = DataDictionary.GetDictionariesAsync<Row, Row>(source).GetAsyncEnumerator();
		Assert.True(await iterator.MoveNextAsync());
		Task completion = early ? iterator.DisposeAsync().AsTask() : iterator.MoveNextAsync().AsTask();

		try
		{
			await source.Disposing.Task.WaitAsync(_timeout);
			Assert.False(completion.IsCompleted);
			source.DisposeRelease.TrySetResult();
			Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => completion.WaitAsync(_timeout)));
			Assert.Equal(1, source.Disposals);
		}
		finally
		{
			source.DisposeRelease.TrySetResult();
			await Record.ExceptionAsync(() => completion.WaitAsync(_timeout));
			await iterator.DisposeAsync();
		}
	}

	[Theory]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public async Task GetDictionariesAsync_NullInputFailsOnlyWhenEnumerated(bool asynchronous, bool handle)
	{
		var sequence = asynchronous ?
			(handle ? DataDictionary.GetDictionariesAsync<Row, Row>(null, null) : DataDictionary.GetDictionariesAsync<Row, Row>(null)) :
			(handle ? DataDictionary.GetDictionariesAsync<Row>(null, null) : DataDictionary.GetDictionariesAsync<Row>(null));
		await using var iterator = sequence.GetAsyncEnumerator();
		var error = await Assert.ThrowsAsync<ArgumentNullException>(() => iterator.MoveNextAsync().AsTask());

		Assert.Equal("items", error.ParamName);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task GetDictionariesAsync_EmptySourceSkipsHandle(bool asynchronous)
	{
		var source = new AsyncSource<Row>([]);
		var memory = new MemorySource([]);
		var handled = 0;
		var sequence = asynchronous ? DataDictionary.GetDictionariesAsync<Row, Row>(source, Handle) :
			DataDictionary.GetDictionariesAsync<Row>(memory, Handle);

		Assert.Empty(await CollectAsync(sequence));
		Assert.Equal(0, handled);
		Assert.Equal(1, memory.Disposals + source.Disposals);

		ValueTask Handle(IDataDictionary<Row> dictionary, CancellationToken token)
		{
			handled++;
			return ValueTask.CompletedTask;
		}
	}

	private static TaskCompletionSource Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
	private static async Task<List<IDataDictionary<T>>> CollectAsync<T>(IAsyncEnumerable<IDataDictionary<T>> sequence)
	{
		var result = new List<IDataDictionary<T>>();

		await foreach(var dictionary in sequence)
			result.Add(dictionary);

		return result;
	}

	public interface IValueModel
	{
		int Value { get; set; }
	}
	private sealed class Row
	{
		public int Value { get; set; }
	}
	private sealed class SourceRow
	{
		public int Value { get; set; }
	}
	private struct ValueRow
	{
		public int Value { get; set; }
	}
	private sealed class MemorySource(object[] items) : IEnumerable
	{
		public int Enumerations { get; private set; }
		public int Reads { get; private set; }
		public int Disposals { get; private set; }
		public IEnumerator GetEnumerator()
		{
			this.Enumerations++;

			try
			{
				foreach(var item in items)
				{
					this.Reads++;
					yield return item;
				}
			}
			finally
			{
				this.Disposals++;
			}
		}
	}
	private sealed class AsyncSource<T> : IAsyncEnumerable<T>
	{
		private readonly T[] _items;
		public AsyncSource(T[] items, bool suspendRead = false, bool suspendDispose = false)
		{
			_items = items;

			if(!suspendRead)
				this.ReadRelease.TrySetResult();

			if(!suspendDispose)
				this.DisposeRelease.TrySetResult();
		}

		public int Enumerations { get; private set; }
		public int Reads { get; private set; }
		public int Disposals { get; private set; }
		public CancellationToken Cancellation { get; private set; }
		public Exception ReadFailure { get; init; }
		public Exception DisposeFailure { get; init; }
		public TaskCompletionSource Reading { get; } = Signal();
		public TaskCompletionSource ReadRelease { get; } = Signal();
		public TaskCompletionSource Disposing { get; } = Signal();
		public TaskCompletionSource DisposeRelease { get; } = Signal();
		public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellation = default) => this.ReadAsync(cancellation).GetAsyncEnumerator();
		private async IAsyncEnumerable<T> ReadAsync([EnumeratorCancellation] CancellationToken cancellation)
		{
			this.Enumerations++;
			this.Cancellation = cancellation;

			try
			{
				this.Reading.TrySetResult();
				await this.ReadRelease.Task.WaitAsync(cancellation);

				if(this.ReadFailure != null)
					throw this.ReadFailure;

				foreach(var item in _items)
				{
					cancellation.ThrowIfCancellationRequested();
					this.Reads++;
					yield return item;
				}
			}
			finally
			{
				this.Disposals++;
				this.Disposing.TrySetResult();
				await this.DisposeRelease.Task;

				if(this.DisposeFailure != null)
					throw this.DisposeFailure;
			}
		}
	}
}
