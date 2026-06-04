using System;
using System.ComponentModel;

using Xunit;

namespace Zongsoft.Versioning;

public class VersionTest
{
	[Fact]
	public void TestConstructors()
	{
		var version = new Version(1, 2, 3, " alpha.1 ", " build.5 ");

		Assert.Equal(1, version.Major);
		Assert.Equal(2, version.Minor);
		Assert.Equal(3, version.Patch);
		Assert.Equal("alpha.1", version.Label);
		Assert.Equal("build.5", version.Extra);
		Assert.True(version.HasLabel());
		Assert.True(version.HasExtra());

		Assert.Throws<ArgumentOutOfRangeException>(() => new Version(-1, 0, 0));
		Assert.Throws<ArgumentOutOfRangeException>(() => new Version(0, -1, 0));
		Assert.Throws<ArgumentOutOfRangeException>(() => new Version(0, 0, -1));
	}

	[Theory]
	[InlineData("1.2.3", 1, 2, 3, null, null)]
	[InlineData("1.2.3-alpha", 1, 2, 3, "alpha", null)]
	[InlineData("1.2.3-alpha.1", 1, 2, 3, "alpha.1", null)]
	[InlineData("1.2.3+build.01", 1, 2, 3, null, "build.01")]
	[InlineData("1.2.3+build-01", 1, 2, 3, null, "build-01")]
	[InlineData("1.2.3-alpha.1+build.01", 1, 2, 3, "alpha.1", "build.01")]
	public void TestParse(string text, int major, int minor, int patch, string label, string extra)
	{
		Assert.True(Version.TryParse(text, out var result));
		Assert.Equal(major, result.Major);
		Assert.Equal(minor, result.Minor);
		Assert.Equal(patch, result.Patch);
		Assert.Equal(label, result.Label);
		Assert.Equal(extra, result.Extra);

		Assert.Equal(result, Version.Parse(text));
		Assert.Equal(result, Version.Parse(text, null));
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData(" ")]
	[InlineData("1")]
	[InlineData("1.2")]
	[InlineData("1.2.3.4")]
	[InlineData("01.2.3")]
	[InlineData("1.02.3")]
	[InlineData("1.2.03")]
	[InlineData("1.2.3-")]
	[InlineData("1.2.3+")]
	[InlineData("1.2.3-alpha..1")]
	[InlineData("1.2.3-alpha_1")]
	[InlineData("1.2.3-01")]
	[InlineData("1.2.3+build..1")]
	[InlineData("1.2.3+build+1")]
	public void TestTryParseFailed(string text)
	{
		Assert.False(Version.TryParse(text, out var result));
		Assert.Null(result);

		Assert.Throws<FormatException>(() => Version.Parse(text));
	}

	[Fact]
	public void TestHasLabelAndExtra()
	{
		var version = new Version(1, 0, 0, "preview", "commit");

		Assert.True(version.HasLabel(out var label));
		Assert.Equal("preview", label);
		Assert.True(version.HasExtra(out var extra));
		Assert.Equal("commit", extra);

		version = new Version(1, 0, 0);

		Assert.False(version.HasLabel(out label));
		Assert.Null(label);
		Assert.False(version.HasLabel());
		Assert.False(version.HasExtra(out extra));
		Assert.Null(extra);
		Assert.False(version.HasExtra());
	}

	[Fact]
	public void TestFormat()
	{
		var version = new Version(1, 2, 3, "alpha.1", "build.5");

		Assert.Equal("1.2.3-alpha.1", version.ToString());
		Assert.Equal("1.2.3-alpha.1", version.ToString("N"));
		Assert.Equal("1.2.3-alpha.1+build.5", version.ToString("F"));
		Assert.Equal("1.2.3", version.ToString("V"));
		Assert.Equal("alpha.1", version.ToString("R"));
		Assert.Equal("build.5", version.ToString("M"));
		Assert.Equal("1.2.3.0", version.ToString("x.y.z.r"));
	}

	[Fact]
	public void TestEqualityIgnoresExtra()
	{
		var left = new Version(1, 2, 3, "alpha", "build.1");
		var right = new Version(1, 2, 3, "ALPHA", "build.2");
		var other = new Version(1, 2, 3, "beta", "build.1");

		Assert.True(left.Equals(right));
		Assert.True(left.Equals((object)right));
		Assert.True(left == right);
		Assert.False(left != right);
		Assert.False(left.Equals(other));
		Assert.Equal(left.GetHashCode(), right.GetHashCode());
	}

	[Fact]
	public void TestCompareTo()
	{
		Assert.Equal(0, new Version(1, 2, 3).CompareTo(new Version(1, 2, 3)));
		Assert.True(new Version(1, 2, 3).CompareTo(new Version(2, 0, 0)) < 0);
		Assert.True(new Version(1, 2, 3).CompareTo(new Version(1, 3, 0)) < 0);
		Assert.True(new Version(1, 2, 3).CompareTo(new Version(1, 2, 4)) < 0);
		Assert.True(new Version(1, 2, 3, "alpha").CompareTo(new Version(1, 2, 3)) < 0);
		Assert.True(new Version(1, 2, 3, "alpha").CompareTo(new Version(1, 2, 3, "alpha.1")) < 0);
		Assert.True(new Version(1, 2, 3, "alpha.2").CompareTo(new Version(1, 2, 3, "alpha.10")) < 0);
		Assert.True(new Version(1, 2, 3, "alpha.10").CompareTo(new Version(1, 2, 3, "alpha.beta")) < 0);
		Assert.True(new Version(1, 2, 3, "beta").CompareTo(new Version(1, 2, 3, "alpha")) > 0);
	}

	[Fact]
	public void TestComparisonOperators()
	{
		var lower = new Version(1, 0, 0, "alpha");
		var same = new Version(1, 0, 0, "ALPHA");
		var higher = new Version(1, 0, 0);

		Assert.True(lower < higher);
		Assert.True(lower <= higher);
		Assert.True(lower <= same);
		Assert.True(higher > lower);
		Assert.True(higher >= lower);
		Assert.True(higher >= same);
	}

	[Fact]
	public void TestTypeConverter()
	{
		var converter = TypeDescriptor.GetConverter(typeof(Version));
		var version = new Version(1, 2, 3, "alpha", "build");

		Assert.True(converter.CanConvertFrom(typeof(string)));
		Assert.True(converter.CanConvertTo(typeof(string)));
		Assert.Equal(version, converter.ConvertFrom("1.2.3-alpha+build"));
		Assert.Equal("1.2.3-alpha", converter.ConvertTo(version, typeof(string)));
		Assert.True(converter.IsValid(version));
		Assert.True(converter.IsValid("1.2.3-alpha"));
		Assert.False(converter.IsValid("1.2"));
	}

	[Fact]
	public void TestJsonConverter()
	{
		var version = new Version(1, 2, 3, "alpha", "build");
		var json = System.Text.Json.JsonSerializer.Serialize(version);

		Assert.Equal("1.2.3-alpha+build", System.Text.Json.JsonSerializer.Deserialize<string>(json));
		Assert.Equal(version, System.Text.Json.JsonSerializer.Deserialize<Version>("\"1.2.3-alpha+build\""));
		Assert.Null(System.Text.Json.JsonSerializer.Deserialize<Version>("null"));
	}
}
