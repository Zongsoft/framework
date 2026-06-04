using System;
using System.ComponentModel;

using Xunit;

namespace Zongsoft.Versioning;

public class VersionNumberTest
{
	[Fact]
	public void TestConstructors()
	{
		var version = new Version.Number(1, 2, 3, 4);
		Assert.Equal(1, version.Major);
		Assert.Equal(2, version.Minor);
		Assert.Equal(3, version.Patch);
		Assert.Equal(4, version.Revision);
		Assert.False(version.IsZero);

		version = new Version.Number(0x0001_0002_0003_0004UL);
		Assert.Equal(1, version.Major);
		Assert.Equal(2, version.Minor);
		Assert.Equal(3, version.Patch);
		Assert.Equal(4, version.Revision);

		version = new Version.Number(-1);
		Assert.Equal(ushort.MaxValue, version.Major);
		Assert.Equal(ushort.MaxValue, version.Minor);
		Assert.Equal(ushort.MaxValue, version.Patch);
		Assert.Equal(ushort.MaxValue, version.Revision);

		Assert.True(default(Version.Number).IsZero);
	}

	[Theory]
	[InlineData("1.2", 1, 2, 0, 0)]
	[InlineData("1.2.3", 1, 2, 3, 0)]
	[InlineData("1.2.3.4", 1, 2, 3, 4)]
	[InlineData("65535.65535.65535.65535", 65535, 65535, 65535, 65535)]
	public void TestParse(string text, int major, int minor, int patch, int revision)
	{
		Assert.True(Version.Number.TryParse(text, out var result));
		Assert.Equal((ushort)major, result.Major);
		Assert.Equal((ushort)minor, result.Minor);
		Assert.Equal((ushort)patch, result.Patch);
		Assert.Equal((ushort)revision, result.Revision);

		Assert.Equal(result, Version.Number.Parse(text));
		Assert.Equal(result, Version.Number.Parse(text, null));
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
		Assert.False(Version.Number.TryParse(text, out var result));
		Assert.True(result.IsZero);

		Assert.Throws<FormatException>(() => Version.Number.Parse(text));
	}

	[Fact]
	public void TestEquality()
	{
		var left = new Version.Number(1, 2, 3, 4);
		var right = new Version.Number(1, 2, 3, 4);
		var other = new Version.Number(1, 2, 3, 5);

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
		Assert.Equal(0, new Version.Number(1, 2, 3, 4).CompareTo(new Version.Number(1, 2, 3, 4)));
		Assert.True(new Version.Number(1, 2, 3, 4).CompareTo(new Version.Number(2, 0, 0, 0)) < 0);
		Assert.True(new Version.Number(1, 2, 3, 4).CompareTo(new Version.Number(1, 3, 0, 0)) < 0);
		Assert.True(new Version.Number(1, 2, 3, 4).CompareTo(new Version.Number(1, 2, 4, 0)) < 0);
		Assert.True(new Version.Number(1, 2, 3, 4).CompareTo(new Version.Number(1, 2, 3, 5)) < 0);
		Assert.True(new Version.Number(2, 0, 0, 0).CompareTo(new Version.Number(1, 2, 3, 4)) > 0);
	}

	[Fact]
	public void TestComparisonOperators()
	{
		var lower = new Version.Number(1, 2, 3, 4);
		var same = new Version.Number(1, 2, 3, 4);
		var higher = new Version.Number(1, 2, 3, 5);

		Assert.True(lower < higher);
		Assert.True(lower <= higher);
		Assert.True(lower <= same);
		Assert.True(higher > lower);
		Assert.True(higher >= lower);
		Assert.True(higher >= same);

		Assert.False(lower > higher);
		Assert.False(lower >= higher);
		Assert.False(higher < lower);
		Assert.False(higher <= lower);
	}

	[Fact]
	public void TestToString()
	{
		Assert.Equal("0.0.0", default(Version.Number).ToString());
		Assert.Equal("1.2.0", new Version.Number(1, 2).ToString());
		Assert.Equal("1.2.3", new Version.Number(1, 2, 3).ToString());
		Assert.Equal("1.2.3.4", new Version.Number(1, 2, 3, 4).ToString());
	}

	[Fact]
	public void TestJsonSerialize()
	{
		Assert.Equal("\"0.0.0\"", Serialization.Serializer.Json.Serialize(default(Version.Number)));
		Assert.Equal("\"1.2.3\"", Serialization.Serializer.Json.Serialize(new Version.Number(1, 2, 3)));
		Assert.Equal("\"1.2.3.4\"", Serialization.Serializer.Json.Serialize(new Version.Number(1, 2, 3, 4)));

		Assert.Equal("\"1.2.3.4\"", System.Text.Json.JsonSerializer.Serialize(new Version.Number(1, 2, 3, 4)));
		Assert.Equal("{\"Version\":\"1.2.3.4\"}", System.Text.Json.JsonSerializer.Serialize(new VersionEntry()
		{
			Version = new Version.Number(1, 2, 3, 4),
		}));
	}

	[Theory]
	[InlineData("\"1.2\"", 1, 2, 0, 0)]
	[InlineData("\"1.2.3\"", 1, 2, 3, 0)]
	[InlineData("\"1.2.3.4\"", 1, 2, 3, 4)]
	[InlineData("281483566841860", 1, 2, 3, 4)]
	public void TestJsonDeserialize(string json, int major, int minor, int patch, int revision)
	{
		var expected = new Version.Number((ushort)major, (ushort)minor, (ushort)patch, (ushort)revision);

		Assert.Equal(expected, Serialization.Serializer.Json.Deserialize<Version.Number>(json));
		Assert.Equal(expected, System.Text.Json.JsonSerializer.Deserialize<Version.Number>(json));
	}

	[Fact]
	public void TestJsonDeserializeObject()
	{
		const string JSON = """
		{
			"Version" : "1.2.3.4"
		}
		""";

		var result = System.Text.Json.JsonSerializer.Deserialize<VersionEntry>(JSON);
		Assert.Equal(new Version.Number(1, 2, 3, 4), result.Version);

		Assert.True(Serialization.Serializer.Json.Deserialize<Version.Number>("null").IsZero);
		Assert.Null(Serialization.Serializer.Json.Deserialize<Version.Number?>("null"));
	}

	[Fact]
	public void TestNumericConversion()
	{
		var version = new Version.Number(1, 2, 3, 4);
		const ulong Packed = 0x0001_0002_0003_0004UL;

		Assert.Equal(Packed, (ulong)version);
		Assert.Equal((long)Packed, (long)version);
		Assert.Equal(version, (Version.Number)Packed);
		Assert.Equal(version, (Version.Number)(long)Packed);

		version = new Version.Number(ushort.MaxValue, ushort.MaxValue, ushort.MaxValue, ushort.MaxValue);
		Assert.Equal(ulong.MaxValue, (ulong)version);
		Assert.Equal(-1L, (long)version);
		Assert.Equal(version, (Version.Number)(-1L));
	}

	[Fact]
	public void TestSystemVersionConversion()
	{
		Version.Number version = new System.Version(1, 2);
		Assert.Equal(new Version.Number(1, 2), version);

		version = new System.Version(1, 2, 3, 4);
		Assert.Equal(new Version.Number(1, 2, 3, 4), version);

		System.Version systemVersion = new Version.Number(1, 2, 3);
		Assert.Equal(1, systemVersion.Major);
		Assert.Equal(2, systemVersion.Minor);
		Assert.Equal(3, systemVersion.Build);
		Assert.Equal(-1, systemVersion.Revision);

		systemVersion = new Version.Number(1, 2, 3, 4);
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
		var converter = TypeDescriptor.GetConverter(typeof(Version.Number));
		var version = new Version.Number(1, 2, 3, 4);
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

	private class VersionEntry
	{
		public Version.Number Version { get; set; }
	}
}
