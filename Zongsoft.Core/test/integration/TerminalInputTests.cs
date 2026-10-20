using System.Threading.Tasks;

using Xunit;

namespace Zongsoft.Terminals.IntegrationTests;

public class TerminalInputTests
{
	[Fact]
	[Trait("Category", "Integration")]
	public Task CancelledInputDoesNotPreventProcessExit() => TerminalProcess.RunAsync("exit");

	[Fact]
	[Trait("Category", "Integration")]
	public Task CancelledInputIsResumedByTheNextWaiter() => TerminalProcess.RunAsync("resume");

	[Fact]
	[Trait("Category", "Integration")]
	public Task StandardInputIsSharedAndIgnoresDisposal() => TerminalProcess.RunAsync("shared");

	[Fact]
	[Trait("Category", "Integration")]
	public Task StandardInputReadsUnicodeBlankLinesAndEof() => TerminalProcess.RunAsync("text");

	[Fact]
	[Trait("Category", "Integration")]
	public Task RepeatedCancellationKeepsOneReadForAsyncMethodsAndRejectsSyncReads() => TerminalProcess.RunAsync("controlled-line");

	[Fact]
	[Trait("Category", "Integration")]
	public Task StandardInputRetainsFailureWithoutReadingAgain() => TerminalProcess.RunAsync("controlled-fault");
}
