using System;

using Xunit;

namespace Zongsoft.Services.Tests;

public class ApplicationIdentifierTest
{
	[Fact]
	public void TryParse()
	{
		Assert.False(ApplicationIdentifier.TryParse("", out _));
		Assert.False(ApplicationIdentifier.TryParse(" ", out _));
		Assert.False(ApplicationIdentifier.TryParse("\t", out _));

		Assert.True(ApplicationIdentifier.TryParse("name:edition@1.0.0", out var identifier));
		Assert.Equal("name", identifier.Name);
		Assert.Equal("edition", identifier.Edition);
		Assert.Equal(new Version(1, 0, 0), identifier.Version);
		Assert.Equal("name(edition)@1.0.0", identifier.ToString());

		Assert.True(ApplicationIdentifier.TryParse("name(edition)@1.1.0", out identifier));
		Assert.Equal("name", identifier.Name);
		Assert.Equal("edition", identifier.Edition);
		Assert.Equal(new Version(1, 1, 0), identifier.Version);
		Assert.Equal("name(edition)@1.1.0", identifier.ToString());

		Assert.True(ApplicationIdentifier.TryParse("name:edition", out identifier));
		Assert.Equal("name", identifier.Name);
		Assert.Equal("edition", identifier.Edition);
		Assert.Null(identifier.Version);
		Assert.Equal("name(edition)", identifier.ToString());

		Assert.True(ApplicationIdentifier.TryParse("name(edition)", out identifier));
		Assert.Equal("name", identifier.Name);
		Assert.Equal("edition", identifier.Edition);
		Assert.Null(identifier.Version);
		Assert.Equal("name(edition)", identifier.ToString());

		Assert.True(ApplicationIdentifier.TryParse("name", out identifier));
		Assert.Equal("name", identifier.Name);
		Assert.Null(identifier.Edition);
		Assert.Null(identifier.Version);
		Assert.Equal("name", identifier.ToString());

		Assert.True(ApplicationIdentifier.TryParse("name", out identifier));
		Assert.Equal("name", identifier.Name);
		Assert.Null(identifier.Edition);
		Assert.Null(identifier.Version);
		Assert.Equal("name", identifier.ToString());

		Assert.True(ApplicationIdentifier.TryParse("name@1.2.3.4", out identifier));
		Assert.Equal("name", identifier.Name);
		Assert.Null(identifier.Edition);
		Assert.Equal(new Version(1, 2, 3, 4), identifier.Version);
		Assert.Equal("name@1.2.3.4", identifier.ToString());

		Assert.True(ApplicationIdentifier.TryParse("name@1.2.3.4", out identifier));
		Assert.Equal("name", identifier.Name);
		Assert.Null(identifier.Edition);
		Assert.Equal(new Version(1, 2, 3, 4), identifier.Version);
		Assert.Equal("name@1.2.3.4", identifier.ToString());
	}
}
