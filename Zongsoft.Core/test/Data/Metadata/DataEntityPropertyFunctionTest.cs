using System;

using Xunit;

namespace Zongsoft.Data.Metadata.Tests;

public class DataEntityPropertyFunctionTest
{
	[Fact]
	public void TestGet()
	{
		var function = DataEntityPropertyFunction.Get(null);
		Assert.Null(function);
		function = DataEntityPropertyFunction.Get(string.Empty);
		Assert.Null(function);

		function = DataEntityPropertyFunction.Get("now");
		Assert.NotNull(function);
		Assert.Equal("now", function.Name, true);
		Assert.False(function.HasArguments);

		function = DataEntityPropertyFunction.Get("now()");
		Assert.NotNull(function);
		Assert.Equal("now", function.Name, true);
		Assert.False(function.HasArguments);

		function = DataEntityPropertyFunction.Get("Now ( utc ) ");
		Assert.NotNull(function);
		Assert.Equal("now", function.Name, true);
		Assert.True(function.HasArguments);
		Assert.Single(function.Arguments);
		Assert.Equal("utc", function.Arguments[0], true);
	}
}
