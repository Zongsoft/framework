using System;
using System.Linq;

using Xunit;

namespace Zongsoft.Components.Tests;

public class CommandLineTest
{
	[Fact]
	public void TestParse()
	{
		var text = " cmdlet -abc --option1 --option2:value2 arg1 arg2 ' arg3-:)arg3 ' ";
		var result = CommandLine.Parse(text).ToArray();

		Assert.NotEmpty(result);
		Assert.Single(result);
		Assert.Equal("cmdlet", result[0].Name);
		Assert.Equal(3, result[0].Options.Count);
		Assert.Equal(3, result[0].Arguments.Count);
		Assert.Equal("abc", result[0].Options[0].Name);
		Assert.Null(result[0].Options[0].Value);
		Assert.Equal(CommandLine.CommandOptionKind.Short, result[0].Options[0].Kind);
		Assert.Equal("option1", result[0].Options[1].Name);
		Assert.Null(result[0].Options[1].Value);
		Assert.Equal(CommandLine.CommandOptionKind.Fully, result[0].Options[1].Kind);
		Assert.Equal("option2", result[0].Options[2].Name);
		Assert.Equal("value2", result[0].Options[2].Value);
		Assert.Equal(CommandLine.CommandOptionKind.Fully, result[0].Options[2].Kind);
		Assert.Equal("arg1", result[0].Arguments[0]);
		Assert.Equal("arg2", result[0].Arguments[1]);
		Assert.Equal(" arg3-:)arg3 ", result[0].Arguments[2]);
	}
}
