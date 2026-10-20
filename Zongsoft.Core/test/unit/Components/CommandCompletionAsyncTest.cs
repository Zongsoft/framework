using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Components.Tests;

public class CommandCompletionAsyncTest
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task ExecuteAsync_PendingCompletion_WaitsInReverseOrderOnSuccessAndFailure(bool failExecution)
	{
		var executor = new CommandExecutor { Error = TextWriter.Null };
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var completed = new List<string>();
		var failure = new InvalidOperationException();
		var firstResult = new object();
		var secondResult = new object();
		var first = new CompletingCommand("first", firstResult, context =>
		{
			completed.Add("first");
			return ValueTask.CompletedTask;
		});
		var second = new CompletingCommand("second", secondResult, async context =>
		{
			completed.Add("second");
			entered.SetResult();
			await release.Task;
		});
		executor.Root.Children.Add(first);
		executor.Root.Children.Add(second);
		executor.Root.Children.Add(new TestCommand("failure", (_, _) => throw failure));
		var returned = new TaskCompletionSource<Task<object>>(TaskCreationOptions.RunContinuationsAsynchronously);
		var invocation = Task.Run(() => returned.SetResult(executor.ExecuteAsync(failExecution ? "first | second | failure" : "first | second").AsTask()));

		try
		{
			var execution = await returned.Task.WaitAsync(TimeSpan.FromSeconds(5));
			await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));

			Assert.False(execution.IsCompleted);
			Assert.Equal(["second"], completed);
			Assert.Same(firstResult, second.Input);
			Assert.Same(secondResult, second.Completion.Result);
			Assert.Same(failExecution ? failure : null, second.Completion.Exception);
			release.SetResult();

			if(failExecution)
				Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => execution.WaitAsync(TimeSpan.FromSeconds(5))));
			else
				Assert.Same(secondResult, await execution.WaitAsync(TimeSpan.FromSeconds(5)));

			Assert.Equal(["second", "first"], completed);
			Assert.Same(firstResult, first.Completion.Result);
			Assert.Same(failExecution ? failure : null, first.Completion.Exception);
			Assert.Equal(1, first.CompletionCount);
			Assert.Equal(1, second.CompletionCount);
		}
		finally
		{
			release.TrySetResult();
			await invocation.WaitAsync(TimeSpan.FromSeconds(5));
			await Record.ExceptionAsync(async () => await (await returned.Task).WaitAsync(TimeSpan.FromSeconds(5)));
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task ExecuteAsync_CleanupFailures_AttemptsEveryCompletionAndRetainsAllFailures(bool failExecution)
	{
		var executor = new CommandExecutor { Error = TextWriter.Null };
		var failure = new InvalidOperationException();
		var synchronousFailure = new ArgumentException();
		var asynchronousFailure = new ApplicationException();
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var completed = new List<string>();
		var first = new CompletingCommand("first", new object(), _ =>
		{
			completed.Add("first");
			return ValueTask.CompletedTask;
		});
		var second = new CompletingCommand("second", new object(), async _ =>
		{
			completed.Add("second");
			entered.SetResult();
			await release.Task;
			throw asynchronousFailure;
		});
		var third = new CompletingCommand("third", new object(), _ =>
		{
			completed.Add("third");
			throw synchronousFailure;
		});
		executor.Root.Children.Add(first);
		executor.Root.Children.Add(second);
		executor.Root.Children.Add(third);
		executor.Root.Children.Add(new TestCommand("failure", (_, _) => throw failure));
		var execution = executor.ExecuteAsync(failExecution ? "first | second | third | failure" : "first | second | third").AsTask();

		try
		{
			await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.False(execution.IsCompleted);
			Assert.Equal(["third", "second"], completed);
			release.SetResult();
			var observed = await Assert.ThrowsAsync<AggregateException>(() => execution.WaitAsync(TimeSpan.FromSeconds(5)));
			var failures = observed.Flatten().InnerExceptions;

			Assert.Equal(failExecution ? 3 : 2, failures.Count);
			Assert.Contains(synchronousFailure, failures);
			Assert.Contains(asynchronousFailure, failures);

			if(failExecution)
				Assert.Contains(failure, failures);

			Assert.Equal(["third", "second", "first"], completed);
			Assert.All(new[] { first, second, third }, command =>
			{
				Assert.Equal(1, command.CompletionCount);
				Assert.Same(failExecution ? failure : null, command.Completion.Exception);
			});
		}
		finally
		{
			release.TrySetResult();
			await Record.ExceptionAsync(() => execution.WaitAsync(TimeSpan.FromSeconds(5)));
		}
	}

	[Fact]
	public async Task ExecuteAsync_CanceledCommand_WaitsForPriorCommandCleanup()
	{
		using var cancellation = new CancellationTokenSource();
		var executor = new CommandExecutor { Error = TextWriter.Null };
		var running = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var cleaning = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var first = new CompletingCommand("first", new object(), async _ =>
		{
			cleaning.SetResult();
			await release.Task;
		});
		executor.Root.Children.Add(first);
		executor.Root.Children.Add(new TestCommand("pending", async (_, token) =>
		{
			running.SetResult();
			await Task.Delay(Timeout.Infinite, token);
			return null;
		}));
		var execution = executor.ExecuteAsync("first | pending", cancellation: cancellation.Token).AsTask();

		try
		{
			await running.Task.WaitAsync(TimeSpan.FromSeconds(5));
			cancellation.Cancel();
			await cleaning.Task.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.False(execution.IsCompleted);
			var failure = Assert.IsAssignableFrom<OperationCanceledException>(first.Completion.Exception);
			Assert.Equal(cancellation.Token, failure.CancellationToken);
			release.SetResult();
			var observed = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution.WaitAsync(TimeSpan.FromSeconds(5)));
			Assert.Same(failure, observed);
			Assert.Equal(1, first.CompletionCount);
		}
		finally
		{
			cancellation.Cancel();
			release.TrySetResult();
			await Record.ExceptionAsync(() => execution.WaitAsync(TimeSpan.FromSeconds(5)));
		}
	}

	private sealed class TestCommand(string name, Func<CommandContext, CancellationToken, ValueTask<object>> execute) : CommandBase<CommandContext>(name)
	{
		protected override ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation) => execute(context, cancellation);
	}

	private sealed class CompletingCommand(string name, object result, Func<CommandCompletionContext, ValueTask> complete) : CommandBase<CommandContext>(name), ICommandCompletion
	{
		public object Input { get; private set; }
		public CommandCompletionContext Completion { get; private set; }
		public int CompletionCount { get; private set; }
		protected override ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
		{
			this.Input = context.Value;
			return ValueTask.FromResult(result);
		}
		public ValueTask OnCompletedAsync(CommandCompletionContext context)
		{
			this.Completion = context;
			this.CompletionCount++;
			return complete(context);
		}
	}
}
