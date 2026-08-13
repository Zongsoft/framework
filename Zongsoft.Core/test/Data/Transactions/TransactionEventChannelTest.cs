using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

using Xunit;

using Zongsoft.Components;

namespace Zongsoft.Data.Transactions.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class TransactionEventChannelCollection
{
	public const string Name = nameof(TransactionEventChannelCollection);
}

[Collection(TransactionEventChannelCollection.Name)]
public sealed class TransactionEventChannelTest
{
	[Fact]
	public async Task SendAsync_ActiveTransaction_DefersUntilCommitAndSnapshotsPayloadAsync()
	{
		var inner = new RecordingChannel();
		var channel = new TransactionEventChannel(inner);

		await RunAsync(channel, async exchanger =>
		{
			using var transaction = new Transaction();
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

			transaction.Commit();
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
		var channel = new TransactionEventChannel(inner);

		await RunAsync(channel, async exchanger =>
		{
			using var transaction = new Transaction();
			await exchanger.ExchangeAsync(CreateContext(new() { Message = "discarded" }), CancellationToken.None);

			Assert.Equal(0, inner.SendCount);
			transaction.Rollback();
			Assert.Equal(TransactionStatus.Aborted, transaction.Context.Status);
		});

		Assert.Equal(0, inner.SendCount);
		Assert.Empty(inner.Contexts);
	}

	[Fact]
	public async Task SendAsync_UndeterminedTransaction_DiscardsBufferedEventsAsync()
	{
		var inner = new RecordingChannel();
		var channel = new TransactionEventChannel(inner);

		await RunAsync(channel, async exchanger =>
		{
			using var transaction = new Transaction();
			Assert.True(transaction.Enlist(new ThrowingEnlistment()));
			await exchanger.ExchangeAsync(CreateContext(new() { Message = "discarded" }), CancellationToken.None);

			Assert.Equal(0, inner.SendCount);
			Assert.Throws<InvalidOperationException>(transaction.Commit);
			Assert.Equal(TransactionStatus.Undetermined, transaction.Context.Status);
		});

		Assert.Equal(0, inner.SendCount);
		Assert.Empty(inner.Contexts);
	}

	[Fact]
	public async Task SendAsync_Commit_DoesNotWaitForUnderlyingAsyncDeliveryAsync()
	{
		var inner = new RecordingChannel(blockSend: true);
		var channel = new TransactionEventChannel(inner);

		await RunAsync(channel, async exchanger =>
		{
			using var transaction = new Transaction();
			await exchanger.ExchangeAsync(CreateContext(new() { Message = "asynchronous" }), CancellationToken.None);

			var committing = Task.Run(transaction.Commit);
			await inner.WaitForSendStartAsync();

			try
			{
				await committing.WaitAsync(TimeSpan.FromSeconds(10));
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
		var channel = new TransactionEventChannel(inner);

		await RunAsync(channel, async exchanger =>
		{
			using var transaction = new Transaction();
			Assert.True(transaction.Enlist(new DelegateEnlistment(context =>
			{
				if(context.Phase == EnlistmentPhase.Commit)
					visibleValue = "new";
			})));

			await exchanger.ExchangeAsync(CreateContext(new() { Message = "updated" }), CancellationToken.None);

			Assert.Equal("old", visibleValue);
			Assert.Equal(0, inner.SendCount);

			transaction.Commit();
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
		var channel = new TransactionEventChannel(inner);

		await RunAsync(channel, async exchanger =>
		{
			using var root = new Transaction();

			using(var child = new Transaction())
			{
				await exchanger.ExchangeAsync(CreateContext(new() { Message = "first" }), CancellationToken.None);
				await exchanger.ExchangeAsync(CreateContext(new() { Message = "second" }), CancellationToken.None);
				child.Commit();
				Assert.Equal(0, inner.SendCount);
			}

			root.Commit();
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
		var channel = new TransactionEventChannel(inner);

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

		public ValueTask OpenAsync(EventExchanger exchanger, CancellationToken cancellation = default)
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

		public Task WaitForSendStartAsync() => _sendStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
		public void ReleaseSend() => _sendRelease?.TrySetResult();

		public async Task WaitForCountAsync(int count)
		{
			for(var index = 0; index < count; index++)
				Assert.True(await _received.WaitAsync(TimeSpan.FromSeconds(10)));
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
		public void OnEnlist(EnlistmentContext context) => throw new InvalidOperationException("Expected enlistment failure.");
		public ValueTask OnEnlistAsync(EnlistmentContext context, CancellationToken cancellation) => throw new InvalidOperationException("Expected enlistment failure.");
	}

	private sealed class DelegateEnlistment(Action<EnlistmentContext> enlist) : IEnlistment
	{
		public void OnEnlist(EnlistmentContext context) => enlist(context);
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
