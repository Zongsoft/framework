using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace Zongsoft.Terminals.IntegrationTests;

[CollectionDefinition(nameof(ShellCommandCollection), DisableParallelization = true)]
public sealed class ShellCommandCollection;

[Collection(nameof(ShellCommandCollection))]
public class ShellCommandTests
{
	private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(15);

	[Fact]
	[Trait("Category", "Integration")]
	public async Task ExecuteAsync_PendingChild_ReturnsPromptlyAndAwaitsOutputAndExit()
	{
		Assert.SkipUnless(OperatingSystem.IsWindows(), "ShellCommand invokes Windows cmd.exe.");
		using var terminal = new TerminalScope();
		await using var child = new ShellProcess();
		var pending = await child.ExecuteAsync(terminal.Instance, "30s", TestContext.Current.CancellationToken, TestContext.Current.CancellationToken);
		await child.WaitForChildAsync(TestContext.Current.CancellationToken);

		Assert.False(pending.IsCompleted);
		Assert.False(child.HasExited);
		await child.ExitAsync(23, TestContext.Current.CancellationToken);

		Assert.Equal(23, Assert.IsType<int>(await pending.WaitAsync(_timeout, TestContext.Current.CancellationToken)));
		Assert.True(child.HasExited);
		Assert.Equal("shell-stdout" + Environment.NewLine, terminal.Output.ToString());
		Assert.Equal("shell-stderr" + Environment.NewLine, terminal.Error.ToString());
	}

	[Fact]
	[Trait("Category", "Integration")]
	public async Task ExecuteAsync_Timeout_ReturnsMinusOneWithoutTerminatingChild()
	{
		Assert.SkipUnless(OperatingSystem.IsWindows(), "ShellCommand invokes Windows cmd.exe.");
		using var terminal = new TerminalScope();
		await using var child = new ShellProcess();
		var pending = await child.ExecuteAsync(terminal.Instance, "5s", TestContext.Current.CancellationToken, TestContext.Current.CancellationToken);
		await child.WaitForChildAsync(TestContext.Current.CancellationToken);

		Assert.False(child.HasExited);
		Assert.Equal(-1, Assert.IsType<int>(await pending.WaitAsync(_timeout, TestContext.Current.CancellationToken)));
		Assert.False(child.HasExited);
	}

	[Fact]
	[Trait("Category", "Integration")]
	public async Task ExecuteAsync_Cancellation_PreservesTokenWithoutTerminatingChild()
	{
		Assert.SkipUnless(OperatingSystem.IsWindows(), "ShellCommand invokes Windows cmd.exe.");
		using var terminal = new TerminalScope();
		await using var child = new ShellProcess();
		using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		var pending = await child.ExecuteAsync(terminal.Instance, "30s", cancellation.Token, TestContext.Current.CancellationToken);
		await child.WaitForChildAsync(TestContext.Current.CancellationToken);

		Assert.False(pending.IsCompleted);
		cancellation.Cancel();
		var failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending.WaitAsync(_timeout, TestContext.Current.CancellationToken));
		Assert.Equal(cancellation.Token, failure.CancellationToken);
		Assert.False(child.HasExited);
	}

	private sealed class TerminalScope : IDisposable
	{
		private readonly TextWriter _output = Terminal.Console.Output;
		private readonly TextWriter _error = Terminal.Console.Error;
		public readonly StringWriter Output = new();
		public readonly StringWriter Error = new();

		public TerminalScope()
		{
			Terminal.Console.Output = this.Output;
			Terminal.Console.Error = this.Error;
		}

		public ITerminal Instance => Terminal.Console;

		public void Dispose()
		{
			Terminal.Console.Output = _output;
			Terminal.Console.Error = _error;
			this.Output.Dispose();
			this.Error.Dispose();
		}
	}
}
