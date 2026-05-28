using System;
using System.ComponentModel;

using Xunit;

namespace Zongsoft.Components.Tests;

public class VersionTest
{
	[Fact]
	public void TestConstructors()
	{
		var version = new Version(1, 2, 3, 4);
		Assert.Equal(1, version.Major);
		Assert.Equal(2, version.Minor);
		Assert.Equal(3, version.Patch);
		Assert.Equal(4, version.Revision);
		Assert.False(version.IsZero);

		version = new Version(0x0001_0002_0003_0004UL);
		Assert.Equal(1, version.Major);
		Assert.Equal(2, version.Minor);
		Assert.Equal(3, version.Patch);
		Assert.Equal(4, version.Revision);

		version = new Version(-1);
		Assert.Equal(ushort.MaxValue, version.Major);
		Assert.Equal(ushort.MaxValue, version.Minor);
		Assert.Equal(ushort.MaxValue, version.Patch);
		Assert.Equal(ushort.MaxValue, version.Revision);

		Assert.True(default(Version).IsZero);
	}

	[Theory]
	[InlineData("1.2", 1, 2, 0, 0)]
	[InlineData("1.2.3", 1, 2, 3, 0)]
	[InlineData("1.2.3.4", 1, 2, 3, 4)]
	[InlineData("65535.65535.65535.65535", 65535, 65535, 65535, 65535)]
	public void TestParse(string text, int major, int minor, int patch, int revision)
	{
		Assert.True(Version.TryParse(text, out var result));
		Assert.Equal((ushort)major, result.Major);
		Assert.Equal((ushort)minor, result.Minor);
		Assert.Equal((ushort)patch, result.Patch);
		Assert.Equal((ushort)revision, result.Revision);

		Assert.Equal(result, Version.Parse(text));
		Assert.Equal(result, Version.Parse(text, null));
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("1")]
	[InlineData("1.2.3.4.5")]
	[InlineData("1.x")]
	[InlineData("1.2.x")]
	[InlineData("1.2.3.x")]
	[InlineData("1..2")]
	[InlineData("-1.0")]
	[InlineData("65536.0")]
	public void TestTryParseFailed(string text)
	{
		Assert.False(Version.TryParse(text, out var result));
		Assert.True(result.IsZero);

		Assert.Throws<FormatException>(() => Version.Parse(text));
	}

	[Fact]
	public void TestEquality()
	{
		var left = new Version(1, 2, 3, 4);
		var right = new Version(1, 2, 3, 4);
		var other = new Version(1, 2, 3, 5);

		Assert.True(left.Equals(right));
		Assert.True(left.Equals((object)right));
		Assert.False(left.Equals(other));
		Assert.False(left.Equals("1.2.3.4"));
		Assert.True(left == right);
		Assert.False(left != right);
		Assert.True(left != other);
		Assert.Equal(left.GetHashCode(), right.GetHashCode());
	}

	[Fact]
	public void TestCompareTo()
	{
		Assert.Equal(0, new Version(1, 2, 3, 4).CompareTo(new Version(1, 2, 3, 4)));
		Assert.True(new Version(1, 2, 3, 4).CompareTo(new Version(2, 0, 0, 0)) < 0);
		Assert.True(new Version(1, 2, 3, 4).CompareTo(new Version(1, 3, 0, 0)) < 0);
		Assert.True(new Version(1, 2, 3, 4).CompareTo(new Version(1, 2, 4, 0)) < 0);
		Assert.True(new Version(1, 2, 3, 4).CompareTo(new Version(1, 2, 3, 5)) < 0);
		Assert.True(new Version(2, 0, 0, 0).CompareTo(new Version(1, 2, 3, 4)) > 0);
	}

	[Fact]
	public void TestToString()
	{
		Assert.Equal("0.0.0", default(Version).ToString());
		Assert.Equal("1.2.0", new Version(1, 2).ToString());
		Assert.Equal("1.2.3", new Version(1, 2, 3).ToString());
		Assert.Equal("1.2.3.4", new Version(1, 2, 3, 4).ToString());
	}

	[Fact]
	public void TestNumericConversion()
	{
		var version = new Version(1, 2, 3, 4);
		const ulong Packed = 0x0001_0002_0003_0004UL;

		Assert.Equal(Packed, (ulong)version);
		Assert.Equal((long)Packed, (long)version);
		Assert.Equal(version, (Version)Packed);
		Assert.Equal(version, (Version)(long)Packed);

		version = new Version(ushort.MaxValue, ushort.MaxValue, ushort.MaxValue, ushort.MaxValue);
		Assert.Equal(ulong.MaxValue, (ulong)version);
		Assert.Equal(-1L, (long)version);
		Assert.Equal(version, (Version)(-1L));
	}

	[Fact]
	public void TestSystemVersionConversion()
	{
		Version version = new System.Version(1, 2);
		Assert.Equal(new Version(1, 2), version);

		version = new System.Version(1, 2, 3, 4);
		Assert.Equal(new Version(1, 2, 3, 4), version);

		System.Version systemVersion = new Version(1, 2, 3);
		Assert.Equal(1, systemVersion.Major);
		Assert.Equal(2, systemVersion.Minor);
		Assert.Equal(3, systemVersion.Build);
		Assert.Equal(-1, systemVersion.Revision);

		systemVersion = new Version(1, 2, 3, 4);
		Assert.Equal(1, systemVersion.Major);
		Assert.Equal(2, systemVersion.Minor);
		Assert.Equal(3, systemVersion.Build);
		Assert.Equal(4, systemVersion.Revision);

		System.Version nullVersion = null;
		version = nullVersion;
		Assert.True(version.IsZero);
	}

	[Fact]
	public void TestTypeConverter()
	{
		var converter = TypeDescriptor.GetConverter(typeof(Version));
		var version = new Version(1, 2, 3, 4);
		const ulong Packed = 0x0001_0002_0003_0004UL;

		Assert.True(converter.CanConvertFrom(typeof(string)));
		Assert.True(converter.CanConvertFrom(typeof(long)));
		Assert.True(converter.CanConvertFrom(typeof(ulong)));
		Assert.True(converter.CanConvertTo(typeof(string)));
		Assert.True(converter.CanConvertTo(typeof(long)));
		Assert.True(converter.CanConvertTo(typeof(ulong)));

		Assert.Equal(version, converter.ConvertFrom("1.2.3.4"));
		Assert.Equal(version, converter.ConvertFrom((long)Packed));
		Assert.Equal(version, converter.ConvertFrom(Packed));
		Assert.Equal("1.2.3.4", converter.ConvertTo(version, typeof(string)));
		Assert.Equal((long)Packed, converter.ConvertTo(version, typeof(long)));
		Assert.Equal(Packed, converter.ConvertTo(version, typeof(ulong)));
		Assert.True(converter.IsValid(version));
		Assert.True(converter.IsValid("1.2"));
		Assert.False(converter.IsValid("1"));
	}
}
