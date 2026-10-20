using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Xunit;

using Zongsoft.Services;
using Zongsoft.Tests;

namespace Zongsoft.Diagnostics.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class LoggingAsyncCollection
{
	public const string Name = nameof(LoggingAsyncCollection);
}

[Collection(LoggingAsyncCollection.Name)]
public class LoggingAsyncTest
{
	[Theory]
	[InlineData(false, false)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public async Task ApplicationDisposeAsync_CleanupLog_WaitsForWriteAndFlushAndPreservesFailures(bool failCleanup, bool failFlush)
	{
		var cleanupFailure = new InvalidOperationException();
		var flushFailure = new ArgumentException();
		var write = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var flush = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var flushing = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var entry = new LogEntry(LogLevel.Info, nameof(LoggingAsyncTest), "cleanup-message");
		var flushCount = 0;
		var logger = new TestLogger((log, _) =>
		{
			if(log != null)
				return new(write.Task);

			if(++flushCount != 1)
				return ValueTask.CompletedTask;

			flushing.TrySetResult();
			return new(flush.Task);
		});
		using var loggers = new LoggerScope(logger);
		var services = new ServiceProviderFactory().CreateServiceProvider(new ServiceCollection());
		var previous = ApplicationContext.Current;
		var application = new TestApplicationContext(services);
		var initializer = new TestInitializer(() =>
		{
			Logging.Log(entry);
			return failCleanup ? ValueTask.FromException(cleanupFailure) : ValueTask.CompletedTask;
		});
		application.Initializers.Add(initializer);

		try
		{
			var pending = application.DisposeAsync().AsTask();
			await logger.Started.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.Same(entry, logger.Entry);
			Assert.Equal(1, initializer.DisposalCount);
			Assert.False(pending.IsCompleted);
			Assert.False(flushing.Task.IsCompleted);

			write.SetResult();
			await flushing.Task.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.False(pending.IsCompleted);
			Assert.Equal(2, logger.Calls);

			if(failFlush)
				flush.SetException(flushFailure);
			else
				flush.SetResult();

			if(failFlush)
			{
				var failure = await Assert.ThrowsAsync<AggregateException>(() => pending.WaitAsync(TimeSpan.FromSeconds(5)));
				Assert.Collection(failure.InnerExceptions,
					error => Assert.Same(cleanupFailure, error),
					error => Assert.Same(flushFailure, error));
			}
			else if(failCleanup)
			{
				var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => pending.WaitAsync(TimeSpan.FromSeconds(5)));
				Assert.Same(cleanupFailure, failure);
			}
			else
				await pending.WaitAsync(TimeSpan.FromSeconds(5));

			Assert.Equal(1, initializer.DisposalCount);
			Assert.Equal(1, flushCount);
		}
		finally
		{
			write.TrySetResult();
			flush.TrySetResult();

			try
			{
				await Record.ExceptionAsync(() => application.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5)));
				await Logging.FlushAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
			}
			finally
			{
				typeof(ApplicationContext).GetField("_current", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, previous);
				await ((IAsyncDisposable)services).DisposeAsync();
			}
		}
	}

	[Fact]
	public async Task Log_PendingSink_ReturnsBeforeWriteAndFlushComplete()
	{
		var write = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var flush = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var flushing = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var entry = new LogEntry(LogLevel.Info, nameof(LoggingAsyncTest), "queued-message");
		var logger = new TestLogger((log, _) =>
		{
			if(log != null)
				return new(write.Task);

			flushing.TrySetResult();
			return new(flush.Task);
		});
		using var loggers = new LoggerScope(logger);
		var enqueue = Task.Run(() => Logging.Log(entry));
		Task barrier = null;

		try
		{
			await enqueue.WaitAsync(TimeSpan.FromSeconds(5));
			await logger.Started.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.Same(entry, logger.Entry);
			Assert.Equal(1, logger.Calls);

			barrier = Logging.FlushAsync().AsTask();
			Assert.False(barrier.IsCompleted);
			Assert.False(flushing.Task.IsCompleted);

			write.SetResult();
			await flushing.Task.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.False(barrier.IsCompleted);
			Assert.Equal(2, logger.Calls);

			flush.SetResult();
			await barrier.WaitAsync(TimeSpan.FromSeconds(5));
		}
		finally
		{
			write.TrySetResult();
			flush.TrySetResult();
			await enqueue.WaitAsync(TimeSpan.FromSeconds(5));
			await Logging.FlushAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task FlushAsync_QueuedWriteFailure_ReportsFailureAndAllowsLaterRecords(bool failFlush)
	{
		var failure = new InvalidOperationException();
		var flushFailure = new ArgumentException();
		var write = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var failedEntry = new LogEntry(LogLevel.Info, nameof(LoggingAsyncTest), "failed-message");
		var laterEntry = new LogEntry(LogLevel.Info, nameof(LoggingAsyncTest), "later-message");
		var writes = new System.Collections.Generic.List<ILog>();
		var flushCount = 0;
		var logger = new TestLogger((log, _) =>
		{
			if(log == null)
				return ++flushCount == 1 && failFlush ? ValueTask.FromException(flushFailure) : ValueTask.CompletedTask;

			writes.Add(log);
			return ReferenceEquals(log, failedEntry) ? new(write.Task) : ValueTask.CompletedTask;
		});
		using var loggers = new LoggerScope(logger);

		try
		{
			Logging.Log(failedEntry);
			await logger.Started.WaitAsync(TimeSpan.FromSeconds(5));
			var barrier = Logging.FlushAsync().AsTask();
			Assert.False(barrier.IsCompleted);
			write.SetException(failure);

			if(failFlush)
			{
				var observed = await Assert.ThrowsAsync<AggregateException>(() => barrier.WaitAsync(TimeSpan.FromSeconds(5)));
				Assert.Collection(observed.InnerExceptions,
					error => Assert.Same(failure, error),
					error => Assert.Same(flushFailure, error));
			}
			else
			{
				var observed = await Assert.ThrowsAsync<InvalidOperationException>(() => barrier.WaitAsync(TimeSpan.FromSeconds(5)));
				Assert.Same(failure, observed);
			}

			Logging.Log(laterEntry);
			await Logging.FlushAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
			Assert.Collection(writes,
				log => Assert.Same(failedEntry, log),
				log => Assert.Same(laterEntry, log));
			Assert.Equal(2, flushCount);
		}
		finally
		{
			write.TrySetException(failure);
			await Record.ExceptionAsync(() => Logging.FlushAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5)));
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task DispatchAsync_SynchronousSinkFailure_StillAwaitsEveryOtherSink(bool flush)
	{
		var failure = new InvalidOperationException();
		var source = new AsyncOperationSource();
		var failed = new TestLogger((_, _) => throw failure);
		var pending = new TestLogger((_, _) => source.Operation);
		var completed = new TestLogger((_, _) => ValueTask.CompletedTask);
		using var loggers = new LoggerScope(failed, pending, completed);
		using var cancellation = new CancellationTokenSource();
		var entry = new LogEntry(LogLevel.Info, nameof(LoggingAsyncTest), "controlled-message");
		var returned = new TaskCompletionSource<Task>(TaskCreationOptions.RunContinuationsAsynchronously);
		var invocation = Task.Run(() =>
		{
			try { returned.SetResult(DispatchAsync(flush, entry, cancellation.Token).AsTask()); }
			catch(Exception exception) { returned.SetResult(Task.FromException(exception)); }
		});
		Task dispatch = null;

		try
		{
			dispatch = await returned.Task.WaitAsync(TimeSpan.FromSeconds(5));
			await Task.WhenAll(failed.Started, pending.Started, completed.Started).WaitAsync(TimeSpan.FromSeconds(5));
			Assert.False(dispatch.IsCompleted);
			Assert.Equal(1, failed.Calls);
			Assert.Equal(1, pending.Calls);
			Assert.Equal(1, completed.Calls);
			Assert.Equal(cancellation.Token, pending.Cancellation);
			Assert.Equal(0, source.ConsumptionCount);

			if(!flush)
				Assert.Same(entry, pending.Entry);
		}
		finally
		{
			source.Complete();
			await invocation.WaitAsync(TimeSpan.FromSeconds(5));
		}

		var observed = await Assert.ThrowsAsync<InvalidOperationException>(() => dispatch.WaitAsync(TimeSpan.FromSeconds(5)));

		Assert.Same(failure, observed);
		Assert.Equal(1, source.ConsumptionCount);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task DispatchAsync_CallerCancellation_IsForwardedAndObserved(bool flush)
	{
		using var cancellation = new CancellationTokenSource();
		var logger = new TestLogger((_, token) => new(Task.Delay(Timeout.Infinite, token)));
		using var loggers = new LoggerScope(logger);
		var entry = new LogEntry(LogLevel.Info, nameof(LoggingAsyncTest), "canceled-message");
		var dispatch = DispatchAsync(flush, entry, cancellation.Token).AsTask();

		try
		{
			await logger.Started.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.False(dispatch.IsCompleted);
			Assert.Equal(1, logger.Calls);
			Assert.Equal(cancellation.Token, logger.Cancellation);
		}
		finally { cancellation.Cancel(); }

		var observed = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => dispatch.WaitAsync(TimeSpan.FromSeconds(5)));

		Assert.Equal(cancellation.Token, observed.CancellationToken);
	}

	private static ValueTask DispatchAsync(bool flush, LogEntry entry, CancellationToken cancellation) =>
		flush ? Logging.FlushAsync(cancellation) : Logging.LogAsync(entry, cancellation);

	private sealed class TestApplicationContext(System.IServiceProvider services) : ApplicationContext(services);

	private sealed class TestInitializer(Func<ValueTask> dispose) : IApplicationInitializer, IAsyncDisposable
	{
		public int DisposalCount { get; private set; }
		public void Initialize(IApplicationContext context) { }

		public ValueTask DisposeAsync()
		{
			this.DisposalCount++;
			return dispose();
		}
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
		private readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public string Name => nameof(TestLogger);
		public Task Started => _started.Task;
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
			_started.TrySetResult();
			return operation(log, cancellation);
		}
	}
}
