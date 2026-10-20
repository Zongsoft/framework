using System.IO;

using Xunit;

namespace Zongsoft.Terminals.Tests;

[Collection(ConsoleExecutorCollection.Name)]
public sealed class TerminalInputTest
{
	[Fact]
	public void StandardInputIsSharedAcrossDisposeAndClose()
	{
		var terminal = Terminal.Console;
		var previous = terminal.Input;

		try
		{
			terminal.Input = null;
			TextReader input = terminal.Input;

			Assert.Same(input, terminal.Input);

			input.Dispose();
			input.Close();
			terminal.Input = null;

			Assert.Same(input, terminal.Input);
		}
		finally { terminal.Input = previous; }
	}
}
