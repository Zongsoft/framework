using System;

using Xunit;

namespace Zongsoft.Components.Tests;

public class CommandLineTest
{
	[Fact]
	public void TestParse1()
	{
		Assert.Empty(CommandLine.Parse(""));
		Assert.Empty(CommandLine.Parse("  "));

		var cmdlets = CommandLine.Parse("cmdlet");
		Assert.NotEmpty(cmdlets);
		Assert.Single(cmdlets);
		Assert.Equal("cmdlet", cmdlets[0].Name);
		Assert.Empty(cmdlets[0].Options);
		Assert.Empty(cmdlets[0].Arguments);

		cmdlets = CommandLine.Parse("/");
		Assert.NotEmpty(cmdlets);
		Assert.Single(cmdlets);
		Assert.Equal("/", cmdlets[0].Name);
		Assert.Empty(cmdlets[0].Options);
		Assert.Empty(cmdlets[0].Arguments);

		cmdlets = CommandLine.Parse(".");
		Assert.NotEmpty(cmdlets);
		Assert.Single(cmdlets);
		Assert.Equal(".", cmdlets[0].Name);
		Assert.Empty(cmdlets[0].Options);
		Assert.Empty(cmdlets[0].Arguments);

		cmdlets = CommandLine.Parse("..");
		Assert.NotEmpty(cmdlets);
		Assert.Single(cmdlets);
		Assert.Equal("..", cmdlets[0].Name);
		Assert.Empty(cmdlets[0].Options);
		Assert.Empty(cmdlets[0].Arguments);

		cmdlets = CommandLine.Parse("/cmdlet");
		Assert.NotEmpty(cmdlets);
		Assert.Single(cmdlets);
		Assert.Equal("/cmdlet", cmdlets[0].Name);
		Assert.Empty(cmdlets[0].Options);
		Assert.Empty(cmdlets[0].Arguments);

		cmdlets = CommandLine.Parse("./cmdlet");
		Assert.NotEmpty(cmdlets);
		Assert.Single(cmdlets);
		Assert.Equal("./cmdlet", cmdlets[0].Name);
		Assert.Empty(cmdlets[0].Options);
		Assert.Empty(cmdlets[0].Arguments);

		cmdlets = CommandLine.Parse("../cmdlet");
		Assert.NotEmpty(cmdlets);
		Assert.Single(cmdlets);
		Assert.Equal("../cmdlet", cmdlets[0].Name);
		Assert.Empty(cmdlets[0].Options);
		Assert.Empty(cmdlets[0].Arguments);

		cmdlets = CommandLine.Parse("cmdlet arg");
		Assert.NotEmpty(cmdlets);
		Assert.Single(cmdlets);
		Assert.Equal("cmdlet", cmdlets[0].Name);
		Assert.Empty(cmdlets[0].Options);
		Assert.Single(cmdlets[0].Arguments);
		Assert.Equal("arg", cmdlets[0].Arguments[0]);

		cmdlets = CommandLine.Parse("cmdlet -opt --option1 --option2:value2 arg1 arg2");
		Assert.NotEmpty(cmdlets);
		Assert.Single(cmdlets);
		Assert.Equal("cmdlet", cmdlets[0].Name);
		Assert.Equal(3, cmdlets[0].Options.Count);
		Assert.Equal(2, cmdlets[0].Arguments.Count);

		Assert.Equal(CommandLine.CmdletOptionKind.Short, cmdlets[0].Options[0].Kind);
		Assert.Equal("opt", cmdlets[0].Options[0].Name);
		Assert.Null(cmdlets[0].Options[0].Value);

		Assert.Equal(CommandLine.CmdletOptionKind.Fully, cmdlets[0].Options[1].Kind);
		Assert.Equal("option1", cmdlets[0].Options[1].Name);
		Assert.Null(cmdlets[0].Options[1].Value);

		Assert.Equal(CommandLine.CmdletOptionKind.Fully, cmdlets[0].Options[2].Kind);
		Assert.Equal("option2", cmdlets[0].Options[2].Name);
		Assert.Equal("value2", cmdlets[0].Options[2].Value);

		Assert.Equal("arg1", cmdlets[0].Arguments[0]);
		Assert.Equal("arg2", cmdlets[0].Arguments[1]);
	}

	[Fact]
	public void TestParse2()
	{
		var cmdlets = CommandLine.Parse("cmdlet1 | cmdlet2");

		Assert.NotEmpty(cmdlets);
		Assert.Equal(2, cmdlets.Count);
		Assert.Equal("cmdlet1", cmdlets[0].Name);
		Assert.Empty(cmdlets[0].Options);
		Assert.Empty(cmdlets[0].Arguments);
		Assert.Equal("cmdlet2", cmdlets[1].Name);
		Assert.Empty(cmdlets[1].Options);
		Assert.Empty(cmdlets[1].Arguments);

		cmdlets = CommandLine.Parse("cmdlet1 -opt | cmdlet2 --opt arg1");

		Assert.NotEmpty(cmdlets);
		Assert.Equal(2, cmdlets.Count);
		Assert.Equal("cmdlet1", cmdlets[0].Name);
		Assert.Single(cmdlets[0].Options);
		Assert.Equal(CommandLine.CmdletOptionKind.Short, cmdlets[0].Options[0].Kind);
		Assert.Equal("opt", cmdlets[0].Options[0].Name);
		Assert.Null(cmdlets[0].Options[0].Value);
		Assert.Empty(cmdlets[0].Arguments);

		Assert.Single(cmdlets[1].Options);
		Assert.Equal(CommandLine.CmdletOptionKind.Fully, cmdlets[1].Options[0].Kind);
		Assert.Equal("opt", cmdlets[1].Options[0].Name);
		Assert.Null(cmdlets[1].Options[0].Value);
		Assert.Single(cmdlets[1].Arguments);
		Assert.Equal("arg1", cmdlets[1].Arguments[0]);
	}

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
