using System;
using System.IO;
using System.Text;
using System.Threading;
using System.ComponentModel;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Components;

namespace Zongsoft.Terminals.Tests;

public sealed class TerminalReactiveTest
{
	private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);

	[Fact]
	public async Task AbortCompletesReactiveModeAndUnsubscribes()
	{
		var terminal = new TestTerminal();
		var context = CreateContext(terminal);
		var expected = new object();
		var exits = 0;
		var cleanupEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var releaseCleanup = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var pending = context.ReactiveAsync((_, _) => ValueTask.CompletedTask, async (current, failure, cancellation) =>
		{
			Assert.Null(failure);
			Assert.False(cancellation.CanBeCanceled);
			Assert.Equal(0, terminal.AbortingSubscribers);
			current.Result = expected;
			exits++;
			cleanupEntered.SetResult();
			await releaseCleanup.Task;
		}).AsTask();

		Assert.False(pending.IsCompleted);
		Assert.Equal(1, terminal.AbortingSubscribers);

		try
		{
			var handled = ((ITerminal)terminal).Abort();
			await cleanupEntered.Task.WaitAsync(_timeout);

			Assert.True(handled);
			Assert.False(pending.IsCompleted);
			Assert.Equal(0, terminal.AbortingSubscribers);
			Assert.False(terminal.Abort());
		}
		finally { releaseCleanup.TrySetResult(); }

		var result = await pending.WaitAsync(_timeout);

		Assert.Same(expected, result);
		Assert.Equal(1, exits);
		Assert.Equal(0, terminal.AbortingSubscribers);
		Assert.False(terminal.Abort());
	}

	[Fact]
	public async Task CancellationWaitsForUncancelledCleanupAndUnsubscribes()
	{
		var terminal = new TestTerminal();
		var context = CreateContext(terminal);
		using var cancellation = new CancellationTokenSource();
		var cleanupEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var releaseCleanup = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		Exception cleanupFailure = null;
		var cleanupCancellation = new CancellationToken(true);
		var exits = 0;
		var pending = context.ReactiveAsync((_, _) => ValueTask.CompletedTask, async (_, failure, token) =>
		{
			cleanupFailure = failure;
			cleanupCancellation = token;
			exits++;
			cleanupEntered.SetResult();
			await releaseCleanup.Task;
		}, cancellation.Token).AsTask();

		try
		{
			cancellation.Cancel();
			await cleanupEntered.Task.WaitAsync(_timeout);

			Assert.False(pending.IsCompleted);
			Assert.IsAssignableFrom<OperationCanceledException>(cleanupFailure);
			Assert.False(cleanupCancellation.CanBeCanceled);
			Assert.Equal(0, terminal.AbortingSubscribers);
		}
		finally { releaseCleanup.TrySetResult(); }

		var observed = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending.WaitAsync(_timeout));

		Assert.Same(cleanupFailure, observed);
		Assert.Equal(cancellation.Token, observed.CancellationToken);
		Assert.Equal(1, exits);
	}

	[Fact]
	public async Task EnterFailureIsPassedToCleanupAndPreserved()
	{
		var terminal = new TestTerminal();
		var context = CreateContext(terminal);
		var failure = new IOException("reactive entry failed");
		var exits = 0;
		var observed = await Assert.ThrowsAsync<IOException>(() => context.ReactiveAsync(
			(_, _) => ValueTask.FromException(failure),
			(_, exception, cancellation) =>
			{
				Assert.Same(failure, exception);
				Assert.False(cancellation.CanBeCanceled);
				Assert.Equal(0, terminal.AbortingSubscribers);
				exits++;
				return ValueTask.CompletedTask;
			}).AsTask().WaitAsync(_timeout));

		Assert.Same(failure, observed);
		Assert.Equal(1, exits);
		Assert.Equal(0, terminal.AbortingSubscribers);
	}

	[Fact]
	public async Task EntryAndCleanupFailuresAreBothPreserved()
	{
		var terminal = new TestTerminal();
		var context = CreateContext(terminal);
		var entryFailure = new IOException("reactive entry failed");
		var cleanupFailure = new ApplicationException("reactive cleanup failed");
		var observed = await Assert.ThrowsAsync<AggregateException>(() => context.ReactiveAsync(
			(_, _) => ValueTask.FromException(entryFailure),
			(_, _, _) => ValueTask.FromException(cleanupFailure)).AsTask().WaitAsync(_timeout));

		Assert.Collection(observed.InnerExceptions,
			exception => Assert.Same(entryFailure, exception),
			exception => Assert.Same(cleanupFailure, exception));
		Assert.Equal(0, terminal.AbortingSubscribers);
	}

	[Fact]
	public async Task CleanupFailureAfterAbortIsPropagatedAndUnsubscribed()
	{
		var terminal = new TestTerminal();
		var context = CreateContext(terminal);
		var failure = new ApplicationException("reactive cleanup failed");
		var pending = context.ReactiveAsync((_, _) => ValueTask.CompletedTask,
			(_, _, _) => ValueTask.FromException(failure)).AsTask();
		terminal.Abort();
		var observed = await Assert.ThrowsAsync<ApplicationException>(() => pending.WaitAsync(_timeout));

		Assert.Same(failure, observed);
		Assert.Equal(0, terminal.AbortingSubscribers);
		Assert.False(terminal.Abort());
	}

	private static CommandContext CreateContext(TestTerminal terminal)
	{
		var executor = new TestExecutor(terminal);
		terminal.Executor = executor;
		return new(executor, new CommandLine.Cmdlet("watch"), new TestCommand(), null);
	}

	private sealed class TestCommand() : CommandBase("watch")
	{
		protected override ValueTask<object> OnExecuteAsync(object argument, CancellationToken cancellation) => default;
	}

	private sealed class TestExecutor(ITerminal terminal) : CommandExecutor, ITerminalExecutor
	{
		public event EventHandler<Terminal.ExitEventArgs> Exit { add { } remove { } }
		public ITerminal Terminal => terminal;
		public Task<int> RunAsync(CancellationToken cancellation = default) => throw new NotSupportedException();
		public Task<int> RunAsync(string splash, CancellationToken cancellation = default) => throw new NotSupportedException();
		public Task<int> RunAsync(CommandOutletContent splash, CancellationToken cancellation = default) => throw new NotSupportedException();
	}

	private sealed class TestTerminal : ITerminal
	{
		private CancelEventHandler _aborting;

		public event EventHandler Resetted { add { } remove { } }
		public event EventHandler Resetting { add { } remove { } }
		public event CancelEventHandler Aborting { add => _aborting += value; remove => _aborting -= value; }
		public int AbortingSubscribers => _aborting?.GetInvocationList().Length ?? 0;
		public ITerminalExecutor Executor { get; set; }
		public TextReader Input { get; set; }
		public TextWriter Error { get; set; } = TextWriter.Null;
		public TextWriter Output { get; set; } = TextWriter.Null;
		public Encoding Encoding { get; set; } = Encoding.UTF8;
		public TextWriter Writer => this.Output;
		public CommandOutletColor BackgroundColor { get; set; }
		public CommandOutletColor ForegroundColor { get; set; }

		public bool Abort()
		{
			var args = new CancelEventArgs();
			_aborting?.Invoke(this, args);
			return args.Cancel;
		}

		public void Clear() => throw new NotSupportedException();
		public void Reset() => throw new NotSupportedException();
		public void ResetStyles(TerminalStyles styles) => throw new NotSupportedException();
		public void Write(CommandOutletContent content) => throw new NotSupportedException();
		public void Write<T>(T value) => throw new NotSupportedException();
		public void Write<T>(CommandOutletColor foregroundColor, T value) => throw new NotSupportedException();
		public void Write<T>(CommandOutletColor foregroundColor, CommandOutletColor backgroundColor, T value) => throw new NotSupportedException();
		public void Write<T>(CommandOutletStyles style, T value) => throw new NotSupportedException();
		public void Write<T>(CommandOutletStyles style, CommandOutletColor foregroundColor, T value) => throw new NotSupportedException();
		public void Write<T>(CommandOutletStyles style, CommandOutletColor foregroundColor, CommandOutletColor backgroundColor, T value) => throw new NotSupportedException();
		public void WriteLine() => throw new NotSupportedException();
		public void WriteLine(CommandOutletContent content) => throw new NotSupportedException();
		public void WriteLine<T>(T value) => throw new NotSupportedException();
		public void WriteLine<T>(CommandOutletColor foregroundColor, T value) => throw new NotSupportedException();
		public void WriteLine<T>(CommandOutletColor foregroundColor, CommandOutletColor backgroundColor, T value) => throw new NotSupportedException();
		public void WriteLine<T>(CommandOutletStyles style, T value) => throw new NotSupportedException();
		public void WriteLine<T>(CommandOutletStyles style, CommandOutletColor foregroundColor, T value) => throw new NotSupportedException();
		public void WriteLine<T>(CommandOutletStyles style, CommandOutletColor foregroundColor, CommandOutletColor backgroundColor, T value) => throw new NotSupportedException();
	}
}
