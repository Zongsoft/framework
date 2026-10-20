using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Opc.Ua;
using Opc.Ua.Client;

using Xunit;

using Zongsoft.Caching;
using Zongsoft.Diagnostics;

namespace Zongsoft.Externals.Opc.Tests;

public class SubscriberSpoolingTest
{
	private const string TEMPERATURE = "ns=2;s=temperature";
	private const string STATUS = "ns=2;s=status";

	[Fact]
	public async Task Notifications_RepeatedAndMultipleNodes_PreserveEveryReadingInBatches()
	{
		await using var fixture = await Fixture.CreateAsync(TEMPERATURE, STATUS);
		var values = new List<Reading>();
		var batches = new List<int>();

		await using var spooler = new Spooler<Reading>((items, _) =>
		{
			var batch = items.ToArray();
			values.AddRange(batch);
			batches.Add(batch.Length);
			return ValueTask.CompletedTask;
		}, TimeSpan.FromDays(1), 3);
		var cancellation = TestContext.Current.CancellationToken;
		fixture.Consume(spooler, cancellation);

		await fixture.NotifyAsync(TEMPERATURE, 10);
		await fixture.NotifyAsync(TEMPERATURE, 10);
		await fixture.NotifyAsync(TEMPERATURE, 11);
		await fixture.NotifyAsync(STATUS, "ready");
		await fixture.NotifyAsync(TEMPERATURE, 12);
		await fixture.NotifyAsync(STATUS, "running");
		await fixture.NotifyAsync(TEMPERATURE, 13);
		fixture.Subscriber.Consume();
		await spooler.FlushAsync(cancellation);
		await spooler.DisposeAsync();

		Assert.Equal(new[]
		{
			new Reading(TEMPERATURE, 10),
			new Reading(TEMPERATURE, 10),
			new Reading(TEMPERATURE, 11),
			new Reading(STATUS, "ready"),
			new Reading(TEMPERATURE, 12),
			new Reading(STATUS, "running"),
			new Reading(TEMPERATURE, 13),
		}, values);
		Assert.Equal(new[] { 3, 3, 1 }, batches);
		Assert.Equal(7UL, fixture.Subscriber.Statistics.NotificationCount);
	}

	[Fact]
	public async Task FullBuffer_AsyncProducerAndFlush_WaitForActiveBatch()
	{
		await using var fixture = await Fixture.CreateAsync(TEMPERATURE, STATUS);
		var flusher = new PausingFlusher();
		await using var spooler = new Spooler<Reading>(flusher.FlushAsync, TimeSpan.FromDays(1), 2);
		var cancellation = TestContext.Current.CancellationToken;
		fixture.Consume(spooler, cancellation);
		await fixture.NotifyAsync(TEMPERATURE, 10);
		await fixture.NotifyAsync(TEMPERATURE, 20);

		var notification = fixture.NotifyAsync(STATUS, "ready");
		Task flushing;

		try
		{
			await flusher.Entered.WaitAsync(TimeSpan.FromSeconds(5), cancellation);

			//同步通知已经返回，但满载投递仍由唯一的异步生产者等待。
			Assert.Equal(3UL, fixture.Subscriber.Statistics.NotificationCount);
			Assert.True(spooler.IsEmpty);
			Assert.False(notification.IsCompleted);

			flushing = spooler.FlushAsync(cancellation).AsTask();

			Assert.False(flushing.IsCompleted);
			Assert.Equal(1, flusher.Calls);
		}
		finally { flusher.Resume(); }

		await Task.WhenAll(notification, flushing).WaitAsync(TimeSpan.FromSeconds(5), cancellation);
		fixture.Subscriber.Consume();
		await spooler.FlushAsync(cancellation);

		Assert.Equal(new[] { new Reading(TEMPERATURE, 10), new Reading(TEMPERATURE, 20), new Reading(STATUS, "ready") }, flusher.Values);
		Assert.Equal(2, flusher.Calls);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task AsyncProducer_ObservesCancellationOrFailure(bool cancel)
	{
		await using var fixture = await Fixture.CreateAsync(TEMPERATURE, STATUS);
		using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		var failure = new ApplicationException("Batch persistence failure.");
		var flusher = new PausingFlusher(cancel ? null : failure);
		var notifications = 0;

		await using var spooler = new Spooler<Reading>(flusher.FlushAsync, TimeSpan.FromDays(1), 2);
		spooler.Failed += (_, _) => Interlocked.Increment(ref notifications);
		fixture.Consume(spooler, cancellation.Token);
		await fixture.NotifyAsync(TEMPERATURE, 10);
		await fixture.NotifyAsync(TEMPERATURE, 20);
		var notification = fixture.NotifyAsync(STATUS, "rejected");

		try
		{
			await flusher.Entered.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
			Assert.False(notification.IsCompleted);

			if(cancel)
				await cancellation.CancelAsync();
		}
		finally { flusher.Resume(); }

		var observed = await Record.ExceptionAsync(() => notification.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));

		if(cancel)
			Assert.Equal(cancellation.Token, Assert.IsAssignableFrom<OperationCanceledException>(observed).CancellationToken);
		else
			Assert.Same(failure, observed);

		Assert.Equal(new[] { new Reading(TEMPERATURE, 10), new Reading(TEMPERATURE, 20) }, flusher.Values);
		Assert.True(spooler.IsEmpty);
		Assert.Equal(0, notifications);
		Assert.Equal(3UL, fixture.Subscriber.Statistics.NotificationCount);
		fixture.Subscriber.Consume();
	}

	[Fact]
	public async Task UnbindThenFlushAndDispose_IgnoresLaterNotifications()
	{
		await using var fixture = await Fixture.CreateAsync(TEMPERATURE, STATUS);
		var flusher = new PausingFlusher();
		await using var spooler = new Spooler<Reading>(flusher.FlushAsync, TimeSpan.FromDays(1), 4);
		var cancellation = TestContext.Current.CancellationToken;
		fixture.Consume(spooler, cancellation);

		await fixture.NotifyAsync(TEMPERATURE, 10);
		await fixture.NotifyAsync(STATUS, "ready");
		fixture.Subscriber.Consume();
		var flushing = spooler.FlushAsync(cancellation).AsTask();
		Task disposal;

		try
		{
			await flusher.Entered.WaitAsync(TimeSpan.FromSeconds(5), cancellation);
			fixture.Notify(TEMPERATURE, 99);

			Assert.True(spooler.IsEmpty);

			disposal = spooler.DisposeAsync().AsTask();

			Assert.False(disposal.IsCompleted);
			Assert.False(flushing.IsCompleted);
			Assert.False(flusher.Cancellation.IsCancellationRequested);
		}
		finally { flusher.Resume(); }

		await Task.WhenAll(flushing, disposal).WaitAsync(TimeSpan.FromSeconds(5), cancellation);
		fixture.Notify(STATUS, "late");

		Assert.Equal(1, flusher.Calls);
		Assert.Equal(new[] { new Reading(TEMPERATURE, 10), new Reading(STATUS, "ready") }, flusher.Values);
		Assert.Equal(4UL, fixture.Subscriber.Statistics.NotificationCount);
	}

	[Fact]
	public async Task DisposeAsync_WithoutFlush_DoesNotConsumeRemainingNotifications()
	{
		await using var fixture = await Fixture.CreateAsync(TEMPERATURE, STATUS);
		var calls = 0;
		await using var spooler = new Spooler<Reading>((_, _) =>
		{
			Interlocked.Increment(ref calls);
			return ValueTask.CompletedTask;
		}, TimeSpan.FromDays(1), 4);
		fixture.Consume(spooler, TestContext.Current.CancellationToken);
		await fixture.NotifyAsync(TEMPERATURE, 10);
		await fixture.NotifyAsync(STATUS, "ready");
		Assert.Equal(2, spooler.Count);

		fixture.Subscriber.Consume();
		await spooler.DisposeAsync();
		fixture.Notify(TEMPERATURE, 99);

		Assert.Equal(0, calls);
		Assert.Equal(3UL, fixture.Subscriber.Statistics.NotificationCount);
		await Assert.ThrowsAsync<ObjectDisposedException>(() => spooler.PutAsync(new(STATUS, "closed")).AsTask());
	}

	[Fact]
	public async Task PeriodicFlush_DisposeAsync_CancelsCallbackAndWaitsForCleanup()
	{
		await using var fixture = await Fixture.CreateAsync(TEMPERATURE, STATUS);
		var clock = new ManualClock();
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var canceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var values = new List<Reading>();
		var notifications = 0;
		await using var spooler = new Spooler<Reading>(async (items, cancellation) =>
		{
			values.AddRange(items);
			entered.TrySetResult();

			try { await Task.Delay(Timeout.InfiniteTimeSpan, cancellation); }
			finally
			{
				canceled.TrySetResult();
				await release.Task;
			}
		}, TimeSpan.FromSeconds(1), 4, clock);
		spooler.Failed += (_, _) => Interlocked.Increment(ref notifications);
		fixture.Consume(spooler, TestContext.Current.CancellationToken);
		await fixture.NotifyAsync(TEMPERATURE, 10);
		await fixture.NotifyAsync(STATUS, "ready");
		clock.Tick();
		Task disposal;

		try
		{
			await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
			fixture.Subscriber.Consume();
			disposal = spooler.DisposeAsync().AsTask();
			await canceled.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

			Assert.False(disposal.IsCompleted);
			Assert.Equal(0, notifications);
		}
		finally { release.TrySetResult(); }

		await disposal.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

		Assert.True(clock.IsDisposed);
		Assert.Equal(0, notifications);
		Assert.Equal(new[] { new Reading(TEMPERATURE, 10), new Reading(STATUS, "ready") }, values);
	}

	[Fact]
	public async Task PeriodicFlush_Failure_NotifiesAndPropagatesOnDisposal()
	{
		await using var fixture = await Fixture.CreateAsync(TEMPERATURE, STATUS);
		var clock = new ManualClock();
		var failure = new ApplicationException("Periodic persistence failure.");
		var notified = new TaskCompletionSource<FailureEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
		var values = new List<Reading>();
		var calls = 0;
		var notifications = 0;
		var spooler = new Spooler<Reading>((items, _) =>
		{
			values.AddRange(items);
			return ++calls == 1 ? ValueTask.FromException(failure) : ValueTask.CompletedTask;
		}, TimeSpan.FromSeconds(1), 4, clock);

		try
		{
			spooler.Failed += (_, args) =>
			{
				Interlocked.Increment(ref notifications);
				notified.TrySetResult(args);
			};
			fixture.Consume(spooler, TestContext.Current.CancellationToken);
			await fixture.NotifyAsync(TEMPERATURE, 10);
			clock.Tick();
			var args = await notified.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

			Assert.Same(failure, args.Exception);
			Assert.True(clock.IsDisposed);
			Assert.True(spooler.IsEmpty);

			//周期运行已经结束；显式刷新仍可处理后续数据，不会重试已经取走的数据。
			await fixture.NotifyAsync(STATUS, "ready");
			clock.Tick();
			Assert.Equal(1, calls);
			Assert.Equal(1, spooler.Count);

			fixture.Subscriber.Consume();
			await spooler.FlushAsync(TestContext.Current.CancellationToken);
			var observed = await Assert.ThrowsAsync<ApplicationException>(() => spooler.DisposeAsync().AsTask());

			Assert.Same(failure, observed);
			Assert.Equal(1, notifications);
			Assert.Equal(2, calls);
			Assert.Equal(new[] { new Reading(TEMPERATURE, 10), new Reading(STATUS, "ready") }, values);
		}
		finally { await Record.ExceptionAsync(() => spooler.DisposeAsync().AsTask()); }
	}

	[Fact]
	public async Task SaveValueInCache_ConsumerFailure_PropagatesAtPublicBoundary()
	{
		await using var fixture = await Fixture.CreateAsync(TEMPERATURE);
		var failure = new ApplicationException("Consumer failure.");
		fixture.Subscriber.Consume((_, _, _) => throw failure);

		Assert.Same(failure, Assert.Throws<ApplicationException>(() => fixture.Notify(TEMPERATURE, 42)));
		Assert.Equal(1UL, fixture.Subscriber.Statistics.NotificationCount);
	}

	private sealed record Reading(string Name, object Value);

	private sealed class PausingFlusher(Exception failure = null)
	{
		private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
		private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
		private int _calls;

		public Task Entered => _entered.Task;
		public int Calls => Volatile.Read(ref _calls);
		public CancellationToken Cancellation { get; private set; }
		public List<Reading> Values { get; } = [];
		public void Resume() => _release.TrySetResult();

		public async ValueTask FlushAsync(IEnumerable<Reading> items, CancellationToken cancellation)
		{
			this.Cancellation = cancellation;
			this.Values.AddRange(items);
			if(Interlocked.Increment(ref _calls) != 1)
				return;

			_entered.TrySetResult();
			await _release.Task.WaitAsync(cancellation);

			if(failure != null)
				throw failure;
		}
	}

	private sealed class Fixture : IAsyncDisposable
	{
		private Task _pending = Task.CompletedTask;

		private Fixture()
		{
			//仅调用真实构造函数建立离线订阅，不绕过初始化或反射调用通知方法。
			var constructor = typeof(Subscriber).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
				[typeof(SubscriberOptions), typeof(Action<Subscriber, Subscriber.Entry, object>)], null);
			Assert.NotNull(constructor);
			this.Subscriber = (Subscriber)constructor.Invoke([new SubscriberOptions { QueueSize = 32 }, null]);

			Assert.False(this.Subscriber.Registered);
			Assert.Null(((Subscription)this.Subscriber.Subscription).Session);
		}

		public Subscriber Subscriber { get; }

		public static async Task<Fixture> CreateAsync(params string[] names)
		{
			var fixture = new Fixture();

			try
			{
				foreach(var name in names)
					Assert.True(await fixture.Subscriber.Entries.AddAsync(name, cancellation: TestContext.Current.CancellationToken));

				Assert.Equal(names.Length, ((Subscription)fixture.Subscriber.Subscription).MonitoredItems.Count());
				Assert.False(fixture.Subscriber.Registered);
				return fixture;
			}
			catch
			{
				await fixture.DisposeAsync();
				throw;
			}
		}

		public void Consume(Spooler<Reading> spooler, CancellationToken cancellation) =>
			this.Subscriber.Consume((_, entry, value) => _pending = spooler.PutAsync(new(entry.Name, value), cancellation).AsTask());

		public Task NotifyAsync(string name, object value)
		{
			//测试控制通知源：每次最多一项投递，并由调用方在通知回调之外等待。
			Assert.True(_pending.IsCompleted);
			this.Notify(name, value);
			return _pending;
		}

		public void Notify(string name, object value)
		{
			var subscription = (Subscription)this.Subscriber.Subscription;
			var monitor = Assert.Single(subscription.MonitoredItems, item => item.StartNodeId == NodeId.Parse(name));
			monitor.SaveValueInCache(new MonitoredItemNotification { Value = new DataValue(new Variant(value)) });
		}

		public async ValueTask DisposeAsync()
		{
			this.Subscriber.Consume();
			await this.Subscriber.DisposeAsync();
		}
	}

	private sealed class ManualClock : TimeProvider
	{
		private ManualTimer _timer;

		public bool IsDisposed => _timer?.IsDisposed == true;
		public void Tick() => _timer?.Tick();
		public override ITimer CreateTimer(TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period) => _timer = new(callback, state);

		private sealed class ManualTimer(TimerCallback callback, object state) : ITimer
		{
			private int _disposed;

			public bool IsDisposed => Volatile.Read(ref _disposed) != 0;
			public bool Change(TimeSpan dueTime, TimeSpan period) => !this.IsDisposed;

			public void Tick()
			{
				if(!this.IsDisposed)
					callback(state);
			}

			public void Dispose() => Interlocked.Exchange(ref _disposed, 1);
			public ValueTask DisposeAsync()
			{
				this.Dispose();
				return ValueTask.CompletedTask;
			}
		}
	}
}
