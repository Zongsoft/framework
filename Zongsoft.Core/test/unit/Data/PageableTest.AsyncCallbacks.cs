using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Data.Tests;

public partial class PageableTest
{
	private static readonly TimeSpan _callbackTimeout = TimeSpan.FromSeconds(5);

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task AsyncCallbacks_AreLazyAndAwaitEachItem(bool filter)
	{
		using var cancellation = new CancellationTokenSource();
		var source = new CallbackAsyncEnumerable<int>([1, 2, 3]);
		var entered = new[] { CallbackGate(), CallbackGate(), CallbackGate() };
		var release = new[] { CallbackGate(), CallbackGate(), CallbackGate() };
		var inspected = new List<int>();
		var result = Transform(source, filter, TransformAsync);
		Assert.Equal(0, source.EnumerationCount);
		Assert.Empty(inspected);
		var iterator = result.GetAsyncEnumerator(cancellation.Token);
		Assert.Equal(0, source.EnumerationCount);
		var pending = iterator.MoveNextAsync().AsTask();

		try
		{
			for(var index = 0; index < entered.Length; index++)
			{
				await entered[index].Task.WaitAsync(_callbackTimeout);
				Assert.False(pending.IsCompleted);
				Assert.Equal(index + 1, source.MoveCount);
				Assert.Equal(index + 1, inspected.Count);
				Assert.Equal(0, source.DisposalCount);
				release[index].SetResult();

				if(filter && index == 0)
					continue;

				Assert.True(await pending.WaitAsync(_callbackTimeout));
				Assert.Equal((index + 1) * 10, iterator.Current);
				Assert.Equal((index + 1) * 10, iterator.Current);
				Assert.Equal(index + 1, inspected.Count);
				pending = iterator.MoveNextAsync().AsTask();
			}

			Assert.False(await pending.WaitAsync(_callbackTimeout));
			Assert.Equal([1, 2, 3], inspected);
			Assert.Equal(cancellation.Token, source.Cancellation);
			Assert.Equal(1, source.EnumerationCount);
			Assert.Equal(4, source.MoveCount);
			Assert.Equal(1, source.DisposalCount);
		}
		finally
		{
			foreach(var gate in release)
				gate.TrySetResult();

			await Record.ExceptionAsync(() => pending.WaitAsync(_callbackTimeout));
			await iterator.DisposeAsync().AsTask().WaitAsync(_callbackTimeout);
		}

		async ValueTask<int> TransformAsync(int value, CancellationToken token)
		{
			Assert.Equal(cancellation.Token, token);
			inspected.Add(value);
			entered[value - 1].TrySetResult();
			await release[value - 1].Task;
			return value * 10;
		}
	}

	[Fact]
	public async Task FilterAsync_PreservesReplacementAndAcceptedNull()
	{
		var source = new CallbackAsyncEnumerable<string>([null, "skip", "replace"]);
		var inspected = new List<string>();
		var result = Pageable.FilterAsync(source, (value, _) =>
		{
			inspected.Add(value);
			return ValueTask.FromResult(new Pageable.FilterResult<string>
			{
				Accepted = value != "skip",
				Data = value == null ? null : "changed",
			});
		});

		Assert.Equal([null, "changed"], await CollectAsync(result));
		Assert.Equal([null, "skip", "replace"], inspected);
		Assert.Equal(1, source.DisposalCount);
	}

	[Fact]
	public async Task MapAsync_NullResult_IsAnItem()
	{
		var source = new CallbackAsyncEnumerable<string>(["item"]);
		var result = Pageable.MapAsync<string, string>(source, (_, _) => ValueTask.FromResult<string>(null));

		Assert.Equal([null], await CollectAsync(result));
		Assert.Equal(2, source.MoveCount);
		Assert.Equal(1, source.DisposalCount);
	}

	[Fact]
	public void AsyncCallbacks_NullArguments_PreserveExistingContract()
	{
		var source = new CallbackAsyncEnumerable<int>([1]);

		Assert.Equal("source", Assert.Throws<ArgumentNullException>(() => Pageable.FilterAsync<int>(null, null)).ParamName);
		Assert.Equal("source", Assert.Throws<ArgumentNullException>(() => Pageable.MapAsync<int, int>(null, null)).ParamName);
		Assert.Same(source, Pageable.FilterAsync(source, null));
		Assert.Equal("mapper", Assert.Throws<ArgumentNullException>(() => Pageable.MapAsync<int, int>(source, null)).ParamName);
		Assert.Equal(0, source.EnumerationCount);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task AsyncCallbacks_EmptySource_DoesNotInvokeCallback(bool filter)
	{
		var source = new CallbackAsyncEnumerable<int>([]);
		var calls = 0;
		var result = Transform(source, filter, (value, _) =>
		{
			calls++;
			return ValueTask.FromResult(value);
		});

		Assert.Empty(await CollectAsync(result));
		Assert.Equal(0, calls);
		Assert.Equal(1, source.MoveCount);
		Assert.Equal(1, source.DisposalCount);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task AsyncCallbacks_CancellationWhilePending_AwaitsSourceDisposal(bool filter)
	{
		using var cancellation = new CancellationTokenSource();
		var entered = CallbackGate();
		var release = CallbackGate();
		var disposing = CallbackGate();
		var disposed = CallbackGate();
		var source = new CallbackAsyncEnumerable<int>([2, 3])
		{
			Release = async () =>
			{
				disposing.TrySetResult();
				await disposed.Task;
			},
		};
		var result = Transform(source, filter, async (value, token) =>
		{
			Assert.Equal(cancellation.Token, token);
			entered.TrySetResult();
			await release.Task.WaitAsync(token);
			return value;
		});
		var iterator = result.GetAsyncEnumerator(cancellation.Token);
		var pending = iterator.MoveNextAsync().AsTask();

		try
		{
			await entered.Task.WaitAsync(_callbackTimeout);
			cancellation.Cancel();
			await disposing.Task.WaitAsync(_callbackTimeout);
			Assert.False(pending.IsCompleted);
			Assert.Equal(cancellation.Token, source.Cancellation);
			Assert.Equal(1, source.MoveCount);
			disposed.SetResult();
			var failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending.WaitAsync(_callbackTimeout));
			Assert.Equal(cancellation.Token, failure.CancellationToken);
			Assert.Equal(1, source.DisposalCount);
		}
		finally
		{
			release.TrySetResult();
			disposed.TrySetResult();
			await Record.ExceptionAsync(() => pending.WaitAsync(_callbackTimeout));
			await iterator.DisposeAsync().AsTask().WaitAsync(_callbackTimeout);
		}
	}

	[Theory]
	[InlineData(false, false, false)]
	[InlineData(false, false, true)]
	[InlineData(false, true, false)]
	[InlineData(false, true, true)]
	[InlineData(true, false, false)]
	[InlineData(true, false, true)]
	[InlineData(true, true, false)]
	[InlineData(true, true, true)]
	public async Task AsyncCallbacks_Failure_AwaitsDisposalAndPreservesExceptionPrecedence(bool filter, bool sourceFails, bool disposalFails)
	{
		var failure = new InvalidOperationException();
		var disposalFailure = new ApplicationException();
		var entered = CallbackGate();
		var release = CallbackGate();
		var disposing = CallbackGate();
		var disposed = CallbackGate();
		var calls = 0;
		var source = new CallbackAsyncEnumerable<int>([2, 3])
		{
			Release = async () =>
			{
				disposing.TrySetResult();
				await disposed.Task;

				if(disposalFails)
					throw disposalFailure;
			},
		};

		if(sourceFails)
			source.Move = _ => FailAsync();

		var result = Transform(source, filter, async (value, _) =>
		{
			calls++;
			await FailAsync();
			return value;
		});
		var iterator = result.GetAsyncEnumerator();
		var pending = iterator.MoveNextAsync().AsTask();

		try
		{
			await entered.Task.WaitAsync(_callbackTimeout);
			Assert.False(pending.IsCompleted);
			Assert.Equal(0, source.DisposalCount);
			release.SetResult();
			await disposing.Task.WaitAsync(_callbackTimeout);
			Assert.False(pending.IsCompleted);
			disposed.SetResult();
			Assert.Same(disposalFails ? disposalFailure : (Exception)failure,
				await Record.ExceptionAsync(() => pending.WaitAsync(_callbackTimeout)));
			Assert.Equal(sourceFails ? 0 : 1, calls);
			Assert.Equal(1, source.MoveCount);
			Assert.Equal(1, source.DisposalCount);
		}
		finally
		{
			release.TrySetResult();
			disposed.TrySetResult();
			await Record.ExceptionAsync(() => pending.WaitAsync(_callbackTimeout));
			await Record.ExceptionAsync(() => iterator.DisposeAsync().AsTask().WaitAsync(_callbackTimeout));
		}

		async ValueTask FailAsync()
		{
			entered.TrySetResult();
			await release.Task;
			throw failure;
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task AsyncCallbacks_EarlyExit_AwaitsDisposal(bool filter)
	{
		var disposing = CallbackGate();
		var release = CallbackGate();
		var calls = 0;
		var source = new CallbackAsyncEnumerable<int>([2, 3])
		{
			Release = async () =>
			{
				disposing.TrySetResult();
				await release.Task;
			},
		};
		var result = Transform(source, filter, (value, _) =>
		{
			calls++;
			return ValueTask.FromResult(value * 10);
		});
		var pending = ConsumeFirstAsync();

		try
		{
			await disposing.Task.WaitAsync(_callbackTimeout);
			Assert.False(pending.IsCompleted);
			Assert.Equal(1, source.MoveCount);
			Assert.Equal(1, calls);
			release.SetResult();
			await pending.WaitAsync(_callbackTimeout);
			Assert.Equal(1, source.DisposalCount);
		}
		finally
		{
			release.TrySetResult();
			await Record.ExceptionAsync(() => pending.WaitAsync(_callbackTimeout));
		}

		async Task ConsumeFirstAsync()
		{
			await foreach(var item in result)
			{
				Assert.Equal(20, item);
				break;
			}
		}
	}

	private static IAsyncEnumerable<int> Transform(IAsyncEnumerable<int> source, bool filter, Func<int, CancellationToken, ValueTask<int>> callback) => filter ?
		Pageable.FilterAsync(source, async (value, token) => new Pageable.FilterResult<int>
		{
			Accepted = value > 1,
			Data = await callback(value, token),
		}) :
		Pageable.MapAsync(source, callback);

	private static TaskCompletionSource CallbackGate() => new(TaskCreationOptions.RunContinuationsAsynchronously);

	private sealed class CallbackAsyncEnumerable<T>(IReadOnlyList<T> items) : IAsyncEnumerable<T>
	{
		public CancellationToken Cancellation { get; private set; }
		public int EnumerationCount { get; private set; }
		public int MoveCount { get; private set; }
		public int DisposalCount { get; private set; }
		public Func<CancellationToken, ValueTask> Move { get; set; }
		public Func<ValueTask> Release { get; set; }

		public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellation = default)
		{
			this.Cancellation = cancellation;
			this.EnumerationCount++;
			return new Enumerator(this, items, cancellation);
		}

		private sealed class Enumerator(CallbackAsyncEnumerable<T> owner, IReadOnlyList<T> items, CancellationToken cancellation) : IAsyncEnumerator<T>
		{
			private int _index = -1;
			public T Current => items[_index];

			public async ValueTask<bool> MoveNextAsync()
			{
				owner.MoveCount++;
				cancellation.ThrowIfCancellationRequested();

				if(owner.Move != null)
					await owner.Move(cancellation);

				return ++_index < items.Count;
			}

			public async ValueTask DisposeAsync()
			{
				owner.DisposalCount++;

				if(owner.Release != null)
					await owner.Release();
			}
		}
	}
}
