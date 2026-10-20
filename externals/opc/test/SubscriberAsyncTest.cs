using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Opc.Ua;
using Opc.Ua.Client;

using Xunit;

namespace Zongsoft.Externals.Opc.Tests;

public class SubscriberAsyncTest
{
	private const string TEMPERATURE = "ns=2;s=async-temperature";
	private const string PRESSURE = "ns=2;s=async-pressure";

	[Fact]
	public async Task FlushAsync_PendingConsumer_ReturnsFromNotificationAndPreservesOrder()
	{
		await using var fixture = await Fixture.CreateAsync(4);
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var readings = new List<Reading>();

		fixture.Subscriber.Consume(async (subscriber, entry, value, cancellation) =>
		{
			Assert.Same(fixture.Subscriber, subscriber);
			Assert.False(cancellation.CanBeCanceled);
			readings.Add(new(entry.Name, (int)value));

			if(readings.Count == 1)
			{
				entered.SetResult();
				await release.Task;
			}
		});
		var notification = Task.Run(() => fixture.Notify(TEMPERATURE, 10));

		try
		{
			await notification.WaitAsync(TimeSpan.FromSeconds(5));
			await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
			fixture.Notify(TEMPERATURE, 10);
			fixture.Notify(PRESSURE, 20);
			fixture.Subscriber.Consume();
			fixture.Notify(TEMPERATURE, 99);
			var flush = fixture.Subscriber.FlushAsync().AsTask();

			Assert.False(flush.IsCompleted);
			Assert.Equal([new Reading(TEMPERATURE, 10)], readings);
			release.SetResult();
			await flush.WaitAsync(TimeSpan.FromSeconds(5));

			Assert.Equal([new Reading(TEMPERATURE, 10), new(TEMPERATURE, 10), new(PRESSURE, 20)], readings);
			Assert.Equal(4UL, fixture.Subscriber.Statistics.NotificationCount);
		}
		finally
		{
			release.TrySetResult();
			await notification.WaitAsync(TimeSpan.FromSeconds(5));
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task FlushAsync_ConsumerFailure_ReportsOnceAndContinuesLaterNotifications(bool synchronous)
	{
		await using var fixture = await Fixture.CreateAsync(4);
		var failure = new InvalidOperationException();
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var values = new List<int>();

		fixture.Subscriber.Consume((_, _, value, _) =>
		{
			values.Add((int)value);
			if((int)value != 10)
				return ValueTask.CompletedTask;

			entered.SetResult();
			return synchronous ? throw failure : new(release.Task);
		});

		try
		{
			fixture.Notify(TEMPERATURE, 10);
			await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
			fixture.Notify(TEMPERATURE, 20);
			var flush = fixture.Subscriber.FlushAsync().AsTask();

			if(!synchronous)
			{
				Assert.False(flush.IsCompleted);
				release.SetException(failure);
			}

			Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => flush.WaitAsync(TimeSpan.FromSeconds(5))));
			fixture.Notify(TEMPERATURE, 30);
			await fixture.Subscriber.FlushAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
			Assert.Equal([10, 20, 30], values);
		}
		finally
		{
			release.TrySetResult();
			await Record.ExceptionAsync(() => fixture.Subscriber.FlushAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5)));
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task DisposeAsync_PendingConsumer_DrainsAcceptedWorkAndSharesOutcome(bool fail)
	{
		var fixture = await Fixture.CreateAsync(4);
		var failure = new InvalidOperationException();
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var values = new List<int>();
		var disposed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		using var registration = fixture.Subscriber.Disposed.RegisterChangeCallback(_ => disposed.TrySetResult(), null);

		fixture.Subscriber.Consume(async (_, entry, value, _) =>
		{
			Assert.Equal(TEMPERATURE, entry.Name);
			values.Add((int)value);

			if((int)value == 10)
			{
				entered.SetResult();
				await release.Task;
				if(fail)
					throw failure;
			}
		});

		try
		{
			fixture.Notify(TEMPERATURE, 10);
			await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
			fixture.Notify(TEMPERATURE, 20);
			var disposal = fixture.Subscriber.DisposeAsync().AsTask();
			var repeated = Enumerable.Range(0, 16).Select(_ => fixture.Subscriber.DisposeAsync().AsTask()).ToArray();

			Assert.All(repeated, task => Assert.Same(disposal, task));
			Assert.False(disposal.IsCompleted);
			Assert.False(disposed.Task.IsCompleted);
			Assert.True(fixture.Subscriber.IsDisposed);
			Assert.Throws<ObjectDisposedException>(() => fixture.Subscriber.Consume());
			await Assert.ThrowsAsync<ObjectDisposedException>(() => fixture.Subscriber.FlushAsync().AsTask());
			fixture.Notify(TEMPERATURE, 99);
			release.SetResult();

			if(fail)
			{
				var observed = await Assert.ThrowsAsync<AggregateException>(() => disposal.WaitAsync(TimeSpan.FromSeconds(5)));
				Assert.Same(failure, Assert.Single(observed.InnerExceptions));
				Assert.Same(observed, await Assert.ThrowsAsync<AggregateException>(() => fixture.Subscriber.DisposeAsync().AsTask()));
			}
			else
				await Task.WhenAll(repeated).WaitAsync(TimeSpan.FromSeconds(5));

			await disposed.Task.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.Equal([10, 20], values);
			Assert.Null(fixture.Subscriber.Subscription);
			Assert.Empty(fixture.Subscriber.Entries);
			fixture.Notify(TEMPERATURE, 100);
			Assert.Equal([10, 20], values);
			Assert.Equal(2UL, fixture.Subscriber.Statistics.NotificationCount);
		}
		finally
		{
			release.TrySetResult();
			await Record.ExceptionAsync(() => fixture.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5)));
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task NotificationCapacity_FullQueue_ReportsOverflowAtFlushOrDisposal(bool dispose)
	{
		var fixture = await Fixture.CreateAsync(1);
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var values = new List<int>();

		fixture.Subscriber.Consume(async (_, _, value, _) =>
		{
			values.Add((int)value);
			if((int)value == 10)
			{
				entered.SetResult();
				await release.Task;
			}
		});

		try
		{
			fixture.Notify(TEMPERATURE, 10);
			await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
			fixture.Notify(TEMPERATURE, 20);
			fixture.Notify(TEMPERATURE, 30);
			var boundary = dispose ? fixture.Subscriber.DisposeAsync().AsTask() : fixture.Subscriber.FlushAsync().AsTask();

			Assert.False(boundary.IsCompleted);
			release.SetResult();

			if(dispose)
			{
				var observed = await Assert.ThrowsAsync<AggregateException>(() => boundary.WaitAsync(TimeSpan.FromSeconds(5)));
				Assert.IsType<InvalidOperationException>(Assert.Single(observed.InnerExceptions));
				Assert.Same(boundary, fixture.Subscriber.DisposeAsync().AsTask());
			}
			else
				await Assert.ThrowsAsync<InvalidOperationException>(() => boundary.WaitAsync(TimeSpan.FromSeconds(5)));

			Assert.Equal([10, 20], values);
			Assert.Equal(3UL, fixture.Subscriber.Statistics.NotificationCount);

			if(!dispose)
			{
				fixture.Notify(TEMPERATURE, 40);
				await fixture.Subscriber.FlushAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
				Assert.Equal([10, 20, 40], values);
			}
		}
		finally
		{
			release.TrySetResult();
			await Record.ExceptionAsync(() => fixture.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5)));
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task FlushAsync_CanceledWait_DoesNotAbandonAcceptedConsumerOrLoseFailure(bool fail)
	{
		var fixture = await Fixture.CreateAsync(4);
		using var cancellation = new CancellationTokenSource();
		var failure = new InvalidOperationException();
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var completed = false;

		fixture.Subscriber.Consume(async (_, _, _, token) =>
		{
			Assert.False(token.CanBeCanceled);
			entered.SetResult();
			await release.Task;
			completed = true;

			if(fail)
				throw failure;
		});

		try
		{
			fixture.Notify(TEMPERATURE, 10);
			await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
			var flush = fixture.Subscriber.FlushAsync(cancellation.Token).AsTask();
			cancellation.Cancel();
			var observed = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => flush.WaitAsync(TimeSpan.FromSeconds(5)));
			Assert.Equal(cancellation.Token, observed.CancellationToken);
			var disposal = fixture.Subscriber.DisposeAsync().AsTask();
			Assert.False(disposal.IsCompleted);
			Assert.False(completed);
			release.SetResult();

			if(fail)
			{
				var error = await Assert.ThrowsAsync<AggregateException>(() => disposal.WaitAsync(TimeSpan.FromSeconds(5)));
				Assert.Same(failure, Assert.Single(error.InnerExceptions));
			}
			else
				await disposal.WaitAsync(TimeSpan.FromSeconds(5));

			Assert.True(completed);
		}
		finally
		{
			release.TrySetResult();
			await Record.ExceptionAsync(() => fixture.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5)));
		}
	}

	private sealed record Reading(string Name, int Value);
	private sealed class Fixture : IAsyncDisposable
	{
		private readonly Dictionary<string, MonitoredItem> _monitors = [];
		private Fixture(int capacity)
		{
			var constructor = typeof(Subscriber).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
				[typeof(SubscriberOptions), typeof(Action<Subscriber, Subscriber.Entry, object>)], null);
			Assert.NotNull(constructor);
			this.Subscriber = (Subscriber)constructor.Invoke([new SubscriberOptions { QueueSize = 32, NotificationCapacity = capacity }, null]);
		}

		public Subscriber Subscriber { get; }
		public static async Task<Fixture> CreateAsync(int capacity)
		{
			var fixture = new Fixture(capacity);

			try
			{
				foreach(var name in new[] { TEMPERATURE, PRESSURE })
				{
					Assert.True(await fixture.Subscriber.Entries.AddAsync(name));
					var subscription = Assert.IsType<Subscription>(fixture.Subscriber.Subscription);
					Assert.Null(subscription.Session);
					fixture._monitors.Add(name, Assert.Single(subscription.MonitoredItems, item => item.StartNodeId == NodeId.Parse(name)));
				}

				Assert.False(fixture.Subscriber.Registered);
				return fixture;
			}
			catch
			{
				await fixture.DisposeAsync();
				throw;
			}
		}

		public void Notify(string name, int value) => _monitors[name].SaveValueInCache(new MonitoredItemNotification { Value = new DataValue(new Variant(value)) });
		public ValueTask DisposeAsync() => this.Subscriber.DisposeAsync();
	}
}
