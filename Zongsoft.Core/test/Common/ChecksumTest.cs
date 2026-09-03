using System;
using System.Collections.Generic;
using System.Security.Cryptography;

using Xunit;

namespace Zongsoft.Common.Tests;

public class ChecksumTest
{
	private readonly byte[] _data = Randomizer.Generate(1024);

	[Fact]
	public void Equality_IgnoresAlgorithmCaseAndUsesCompatibleHashCode()
	{
		var value = SHA256.HashData(_data);
		var first = new Checksum("sha256", value);
		var second = new Checksum("SHA256", value);
		var checksums = new Dictionary<Checksum, string> { [first] = "checksum" };

		Assert.Equal(first, second);
		Assert.Equal(first.GetHashCode(), second.GetHashCode());
		Assert.Equal("checksum", checksums[second]);
	}

	[Fact]
	public void ConstructorAndValue_DoNotExposeMutableState()
	{
		var value = SHA256.HashData(_data);
		var expected = (byte[])value.Clone();
		var checksum = new Checksum("SHA256", value);
		var text = checksum.ToString();
		var hashCode = checksum.GetHashCode();

		value[0] ^= byte.MaxValue;
		var copy = checksum.Value.ToArray();
		copy[1] ^= byte.MaxValue;

		Assert.True(checksum.Value.Span.SequenceEqual(expected));
		Assert.Equal(text, checksum.ToString());
		Assert.Equal(hashCode, checksum.GetHashCode());
		Assert.Equal(new Checksum("SHA256", expected), checksum);
	}

	[Fact]
	public void Constructor()
	{
		var checksum = new Checksum();
		Assert.True(checksum.IsEmpty);
		Assert.True(checksum.Value.IsEmpty);
		Assert.Equal(default, checksum);

		checksum = new Checksum(MD5.HashData(_data));
		Assert.False(checksum.IsEmpty);
		Assert.False(checksum.Value.IsEmpty);
		Assert.Equal("MD5", checksum.Name, true);
		Assert.Equal(MD5.HashSizeInBytes, checksum.Value.Length);
		Assert.NotEmpty(checksum.ToString());
		Assert.StartsWith("MD5:", checksum.ToString());

		checksum = new Checksum(SHA1.HashData(_data));
		Assert.False(checksum.IsEmpty);
		Assert.False(checksum.Value.IsEmpty);
		Assert.Equal("SHA1", checksum.Name, true);
		Assert.Equal(SHA1.HashSizeInBytes, checksum.Value.Length);
		Assert.NotEmpty(checksum.ToString());
		Assert.StartsWith("SHA1:", checksum.ToString());
	}

	[Fact]
	public void Compute()
	{
		var checksum1 = Checksum.Compute("SHA256", _data);
		Assert.False(checksum1.IsEmpty);
		Assert.False(checksum1.Value.IsEmpty);
		Assert.Equal("SHA256", checksum1.Name, true);
		Assert.Equal(SHA256.HashSizeInBytes, checksum1.Value.Length);
		Assert.NotEmpty(checksum1.ToString());
		Assert.StartsWith("SHA256:", checksum1.ToString());

		var checksum2 = new Checksum(SHA256.HashData(_data));
		Assert.False(checksum2.IsEmpty);
		Assert.False(checksum2.Value.IsEmpty);
		Assert.Equal("SHA256", checksum2.Name, true);
		Assert.Equal(SHA256.HashSizeInBytes, checksum2.Value.Length);
		Assert.NotEmpty(checksum2.ToString());
		Assert.StartsWith("SHA256:", checksum2.ToString());

		Assert.Equal(checksum1.Name, checksum2.Name, true);
		Assert.True(checksum1.Value.Span.SequenceEqual(checksum2.Value.Span));
		Assert.Equal(checksum1, checksum2);
	}

	[Fact]
	public void Parse()
	{
		var checksum = Checksum.Compute("SHA512", _data);
		Assert.False(checksum.IsEmpty);
		Assert.False(checksum.Value.IsEmpty);
		Assert.Equal("SHA512", checksum.Name, true);
		Assert.Equal(SHA512.HashSizeInBytes, checksum.Value.Length);
		Assert.NotEmpty(checksum.ToString());
		Assert.StartsWith("SHA512:", checksum.ToString());

		var result = Checksum.Parse(checksum.ToString());
		Assert.False(result.IsEmpty);
		Assert.Equal(checksum, result);
	}

	[Fact]
	public void Verify()
	{
		var checksum = Checksum.Compute("SHA3-512", _data);
		Assert.False(checksum.IsEmpty);
		Assert.False(checksum.Value.IsEmpty);

		var data = new byte[_data.Length];
		Array.Copy(_data, data, data.Length);

		Assert.True(checksum.Verify(data));
		Assert.True(checksum.Verify(_data));
		Assert.False(checksum.Verify(Randomizer.Generate(512)));
		Assert.False(checksum.Verify(Randomizer.Generate(data.Length)));
	}

	[Fact]
	public void Convert()
	{
		var checksum = Checksum.Compute("SHA384", _data);
		Assert.False(checksum.IsEmpty);

		var result = Common.Convert.ConvertValue<Checksum>(checksum.ToString());
		Assert.False(result.IsEmpty);
		Assert.Equal(checksum, result);

		var json = Serialization.Serializer.Json.Serialize(checksum);
		Assert.NotNull(json);
		Assert.NotEmpty(json);

		result = Serialization.Serializer.Json.Deserialize<Checksum>(json);
		Assert.False(result.IsEmpty);
		Assert.Equal(checksum, result);
	}
}
