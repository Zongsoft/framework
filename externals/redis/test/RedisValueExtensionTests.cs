using System;

using StackExchange.Redis;

using Xunit;

namespace Zongsoft.Externals.Redis.Tests;

public class RedisValueExtensionTests
{
	[Fact]
	public void GetValue_Null_ReturnsRequestedTypeDefault()
	{
		var value = RedisValue.Null;

		Assert.Null(value.GetValue<string>());
		Assert.Null(value.GetValue<byte[]>());
		Assert.Null(value.GetValue<object>());
		Assert.Equal(0, value.GetValue<int>());
		Assert.Equal(0m, value.GetValue<decimal>());
	}

	[Fact]
	public void GetValue_StringObjectAndBinary_PreserveRepresentation()
	{
		RedisValue text = "Hello Redis";
		RedisValue binary = new byte[] { 0, 1, 127, 128, 255 };

		Assert.Equal("Hello Redis", text.GetValue<string>());
		Assert.IsType<string>(text.GetValue<object>());
		Assert.Equal("Hello Redis", text.GetValue<object>());
		Assert.Equal(new byte[] { 0, 1, 127, 128, 255 }, binary.GetValue<byte[]>());
	}

	[Fact]
	public void GetValue_IntegralNumbers_ReturnExactRequestedTypes()
	{
		Assert.IsType<byte>(((RedisValue)"255").GetValue<byte>());
		Assert.Equal(byte.MaxValue, ((RedisValue)"255").GetValue<byte>());
		Assert.IsType<sbyte>(((RedisValue)"-128").GetValue<sbyte>());
		Assert.Equal(sbyte.MinValue, ((RedisValue)"-128").GetValue<sbyte>());
		Assert.IsType<short>(((RedisValue)"-32768").GetValue<short>());
		Assert.Equal(short.MinValue, ((RedisValue)"-32768").GetValue<short>());
		Assert.IsType<ushort>(((RedisValue)"65535").GetValue<ushort>());
		Assert.Equal(ushort.MaxValue, ((RedisValue)"65535").GetValue<ushort>());
		Assert.IsType<int>(((RedisValue)"-2147483648").GetValue<int>());
		Assert.Equal(int.MinValue, ((RedisValue)"-2147483648").GetValue<int>());
		Assert.IsType<uint>(((RedisValue)"4294967295").GetValue<uint>());
		Assert.Equal(uint.MaxValue, ((RedisValue)"4294967295").GetValue<uint>());
		Assert.IsType<long>(((RedisValue)"-9223372036854775808").GetValue<long>());
		Assert.Equal(long.MinValue, ((RedisValue)"-9223372036854775808").GetValue<long>());
		Assert.IsType<ulong>(((RedisValue)"18446744073709551615").GetValue<ulong>());
		Assert.Equal(ulong.MaxValue, ((RedisValue)"18446744073709551615").GetValue<ulong>());
	}

	[Fact]
	public void GetValue_FloatingPointAndFallbackTypes_ReturnExactValues()
	{
		Assert.IsType<float>(((RedisValue)"1.25").GetValue<float>());
		Assert.Equal(1.25f, ((RedisValue)"1.25").GetValue<float>());
		Assert.IsType<double>(((RedisValue)"-2.5").GetValue<double>());
		Assert.Equal(-2.5d, ((RedisValue)"-2.5").GetValue<double>());
		Assert.Equal(123.45m, ((RedisValue)"123.45").GetValue<decimal>());
		Assert.True(((RedisValue)"true").GetValue<bool>());
		Assert.Equal(Guid.Parse("ab8d22bd-f9b3-4daa-8cb3-e7753529cb5a"),
			((RedisValue)"ab8d22bd-f9b3-4daa-8cb3-e7753529cb5a").GetValue<Guid>());
	}
}
