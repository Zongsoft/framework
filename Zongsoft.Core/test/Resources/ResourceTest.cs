using System;
using System.Collections.Generic;

using Xunit;

using Zongsoft.Common;
using Zongsoft.Tests;

namespace Zongsoft.Resources.Tests;

public class ResourceTest
{
	[Fact]
	public void Test()
	{
		var resource = ResourceAssistant.GetResource<Gender>();
		Assert.NotNull(resource);

		var text = resource.GetString("Gender.Male");
		Assert.NotNull(text);
		Assert.Equal("男士", text);

		text = resource.GetString("Gender.Female");
		Assert.NotNull(text);
		Assert.Equal("女士", text);

		text = EnumUtility.GetEnumDescription(Gender.Male);
		Assert.NotNull(text);
		Assert.Equal("男士", text);

		text = EnumUtility.GetEnumDescription(Gender.Female);
		Assert.NotNull(text);
		Assert.Equal("女士", text);
	}
}
