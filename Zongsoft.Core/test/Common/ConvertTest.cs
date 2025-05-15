using System;
using System.Collections.Generic;

using Zongsoft.Tests;

using Xunit;

namespace Zongsoft.Common.Tests;

public class ConvertTest
{
	[Fact]
	public void TestConvertValue()
	{
		Assert.Null(Zongsoft.Common.Convert.ConvertValue<int?>("", default(int?)));
		Assert.Null(Zongsoft.Common.Convert.ConvertValue<int?>("x", () => default(int?)));
		Assert.NotNull(Zongsoft.Common.Convert.ConvertValue<int?>("123", () => default(int?)));
		Assert.Equal(123, Zongsoft.Common.Convert.ConvertValue<int?>("123", () => default(int?)));

		object value = 123;
		Assert.Equal(123.0f, Zongsoft.Common.Convert.ConvertValue<float>(value));
		Assert.Equal(123.0d, Zongsoft.Common.Convert.ConvertValue<double>(value));
		Assert.Equal(123.0m, Zongsoft.Common.Convert.ConvertValue<decimal>(value));

		value = "123";
		Assert.Equal(123, Zongsoft.Common.Convert.ConvertValue<int>(value));

		Assert.Equal("100", Zongsoft.Common.Convert.ConvertValue<string>(100));
		Assert.Equal("100", Zongsoft.Common.Convert.ConvertValue<string>(100L));
		Assert.Equal("100.5", Zongsoft.Common.Convert.ConvertValue<string>(100.50));
		Assert.Equal("100.50", Zongsoft.Common.Convert.ConvertValue<string>(100.50m));

		var duration = TimeSpan.Parse("1:20:30");
		Assert.Equal(duration, Zongsoft.Common.Convert.ConvertValue<TimeSpan>("1:20:30"));
		Assert.True(TimeSpanUtility.TryParse("15S", out duration));
		Assert.Equal(duration, Zongsoft.Common.Convert.ConvertValue<TimeSpan>("15s"));
		Assert.True(TimeSpanUtility.TryParse("20m", out duration));
		Assert.Equal(duration, Zongsoft.Common.Convert.ConvertValue<TimeSpan>("20M"));
		Assert.True(TimeSpanUtility.TryParse("30h", out duration));
		Assert.Equal(duration, Zongsoft.Common.Convert.ConvertValue<TimeSpan>("30H"));
		Assert.True(TimeSpanUtility.TryParse("40D", out duration));
		Assert.Equal(duration, Zongsoft.Common.Convert.ConvertValue<TimeSpan>("40d"));

		Assert.True(TimeSpanUtility.TryParse("0.5S", out duration));
		Assert.Equal(0.5, duration.TotalSeconds);
		Assert.Equal(500d, duration.TotalMilliseconds);
		Assert.Equal(duration, Zongsoft.Common.Convert.ConvertValue<TimeSpan>("0.5s"));

		Assert.Equal(Gender.Male, Zongsoft.Common.Convert.ConvertValue("male", typeof(Gender)));
		Assert.Equal(Gender.Male, Zongsoft.Common.Convert.ConvertValue("Male", typeof(Gender)));
		Assert.Equal(Gender.Female, Zongsoft.Common.Convert.ConvertValue("female", typeof(Gender)));
		Assert.Equal(Gender.Female, Zongsoft.Common.Convert.ConvertValue("Female", typeof(Gender)));

		Assert.Equal(Gender.Male, Zongsoft.Common.Convert.ConvertValue(1, typeof(Gender)));
		Assert.Equal(Gender.Male, Zongsoft.Common.Convert.ConvertValue("1", typeof(Gender)));
		Assert.Equal(Gender.Female, Zongsoft.Common.Convert.ConvertValue(0, typeof(Gender)));
		Assert.Equal(Gender.Female, Zongsoft.Common.Convert.ConvertValue("0", typeof(Gender)));

		//根据枚举项的 AliasAttribute 值来解析
		Assert.Equal(Gender.Male, Zongsoft.Common.Convert.ConvertValue<Gender>("M"));
		Assert.Equal(Gender.Female, Zongsoft.Common.Convert.ConvertValue<Gender>("F"));
	}

	[Fact]
	public void TestToHexString()
	{
		var source = new byte[16];

		for(int i = 0; i < source.Length; i++)
			source[i] = (byte)i;

		var hexString1 = Zongsoft.Common.Convert.ToHexString(source);
		var hexString2 = Zongsoft.Common.Convert.ToHexString(source, '-');

		Assert.Equal("000102030405060708090A0B0C0D0E0F", hexString1);
		Assert.Equal("00-01-02-03-04-05-06-07-08-09-0A-0B-0C-0D-0E-0F", hexString2);

		var bytes1 = Zongsoft.Common.Convert.FromHexString(hexString1);
		var bytes2 = Zongsoft.Common.Convert.FromHexString(hexString2, '-');

		Assert.Equal(source.Length, bytes1.Length);
		Assert.Equal(source.Length, bytes2.Length);

		Assert.True(BinaryCompare(source, bytes1));
		Assert.True(BinaryCompare(source, bytes2));
	}

	[Fact]
	public void TestBitVector32()
	{
		Zongsoft.Common.BitVector32 vector = 1;

		Assert.Equal(1, vector.Data);
		Assert.True(vector[1]);
		Assert.False(vector[2]);
		Assert.False(vector[3]);
		Assert.False(vector[4]);
		Assert.False(vector[5]);

		vector[5] = true;
		Assert.Equal(5, vector.Data);
		Assert.True(vector[1]);
		Assert.False(vector[2]);
		Assert.False(vector[3]);
		Assert.True(vector[4]);
		Assert.True(vector[5]);
	}

	private static bool BinaryCompare(byte[] binary1, byte[] binary2)
	{
		if(binary1 == null || binary2 == null || binary1.Length != binary2.Length)
			return false;

		for(int i = 0; i < binary1.Length; i++)
		{
			if(binary1[i] != binary2[i])
				return false;
		}

		return true;
	}
}
