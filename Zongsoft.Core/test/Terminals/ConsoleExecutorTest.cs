using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Zongsoft.Components;

namespace Zongsoft.Terminals.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ConsoleExecutorCollection
{
	public const string Name = nameof(ConsoleExecutorCollection);
}

[Collection(ConsoleExecutorCollection.Name)]
public sealed class ConsoleExecutorTest
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task HandledFailureDoesNotPrintAgainAsync(bool clearException)
	{
		var failure = new InvalidOperationException("failure ${name}{0}");
		using var scope = new ExecutionScope(failure);
		CommandExecutorFailureEventArgs notification = null;
		scope.Executor.Failed += OnFailed;

		try
		{
			await scope.ExecuteAsync();

			Assert.NotNull(notification);
			Assert.True(notification.ExceptionHandled);
			Assert.Equal("handled" + Environment.NewLine, scope.Output.ToString());
		}
		finally { scope.Executor.Failed -= OnFailed; }

		void OnFailed(object sender, CommandExecutorFailureEventArgs args)
		{
			notification = args;
			Assert.Same(failure, args.Exception);
			scope.Executor.Output.WriteLine("handled");
			args.ExceptionHandled = true;

			if(clearException)
				args.Exception = null;
		}
	}

	[Fact]
	public async Task UnhandledFailureRetainsDefaultOutputAsync()
	{
		var failure = new InvalidOperationException("test failure");
		using var scope = new ExecutionScope(failure);
		await scope.ExecuteAsync();

		Assert.Contains(failure.Message, scope.Output.ToString());
		Assert.Equal(1, scope.Output.ToString().Split(failure.Message).Length - 1);
	}

	[Fact]
	public async Task ClearingUnhandledExceptionSuppressesOutputButPreservesPropagationAsync()
	{
		var failure = new InvalidOperationException("test failure");
		using var scope = new ExecutionScope(failure);
		scope.Executor.Failed += OnFailed;

		try
		{
			var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => scope.ExecuteAsync());

			Assert.Same(failure, exception);
			Assert.Empty(scope.Output.ToString());
		}
		finally { scope.Executor.Failed -= OnFailed; }

		static void OnFailed(object sender, CommandExecutorFailureEventArgs args) => args.Exception = null;
	}

	[Fact]
	public async Task ExitExceptionStillReachesTheInteractiveLoopWithoutErrorOutputAsync()
	{
		var failure = new Terminal.ExitException();
		using var scope = new ExecutionScope(failure);
		var exception = await Assert.ThrowsAsync<Terminal.ExitException>(() => scope.ExecuteAsync());

		Assert.Same(failure, exception);
		Assert.Empty(scope.Output.ToString());
	}

	private sealed class ExecutionScope : IDisposable
	{
		private readonly TextWriter _previous = Console.Out;
		private readonly string _name = "failure_" + Guid.NewGuid().ToString("N");

		public ExecutionScope(Exception exception)
		{
			this.Executor.Root.Children.Add(new FailureCommand(_name, exception));
			Console.SetOut(this.Output);
		}

		public ITerminalExecutor Executor => Terminal.Console.Executor;
		public StringWriter Output { get; } = new();
		public async Task ExecuteAsync() => await this.Executor.ExecuteAsync(_name, cancellation: TestContext.Current.CancellationToken);

		public void Dispose()
		{
			Console.SetOut(_previous);
			this.Executor.Root.Children.Remove(_name);
			this.Output.Dispose();
		}
	}

	private sealed class FailureCommand(string name, Exception exception) : CommandBase(name)
	{
		protected override ValueTask<object> OnExecuteAsync(object argument, CancellationToken cancellation) => throw exception;
	}
}
