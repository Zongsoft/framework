using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Diagnostics.Tests;

public class FileLoggerLifecycleTest
{
	private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);
	private static readonly TimeSpan _period = TimeSpan.FromHours(1);

	[Fact]
	public async Task DisposalDoesNotFlushBufferedEntries()
	{
		using var context = new LoggerContext();
		var logger = context.CreateLogger(_period, 4, WriteEntriesAsync);

		try
		{
			Assert.False(typeof(IDisposable).IsAssignableFrom(logger.GetType()));
			Assert.Null(logger.GetType().GetMethod("Dispose", Type.EmptyTypes));
			var disposable = Assert.IsAssignableFrom<IAsyncDisposable>(logger);

			await logger.LogAsync(CreateEntry("first"));
			await logger.LogAsync(CreateEntry("second"));

			Assert.Equal(0, logger.WriteCount);
			Assert.False(File.Exists(context.FilePath));

			await WaitAsync(disposable.DisposeAsync().AsTask());

			await AssertClosedAsync(logger);
			await WaitAsync(logger.DisposeAsync().AsTask());

			Assert.Equal(0, logger.WriteCount);
			Assert.False(File.Exists(context.FilePath));
		}
		finally { await CleanupAsync(logger); }
	}

	[Fact]
	public async Task ExplicitFlushBeforeDisposalPreservesEntries()
	{
		using var context = new LoggerContext();
		var logger = context.CreateLogger(_period, 4, WriteEntriesAsync);

		try
		{
			await logger.LogAsync(CreateEntry("first"));
			await logger.LogAsync(CreateEntry("second"));

			Assert.False(File.Exists(context.FilePath));

			await WaitAsync(logger.FlushAsync().AsTask());
			await WaitAsync(logger.DisposeAsync().AsTask());

			Assert.Equal(new[] { "first", "second" }, File.ReadAllLines(context.FilePath));
			Assert.Equal(1, logger.WriteCount);
			await AssertClosedAsync(logger);
		}
		finally { await CleanupAsync(logger); }
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task DisposalWaitsForInFlightWrites(bool buffered)
	{
		using var context = new LoggerContext();
		var firstEntered = NewSignal();
		var firstRelease = NewSignal();
		var secondEntered = NewSignal();
		var secondRelease = NewSignal();
		var calls = 0;
		var logger = context.CreateLogger(buffered ? _period : TimeSpan.Zero, buffered ? 4 : 1, async (output, entries, _) =>
		{
			var logs = entries.ToArray();

			if(Interlocked.Increment(ref calls) == 1)
			{
				firstEntered.TrySetResult();
				await WaitAsync(firstRelease.Task);
			}
			else
			{
				secondEntered.TrySetResult();
				await WaitAsync(secondRelease.Task);
			}

			Assert.True(output.CanWrite);
			await WriteEntriesAsync(output, logs, CancellationToken.None);
		});

		try
		{
			Task first;

			if(buffered)
			{
				await logger.LogAsync(CreateEntry("first"));
				first = logger.FlushAsync().AsTask();
			}
			else
				first = logger.LogAsync(CreateEntry("first")).AsTask();

			await WaitAsync(firstEntered.Task);
			var second = logger.LogAsync(CreateEntry("second")).AsTask();

			if(buffered)
				await WaitAsync(second);
			else
				Assert.False(second.IsCompleted);

			var disposals = Enumerable.Range(0, 4).Select(_ => logger.DisposeAsync().AsTask()).ToArray();

			Assert.All(disposals, disposal => Assert.False(disposal.IsCompleted));
			await AssertClosedAsync(logger);
			firstRelease.TrySetResult();

			if(!buffered)
			{
				await WaitAsync(secondEntered.Task);

				Assert.All(disposals, disposal => Assert.False(disposal.IsCompleted));
				Assert.False(second.IsCompleted);
				secondRelease.TrySetResult();
			}

			await WaitAsync(Task.WhenAll(first, second));
			await WaitAsync(Task.WhenAll(disposals));

			Assert.Equal(buffered ? 1 : 2, logger.WriteCount);
			Assert.Equal(buffered ? new[] { "first" } : new[] { "first", "second" }, File.ReadAllLines(context.FilePath));
		}
		finally
		{
			firstRelease.TrySetResult();
			secondRelease.TrySetResult();
			await CleanupAsync(logger);
		}
	}

	[Fact]
	public async Task CancelingQueuedDirectWriteDoesNotDelayDisposal()
	{
		using var context = new LoggerContext();
		using var cancellation = new CancellationTokenSource();
		var entered = NewSignal();
		var release = NewSignal();
		var logger = context.CreateLogger(TimeSpan.Zero, 1, async (output, entries, _) =>
		{
			var entry = Assert.Single(entries);

			Assert.Equal("first", entry.Message);
			entered.TrySetResult();
			await WaitAsync(release.Task);
			await WriteEntriesAsync(output, [entry], CancellationToken.None);
		});

		try
		{
			var first = logger.LogAsync(CreateEntry("first")).AsTask();
			await WaitAsync(entered.Task);
			var second = logger.LogAsync(CreateEntry("canceled"), cancellation.Token).AsTask();

			Assert.False(second.IsCompleted);

			cancellation.Cancel();
			var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => WaitAsync(second));
			var disposal = logger.DisposeAsync().AsTask();

			Assert.Equal(cancellation.Token, exception.CancellationToken);
			Assert.False(disposal.IsCompleted);
			Assert.Equal(1, logger.WriteCount);
			release.TrySetResult();
			await WaitAsync(Task.WhenAll(first, disposal));

			Assert.Equal(new[] { "first" }, File.ReadAllLines(context.FilePath));
			Assert.Equal(1, logger.WriteCount);
			await AssertClosedAsync(logger);
		}
		finally
		{
			release.TrySetResult();
			await CleanupAsync(logger);
		}
	}

	[Theory]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public async Task WriteCallbackRejectsSelfWaiting(bool buffered, bool externalDisposal)
	{
		using var context = new LoggerContext();
		var entered = NewSignal();
		var inspect = NewSignal();
		var inspected = NewSignal();
		TestFileLogger logger = null;
		logger = context.CreateLogger(buffered ? _period : TimeSpan.Zero, buffered ? 4 : 1, async (output, entries, _) =>
		{
			entered.TrySetResult();
			await WaitAsync(inspect.Task);
			var disposal = CaptureDisposal(logger);

			Assert.True(disposal.IsCompleted);
			await Assert.ThrowsAsync<InvalidOperationException>(() => disposal);
			await WriteEntriesAsync(output, entries, CancellationToken.None);
			inspected.TrySetResult();
		});

		try
		{
			Task writing;

			if(buffered)
			{
				await logger.LogAsync(CreateEntry("callback"));
				writing = logger.FlushAsync().AsTask();
			}
			else
				writing = logger.LogAsync(CreateEntry("callback")).AsTask();

			await WaitAsync(entered.Task);
			Task external = null;

			if(externalDisposal)
			{
				external = logger.DisposeAsync().AsTask();
				Assert.False(external.IsCompleted);
			}

			inspect.TrySetResult();
			await WaitAsync(inspected.Task);
			await WaitAsync(writing);

			if(!externalDisposal)
			{
				await logger.LogAsync(CreateEntry("continued"));
				await WaitAsync(logger.FlushAsync().AsTask());
			}

			await WaitAsync(external ?? logger.DisposeAsync().AsTask());

			Assert.Equal(externalDisposal ? new[] { "callback" } : new[] { "callback", "continued" }, File.ReadAllLines(context.FilePath));
			Assert.Equal(externalDisposal ? 1 : 2, logger.WriteCount);
			await AssertClosedAsync(logger);
		}
		finally
		{
			inspect.TrySetResult();
			await CleanupAsync(logger);
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task PeriodicFailureForwardsToAllSubscribers(bool externalDisposal)
	{
		using var context = new LoggerContext();
		var entered = NewSignal();
		var release = NewSignal();
		var notified = NewSignal();
		var failure = new IOException();
		var observerFailure = new ApplicationException();
		var notifications = new List<int>();
		var calls = 0;
		FailureEventArgs forwarded = null;
		Task rejected = null;
		var logger = context.CreateLogger(TimeSpan.FromMilliseconds(10), 4, async (output, entries, _) =>
		{
			var entry = Assert.Single(entries);

			if(Interlocked.Increment(ref calls) == 1)
			{
				Assert.Equal("periodic", entry.Message);
				entered.TrySetResult();
				await WaitAsync(release.Task);
				throw failure;
			}

			await WriteEntriesAsync(output, [entry], CancellationToken.None);
		});
		logger.Failed += (sender, args) =>
		{
			Assert.Same(logger, sender);
			Assert.Same(failure, args.Exception);
			Assert.Null(args.State);
			forwarded = args;
			args.ExceptionHandled = true;
			args.Exception = null;
			notifications.Add(1);
			throw observerFailure;
		};
		logger.Failed += (_, args) =>
		{
			Assert.Same(forwarded, args);
			Assert.True(args.ExceptionHandled);
			Assert.Null(args.Exception);
			rejected = CaptureDisposal(logger);

			Assert.True(rejected.IsCompleted);
			notifications.Add(2);
		};
		logger.Failed += (_, _) =>
		{
			notifications.Add(3);
			notified.TrySetResult();
		};

		try
		{
			await logger.LogAsync(CreateEntry("periodic"));
			await WaitAsync(entered.Task);

			Assert.Equal(1, logger.WriteCount);
			Task external = null;

			if(externalDisposal)
			{
				external = logger.DisposeAsync().AsTask();
				Assert.False(external.IsCompleted);
			}

			release.TrySetResult();
			await WaitAsync(notified.Task);
			await Assert.ThrowsAsync<InvalidOperationException>(() => rejected);

			if(!externalDisposal)
			{
				await logger.LogAsync(CreateEntry("continued"));
				await WaitAsync(logger.FlushAsync().AsTask());

				Assert.Equal(new[] { "continued" }, File.ReadAllLines(context.FilePath));
			}

			var exception = await Assert.ThrowsAsync<AggregateException>(() => WaitAsync(external ?? logger.DisposeAsync().AsTask()));

			Assert.Equal(new[] { 1, 2, 3 }, notifications);
			//日志器已转发三个订阅者；转发异常使底层 Spooler 跳过后续订阅者。
			Assert.Null(logger.SourceFailure);
			Assert.Collection(exception.Flatten().InnerExceptions,
				error => Assert.Same(failure, error),
				error => Assert.Same(observerFailure, error));
			Assert.Same(exception, await Assert.ThrowsAsync<AggregateException>(() => logger.DisposeAsync().AsTask()));
			Assert.Equal(externalDisposal ? 1 : 2, logger.WriteCount);
			await AssertClosedAsync(logger);
		}
		finally
		{
			release.TrySetResult();
			await CleanupAsync(logger);
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task ExplicitWriteFailureBelongsToCaller(bool buffered)
	{
		using var context = new LoggerContext();
		var failure = new IOException();
		var notifications = 0;
		var entry = CreateEntry("explicit");
		var logger = context.CreateLogger(buffered ? _period : TimeSpan.Zero, buffered ? 4 : 1, (_, entries, _) =>
		{
			Assert.Same(entry, Assert.Single(entries));
			return ValueTask.FromException(failure);
		});
		logger.Failed += (_, _) => Interlocked.Increment(ref notifications);

		try
		{
			Task writing;

			if(buffered)
			{
				await logger.LogAsync(entry);
				writing = logger.FlushAsync().AsTask();
			}
			else
				writing = logger.LogAsync(entry).AsTask();

			Assert.Same(failure, await Assert.ThrowsAsync<IOException>(() => WaitAsync(writing)));
			await WaitAsync(logger.DisposeAsync().AsTask());

			Assert.Equal(1, logger.WriteCount);
			Assert.Equal(0, notifications);
			Assert.Equal(0, new FileInfo(context.FilePath).Length);
			await AssertClosedAsync(logger);
		}
		finally { await CleanupAsync(logger); }
	}

	private static LogEntry CreateEntry(string message, LogLevel level = LogLevel.Info) => new(level, "FileLoggerLifecycle", message);
	private static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
	private static Task WaitAsync(Task task) => task.WaitAsync(_timeout, TestContext.Current.CancellationToken);
	private static async ValueTask WriteEntriesAsync(Stream output, IEnumerable<LogEntry> entries, CancellationToken cancellation)
	{
		foreach(var entry in entries)
			await output.WriteAsync(Encoding.UTF8.GetBytes(entry.Message + "\n"), cancellation);
	}

	private static async Task AssertClosedAsync(TestFileLogger logger)
	{
		await Assert.ThrowsAsync<ObjectDisposedException>(() => logger.LogAsync(CreateEntry("late")).AsTask());
		await Assert.ThrowsAsync<ObjectDisposedException>(() => logger.LogAsync(CreateEntry("filtered", LogLevel.Debug)).AsTask());
		await Assert.ThrowsAsync<ObjectDisposedException>(() => logger.FlushAsync().AsTask());
	}

	private static Task CaptureDisposal(TestFileLogger logger)
	{
		try { return logger.DisposeAsync().AsTask(); }
		catch(Exception exception) { return Task.FromException(exception); }
	}

	private static async Task CleanupAsync(TestFileLogger logger) =>
		await Record.ExceptionAsync(() => WaitAsync(logger.DisposeAsync().AsTask()));

	private sealed class LoggerContext : IDisposable
	{
		private readonly string _directory;

		public LoggerContext()
		{
			_directory = Path.Combine(Path.GetTempPath(), $"FileLoggerLifecycleTest-{Guid.NewGuid():N}");
			Directory.CreateDirectory(_directory);
			this.FilePath = Path.Combine(_directory, "diagnostics.log");
		}

		public string FilePath { get; }
		public TestFileLogger CreateLogger(TimeSpan period, int limit, Func<Stream, IEnumerable<LogEntry>, CancellationToken, ValueTask> writer) =>
			new(period, limit, this.FilePath, writer);

		public void Dispose()
		{
			if(Directory.Exists(_directory))
				Directory.Delete(_directory, true);
		}
	}

	private sealed class TestFileLogger : FileLogger<LogEntry, string>
	{
		private readonly Func<Stream, IEnumerable<LogEntry>, CancellationToken, ValueTask> _writer;
		private int _writeCount;

		public TestFileLogger(TimeSpan period, int limit, string filePath, Func<Stream, IEnumerable<LogEntry>, CancellationToken, ValueTask> writer) : base(period, limit, filePath)
		{
			_writer = writer;

			if(this.Logging != null)
				this.Logging.Failed += (_, args) => this.SourceFailure = args;
		}

		public int WriteCount => Volatile.Read(ref _writeCount);
		public FailureEventArgs SourceFailure { get; private set; }
		public ValueTask FlushAsync(CancellationToken cancellation = default) => ((ILogger)this).FlushAsync(cancellation);

		protected override string GetFilePath(ILog log) => this.FilePath;
		protected override ValueTask WriteLogsAsync(Stream output, IEnumerable<LogEntry> logs, CancellationToken cancellation)
		{
			Interlocked.Increment(ref _writeCount);
			return _writer(output, logs, cancellation);
		}
	}
}
