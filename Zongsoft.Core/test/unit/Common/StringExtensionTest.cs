using System;
using System.Linq;

using Xunit;

namespace Zongsoft.Common.Tests;

public class StringExtensionTest
{
	[Fact]
	public void TestRemoveAny()
	{
		const string TEXT = @"Read^me??.txt";

		Assert.Equal("Read^me.txt", StringExtension.RemoveAny(TEXT, '?'));
		Assert.Equal("Readme.txt", StringExtension.RemoveAny(TEXT, ['?', '^']));
	}

	[Fact]
	public void TestTrimString()
	{
		Assert.Equal("ContentSuffix", StringExtension.Trim("PrefixPrefixContentSuffix", "Prefix"));
		Assert.Equal("PrefixPrefixContent", StringExtension.Trim("PrefixPrefixContentSuffix", "Suffix"));
		Assert.Equal("Content", StringExtension.Trim("PrefixPrefixContentSuffix", "Prefix", "Suffix"));
	}

	[Fact]
	public void TestIsDigits()
	{
		string digits;

		Assert.True(StringExtension.IsDigits("123", out digits));
		Assert.Equal("123", digits);

		Assert.True(StringExtension.IsDigits(" \t123   ", out digits));
		Assert.Equal("123", digits);

		Assert.False(StringExtension.IsDigits("1 23"));
		Assert.False(StringExtension.IsDigits("1#23"));
		Assert.False(StringExtension.IsDigits("$123"));
	}

	[Fact]
	public void TestSlice()
	{
		var parts = StringExtension.Slice("a - b --  c  ", '-').ToArray();

		Assert.NotEmpty(parts);
		Assert.Equal(3, parts.Length);
		Assert.Equal("a", parts[0]);
		Assert.Equal("b", parts[1]);
		Assert.Equal("c", parts[2]);

		var hasColon = false;
		parts = StringExtension.Slice("issue-100-park:10001-1", chr => hasColon ? false : !(hasColon = chr == ':') && chr == '-').ToArray();

		Assert.NotEmpty(parts);
		Assert.Equal(3, parts.Length);
		Assert.Equal("issue", parts[0]);
		Assert.Equal("100", parts[1]);
		Assert.Equal("park:10001-1", parts[2]);
	}
}
