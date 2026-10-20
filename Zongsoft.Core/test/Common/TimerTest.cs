using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Common.Tests;

public class TimerTest
{
	private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);
	private static readonly TimeSpan _period = TimeSpan.FromSeconds(1);

	[Fact]
	public async Task ConstructionAndIdleStopDoNotCreateClock()
	{
		var clock = new ManualTimeProvider();
		var count = 0;
		var notifications = 0;

		var timer = new Timer(_period, (_, _) => { Interlocked.Increment(ref count); return ValueTask.CompletedTask; }, clock);
		timer.Stopped += (_, _) => Interlocked.Increment(ref notifications);

		clock.Advance(TimeSpan.FromDays(1));
		timer.Stop();
		await timer.StopAsync();

		Assert.Equal(0, clock.CreationCount);
		Assert.Equal(0, count);
		Assert.False(timer.IsRunning);
		Assert.False(timer.IsDisposed);

		timer.Dispose();
		timer.Dispose();
		await timer.DisposeAsync();
		await timer.DisposeAsync();

		Assert.True(timer.IsDisposed);
		Assert.Equal(0, notifications);
		Assert.Equal(0, clock.CreationCount);
		Assert.Throws<ObjectDisposedException>(() => timer.Start());
		Assert.Throws<ObjectDisposedException>(() => timer.Period);
		Assert.Throws<ObjectDisposedException>(() => timer.Period = _period);
	}

	[Theory]
	[InlineData(-10000L, 10000L)]
	[InlineData(0L, 10000L)]
	[InlineData(5000L, 10000L)]
	[InlineData(10000L, 10000L)]
	[InlineData(12500L, 12500L)]
	[InlineData(42949672940000L, 42949672940000L)]
	[InlineData(42949672950000L, 42949672940000L)]
	[InlineData(long.MinValue, 10000L)]
	[InlineData(long.MaxValue, 42949672940000L)]
	public async Task ConstructionAndIdlePeriodChangesClampWithoutCreatingClock(long ticks, long expectedTicks)
	{
		var clock = new ManualTimeProvider();
		var period = TimeSpan.FromTicks(ticks);
		var expected = TimeSpan.FromTicks(expectedTicks);

		await using var timer = new Timer(period, (_, _) => ValueTask.CompletedTask, clock);

		Assert.Equal(expected, timer.Period);
		Assert.Equal(0, clock.CreationCount);
		Assert.False(timer.IsRunning);

		timer.Period = _period;
		timer.Period = period;

		Assert.Equal(expected, timer.Period);
		Assert.Equal(0, clock.CreationCount);
		Assert.False(timer.IsRunning);
	}

	[Theory]
	[InlineData(0L, 10000L)]
	[InlineData(12500L, 12500L)]
	[InlineData(long.MaxValue, 42949672940000L)]
	public async Task RunningPeriodChangesUseClampedScheduleAndPreserveFractions(long ticks, long expectedTicks)
	{
		var clock = new ManualTimeProvider();
		var tick = NewSignal();
		var calls = 0;

		await using var timer = new Timer(_period, (_, _) =>
		{
			Interlocked.Increment(ref calls);
			tick.TrySetResult();
			return ValueTask.CompletedTask;
		}, clock);

		timer.Start();
		await clock.WaitForCreationAsync();
		timer.Period = TimeSpan.FromTicks(ticks);

		Assert.Equal(TimeSpan.FromTicks(expectedTicks), timer.Period);

		clock.Advance(TimeSpan.FromTicks(expectedTicks - 1));

		Assert.Equal(0, Volatile.Read(ref calls));
		Assert.False(tick.Task.IsCompleted);

		clock.Advance(TimeSpan.FromTicks(1));
		await WaitAsync(tick.Task);
		await WaitAsync(timer.StopAsync());

		Assert.Equal(1, calls);
		Assert.False(timer.IsRunning);
		Assert.Equal(1, clock.CreationCount);
		Assert.Equal(1, clock.DisposalCount);
	}

	[Fact]
	public async Task SystemClockAcceptsClampedMaximumForCreationAndChange()
	{
		var calls = 0;
		await using var timer = new Timer(TimeSpan.MaxValue, (_, _) =>
		{
			Interlocked.Increment(ref calls);
			return ValueTask.CompletedTask;
		});

		timer.Start();

		Assert.Equal(TimeSpan.FromMilliseconds(4294967294), timer.Period);
		Assert.True(timer.IsRunning);

		timer.Period = TimeSpan.FromMilliseconds(uint.MaxValue);
		await WaitAsync(timer.StopAsync());

		Assert.Equal(TimeSpan.FromMilliseconds(4294967294), timer.Period);
		Assert.Equal(0, calls);
		Assert.False(timer.IsRunning);
	}

	[Fact]
	public async Task ClockChangeFailurePreservesPublishedPeriodAndCanStop()
	{
		var clock = new ManualTimeProvider();
		var tick = NewSignal();
		var failure = new ApplicationException();
		await using var timer = new Timer(_period, (_, _) => { tick.TrySetResult(); return ValueTask.CompletedTask; }, clock);

		timer.Start();
		await clock.WaitForCreationAsync();
		clock.ChangeFailure = failure;
		var observed = Assert.Throws<ApplicationException>(() => timer.Period = TimeSpan.FromSeconds(2));
		clock.Advance(_period);
		await WaitAsync(tick.Task);
		await timer.StopAsync();

		Assert.Same(failure, observed);
		Assert.Equal(_period, timer.Period);
		Assert.False(timer.IsRunning);
		Assert.Equal(1, clock.DisposalCount);
	}

	[Fact]
	public async Task RejectedClockChangePreservesPeriodAndCanStop()
	{
		var clock = new ManualTimeProvider();
		var tick = NewSignal();
		await using var timer = new Timer(_period, (_, _) => { tick.TrySetResult(); return ValueTask.CompletedTask; }, clock);

		timer.Start();
		await clock.WaitForCreationAsync();
		clock.RejectChanges = true;
		Assert.Throws<ObjectDisposedException>(() => timer.Period = TimeSpan.FromSeconds(2));
		clock.Advance(_period);
		await WaitAsync(tick.Task);
		await timer.StopAsync();

		Assert.Equal(_period, timer.Period);
		Assert.False(timer.IsRunning);
		Assert.Equal(1, clock.DisposalCount);
	}

	[Fact]
	public void ConstructorRequiresCallback()
	{
		var exception = Assert.Throws<ArgumentNullException>(() => new Timer(_period, null));

		Assert.Equal("tick", exception.ParamName);
	}

	[Fact]
	public async Task PeriodChangesControlNextTickWithoutStartingAnIdleTimer()
	{
		var clock = new ManualTimeProvider();
		var first = NewSignal();
		var second = NewSignal();
		var count = 0;
		await using var timer = new Timer(_period, (_, _) =>
		{
			if(Interlocked.Increment(ref count) == 1)
				first.TrySetResult();
			else
				second.TrySetResult();

			return ValueTask.CompletedTask;
		}, clock);

		timer.Period = TimeSpan.FromSeconds(2);
		Assert.Equal(0, clock.CreationCount);
		timer.Start();
		await clock.WaitForCreationAsync();
		clock.Advance(_period);

		Assert.Equal(0, Volatile.Read(ref count));

		clock.Advance(_period);
		await WaitAsync(first.Task);
		timer.Period = TimeSpan.FromSeconds(3);
		clock.Advance(TimeSpan.FromSeconds(2));

		Assert.Equal(1, Volatile.Read(ref count));

		clock.Advance(_period);
		await WaitAsync(second.Task);
		await timer.StopAsync();

		Assert.Equal(2, count);
		Assert.Equal(TimeSpan.FromSeconds(3), timer.Period);
		Assert.Equal(1, clock.DisposalCount);
	}

	[Fact]
	public async Task ConcurrentStartsShareRunAndRestartCreatesAnotherRun()
	{
		var clock = new ManualTimeProvider();
		var states = Channel.CreateUnbounded<object>();
		var state = new object();
		var notifications = 0;
		await using var timer = new Timer(_period, (value, _) =>
		{
			states.Writer.TryWrite(value);
			return ValueTask.CompletedTask;
		}, clock);

		timer.Stopped += (_, _) => Interlocked.Increment(ref notifications);

		var starts = Enumerable.Range(0, 16).Select(_ => Task.Run(() => timer.Start(state), TestContext.Current.CancellationToken));
		await WaitAsync(Task.WhenAll(starts));
		await clock.WaitForCreationAsync();
		timer.Start(new object());
		clock.Advance(_period);
		var received = await states.Reader.ReadAsync(TestContext.Current.CancellationToken).AsTask().WaitAsync(_timeout);

		Assert.Same(state, received);
		Assert.Equal(1, clock.CreationCount);

		await timer.StopAsync();

		Assert.False(timer.IsRunning);
		Assert.Equal(1, notifications);

		timer.Start();
		await clock.WaitForCreationAsync();
		clock.Advance(_period);
		received = await states.Reader.ReadAsync(TestContext.Current.CancellationToken).AsTask().WaitAsync(_timeout);
		await timer.StopAsync();

		Assert.Null(received);
		Assert.Equal(2, notifications);
		Assert.Equal(2, clock.CreationCount);
		Assert.Equal(2, clock.DisposalCount);
	}

	[Fact]
	public async Task RestartDiscardsPendingTicksAndPreviousCancellation()
	{
		var clock = new ManualTimeProvider();
		var entered = NewSignal();
		var release = NewSignal();
		var restarted = NewSignal();
		var previous = new object();
		var current = new object();
		var previousCalls = 0;
		var currentCalls = 0;
		CancellationToken previousCancellation = default;
		CancellationToken currentCancellation = default;
		await using var timer = new Timer(_period, async (state, token) =>
		{
			if(ReferenceEquals(state, previous))
			{
				previousCancellation = token;
				Interlocked.Increment(ref previousCalls);
				entered.TrySetResult();
				await WaitAsync(release.Task);
			}
			else
			{
				Assert.Same(current, state);
				currentCancellation = token;
				Interlocked.Increment(ref currentCalls);
				restarted.TrySetResult();
			}
		}, clock);

		try
		{
			timer.Start(previous);
			await clock.WaitForCreationAsync();
			clock.Advance(_period);
			await WaitAsync(entered.Task);
			clock.Advance(TimeSpan.FromSeconds(8));
			var stopping = timer.StopAsync();

			Assert.False(stopping.IsCompleted);
			Assert.Equal(1, Volatile.Read(ref previousCalls));
			Assert.True(previousCancellation.IsCancellationRequested);

			release.TrySetResult();
			await WaitAsync(stopping);
			timer.Period = TimeSpan.FromSeconds(2);
			timer.Start(current);
			await clock.WaitForCreationAsync();
			clock.Advance(_period);

			Assert.True(timer.IsRunning);
			Assert.Equal(1, Volatile.Read(ref previousCalls));
			Assert.Equal(0, Volatile.Read(ref currentCalls));
			Assert.False(restarted.Task.IsCompleted);

			clock.Advance(_period);
			await WaitAsync(restarted.Task);

			Assert.False(currentCancellation.IsCancellationRequested);
			Assert.True(previousCancellation.IsCancellationRequested);
			Assert.NotEqual(previousCancellation, currentCancellation);

			await WaitAsync(timer.StopAsync());

			Assert.True(currentCancellation.IsCancellationRequested);
			Assert.Equal(1, previousCalls);
			Assert.Equal(1, currentCalls);
			Assert.Equal(2, clock.CreationCount);
			Assert.Equal(2, clock.DisposalCount);
			Assert.False(timer.IsRunning);
		}
		finally { release.TrySetResult(); }
	}

	[Fact]
	public async Task TicksWaitForPreviousCallbackAndCoalesceWhileItIsBusy()
	{
		var clock = new ManualTimeProvider();
		var entered = NewSignal();
		var release = NewSignal();
		var second = NewSignal();
		var count = 0;
		var active = 0;
		var overlap = 0;
		Timer timer = null;
		timer = new Timer(_period, async (_, cancellation) =>
		{
			if(Interlocked.Increment(ref active) != 1)
				Interlocked.Increment(ref overlap);

			try
			{
				if(Interlocked.Increment(ref count) == 1)
				{
					entered.TrySetResult();
					await release.Task.WaitAsync(cancellation);
				}
				else
				{
					timer.Stop();
					second.TrySetResult();
				}
			}
			finally { Interlocked.Decrement(ref active); }
		}, clock);

		try
		{
			timer.Start();
			await clock.WaitForCreationAsync();
			clock.Advance(_period);
			await WaitAsync(entered.Task);
			clock.Advance(TimeSpan.FromSeconds(8));

			Assert.Equal(1, Volatile.Read(ref count));
			Assert.Equal(1, Volatile.Read(ref active));

			release.TrySetResult();
			await WaitAsync(second.Task);
			await WaitAsync(timer.StopAsync());

			Assert.Equal(2, count);
			Assert.Equal(0, active);
			Assert.Equal(0, overlap);
			Assert.Equal(1, clock.DisposalCount);
		}
		finally
		{
			release.TrySetResult();
			await timer.DisposeAsync();
		}
	}

	[Fact]
	public async Task StopAsyncWaitsForCallbackAndRejectsRestartUntilItReturns()
	{
		var clock = new ManualTimeProvider();
		var entered = NewSignal();
		var release = NewSignal();
		CancellationToken callbackCancellation = default;
		await using var timer = new Timer(_period, async (_, cancellation) =>
		{
			callbackCancellation = cancellation;
			entered.TrySetResult();
			await release.Task.WaitAsync(_timeout, TestContext.Current.CancellationToken);
		}, clock);

		try
		{
			timer.Start();
			await clock.WaitForCreationAsync();
			clock.Advance(_period);
			await WaitAsync(entered.Task);
			var stops = Enumerable.Range(0, 16).Select(_ => timer.StopAsync()).ToArray();

			Assert.All(stops, task => Assert.False(task.IsCompleted));
			Assert.True(timer.IsRunning);
			Assert.True(callbackCancellation.IsCancellationRequested);
			Assert.Equal(0, clock.DisposalCount);
			Assert.Throws<InvalidOperationException>(() => timer.Start());

			release.TrySetResult();
			await WaitAsync(Task.WhenAll(stops));
			Assert.False(timer.IsRunning);
			Assert.Equal(1, clock.DisposalCount);
		}
		finally { release.TrySetResult(); }
	}

	[Fact]
	public async Task CancelingExternalWaitDoesNotEndCallbackOrOtherStopWaiters()
	{
		var clock = new ManualTimeProvider();
		var entered = NewSignal();
		var release = NewSignal();
		await using var timer = new Timer(_period, async (_, _) =>
		{
			entered.TrySetResult();
			await release.Task.WaitAsync(_timeout, TestContext.Current.CancellationToken);
		}, clock);
		using var cancellation = new CancellationTokenSource();

		try
		{
			timer.Start();
			await clock.WaitForCreationAsync();
			clock.Advance(_period);
			await WaitAsync(entered.Task);
			var canceled = timer.StopAsync().WaitAsync(cancellation.Token);
			var remaining = timer.StopAsync();
			cancellation.Cancel();

			var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceled);
			Assert.Equal(cancellation.Token, exception.CancellationToken);
			Assert.False(remaining.IsCompleted);
			Assert.True(timer.IsRunning);
			Assert.Equal(0, clock.DisposalCount);

			release.TrySetResult();
			await WaitAsync(remaining);

			Assert.False(timer.IsRunning);
			Assert.Equal(1, clock.DisposalCount);
		}
		finally { release.TrySetResult(); }
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task CallbackCanRequestStopOrDisposalWithoutWaitingForItself(bool dispose)
	{
		var clock = new ManualTimeProvider();
		Timer timer = null;
		timer = new Timer(_period, (_, cancellation) =>
		{
			if(dispose)
				timer.Dispose();
			else
				timer.Stop();

			Assert.Equal(dispose, timer.IsDisposed);
			Assert.True(cancellation.IsCancellationRequested);
			return ValueTask.CompletedTask;
		}, clock);

		try
		{
			var stopped = ObserveStoppedAsync(timer);
			timer.Start();
			await clock.WaitForCreationAsync();
			clock.Advance(_period);
			await WaitAsync(stopped);

			if(dispose)
				await WaitAsync(timer.DisposeAsync().AsTask());
			else
				await WaitAsync(timer.StopAsync());

			Assert.False(timer.IsRunning);
			Assert.Equal(dispose, timer.IsDisposed);
			Assert.Equal(1, clock.DisposalCount);
		}
		finally { await WaitAsync(timer.DisposeAsync().AsTask()); }
	}

	[Fact]
	public async Task StopCancelsRunningCallbackWithoutReportingFailure()
	{
		var clock = new ManualTimeProvider();
		var entered = NewSignal();
		var exited = 0;
		var failures = 0;
		await using var timer = new Timer(_period, async (_, cancellation) =>
		{
			entered.TrySetResult();

			try { await Task.Delay(Timeout.InfiniteTimeSpan, cancellation); }
			finally { Interlocked.Increment(ref exited); }
		}, clock);
		timer.Failed += (_, _) => Interlocked.Increment(ref failures);
		var stopped = ObserveStoppedAsync(timer);

		timer.Start();
		await clock.WaitForCreationAsync();
		clock.Advance(_period);
		await WaitAsync(entered.Task);
		timer.Stop();
		await WaitAsync(stopped);
		await WaitAsync(timer.StopAsync());

		Assert.Equal(1, exited);
		Assert.Equal(0, failures);
		Assert.False(timer.IsRunning);
		Assert.Equal(1, clock.DisposalCount);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task CancellationCallbackCanRequestStopOrDisposal(bool dispose)
	{
		var clock = new ManualTimeProvider();
		var entered = NewSignal();
		var canceled = NewSignal();
		var notifications = 0;
		Timer timer = null;
		timer = new Timer(_period, async (_, cancellation) =>
		{
			using var registration = cancellation.UnsafeRegister(state =>
			{
				try
				{
					if(dispose)
						timer.Dispose();
					else
						timer.Stop();

					Interlocked.Increment(ref notifications);
				}
				finally { canceled.TrySetResult(); }
			}, null);
			entered.TrySetResult();
			await WaitAsync(canceled.Task);
		}, clock);

		try
		{
			timer.Start();
			await clock.WaitForCreationAsync();
			clock.Advance(_period);
			await WaitAsync(entered.Task);
			await WaitAsync(timer.StopAsync());

			Assert.Equal(1, notifications);
			Assert.Equal(dispose, timer.IsDisposed);
			Assert.False(timer.IsRunning);
			Assert.Equal(1, clock.DisposalCount);
		}
		finally { await WaitAsync(timer.DisposeAsync().AsTask()); }
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task SynchronousStopAndDisposeDoNotWaitForCancellationNotifications(bool dispose)
	{
		var clock = new ManualTimeProvider();
		var entered = NewSignal();
		var notifying = NewSignal();
		//同步继续回调后的运行流程，确认异步停止仍在等待取消通知。
		var releaseCallback = new TaskCompletionSource();
		var notifications = 0;
		using var releaseNotification = new ManualResetEventSlim();
		CancellationTokenRegistration registration = default;
		var timer = new Timer(_period, (_, cancellation) =>
		{
			registration = cancellation.UnsafeRegister(_ =>
			{
				notifying.TrySetResult();
				Assert.True(releaseNotification.Wait(_timeout, TestContext.Current.CancellationToken));
				Interlocked.Increment(ref notifications);
			}, null);
			entered.TrySetResult();
			return new ValueTask(releaseCallback.Task);
		}, clock);

		try
		{
			timer.Start();
			await clock.WaitForCreationAsync();
			clock.Advance(_period);
			await WaitAsync(entered.Task);

			if(dispose)
				timer.Dispose();
			else
				timer.Stop();

			await WaitAsync(notifying.Task);

			Assert.Equal(dispose, timer.IsDisposed);
			Assert.Equal(0, Volatile.Read(ref notifications));

			var stopping = timer.StopAsync();
			var disposing = timer.DisposeAsync().AsTask();
			releaseCallback.TrySetResult();

			Assert.False(stopping.IsCompleted);
			Assert.False(disposing.IsCompleted);
			Assert.True(timer.IsRunning);
			Assert.Equal(0, Volatile.Read(ref notifications));

			releaseNotification.Set();
			await WaitAsync(Task.WhenAll(stopping, disposing));

			Assert.Equal(1, notifications);
			Assert.Equal(1, clock.DisposalCount);
			Assert.False(timer.IsRunning);
		}
		finally
		{
			releaseCallback.TrySetResult();
			releaseNotification.Set();
			await registration.DisposeAsync();
			await timer.DisposeAsync();
		}
	}

	[Fact]
	public async Task CancellationCallbackFailureIsObservedAfterCleanup()
	{
		var clock = new ManualTimeProvider();
		var entered = NewSignal();
		var canceled = NewSignal();
		var failure = new ApplicationException();
		Exception notified = null;
		var timer = new Timer(_period, async (_, cancellation) =>
		{
			using var registration = cancellation.Register(() =>
			{
				canceled.TrySetResult();
				throw failure;
			});
			entered.TrySetResult();
			await WaitAsync(canceled.Task);
		}, clock);
		timer.Failed += (_, args) => notified = args.Exception;

		timer.Start();
		await clock.WaitForCreationAsync();
		clock.Advance(_period);
		await WaitAsync(entered.Task);
		var observed = await Assert.ThrowsAsync<AggregateException>(() => WaitAsync(timer.StopAsync()));

		Assert.Same(failure, Assert.Single(observed.Flatten().InnerExceptions));
		Assert.Same(observed, notified);
		Assert.False(timer.IsRunning);
		Assert.Equal(1, clock.DisposalCount);

		Assert.Same(observed, await Assert.ThrowsAsync<AggregateException>(async () => await timer.DisposeAsync()));
	}

	[Fact]
	public async Task FailureNotifiesAfterClockCleanupAndPreservesOriginalException()
	{
		var clock = new ManualTimeProvider();
		var failure = new ApplicationException();
		var state = new object();
		var timer = new Timer(_period, (_, _) => ValueTask.FromException(failure), clock);
		Diagnostics.FailureEventArgs notification = null;
		object sender = null;
		var disposedAtNotification = 0;
		var runningAtNotification = false;
		timer.Failed += (source, args) =>
		{
			sender = source;
			notification = args;
			disposedAtNotification = clock.DisposalCount;
			runningAtNotification = timer.IsRunning;
		};

		var stopped = ObserveStoppedAsync(timer);
		timer.Start(state);
		await clock.WaitForCreationAsync();
		clock.Advance(_period);
		await WaitAsync(stopped);
		var observed = await Assert.ThrowsAsync<ApplicationException>(() => WaitAsync(timer.StopAsync()));

		Assert.Same(failure, observed);
		Assert.Same(timer, sender);
		Assert.Same(failure, notification.Exception);
		Assert.Same(state, notification.State);
		Assert.Equal(1, disposedAtNotification);
		Assert.True(runningAtNotification);
		Assert.False(timer.IsRunning);

		Assert.Same(failure, await Assert.ThrowsAsync<ApplicationException>(() => timer.StopAsync()));
		Assert.Same(failure, await Assert.ThrowsAsync<ApplicationException>(async () => await timer.DisposeAsync()));
		Assert.Same(failure, await Assert.ThrowsAsync<ApplicationException>(async () => await timer.DisposeAsync()));
		Assert.True(timer.IsDisposed);
		Assert.Equal(1, clock.DisposalCount);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task FailureNotificationChangesDoNotReplaceOrSuppressRunFailure(bool replace)
	{
		var clock = new ManualTimeProvider();
		var original = new ApplicationException();
		var replacement = replace ? new InvalidOperationException() : null;
		var state = new object();
		var notifications = new List<int>();
		var timer = new Timer(_period, (_, _) => ValueTask.FromException(original), clock);
		timer.Failed += (_, args) =>
		{
			Assert.Same(original, args.Exception);
			Assert.Same(state, args.State);
			Assert.False(args.ExceptionHandled);
			args.ExceptionHandled = true;
			args.Exception = replacement;
			notifications.Add(1);
		};
		timer.Failed += (_, args) =>
		{
			Assert.True(args.ExceptionHandled);
			Assert.Same(replacement, args.Exception);
			Assert.Same(state, args.State);
			notifications.Add(2);
		};

		var stopped = ObserveStoppedAsync(timer);
		timer.Start(state);
		await clock.WaitForCreationAsync();
		clock.Advance(_period);
		await WaitAsync(stopped);
		var observed = await Assert.ThrowsAsync<ApplicationException>(() => WaitAsync(timer.StopAsync()));

		Assert.Same(original, observed);
		Assert.Equal(new[] { 1, 2 }, notifications);
		Assert.False(timer.IsRunning);
		Assert.Equal(1, clock.DisposalCount);

		Assert.Same(original, await Assert.ThrowsAsync<ApplicationException>(() => timer.StopAsync()));
		Assert.Same(original, await Assert.ThrowsAsync<ApplicationException>(async () => await timer.DisposeAsync()));
		Assert.True(timer.IsDisposed);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task UnrelatedCancellationFaultsEvenWhenStopWasRequested(bool stop)
	{
		var clock = new ManualTimeProvider();
		using var unrelated = new CancellationTokenSource();
		unrelated.Cancel();
		var failure = new OperationCanceledException(unrelated.Token);
		var notifications = 0;
		Timer timer = null;
		timer = new Timer(_period, (_, _) =>
		{
			if(stop)
				timer.Stop();

			return ValueTask.FromException(failure);
		}, clock);
		timer.Failed += (_, args) =>
		{
			Assert.Same(failure, args.Exception);
			Interlocked.Increment(ref notifications);
		};

		var stopped = ObserveStoppedAsync(timer);
		timer.Start();
		await clock.WaitForCreationAsync();
		clock.Advance(_period);
		await WaitAsync(stopped);
		var completion = timer.StopAsync();
		var observed = await Assert.ThrowsAsync<OperationCanceledException>(() => WaitAsync(completion));

		Assert.Same(failure, observed);
		Assert.False(timer.IsRunning);
		Assert.True(completion.IsFaulted);
		Assert.False(completion.IsCanceled);
		Assert.Equal(1, notifications);
		Assert.Equal(1, clock.DisposalCount);

		Assert.Same(failure, await Assert.ThrowsAsync<OperationCanceledException>(async () => await timer.DisposeAsync()));
	}

	[Fact]
	public async Task FailureHandlersAllRunAndTheirErrorsFollowOriginalFailure()
	{
		var clock = new ManualTimeProvider();
		var original = new ApplicationException();
		var first = new InvalidOperationException();
		var last = new ArgumentException();
		var notifications = new List<int>();
		var timer = new Timer(_period, (_, _) => ValueTask.FromException(original), clock);
		timer.Failed += (_, _) => { notifications.Add(1); throw first; };
		timer.Failed += (_, _) => notifications.Add(2);
		timer.Failed += (_, _) => { notifications.Add(3); throw last; };

		var stopped = ObserveStoppedAsync(timer);
		timer.Start();
		await clock.WaitForCreationAsync();
		clock.Advance(_period);
		await WaitAsync(stopped);
		var failure = await Assert.ThrowsAsync<AggregateException>(() => WaitAsync(timer.StopAsync()));

		Assert.Equal([1, 2, 3], notifications);
		Assert.Collection(failure.InnerExceptions,
			exception => Assert.Same(original, exception),
			exception => Assert.Same(first, exception),
			exception => Assert.Same(last, exception));
		Assert.False(timer.IsRunning);
		Assert.Equal(1, clock.DisposalCount);

		Assert.Same(failure, await Assert.ThrowsAsync<AggregateException>(async () => await timer.DisposeAsync()));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task FailureHandlerCanRequestStopOrDisposal(bool dispose)
	{
		var clock = new ManualTimeProvider();
		var failure = new ApplicationException();
		var timer = new Timer(_period, (_, _) => ValueTask.FromException(failure), clock);
		var notifications = 0;
		timer.Failed += (_, _) =>
		{
			if(dispose)
				timer.Dispose();
			else
				timer.Stop();

			Interlocked.Increment(ref notifications);
		};

		var stopped = ObserveStoppedAsync(timer);
		timer.Start();
		await clock.WaitForCreationAsync();
		clock.Advance(_period);
		await WaitAsync(stopped);
		Assert.Same(failure, await Assert.ThrowsAsync<ApplicationException>(() => WaitAsync(timer.StopAsync())));
		Assert.Equal(1, notifications);

		Assert.Equal(dispose, timer.IsDisposed);
		Assert.False(timer.IsRunning);

		Assert.Same(failure, await Assert.ThrowsAsync<ApplicationException>(async () => await timer.DisposeAsync()));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task DisposeAsyncWaitsForCallbackAndSharesSynchronousDisposal(bool synchronous)
	{
		var clock = new ManualTimeProvider();
		var entered = NewSignal();
		var release = NewSignal();
		var calls = 0;
		var notifications = 0;
		CancellationToken callbackCancellation = default;
		var timer = new Timer(_period, async (_, cancellation) =>
		{
			callbackCancellation = cancellation;
			Interlocked.Increment(ref calls);
			entered.TrySetResult();
			await release.Task.WaitAsync(_timeout, TestContext.Current.CancellationToken);
		}, clock);
		timer.Stopped += (_, _) => Interlocked.Increment(ref notifications);

		try
		{
			timer.Start();
			await clock.WaitForCreationAsync();
			clock.Advance(_period);
			await WaitAsync(entered.Task);

			if(synchronous)
			{
				timer.Dispose();
				timer.Dispose();
			}

			var first = timer.DisposeAsync().AsTask();
			var others = Enumerable.Range(0, 16)
				.Select(_ => Task.Run(async () => await timer.DisposeAsync(), TestContext.Current.CancellationToken))
				.ToArray();

			Assert.True(timer.IsDisposed);
			Assert.True(timer.IsRunning);
			Assert.True(callbackCancellation.IsCancellationRequested);
			Assert.False(first.IsCompleted);
			Assert.Equal(0, notifications);
			Assert.Equal(0, clock.DisposalCount);
			Assert.Throws<ObjectDisposedException>(() => timer.Start());
			Assert.Throws<ObjectDisposedException>(() => timer.Period);
			Assert.Throws<ObjectDisposedException>(() => timer.Period = _period);

			clock.Advance(TimeSpan.FromSeconds(8));
			release.TrySetResult();
			await WaitAsync(Task.WhenAll(others.Append(first)));
			timer.Dispose();
			await timer.DisposeAsync();
			clock.Advance(TimeSpan.FromSeconds(8));

			Assert.False(timer.IsRunning);
			Assert.Equal(1, calls);
			Assert.Equal(1, notifications);
			Assert.Equal(1, clock.DisposalCount);
		}
		finally
		{
			release.TrySetResult();
			await timer.DisposeAsync();
		}
	}

	[Theory]
	[InlineData("stop")]
	[InlineData("stopAsync")]
	[InlineData("failure")]
	[InlineData("dispose")]
	[InlineData("disposeAsync")]
	public async Task StoppedNotifiesOnceAfterCallbackCleanup(string ending)
	{
		var clock = new ManualTimeProvider();
		var entered = NewSignal();
		var release = NewSignal();
		var failure = new ApplicationException();
		var order = new List<string>();
		var timer = new Timer(_period, async (_, _) =>
		{
			entered.TrySetResult();
			await WaitAsync(release.Task);
			order.Add("callback");

			if(ending == "failure")
				throw failure;
		}, clock);
		timer.Failed += (_, args) =>
		{
			Assert.Same(failure, args.Exception);
			Assert.Equal(1, clock.DisposalCount);
			order.Add("failed");
		};
		timer.Stopped += (sender, args) =>
		{
			Assert.Same(timer, sender);
			Assert.Same(EventArgs.Empty, args);
			Assert.True(timer.IsRunning);
			Assert.Equal(1, clock.DisposalCount);
			order.Add("stopped");
		};
		var stopped = ObserveStoppedAsync(timer);
		Task disposing = null;

		try
		{
			timer.Start();
			await clock.WaitForCreationAsync();
			clock.Advance(_period);
			await WaitAsync(entered.Task);

			switch(ending)
			{
				case "stop":
					timer.Stop();
					timer.Stop();
					break;
				case "stopAsync":
					disposing = timer.StopAsync();
					break;
				case "dispose":
					timer.Dispose();
					break;
				case "disposeAsync":
					disposing = timer.DisposeAsync().AsTask();
					break;
			}

			Assert.False(stopped.IsCompleted);
			Assert.Equal(0, clock.DisposalCount);
			release.TrySetResult();
			await WaitAsync(stopped);

			if(ending == "failure")
			{
				Assert.Same(failure, await Assert.ThrowsAsync<ApplicationException>(() => WaitAsync(timer.StopAsync())));
				Assert.Same(failure, await Assert.ThrowsAsync<ApplicationException>(async () => await timer.DisposeAsync()));
			}
			else
			{
				await WaitAsync(disposing ?? timer.StopAsync());
				await timer.StopAsync();
				await timer.DisposeAsync();
			}

			timer.Stop();
			Assert.Equal(ending == "failure" ? ["callback", "failed", "stopped"] : new[] { "callback", "stopped" }, order);
			Assert.False(timer.IsRunning);
			Assert.True(timer.IsDisposed);
			Assert.Equal(1, clock.DisposalCount);
		}
		finally
		{
			release.TrySetResult();
			await timer.DisposeAsync().AsTask().ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing | ConfigureAwaitOptions.ContinueOnCapturedContext);
		}
	}

	[Fact]
	public async Task DisposeAsyncAfterDisposeWaitsForStoppedNotification()
	{
		var clock = new ManualTimeProvider();
		var entered = NewSignal();
		var notifications = 0;
		using var release = new ManualResetEventSlim();
		await using var timer = new Timer(_period, (_, _) => ValueTask.CompletedTask, clock);
		timer.Stopped += (_, _) =>
		{
			entered.TrySetResult();
			Assert.True(release.Wait(_timeout, TestContext.Current.CancellationToken));
			Interlocked.Increment(ref notifications);
		};

		try
		{
			timer.Start();
			await clock.WaitForCreationAsync();
			var stopping = timer.StopAsync();
			await WaitAsync(entered.Task);
			timer.Dispose();
			timer.Dispose();
			var disposing = timer.DisposeAsync().AsTask();

			Assert.False(stopping.IsCompleted);
			Assert.False(disposing.IsCompleted);
			Assert.True(timer.IsRunning);
			Assert.True(timer.IsDisposed);
			Assert.Equal(1, clock.DisposalCount);

			release.Set();
			await WaitAsync(Task.WhenAll(stopping, disposing));

			Assert.Equal(1, notifications);
			Assert.False(timer.IsRunning);
		}
		finally { release.Set(); }
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task StoppedHandlerFailureSkipsLaterSubscribersAndCompletesRun(bool callbackFails)
	{
		var clock = new ManualTimeProvider();
		var original = new ApplicationException();
		var failureNotification = new InvalidOperationException();
		var first = new ArgumentException();
		var stopped = NewSignal();
		var order = new List<string>();
		Timer timer = null;
		timer = new Timer(_period, (_, _) =>
		{
			if(callbackFails)
				return ValueTask.FromException(original);

			timer.Stop();
			return ValueTask.CompletedTask;
		}, clock);
		timer.Failed += (_, _) => { order.Add("failed"); throw failureNotification; };
		timer.Stopped += (_, _) =>
		{
			order.Add("first");
			stopped.TrySetResult();
			throw first;
		};
		timer.Stopped += (_, _) => order.Add("middle");
		timer.Stopped += (_, _) => order.Add("last");

		timer.Start();
		await clock.WaitForCreationAsync();
		clock.Advance(_period);
		await WaitAsync(stopped.Task);
		var observed = await Record.ExceptionAsync(() => WaitAsync(timer.StopAsync()));

		if(callbackFails)
		{
			var aggregate = Assert.IsType<AggregateException>(observed);
			Assert.Equal(new Exception[] { original, failureNotification, first }, aggregate.InnerExceptions);
		}
		else
			Assert.Same(first, observed);

		Assert.Equal(callbackFails ? ["failed", "first"] : new[] { "first" }, order);
		Assert.False(timer.IsRunning);
		Assert.Equal(1, clock.DisposalCount);
		Assert.Same(observed, await Record.ExceptionAsync(() => WaitAsync(timer.DisposeAsync().AsTask())));
		Assert.True(timer.IsDisposed);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task StoppedHandlerCanRequestStopOrDisposal(bool dispose)
	{
		var clock = new ManualTimeProvider();
		var notifications = 0;
		await using var timer = new Timer(_period, (_, _) => ValueTask.CompletedTask, clock);
		timer.Stopped += (_, _) =>
		{
			if(dispose)
				timer.Dispose();
			else
				timer.Stop();

			Assert.Equal(dispose, timer.IsDisposed);
			Interlocked.Increment(ref notifications);
		};

		timer.Start();
		await clock.WaitForCreationAsync();
		await WaitAsync(timer.StopAsync());

		Assert.Equal(1, notifications);
		Assert.False(timer.IsRunning);
		Assert.Equal(dispose, timer.IsDisposed);
	}

	[Fact]
	public async Task BusinessTaskCanContinueAfterStoppedNotification()
	{
		var clock = new ManualTimeProvider();
		var stopped = NewSignal();
		var notifying = NewSignal();
		var continuing = NewSignal();
		var order = new List<string>();
		using var release = new ManualResetEventSlim();
		Timer timer = null;
		timer = new Timer(_period, (_, _) =>
		{
			timer.Stop();
			order.Add("callback");
			return ValueTask.CompletedTask;
		}, clock);
		timer.Stopped += (_, _) => stopped.TrySetResult();
		timer.Stopped += (_, _) =>
		{
			notifying.TrySetResult();
			Assert.True(release.Wait(_timeout, TestContext.Current.CancellationToken));
			order.Add("notification");
		};

		//业务任务在启动计时器之前建立自己的调用链，事件只负责发送信号。
		var execution = ContinueAsync();
		Task ticking = null;

		try
		{
			timer.Start();
			await clock.WaitForCreationAsync();
			ticking = Task.Run(() => clock.Advance(_period), TestContext.Current.CancellationToken);
			await WaitAsync(notifying.Task);
			await WaitAsync(continuing.Task);

			Assert.False(execution.IsCompleted);
			Assert.True(timer.IsRunning);
			release.Set();
			await WaitAsync(execution);

			Assert.Equal(["callback", "notification", "business"], order);
			Assert.False(timer.IsRunning);
			Assert.True(timer.IsDisposed);
			Assert.Equal(1, clock.DisposalCount);
		}
		finally
		{
			release.Set();

			try
			{
				if(ticking != null)
					await ticking.WaitAsync(_timeout);
			}
			finally { await timer.DisposeAsync(); }
		}

		async Task ContinueAsync()
		{
			await WaitAsync(stopped.Task);
			var stopping = timer.StopAsync();
			continuing.TrySetResult();
			await WaitAsync(stopping);
			order.Add("business");
			await timer.DisposeAsync();
		}
	}

	[Fact]
	public async Task NotificationOverridesPreserveArgumentsAndEventOrder()
	{
		var clock = new ManualTimeProvider();
		var failure = new ApplicationException();
		var state = new object();
		var order = new List<string>();
		Diagnostics.FailureEventArgs failureArgs = null;
		EventArgs stoppedArgs = null;
		NotificationTimer timer = null;
		timer = new NotificationTimer(_period, (_, _) =>
		{
			order.Add("callback");
			return ValueTask.FromException(failure);
		}, clock, args =>
		{
			Assert.Same(failure, args.Exception);
			Assert.Same(state, args.State);
			Assert.True(timer.IsRunning);
			Assert.Equal(1, clock.DisposalCount);
			failureArgs = args;
			order.Add("failed-override");
		}, args =>
		{
			Assert.Same(EventArgs.Empty, args);
			Assert.True(timer.IsRunning);
			Assert.Equal(1, clock.DisposalCount);
			stoppedArgs = args;
			order.Add("stopped-override");
		});
		timer.Failed += (sender, args) =>
		{
			Assert.Same(timer, sender);
			Assert.Same(failureArgs, args);
			order.Add("failed-event");
		};
		timer.Stopped += (sender, args) =>
		{
			Assert.Same(timer, sender);
			Assert.Same(stoppedArgs, args);
			order.Add("stopped-event");
		};
		var stopped = ObserveStoppedAsync(timer);

		timer.Start(state);
		await clock.WaitForCreationAsync();
		clock.Advance(_period);
		await WaitAsync(stopped);
		Assert.Same(failure, await Assert.ThrowsAsync<ApplicationException>(() => WaitAsync(timer.StopAsync())));

		Assert.Equal(["callback", "failed-override", "failed-event", "stopped-override", "stopped-event"], order);
		Assert.False(timer.IsRunning);
		Assert.False(timer.IsDisposed);
		Assert.Equal(1, clock.DisposalCount);
		Assert.Same(failure, await Assert.ThrowsAsync<ApplicationException>(() => WaitAsync(timer.DisposeAsync().AsTask())));
		Assert.True(timer.IsDisposed);
	}

	[Theory]
	[InlineData(true, false)]
	[InlineData(false, true)]
	[InlineData(true, true)]
	public async Task NotificationOverrideFailuresDoNotPreventRunCleanup(bool failedThrows, bool stoppedThrows)
	{
		var clock = new ManualTimeProvider();
		var original = new ApplicationException();
		var failedError = new InvalidOperationException();
		var stoppedError = new NotSupportedException();
		var notified = NewSignal();
		var failedEvents = 0;
		var stoppedEvents = 0;
		var order = new List<string>();
		var timer = new NotificationTimer(_period, (_, _) => ValueTask.FromException(original), clock, args =>
		{
			Assert.Same(original, args.Exception);
			order.Add("failed");

			if(failedThrows)
				throw failedError;
		}, _ =>
		{
			order.Add("stopped");
			notified.TrySetResult();

			if(stoppedThrows)
				throw stoppedError;
		});
		timer.Failed += (_, _) => Interlocked.Increment(ref failedEvents);
		timer.Stopped += (_, _) => Interlocked.Increment(ref stoppedEvents);

		timer.Start();
		await clock.WaitForCreationAsync();
		clock.Advance(_period);
		await WaitAsync(notified.Task);
		var observed = await Assert.ThrowsAsync<AggregateException>(() => WaitAsync(timer.StopAsync()));
		List<Exception> expected = [original];

		if(failedThrows)
			expected.Add(failedError);
		if(stoppedThrows)
			expected.Add(stoppedError);

		Assert.Equal(expected, observed.Flatten().InnerExceptions);
		Assert.Equal(["failed", "stopped"], order);
		Assert.Equal(failedThrows ? 0 : 1, failedEvents);
		Assert.Equal(stoppedThrows ? 0 : 1, stoppedEvents);
		Assert.False(timer.IsRunning);
		Assert.False(timer.IsDisposed);
		Assert.Equal(1, clock.DisposalCount);
		Assert.Same(observed, await Assert.ThrowsAsync<AggregateException>(() => WaitAsync(timer.DisposeAsync().AsTask())));
		Assert.True(timer.IsDisposed);
	}

	private static Task ObserveStoppedAsync(Timer timer)
	{
		var stopped = NewSignal();
		timer.Stopped += (_, _) => stopped.TrySetResult();
		return stopped.Task;
	}

	private static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
	private static Task WaitAsync(Task task) => task.WaitAsync(_timeout, TestContext.Current.CancellationToken);

	private sealed class NotificationTimer(
		TimeSpan period,
		Func<object, CancellationToken, ValueTask> tick,
		TimeProvider clock,
		Action<Diagnostics.FailureEventArgs> failed,
		Action<EventArgs> stopped) : Timer(period, tick, clock)
	{
		protected override void OnFailed(Diagnostics.FailureEventArgs args)
		{
			failed(args);
			base.OnFailed(args);
		}

		protected override void OnStopped(EventArgs args)
		{
			stopped(args);
			base.OnStopped(args);
		}
	}

	private sealed class ManualTimeProvider : TimeProvider
	{
		private readonly object _sync = new();
		private readonly List<ManualTimer> _timers = [];
		private readonly Channel<ManualTimer> _created = Channel.CreateUnbounded<ManualTimer>();
		private long _ticks;
		private int _creations;
		private int _disposals;

		public int CreationCount => Volatile.Read(ref _creations);
		public int DisposalCount => Volatile.Read(ref _disposals);
		public Exception ChangeFailure { get; set; }
		public bool RejectChanges { get; set; }

		public override ITimer CreateTimer(TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period)
		{
			ManualTimer timer;

			lock(_sync)
			{
				timer = new ManualTimer(this, callback, state);
				timer.Change(dueTime, period);
				_timers.Add(timer);
			}

			Interlocked.Increment(ref _creations);
			_created.Writer.TryWrite(timer);
			return timer;
		}

		public Task WaitForCreationAsync() => _created.Reader.ReadAsync(TestContext.Current.CancellationToken).AsTask().WaitAsync(_timeout);

		public void Advance(TimeSpan elapsed)
		{
			List<ManualTimer> pending = [];

			lock(_sync)
			{
				_ticks += elapsed.Ticks;

				foreach(var timer in _timers)
				{
					while(!timer.Disposed && timer.DueTicks <= _ticks)
					{
						pending.Add(timer);
						timer.DueTicks = timer.PeriodTicks > 0 ? timer.DueTicks + timer.PeriodTicks : long.MaxValue;
					}
				}
			}

			foreach(var timer in pending)
				timer.Fire();
		}

		private sealed class ManualTimer(ManualTimeProvider owner, TimerCallback callback, object state) : ITimer
		{
			public bool Disposed { get; private set; }
			public long DueTicks { get; set; }
			public long PeriodTicks { get; private set; }

			public bool Change(TimeSpan dueTime, TimeSpan period)
			{
				lock(owner._sync)
				{
					if(this.Disposed)
						return false;

					if(owner.ChangeFailure != null)
						throw owner.ChangeFailure;

					if(owner.RejectChanges)
						return false;

					this.DueTicks = dueTime == Timeout.InfiniteTimeSpan ? long.MaxValue : owner._ticks + dueTime.Ticks;
					this.PeriodTicks = period.Ticks;

					return true;
				}
			}

			public void Fire() => callback(state);

			public void Dispose()
			{
				lock(owner._sync)
				{
					if(this.Disposed)
						return;

					this.Disposed = true;
					Interlocked.Increment(ref owner._disposals);
				}
			}

			public ValueTask DisposeAsync()
			{
				this.Dispose();
				return ValueTask.CompletedTask;
			}
		}
	}
}
