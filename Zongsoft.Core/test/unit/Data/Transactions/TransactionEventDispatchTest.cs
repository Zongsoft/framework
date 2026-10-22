using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

using Xunit;

using Zongsoft.Components;
using Zongsoft.Common;

namespace Zongsoft.Data.Transactions.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class TransactionEventDispatchCollection
{
	public const string Name = nameof(TransactionEventDispatchCollection);
}

[Collection(TransactionEventDispatchCollection.Name)]
public sealed class TransactionEventDispatchTest
{
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task SharedProviderCapturesEachRootTransaction(bool commit)
	{
		var provider = Transaction.Features;
		Assert.Contains(provider, FeatureProvider.Default.Providers);
		Assert.Empty(provider.GetFeatures());
		IPredication captured;

		await using(var root = new Transaction())
		{
			captured = Assert.IsAssignableFrom<IPredication>(Assert.Single(provider.GetFeatures()));
			var waiting = captured.PredicateAsync(null).AsTask();
			var readyAtNotification = false;
			root.Context.Completed += (_, _, _) =>
			{
				readyAtNotification = waiting.IsCompletedSuccessfully;
				return ValueTask.CompletedTask;
			};

			await using(var child = new Transaction())
			{
				Assert.Same(captured, Assert.Single(provider.GetFeatures()));
				await child.CommitAsync();
				Assert.False(waiting.IsCompleted);
			}

			if(commit)
				await root.CommitAsync();
			else
				await root.RollbackAsync();

			Assert.True(readyAtNotification);
			Assert.Equal(commit, await waiting.WaitAsync(TimeSpan.FromSeconds(30)));
		}

		Assert.Empty(provider.GetFeatures());

		await using(var next = new Transaction())
		{
			Assert.NotSame(captured, Assert.Single(provider.GetFeatures()));
			Assert.Equal(commit, await captured.PredicateAsync(null));
			Assert.Equal(commit, await captured.PredicateAsync(new object(), new Collections.Parameters()));
		}
	}

	[Fact]
	public async Task ConcurrentCaptureSharesDecisionAndCancellationOnlyEndsOneWait()
	{
		await using var transaction = new Transaction();
		var captures = await Task.WhenAll(Enumerable.Range(0, 16).Select(_ => Task.Run(() =>
			Assert.IsAssignableFrom<IPredication>(Assert.Single(Transaction.Features.GetFeatures())))));
		var feature = captures[0];
		Assert.All(captures, captured => Assert.Same(feature, captured));
		using var cancellation = new CancellationTokenSource();
		var canceled = feature.PredicateAsync(null, cancellation.Token).AsTask();
		var pending = feature.PredicateAsync(null).AsTask();

		cancellation.Cancel();
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceled);
		Assert.False(pending.IsCompleted);
		Assert.Equal(TransactionStatus.Active, transaction.Context.Status);
		await transaction.CommitAsync();

		Assert.True(await pending.WaitAsync(TimeSpan.FromSeconds(10)));
		Assert.True(await feature.PredicateAsync(null));
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task Completed_ObserverCanAwaitFeatureBeforeFinishing(bool commit)
	{
		await using var transaction = new Transaction();
		var feature = Assert.IsAssignableFrom<IPredication>(Assert.Single(Transaction.Features.GetFeatures()));
		var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		transaction.Context.Completed += async (_, _, cancellation) =>
		{
			entered.TrySetResult(await feature.PredicateAsync(null, cancellation).AsTask().WaitAsync(TimeSpan.FromSeconds(5)));
			await release.Task;
		};
		var completion = commit ? transaction.CommitAsync() : transaction.RollbackAsync();

		try
		{
			Assert.Equal(commit, await entered.Task.WaitAsync(TimeSpan.FromSeconds(5)));
			Assert.False(completion.IsCompleted);
			Assert.Equal(commit ? TransactionStatus.Committed : TransactionStatus.Aborted, transaction.Context.Status);
		}
		finally { release.TrySetResult(); }

		await completion.WaitAsync(TimeSpan.FromSeconds(5));
	}

	[Fact]
	public async Task CapturedTransactionDoesNotFollowLaterAmbientTransaction()
	{
		var inner = new RecordingChannel();
		await RunAsync(inner, async exchanger =>
		{
			await using(var transaction = new Transaction())
			{
				await exchanger.ExchangeAsync(CreateContext(new() { Message = "first" }), CancellationToken.None);
				await transaction.RollbackAsync();
			}

			await using(var transaction = new Transaction())
			{
				await exchanger.ExchangeAsync(CreateContext(new() { Message = "second" }), CancellationToken.None);
				await transaction.CommitAsync();
			}

			await inner.WaitForCountAsync(1);
		});

		var received = Assert.IsType<EventContext<object>>(Assert.Single(inner.Contexts));
		Assert.Equal("second", Assert.IsType<MutableArgument>(received.Argument).Message);
	}

	[Fact]
	public async Task EventsAfterRollbackAreDiscardedUntilAmbientTransactionExits()
	{
		var channel = new RecordingChannel();
		await RunAsync(channel, async exchanger =>
		{
			await using var transaction = new Transaction();
			await transaction.RollbackAsync();
			await exchanger.ExchangeAsync(CreateContext(new() { Message = "discarded" }), CancellationToken.None);
		});

		Assert.Empty(channel.Contexts);
	}

	[Fact]
	public async Task SendAsync_ActiveTransaction_DefersUntilCommitAndSnapshotsPayloadAsync()
	{
		var inner = new RecordingChannel();
		var channel = inner;

		await RunAsync(channel, async exchanger =>
		{
			await using var transaction = new Transaction();
			var argument = new MutableArgument
			{
				Message = "before",
				Detail = new() { Value = "nested-before" },
			};
			var context = CreateContext(argument, "before");

			await exchanger.ExchangeAsync(context, CancellationToken.None);

			Assert.Equal(0, inner.SendCount);
			argument.Message = "after";
			argument.Detail.Value = "nested-after";
			context.Parameters["Value"] = "after";

			await transaction.CommitAsync();
			await inner.WaitForCountAsync(1);

			var received = Assert.IsType<EventContext<object>>(Assert.Single(inner.Contexts));
			var receivedArgument = Assert.IsType<MutableArgument>(received.Argument);
			Assert.Equal("before", receivedArgument.Message);
			Assert.Equal("nested-before", receivedArgument.Detail.Value);
			Assert.Equal("before", received.Parameters.GetValue("Value"));
			Assert.Equal(TransactionStatus.Committed, transaction.Context.Status);
		});
	}

	[Fact]
	public async Task SendAsync_RolledBackTransaction_DiscardsBufferedEventsAsync()
	{
		var inner = new RecordingChannel();
		var channel = inner;

		await RunAsync(channel, async exchanger =>
		{
			await using var transaction = new Transaction();
			await exchanger.ExchangeAsync(CreateContext(new() { Message = "discarded" }), CancellationToken.None);

			Assert.Equal(0, inner.SendCount);
			await transaction.RollbackAsync();
			Assert.Equal(TransactionStatus.Aborted, transaction.Context.Status);
		});

		Assert.Equal(0, inner.SendCount);
		Assert.Empty(inner.Contexts);
	}

	[Fact]
	public async Task SendAsync_UndeterminedTransaction_DiscardsBufferedEventsAsync()
	{
		var inner = new RecordingChannel();
		var channel = inner;

		await RunAsync(channel, async exchanger =>
		{
			var transaction = new Transaction();
			Assert.True(transaction.Context.Enlist(new ThrowingEnlistment()));

			try
			{
				await exchanger.ExchangeAsync(CreateContext(new() { Message = "discarded" }), CancellationToken.None);
				Assert.Equal(0, inner.SendCount);
				await Assert.ThrowsAsync<InvalidOperationException>(() => transaction.CommitAsync());
				Assert.Equal(TransactionStatus.Undetermined, transaction.Context.Status);
			}
			finally
			{
				await Assert.ThrowsAsync<InvalidOperationException>(() => transaction.DisposeAsync().AsTask());
			}
		});

		Assert.Equal(0, inner.SendCount);
		Assert.Empty(inner.Contexts);
	}

	[Fact]
	public async Task SendAsync_Commit_DoesNotWaitForUnderlyingAsyncDeliveryAsync()
	{
		var inner = new RecordingChannel(blockSend: true);
		var channel = inner;

		await RunAsync(channel, async exchanger =>
		{
			await using var transaction = new Transaction();
			await exchanger.ExchangeAsync(CreateContext(new() { Message = "asynchronous" }), CancellationToken.None);

			var committing = transaction.CommitAsync();
			await inner.WaitForSendStartAsync();

			try
			{
				await committing.WaitAsync(TimeSpan.FromSeconds(30));
			}
			finally
			{
				inner.ReleaseSend();
			}

			Assert.Equal(TransactionStatus.Committed, transaction.Context.Status);
			Assert.Equal(1, inner.SendCount);
		});
	}

	[Fact]
	public async Task SendAsync_CommitCompletion_DeliversAfterCommittedValueIsVisibleAsync()
	{
		var visibleValue = "old";
		var inner = new RecordingChannel(() => visibleValue);
		var channel = inner;

		await RunAsync(channel, async exchanger =>
		{
			await using var transaction = new Transaction();
			Assert.True(transaction.Context.Enlist(new DelegateEnlistment(context =>
			{
				if(context.Phase == EnlistmentPhase.Commit)
					visibleValue = "new";
			})));

			await exchanger.ExchangeAsync(CreateContext(new() { Message = "updated" }), CancellationToken.None);

			Assert.Equal("old", visibleValue);
			Assert.Equal(0, inner.SendCount);

			await transaction.CommitAsync();
			await inner.WaitForCountAsync(1);

			Assert.Equal(TransactionStatus.Committed, transaction.Context.Status);
			Assert.Equal("new", visibleValue);
			Assert.Equal("new", Assert.Single(inner.ObservedValues));
		});
	}

	[Fact]
	public async Task SendAsync_NestedTransaction_FlushesFromRootInEnqueueOrderAsync()
	{
		var inner = new RecordingChannel();
		var channel = inner;

		await RunAsync(channel, async exchanger =>
		{
			await using var root = new Transaction();

			await using(var child = new Transaction())
			{
				await exchanger.ExchangeAsync(CreateContext(new() { Message = "first" }), CancellationToken.None);
				await exchanger.ExchangeAsync(CreateContext(new() { Message = "second" }), CancellationToken.None);
				await child.CommitAsync();
				Assert.Equal(0, inner.SendCount);
			}

			await root.CommitAsync();
			await inner.WaitForCountAsync(2);

			Assert.Equal(
				["first", "second"],
				inner.Contexts.Select(context => Assert.IsType<MutableArgument>(Assert.IsType<EventContext<object>>(context).Argument).Message));
		});
	}

	[Fact]
	public async Task SendAsync_WithoutTransaction_DelegatesImmediatelyAsync()
	{
		var inner = new RecordingChannel();
		var channel = inner;

		await RunAsync(channel, async exchanger =>
		{
			await exchanger.ExchangeAsync(CreateContext(new() { Message = "immediate" }), CancellationToken.None);
			await inner.WaitForCountAsync(1);

			var received = Assert.IsType<EventContext<MutableArgument>>(Assert.Single(inner.Contexts));
			Assert.Equal("immediate", received.Argument.Message);
		});
	}

	private static EventContext<MutableArgument> CreateContext(MutableArgument argument, string value = null)
	{
		var context = new EventContext<MutableArgument>(new TestEventRegistry(), TestEventRegistry.EventName, argument);

		if(value != null)
			context.Parameters["Value"] = value;

		return context;
	}

	private static async Task RunAsync(IEventChannel channel, Func<EventExchanger, Task> action)
	{
		var exchanger = EventExchanger.Instance;
		Assert.Equal(WorkerState.Stopped, exchanger.State);
		Assert.Empty(exchanger.Channels);

		await using var dispatcher = new EventDispatcher();
		var original = exchanger.Dispatcher;
		exchanger.Dispatcher = dispatcher;
		exchanger.Channels.Add(channel);

		try
		{
			await exchanger.StartAsync([]);
			Assert.Equal(WorkerState.Running, exchanger.State);
			await action(exchanger);
		}
		finally
		{
			await exchanger.StopAsync([]);
			exchanger.Channels.Remove(channel);
			exchanger.Dispatcher = original;
			await channel.DisposeAsync();
		}

		Assert.Equal(WorkerState.Stopped, exchanger.State);
		Assert.Empty(exchanger.Channels);
	}

	private sealed class RecordingChannel : IEventChannel
	{
		private int _sendCount;
		private readonly Func<string> _observe;
		private readonly SemaphoreSlim _received = new(0);
		private readonly ConcurrentQueue<EventContext> _contexts = new();
		private readonly ConcurrentQueue<string> _observedValues = new();
		private readonly TaskCompletionSource _sendStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
		private readonly TaskCompletionSource _sendRelease;

		public RecordingChannel(Func<string> observe = null, bool blockSend = false)
		{
			_observe = observe;

			if(blockSend)
				_sendRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);
		}

		public event EventHandler Closed;

		public bool IsClosed { get; private set; } = true;
		public bool IsDisposed { get; private set; }
		public int SendCount => Volatile.Read(ref _sendCount);
		public EventContext[] Contexts => _contexts.ToArray();
		public string[] ObservedValues => _observedValues.ToArray();

		public ValueTask OpenAsync(IEventReceiver receiver, CancellationToken cancellation = default)
		{
			this.IsClosed = false;
			return ValueTask.CompletedTask;
		}

		public async ValueTask SendAsync(EventContext data, CancellationToken cancellation = default)
		{
			_contexts.Enqueue(data);

			if(_observe != null)
				_observedValues.Enqueue(_observe());

			Interlocked.Increment(ref _sendCount);
			_received.Release();
			_sendStarted.TrySetResult();

			if(_sendRelease != null)
				await _sendRelease.Task.WaitAsync(cancellation);
		}

		public Task WaitForSendStartAsync() => _sendStarted.Task.WaitAsync(TimeSpan.FromSeconds(30));
		public void ReleaseSend() => _sendRelease?.TrySetResult();

		public async Task WaitForCountAsync(int count)
		{
			for(var index = 0; index < count; index++)
				Assert.True(await _received.WaitAsync(TimeSpan.FromSeconds(30)));
		}

		public ValueTask CloseAsync(CancellationToken cancellation = default)
		{
			if(!this.IsClosed)
			{
				this.IsClosed = true;
				this.Closed?.Invoke(this, EventArgs.Empty);
			}

			return ValueTask.CompletedTask;
		}

		public async ValueTask DisposeAsync()
		{
			await this.CloseAsync();
			_received.Dispose();
			this.IsDisposed = true;
		}
	}

	private sealed class ThrowingEnlistment : IEnlistment
	{
		public ValueTask OnEnlistAsync(EnlistmentContext context, CancellationToken cancellation) => throw new InvalidOperationException("Expected enlistment failure.");
	}

	private sealed class DelegateEnlistment(Action<EnlistmentContext> enlist) : IEnlistment
	{
		public ValueTask OnEnlistAsync(EnlistmentContext context, CancellationToken cancellation)
		{
			enlist(context);
			return ValueTask.CompletedTask;
		}
	}

	private sealed class TestEventRegistry : EventRegistryBase
	{
		public const string EventName = "Updated";

		public TestEventRegistry() : base("Tests") => this.Event<MutableArgument>(EventName);
	}

	public sealed class MutableArgument
	{
		public string Message { get; set; }
		public MutableDetail Detail { get; set; }
	}

	public sealed class MutableDetail
	{
		public string Value { get; set; }
	}
}
