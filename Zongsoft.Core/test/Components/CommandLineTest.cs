using System;

using Xunit;

namespace Zongsoft.Components.Tests;

public class CommandLineTest
{
	[Fact]
	public void TestParse()
	{
		var text = " cmdlet -abc --option1 --option2:value2 arg1 arg2 ' arg3-:)arg3 ' ";
		var cmdlets = CommandLine.Parse(text);

		Assert.NotEmpty(cmdlets);
		Assert.Single(cmdlets);
		Assert.Equal("cmdlet", cmdlets[0].Name);
		Assert.Equal(3, cmdlets[0].Options.Count);
		Assert.Equal(3, cmdlets[0].Arguments.Count);
		Assert.Equal("abc", cmdlets[0].Options[0].Name);
		Assert.Null(cmdlets[0].Options[0].Value);
		Assert.Equal(CommandLine.CmdletOptionKind.Short, cmdlets[0].Options[0].Kind);
		Assert.Equal("option1", cmdlets[0].Options[1].Name);
		Assert.Null(cmdlets[0].Options[1].Value);
		Assert.Equal(CommandLine.CmdletOptionKind.Fully, cmdlets[0].Options[1].Kind);
		Assert.Equal("option2", cmdlets[0].Options[2].Name);
		Assert.Equal("value2", cmdlets[0].Options[2].Value);
		Assert.Equal(CommandLine.CmdletOptionKind.Fully, cmdlets[0].Options[2].Kind);
		Assert.Equal("arg1", cmdlets[0].Arguments[0]);
		Assert.Equal("arg2", cmdlets[0].Arguments[1]);
		Assert.Equal(" arg3-:)arg3 ", cmdlets[0].Arguments[2]);
	}
}
