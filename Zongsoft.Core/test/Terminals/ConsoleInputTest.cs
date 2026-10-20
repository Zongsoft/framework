using System;
using System.IO;
using System.Text;
using System.Threading;
using System.ComponentModel;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Components;

namespace Zongsoft.Terminals.Tests;

[Collection(ConsoleExecutorCollection.Name)]
public sealed class ConsoleInputTest
{
	private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);

	[Fact]
	public void AbortReturnsFinalHandlerDecisionSynchronously()
	{
		var terminal = Terminal.Console;
		var notifications = 0;
		Assert.False(terminal.Abort());

		terminal.Aborting += OnAbort;
		terminal.Aborting += OnContinue;

		try
		{
			var handled = terminal.Abort();

			Assert.False(handled);
			Assert.Equal(2, notifications);

			terminal.Aborting -= OnContinue;
			handled = terminal.Abort();

			Assert.True(handled);
			Assert.Equal(3, notifications);
		}
		finally
		{
			terminal.Aborting -= OnAbort;
			terminal.Aborting -= OnContinue;
		}

		Assert.False(terminal.Abort());

		void OnAbort(object sender, CancelEventArgs args)
		{
			Assert.Same(terminal, sender);
			Assert.False(args.Cancel);
			args.Cancel = true;
			notifications++;
		}

		void OnContinue(object sender, CancelEventArgs args)
		{
			Assert.Same(terminal, sender);
			Assert.True(args.Cancel);
			args.Cancel = false;
			notifications++;
		}
	}

	[Fact]
	public void AbortPropagatesHandlerFailure()
	{
		var failure = new InvalidOperationException("abort handler failed");
		Terminal.Console.Aborting += OnAbort;

		try
		{
			var observed = Assert.Throws<InvalidOperationException>(() => Terminal.Console.Abort());

			Assert.Same(failure, observed);
		}
		finally { Terminal.Console.Aborting -= OnAbort; }

		Assert.False(Terminal.Console.Abort());

		void OnAbort(object sender, CancelEventArgs args) => throw failure;
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task AbortDoesNotCancelInputOrStopExecutor(bool handled)
	{
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var inputCancellation = CancellationToken.None;
		using var input = new TestInput(async cancellation =>
		{
			inputCancellation = cancellation;
			entered.TrySetResult();
			await release.Task.WaitAsync(cancellation);
			return null;
		});
		using var scope = new ConsoleScope(input);
		Terminal.Console.Aborting += OnAbort;
		var pending = scope.Executor.RunAsync("test", TestContext.Current.CancellationToken);

		try
		{
			await entered.Task.WaitAsync(_timeout);
			var result = Terminal.Console.Abort();

			Assert.Equal(handled, result);
			Assert.False(inputCancellation.IsCancellationRequested);
			Assert.False(pending.IsCompleted);
			Assert.Equal(1, input.ReadCount);
			Assert.Equal(1, input.ActiveReads);
			Assert.Equal(0, input.DisposalCount);
		}
		finally
		{
			Terminal.Console.Aborting -= OnAbort;
			release.TrySetResult();
		}

		Assert.Equal(0, await pending.WaitAsync(_timeout));
		Assert.Equal(1, input.ReadCount);
		Assert.Equal(0, input.ActiveReads);
		Assert.Equal(0, input.DisposalCount);

		void OnAbort(object sender, CancelEventArgs args) => args.Cancel = handled;
	}

	[Fact]
	public async Task EofStopsAfterBlankLineWithoutDisposingCallerInput()
	{
		using var input = new StringReader("\n");
		using var scope = new ConsoleScope(input);
		var result = await scope.Executor.RunAsync("test", TestContext.Current.CancellationToken).WaitAsync(_timeout);

		Assert.Equal(0, result);
		Assert.Same(input, Terminal.Console.Input);
		Assert.Null(input.ReadLine());
	}

	[Fact]
	public async Task ExitConfirmationEofDoesNotExitOrReportFailure()
	{
		var reads = 0;
		using var input = new TestInput(_ => ValueTask.FromResult(++reads == 1 ? "exit" : null));
		using var scope = new ConsoleScope(input);
		var exits = 0;
		var failures = 0;
		scope.Executor.Exit += OnExit;
		scope.Executor.Failed += OnFailed;

		try
		{
			var result = await scope.Executor.RunAsync("test", TestContext.Current.CancellationToken).WaitAsync(_timeout);

			Assert.Equal(0, result);
			Assert.Equal(3, input.ReadCount);
			Assert.Equal(0, exits);
			Assert.Equal(0, failures);
			Assert.Equal(0, input.DisposalCount);
		}
		finally
		{
			scope.Executor.Exit -= OnExit;
			scope.Executor.Failed -= OnFailed;
		}

		void OnExit(object sender, Terminal.ExitEventArgs args) => exits++;
		void OnFailed(object sender, CommandExecutorFailureEventArgs args) => failures++;
	}

	[Fact]
	public async Task CancellationStopsWaitingAndReusesCallerInput()
	{
		using var cancellation = new CancellationTokenSource();
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		using var input = new TestInput(async token =>
		{
			entered.TrySetResult();
			await release.Task.WaitAsync(token);
			return null;
		});
		using var scope = new ConsoleScope(input);
		var pending = scope.Executor.RunAsync("test", cancellation.Token);
		Task<int> next = null;

		try
		{
			await entered.Task.WaitAsync(_timeout);
			cancellation.Cancel();

			Assert.Equal(0, await pending.WaitAsync(_timeout));
			Assert.False(release.Task.IsCompleted);
			Assert.Equal(0, input.ActiveReads);
			Assert.Equal(0, input.DisposalCount);

			next = scope.Executor.RunAsync("test", TestContext.Current.CancellationToken);

			Assert.False(next.IsCompleted);
			Assert.Equal(1, input.ActiveReads);
			Assert.Same(input, Terminal.Console.Input);
			Assert.Equal(0, input.DisposalCount);
		}
		finally { release.TrySetResult(); }

		Assert.Equal(0, await next.WaitAsync(_timeout));
		Assert.Equal(2, input.ReadCount);
		Assert.Equal(0, input.ActiveReads);
		Assert.Equal(0, input.DisposalCount);
	}

	[Fact]
	public async Task ConcurrentRunIsRejectedWithoutStartingAnotherRead()
	{
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		using var input = new TestInput(async cancellation =>
		{
			entered.TrySetResult();
			await release.Task.WaitAsync(cancellation);
			return null;
		});
		using var scope = new ConsoleScope(input);
		var pending = scope.Executor.RunAsync("test", TestContext.Current.CancellationToken);

		try
		{
			await entered.Task.WaitAsync(_timeout);
			await Assert.ThrowsAsync<InvalidOperationException>(() => scope.Executor.RunAsync("other").WaitAsync(_timeout));

			Assert.Equal(1, input.ReadCount);
			Assert.False(pending.IsCompleted);
		}
		finally { release.TrySetResult(); }

		Assert.Equal(0, await pending.WaitAsync(_timeout));
		Assert.Equal(0, input.ActiveReads);
		Assert.Equal(0, input.DisposalCount);
	}

	[Fact]
	public async Task InputFaultPropagatesAndReleasesRunGuard()
	{
		var failure = new IOException("input failure");
		var first = true;
		using var input = new TestInput(_ =>
		{
			if(!first)
				return ValueTask.FromResult<string>(null);

			first = false;
			return ValueTask.FromException<string>(failure);
		});
		using var scope = new ConsoleScope(input);
		var observed = await Assert.ThrowsAsync<IOException>(() => scope.Executor.RunAsync("test").WaitAsync(_timeout));

		Assert.Same(failure, observed);
		Assert.Equal(0, input.ActiveReads);
		Assert.Equal(0, input.DisposalCount);

		var result = await scope.Executor.RunAsync("test", TestContext.Current.CancellationToken).WaitAsync(_timeout);

		Assert.Equal(0, result);
		Assert.Equal(2, input.ReadCount);
		Assert.Equal(0, input.DisposalCount);
	}

	private sealed class ConsoleScope : IDisposable
	{
		private readonly TextReader _input = Terminal.Console.Input;
		private readonly TextWriter _output = Console.Out;
		private readonly Encoding _encoding = Console.OutputEncoding;

		public ConsoleScope(TextReader input)
		{
			Terminal.Console.Input = input;
			Console.SetOut(TextWriter.Null);
		}

		public ITerminalExecutor Executor => Terminal.Console.Executor;

		public void Dispose()
		{
			Terminal.Console.Input = _input;
			Console.OutputEncoding = _encoding;
			Console.SetOut(_output);
		}
	}

	private sealed class TestInput(Func<CancellationToken, ValueTask<string>> read) : TextReader
	{
		private int _reads;
		private int _active;
		private int _disposals;

		public int ReadCount => Volatile.Read(ref _reads);
		public int ActiveReads => Volatile.Read(ref _active);
		public int DisposalCount => Volatile.Read(ref _disposals);

		public override async ValueTask<string> ReadLineAsync(CancellationToken cancellation = default)
		{
			Interlocked.Increment(ref _reads);
			Interlocked.Increment(ref _active);

			try { return await read(cancellation); }
			finally { Interlocked.Decrement(ref _active); }
		}

		protected override void Dispose(bool disposing)
		{
			if(disposing)
				Interlocked.Increment(ref _disposals);

			base.Dispose(disposing);
		}
	}
}
