using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

using Xunit;

using Zongsoft.Tests;

namespace Zongsoft.Diagnostics.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class LoggingCollection
{
	public const string Name = nameof(LoggingCollection);
}

[Collection(LoggingCollection.Name)]
public class LoggingTest
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Dispatch_PendingSink_ReturnsBeforeCallerContextContinuation(bool flush)
	{
		using var context = new PumpSynchronizationContext();
		var ambient = new AsyncLocal<string>();
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var returned = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var continued = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
		var callerThread = 0;
		var observedThread = 0;
		string observedContext = null;

		var logger = new TestLogger(async (_, _) =>
		{
			observedThread = Environment.CurrentManagedThreadId;
			observedContext = ambient.Value;
			await release.Task;
			continued.SetResult(Environment.CurrentManagedThreadId);
		});

		using var loggers = new LoggerScope(logger);
		var entry = new LogEntry(LogLevel.Info, nameof(LoggingTest), "synchronous-context-message");
		var invocation = Task.Factory.StartNew(() =>
		{
			var previous = SynchronizationContext.Current;
			SynchronizationContext.SetSynchronizationContext(context);

			try
			{
				callerThread = Environment.CurrentManagedThreadId;
				ambient.Value = "synchronous-caller";
				Dispatch(flush, entry);
				returned.SetResult();

				if(!continued.Task.IsCompleted)
					context.RunOne();
			}
			finally { SynchronizationContext.SetSynchronizationContext(previous); }
		}, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

		try
		{
			await returned.Task.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.Equal(1, logger.Calls);
			Assert.Equal(callerThread, observedThread);
			Assert.Equal("synchronous-caller", observedContext);
			Assert.False(continued.Task.IsCompleted);
			Assert.Same(flush ? null : entry, logger.Entry);

			release.SetResult();
			await invocation.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.Equal(callerThread, await continued.Task.WaitAsync(TimeSpan.FromSeconds(5)));
		}
		finally
		{
			release.TrySetResult();

			//旧式同步等待会占住调用线程；清理时执行其续体，避免失败测试遗留阻塞线程。
			if(!returned.Task.IsCompleted && !invocation.IsCompleted)
				context.RunOne();

			await invocation.WaitAsync(TimeSpan.FromSeconds(5));
		}
	}

	[Fact]
	public async Task FlushAsync_PendingEarlierWrite_DoesNotWaitForThatWrite()
	{
		var write = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var logger = new TestLogger((log, _) => log == null ? ValueTask.CompletedTask : new(write.Task));
		using var loggers = new LoggerScope(logger);
		var entry = new LogEntry(LogLevel.Info, nameof(LoggingTest), "independent-flush-message");
		var invocation = Task.Run(() => Logging.Log(entry));

		try
		{
			await invocation.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.Equal(1, logger.Calls);
			Assert.Same(entry, logger.Entry);
			await Logging.FlushAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
			Assert.Equal(2, logger.Calls);
			Assert.False(write.Task.IsCompleted);
		}
		finally
		{
			write.TrySetResult();
			await invocation.WaitAsync(TimeSpan.FromSeconds(5));
		}
	}

	[Theory]
	[InlineData(false, 0)]
	[InlineData(false, 1)]
	[InlineData(false, 2)]
	[InlineData(true, 0)]
	[InlineData(true, 1)]
	[InlineData(true, 2)]
	public async Task Dispatch_SinkFailure_DoesNotThrowOrSkipOtherSinks(bool flush, int failureMode)
	{
		var failure = new InvalidOperationException();
		var write = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var failed = new TestLogger((_, _) => failureMode switch
		{
			0 => throw failure,
			1 => ValueTask.FromException(failure),
			_ => new(write.Task),
		});

		var completed = new TestLogger((_, _) => ValueTask.CompletedTask);
		using var loggers = new LoggerScope(failed, completed);
		var entry = new LogEntry(LogLevel.Info, nameof(LoggingTest), "void-failure-message");
		var invocation = Task.Run(() => Dispatch(flush, entry));

		try
		{
			await invocation.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.Equal(1, failed.Calls);
			Assert.Equal(1, completed.Calls);

			if(failureMode == 2)
				write.SetException(failure);

			var observed = await Assert.ThrowsAsync<InvalidOperationException>(() =>
				DispatchAsync(flush, entry, CancellationToken.None).AsTask().WaitAsync(TimeSpan.FromSeconds(5)));
			Assert.Same(failure, observed);
			Assert.Equal(2, failed.Calls);
			Assert.Equal(2, completed.Calls);
		}
		finally
		{
			write.TrySetResult();
			await Record.ExceptionAsync(() => invocation.WaitAsync(TimeSpan.FromSeconds(5)));
		}
	}

	[Fact]
	public async Task LogAsync_PendingSink_InvokesEverySinkInlineWithCallerContext()
	{
		var context = new AsyncLocal<string> { Value = "caller-context" };
		var thread = Environment.CurrentManagedThreadId;
		var observedContexts = new string[2];
		var observedThreads = new int[2];
		var write = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

		var pending = new TestLogger((_, _) =>
		{
			observedContexts[0] = context.Value;
			observedThreads[0] = Environment.CurrentManagedThreadId;
			return new(write.Task);
		});
		var completed = new TestLogger((_, _) =>
		{
			observedContexts[1] = context.Value;
			observedThreads[1] = Environment.CurrentManagedThreadId;
			return ValueTask.CompletedTask;
		});

		using var loggers = new LoggerScope(pending, completed);
		var entry = new LogEntry(LogLevel.Info, nameof(LoggingTest), "context-message");
		Task dispatch = null;

		try
		{
			dispatch = Logging.LogAsync(entry).AsTask();

			Assert.Equal(1, pending.Calls);
			Assert.Equal(1, completed.Calls);
			Assert.Same(entry, pending.Entry);
			Assert.Same(entry, completed.Entry);
			Assert.Equal(new[] { "caller-context", "caller-context" }, observedContexts);
			Assert.Equal(new[] { thread, thread }, observedThreads);
			Assert.False(dispatch.IsCompleted);

			write.SetResult();
			await dispatch.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.Equal(1, pending.Calls);
			Assert.Equal(1, completed.Calls);
		}
		finally
		{
			write.TrySetResult();

			if(dispatch != null)
				await dispatch.WaitAsync(TimeSpan.FromSeconds(5));
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task DispatchAsync_SynchronousSinkFailure_StillAwaitsEveryOtherSink(bool flush)
	{
		var failure = new InvalidOperationException();
		var source = new AsyncOperationSource();
		var released = false;
		var failed = new TestLogger((_, _) => throw failure);
		var pending = new TestLogger((_, _) => source.Operation);
		var completed = new TestLogger((_, _) => ValueTask.CompletedTask);
		using var loggers = new LoggerScope(failed, pending, completed);
		using var cancellation = new CancellationTokenSource();

		var entry = new LogEntry(LogLevel.Info, nameof(LoggingTest), "failure-message");
		Task dispatch = null;

		try
		{
			dispatch = DispatchAsync(flush, entry, cancellation.Token).AsTask();

			Assert.Equal(1, failed.Calls);
			Assert.Equal(1, pending.Calls);
			Assert.Equal(1, completed.Calls);
			Assert.Equal(cancellation.Token, pending.Cancellation);
			Assert.False(dispatch.IsCompleted);
			Assert.Equal(0, source.ConsumptionCount);

			if(!flush)
				Assert.Same(entry, pending.Entry);

			source.Complete();
			released = true;
			var observed = await Assert.ThrowsAsync<InvalidOperationException>(() => dispatch.WaitAsync(TimeSpan.FromSeconds(5)));

			Assert.Same(failure, observed);
			Assert.Equal(1, source.ConsumptionCount);
			Assert.Equal(1, completed.Calls);
		}
		finally
		{
			if(!released)
				source.Complete();

			if(dispatch != null)
				await Record.ExceptionAsync(() => dispatch.WaitAsync(TimeSpan.FromSeconds(5)));
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task DispatchAsync_CallerCancellation_IsForwardedAndObserved(bool flush)
	{
		using var cancellation = new CancellationTokenSource();
		var pending = new TestLogger((_, token) => new(Task.Delay(Timeout.Infinite, token)));
		var completed = new TestLogger((_, _) => ValueTask.CompletedTask);
		using var loggers = new LoggerScope(pending, completed);
		var entry = new LogEntry(LogLevel.Info, nameof(LoggingTest), "cancellation-message");
		Task dispatch = null;

		try
		{
			dispatch = DispatchAsync(flush, entry, cancellation.Token).AsTask();

			Assert.False(dispatch.IsCompleted);
			Assert.Equal(1, pending.Calls);
			Assert.Equal(1, completed.Calls);
			Assert.Equal(cancellation.Token, pending.Cancellation);
			Assert.Equal(cancellation.Token, completed.Cancellation);

			cancellation.Cancel();
			var observed = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => dispatch.WaitAsync(TimeSpan.FromSeconds(5)));

			Assert.Equal(cancellation.Token, observed.CancellationToken);
			Assert.True(dispatch.IsCanceled);
			Assert.Equal(1, completed.Calls);
		}
		finally
		{
			cancellation.Cancel();

			if(dispatch != null)
				await Record.ExceptionAsync(() => dispatch.WaitAsync(TimeSpan.FromSeconds(5)));
		}
	}

	[Theory]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public async Task DispatchAsync_ValueTask_ConsumesOperationOnce(bool flush, bool completedSynchronously)
	{
		var source = new AsyncOperationSource();
		var released = completedSynchronously;
		var logger = new TestLogger((_, _) => source.Operation);
		using var loggers = new LoggerScope(logger);
		var entry = new LogEntry(LogLevel.Info, nameof(LoggingTest), "single-consumption-message");
		Task dispatch = null;

		if(completedSynchronously)
			source.Complete();

		try
		{
			dispatch = DispatchAsync(flush, entry, CancellationToken.None).AsTask();

			Assert.Equal(1, logger.Calls);
			Assert.Equal(completedSynchronously ? 1 : 0, source.ConsumptionCount);

			if(!completedSynchronously)
			{
				Assert.False(dispatch.IsCompleted);
				source.Complete();
				released = true;
			}

			await dispatch.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.Equal(1, source.ConsumptionCount);
			Assert.Equal(1, logger.Calls);
		}
		finally
		{
			if(!released)
				source.Complete();

			if(dispatch != null)
				await dispatch.WaitAsync(TimeSpan.FromSeconds(5));
		}
	}

	private static void Dispatch(bool flush, LogEntry entry)
	{
		if(flush)
			Logging.Flush();
		else
			Logging.Log(entry);
	}

	private static ValueTask DispatchAsync(bool flush, LogEntry entry, CancellationToken cancellation) =>
		flush ? Logging.FlushAsync(cancellation) : Logging.LogAsync(entry, cancellation);

	private sealed class PumpSynchronizationContext : SynchronizationContext, IDisposable
	{
		private readonly BlockingCollection<(SendOrPostCallback Callback, object State)> _callbacks = new();
		public override void Post(SendOrPostCallback callback, object state) => _callbacks.Add((callback, state));

		public void RunOne()
		{
			if(!_callbacks.TryTake(out var callback, TimeSpan.FromSeconds(5)))
				throw new TimeoutException();

			callback.Callback(callback.State);
		}

		public void Dispose() => _callbacks.Dispose();
	}

	private sealed class LoggerScope : IDisposable
	{
		private readonly ILogger[] _previous = Logging.Loggers.ToArray();

		public LoggerScope(params ILogger[] loggers)
		{
			Logging.Loggers.Clear();

			foreach(var logger in loggers)
				Logging.Loggers.Add(logger);
		}

		public void Dispose()
		{
			Logging.Loggers.Clear();

			foreach(var logger in _previous)
				Logging.Loggers.Add(logger);
		}
	}

	private sealed class TestLogger(Func<ILog, CancellationToken, ValueTask> operation) : ILogger
	{
		public string Name => nameof(TestLogger);
		public int Calls { get; private set; }
		public ILog Entry { get; private set; }
		public CancellationToken Cancellation { get; private set; }
		public ValueTask FlushAsync(CancellationToken cancellation = default) => this.Invoke(null, cancellation);
		public ValueTask LogAsync<TLog>(TLog log, CancellationToken cancellation = default) where TLog : ILog => this.Invoke(log, cancellation);

		private ValueTask Invoke(ILog log, CancellationToken cancellation)
		{
			this.Calls++;
			this.Entry = log;
			this.Cancellation = cancellation;
			return operation(log, cancellation);
		}
	}
}
