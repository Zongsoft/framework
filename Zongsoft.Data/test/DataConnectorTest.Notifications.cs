using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Xunit;

using Zongsoft.Diagnostics;
using Zongsoft.Data.Common;

namespace Zongsoft.Data.Tests;

public partial class DataConnectorTest
{
	[Fact]
	public async Task FailureNotifications_PendingHandlers_AreAwaitedInOrderAndIgnoreCallerCancellation()
	{
		using var cancellation = new CancellationTokenSource();
		var connector = CreateConnector(new DataSourceMocker(), new ManualTimeProvider(), false);
		var failure = new InvalidOperationException();
		var entered = new[] { NewGate(), NewGate(), NewGate() };
		var release = new[] { NewGate(), NewGate(), NewGate() };
		var trace = new List<string>();
		var observed = new List<(object Sender, DataConnectionFailureEventArgs Args, CancellationToken Token)>();
		var logger = new RecordingLogger();

		Logging.Loggers.Add(logger);
		connector.Failed += (sender, args, token) => NotifyAsync(0, sender, args, token);
		connector.Failed += (sender, args, token) => NotifyAsync(1, sender, args, token);
		connector.Breaker.Failed += (sender, args, token) => NotifyAsync(2, sender, args, token);

		var attempts = 0;
		var operation = connector.ConnectAsync<int>(_ =>
		{
			attempts++;
			return Task.FromException<int>(failure);
		}, cancellation.Token);

		try
		{
			for(var index = 0; index < entered.Length; index++)
			{
				await entered[index].Task.WaitAsync(_timeout);
				Assert.False(operation.IsCompleted);
				Assert.Equal(index + 1, observed.Count);
				Assert.Empty(logger.Entries);

				if(index == 0)
				{
					cancellation.Cancel();
					await Assert.ThrowsAsync<DataConnectionException>(() => connector.ConnectAsync(_ => Task.FromResult(++attempts)));
					Assert.Equal(1, attempts);
				}

				release[index].SetResult();
			}

			Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => operation.WaitAsync(_timeout)));
			Assert.Equal(["enter0", "exit0", "enter1", "exit1", "enter2", "exit2"], trace);
			Assert.Same(connector, observed[0].Sender);
			Assert.Same(connector, observed[1].Sender);
			Assert.Same(connector.Breaker, observed[2].Sender);
			Assert.All(observed, notification =>
			{
				Assert.Same(failure, notification.Args.Exception);
				Assert.Same(observed[0].Args, notification.Args);
				Assert.Equal(CancellationToken.None, notification.Token);
			});
			Assert.Empty(logger.Entries);
		}
		finally
		{
			foreach(var gate in release)
				gate.TrySetResult();

			await Record.ExceptionAsync(() => operation.WaitAsync(_timeout));
			Logging.Loggers.Remove(logger);
		}

		async ValueTask NotifyAsync(int index, object sender, DataConnectionFailureEventArgs args, CancellationToken token)
		{
			observed.Add((sender, args, token));
			trace.Add($"enter{index}");
			entered[index].TrySetResult();
			await release[index].Task;

			if(index == 1)
				args.ExceptionHandled = true;

			trace.Add($"exit{index}");
		}
	}

	[Fact]
	public async Task FailureNotifications_HandlerFailures_PreserveOriginalFailure()
	{
		var connector = CreateConnector(new DataSourceMocker(), new ManualTimeProvider(), false);
		var original = new InvalidOperationException();
		var subscriberFailure = new ApplicationException();
		var trace = new List<string>();
		var observed = new List<Exception>();

		connector.Failed += (_, _, _) =>
		{
			trace.Add("connector-sync");
			throw subscriberFailure;
		};
		connector.Failed += async (_, _, _) =>
		{
			await Task.Yield();
			trace.Add("connector-async");
			throw subscriberFailure;
		};
		connector.Failed += (_, args, _) =>
		{
			trace.Add("connector-last");
			observed.Add(args.Exception);
			args.ExceptionHandled = true;
			return ValueTask.CompletedTask;
		};
		connector.Breaker.Failed += (_, _, _) =>
		{
			trace.Add("breaker-sync");
			throw subscriberFailure;
		};
		connector.Breaker.Failed += async (_, _, _) =>
		{
			await Task.Yield();
			trace.Add("breaker-async");
			throw subscriberFailure;
		};
		connector.Breaker.Failed += (_, args, _) =>
		{
			trace.Add("breaker-last");
			observed.Add(args.Exception);
			return ValueTask.CompletedTask;
		};

		var failure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
			connector.ConnectAsync<int>(_ => Task.FromException<int>(original)).WaitAsync(_timeout));

		Assert.Same(original, failure);
		Assert.Equal(["connector-sync", "connector-async", "connector-last", "breaker-sync", "breaker-async", "breaker-last"], trace);
		Assert.Equal(2, observed.Count);
		Assert.All(observed, exception => Assert.Same(original, exception));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task FailureLogging_PendingWrite_IsAwaitedAndCannotReplaceConnectionFailure(bool logFails)
	{
		using var cancellation = new CancellationTokenSource();
		var connector = CreateConnector(new DataSourceMocker(), new ManualTimeProvider(), false);
		var failure = new InvalidOperationException();
		var entered = NewGate();
		var release = NewGate();
		var observed = new List<(LogEntry Entry, CancellationToken Token)>();

		var logger = new CallbackLogger(async (entry, token) =>
		{
			observed.Add((entry, token));
			entered.TrySetResult();
			await release.Task;

			if(logFails)
				throw new ApplicationException();
		});

		Logging.Loggers.Add(logger);
		var operation = connector.ConnectAsync<int>(_ => Task.FromException<int>(failure), cancellation.Token);

		try
		{
			await entered.Task.WaitAsync(_timeout);
			cancellation.Cancel();
			Assert.False(operation.IsCompleted);

			var notification = Assert.Single(observed);
			Assert.Equal(CancellationToken.None, notification.Token);
			Assert.Equal(LogLevel.Error, notification.Entry.Level);
			Assert.Same(failure, notification.Entry.Exception);
			release.SetResult();
			Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => operation.WaitAsync(_timeout)));
			Assert.Single(observed);
		}
		finally
		{
			release.TrySetResult();
			await Record.ExceptionAsync(() => operation.WaitAsync(_timeout));
			Logging.Loggers.Remove(logger);
		}
	}

	private sealed class CallbackLogger(Func<LogEntry, CancellationToken, ValueTask> callback) : ILogger
	{
		public string Name => nameof(CallbackLogger);
		public ValueTask FlushAsync(CancellationToken cancellation = default) => ValueTask.CompletedTask;
		public ValueTask LogAsync<TLog>(TLog log, CancellationToken cancellation = default) where TLog : ILog =>
			log is LogEntry entry ? callback(entry, cancellation) : ValueTask.CompletedTask;
	}
}

[CollectionDefinition(nameof(DataConnectorCollection), DisableParallelization = true)]
public sealed class DataConnectorCollection;
