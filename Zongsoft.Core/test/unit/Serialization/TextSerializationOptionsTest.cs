using System;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Serialization.Tests;

public class TextSerializationOptionsTest
{
	[Fact]
	public void TestBuild()
	{
		var builder = new TextSerializationOptionsBuilder();
		var options = builder.Default;
		Assert.Same(options, builder.Default);

		options = builder.Indented();
		Assert.Same(options, builder.Indented());
		Assert.NotSame(options, builder.Indented(true));

		options = builder.Indented(true);
		Assert.Same(options, builder.Indented(true));
		Assert.NotSame(options, builder.Indented(false));

		options = builder.Typified();
		Assert.Same(options, builder.Typified());
		Assert.NotSame(options, builder.Typified(true));

		options = builder.Typified(true);
		Assert.Same(options, builder.Typified(true));
		Assert.NotSame(options, builder.Typified(false));

		options = builder.Pascal();
		Assert.Same(options, builder.Pascal());
		Assert.NotSame(options, builder.Pascal(true));

		options = builder.Pascal(true);
		Assert.Same(options, builder.Pascal(true));
		Assert.NotSame(options, builder.Pascal(false));

		options = builder.Camel();
		Assert.Same(options, builder.Camel());
		Assert.NotSame(options, builder.Camel(true));

		options = builder.Camel(true);
		Assert.Same(options, builder.Camel(true));
		Assert.NotSame(options, builder.Camel(false));
	}
}
