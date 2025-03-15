using System;
using System.Linq;

using Xunit;

namespace Zongsoft.Components.Tests;

public class IdentifierTest
{
	[Fact]
	public void TestGeneric()
	{
		var identifier = new Identifier<uint>(typeof(IdentifierTest), 100u);
		Assert.True(identifier.HasValue);
		Assert.False(identifier.IsEmpty);
		Assert.Equal(typeof(IdentifierTest), identifier.Type);
		Assert.Null(identifier.Label);
		Assert.Null(identifier.Description);
		Assert.Equal(100u, identifier.Value);
		Assert.Equal(100u, (uint)identifier);

		Assert.True(identifier.Validate(out var value));
		Assert.Equal(100U, value);
		Assert.True(identifier.Validate<IdentifierTest>(out value));
		Assert.Equal(100U, value);
		Assert.False(identifier.Validate<Exception>(out value));
	}

	[Fact]
	public void TestGeneral()
	{
		var identifier = new Identifier(typeof(IdentifierTest), 100U);
		Assert.True(identifier.HasValue);
		Assert.False(identifier.IsEmpty);
		Assert.Equal(typeof(IdentifierTest), identifier.Type);
		Assert.Null(identifier.Label);
		Assert.Null(identifier.Description);
		Assert.Equal(100u, identifier.Value);
		Assert.Equal(100u, (uint)identifier);

		Assert.True(identifier.Validate(out uint value));
		Assert.Equal(100U, value);
		Assert.True(identifier.Validate<IdentifierTest, uint>(out value));
		Assert.Equal(100U, value);
		Assert.False(identifier.Validate<Exception, uint>(out value));
	}

	[Fact]
	public void TestConvertible()
	{
		var identifier = new Identifier(typeof(IdentifierTest), 100);
		Assert.Equal(100, (int)identifier);
		Assert.Equal(100U, (uint)identifier);
		Assert.Equal(100, (byte)identifier);
		Assert.Equal(100, (sbyte)identifier);
		Assert.Equal(100, (short)identifier);
		Assert.Equal(100, (ushort)identifier);
		Assert.Equal(100L, (long)identifier);
		Assert.Equal(100UL, (ulong)identifier);
		Assert.Equal(100f, (float)identifier);
		Assert.Equal(100d, (double)identifier);
		Assert.Equal(100m, (decimal)identifier);
		Assert.Equal("100", (string)identifier);
	}
}
