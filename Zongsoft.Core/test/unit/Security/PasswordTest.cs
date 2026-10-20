using System;
using System.Globalization;
using System.Collections.Generic;
using System.Security.Cryptography;

using Xunit;

namespace Zongsoft.Security.Tests;

public class PasswordTest
{
	private const string HASH_TEXT = "AQIDBAUGBwgJCgsMDQ4PEBESExQVFhcYGRobHB0eHyA=";
	private const string PASSWORD_TEXT = "SHA256#127:0102030405060708|" + HASH_TEXT;

	[Fact]
	public void ParsingContracts_IgnoreProviderAndPreserveFields()
	{
		var text = "sha256#127:0102030405060708|" + HASH_TEXT;
		var password = Password.Parse(text);

		Assert.Equal("SHA256", password.Algorithm);
		Assert.Equal(127, password.Factor);
		Assert.Equal(Convert.FromHexString("0102030405060708"), password.Nonce);
		Assert.Equal(Convert.FromHexString("0102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F20"), password.Value);
		Assert.Equal(PASSWORD_TEXT, password.ToString());

		AssertParsingContract(text, password);
	}

	[Fact]
	public void Parse_SpanSlice_OwnsDecodedData()
	{
		var buffer = ("prefix" + PASSWORD_TEXT + "suffix").ToCharArray();
		var text = buffer.AsSpan(6, PASSWORD_TEXT.Length);
		var password = Password.Parse(text);

		Assert.True(Password.TryParse(text, out var result));
		Assert.Equal(password, result);

		Array.Fill(buffer, '?');

		Assert.Equal(PASSWORD_TEXT, password.ToString());
		Assert.Equal(PASSWORD_TEXT, result.ToString());
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	public void ParsingContracts_EmptyInput_ReturnsEmptyPassword(string text)
	{
		Assert.Equal(default, Password.Parse(text));
		Assert.Equal(default, Password.Parse(text.AsSpan()));
		AssertParsingContract(text, default(Password));
	}

	[Theory]
	[InlineData("invalid")]
	[InlineData("SHA256:01020304")]
	[InlineData("unsupported:01020304|" + HASH_TEXT)]
	[InlineData("SHA256#:01020304|" + HASH_TEXT)]
	[InlineData("SHA256#invalid:01020304|" + HASH_TEXT)]
	[InlineData("SHA256#-1:01020304|" + HASH_TEXT)]
	[InlineData("SHA256#256:01020304|" + HASH_TEXT)]
	[InlineData("SHA256:|" + HASH_TEXT)]
	[InlineData("SHA256:0|" + HASH_TEXT)]
	[InlineData("SHA256:GG|" + HASH_TEXT)]
	[InlineData("SHA256:01020304|invalid!")]
	[InlineData("SHA256:01020304|")]
	[InlineData("SHA256:01020304|AQIDBA==")]
	public void ParsingContracts_InvalidInput_ReturnsFalseAndParseThrows(string text)
	{
		Assert.False(Password.TryParse(text, out var stringResult));
		Assert.Equal(default, stringResult);
		Assert.False(Password.TryParse(text.AsSpan(), out var spanResult));
		Assert.Equal(default, spanResult);
		Assert.Throws<FormatException>(() => Password.Parse(text));
		Assert.Throws<FormatException>(() => Password.Parse(text.AsSpan()));

		AssertInvalidParsingContract<Password>(text);
	}

	[Fact]
	public void EqualParsedValues_HaveContentHashAndWorkAsDictionaryKeys()
	{
		var original = Password.Generate("correct horse", [1, 2, 3, 4, 5, 6, 7, 8], 0, "SHA256");
		Assert.True(Password.TryParse(original.ToString(), out var first));
		Assert.True(Password.TryParse(original.ToString(), out var second));
		var passwords = new Dictionary<Password, string> { [first] = "credential" };

		Assert.Equal(first, second);
		Assert.Equal(first.GetHashCode(), second.GetHashCode());
		Assert.Equal("credential", passwords[second]);
	}

	[Fact]
	public void ByteArrayConversion_ReturnsIndependentCopies()
	{
		const string TEXT = "correct horse";

		var password = Password.Generate(TEXT, [1, 2, 3, 4, 5, 6, 7, 8], 0, "SHA256");
		byte[] first = password;
		byte[] second = password;

		Assert.NotSame(first, second);
		first[0] = 0;

		Assert.True(password.Verify(TEXT));
		Assert.Equal((byte)'Z', second[0]);
		Assert.Equal(second, (byte[])password);
	}

	[Fact]
	public void TestGenerateEmpty()
	{
		var empty = default(Password);
		Assert.True(empty.IsEmpty);
		Assert.False(empty.HasValue);
		Assert.Null(empty.Algorithm);
		Assert.Equal(0, empty.Factor);
		Assert.True(empty.Nonce.IsEmpty);
		Assert.True(empty.Value.IsEmpty);

		var result = Password.Generate(null);
		Assert.Equal(result, empty);
		result = Password.Generate(string.Empty);
		Assert.Equal(result, empty);
	}

	[Fact]
	public void Generate_WithoutAlgorithm_UsesSHA256()
	{
		const string PASSWORD = "123456";
		byte[] nonce = [1, 2, 3, 4, 5, 6, 7, 8];
		Password[] results =
		[
			Password.Generate(PASSWORD),
			Password.Generate(PASSWORD, 0),
			Password.Generate(PASSWORD, nonce),
			Password.Generate(PASSWORD, nonce, 0),
		];

		Assert.All(results, result =>
		{
			Assert.Equal("SHA256", result.Algorithm);
			Assert.Equal(32, result.Value.Length);
			Assert.True(result.Verify(PASSWORD));
		});

		Assert.Equal(10, results[0].Factor);
		Assert.Equal(0, results[1].Factor);
		Assert.Equal(10, results[2].Factor);
		Assert.Equal(0, results[3].Factor);
	}

	[Theory]
	[InlineData(int.MinValue, 0, 1)]
	[InlineData(0, 0, 1)]
	[InlineData(1, 1, 65_535)]
	[InlineData(2, 2, 131_070)]
	[InlineData(10, 10, 655_350)]
	[InlineData(127, 127, 8_322_945)]
	[InlineData(255, 255, 16_711_425)]
	[InlineData(int.MaxValue, 255, 16_711_425)]
	public void Generate_Factor_UsesLinearIterations(int factor, int expectedFactor, int iterations)
	{
		const string PASSWORD = "correct horse";
		byte[] nonce = [1, 2, 3, 4, 5, 6, 7, 8];
		var expected = Rfc2898DeriveBytes.Pbkdf2(PASSWORD, nonce, iterations, HashAlgorithmName.SHA256, 32);

		var password = Password.Generate(PASSWORD, nonce, factor: factor);

		Assert.Equal(expectedFactor, password.Factor);
		Assert.Equal("SHA256", password.Algorithm);
		Assert.Equal(nonce, password.Nonce);
		Assert.Equal(expected, password.Value);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(255)]
	public void Parse_Factor_RoundTripsTextAndBytesAndVerifies(int factor)
	{
		const string PASSWORD = "correct horse";
		byte[] nonce = [1, 2, 3, 4, 5, 6, 7, 8];
		var password = Password.Generate(PASSWORD, nonce, factor);
		var text = password.ToString();
		byte[] data = password;

		Assert.StartsWith(factor == 0 ? "SHA256:" : "SHA256#255:", text);
		Assert.Equal(factor, data[3]);
		Assert.True(Password.TryParse(text, out var fromText));
		Assert.True(Password.TryParse(data, out var fromData));
		Assert.Equal(factor, fromText.Factor);
		Assert.Equal(factor, fromData.Factor);
		Assert.Equal(password, fromText);
		Assert.Equal(password, fromData);
		Assert.True(fromText.Verify(PASSWORD));
		Assert.True(fromData.Verify(PASSWORD));
		Assert.False(fromData.Verify(PASSWORD + "!"));
	}

	[Fact]
	public void TestGenerateSHA1()
	{
		const string PASSWORD = "123456";

		var nonce = new byte[16];
		Random.Shared.NextBytes(nonce);

		var result = Password.Generate(PASSWORD, nonce, 0, "sha1");
		Assert.True(result.HasValue);
		Assert.False(result.IsEmpty);
		Assert.Equal("SHA1", result.Algorithm);
		Assert.Equal(0, result.Factor);
		Assert.False(result.Nonce.IsEmpty);
		Assert.False(result.Value.IsEmpty);
		Assert.Equal(20, result.Value.Length);
		Assert.Equal(nonce, result.Nonce);

		Assert.True(result.Verify(PASSWORD));
		Assert.False(result.Verify(null));
		Assert.False(result.Verify(string.Empty));
		Assert.False(result.Verify(PASSWORD + "!"));
	}

	[Fact]
	public void TestGenerateSHA256()
	{
		const string PASSWORD = "123456";

		var result = Password.Generate(PASSWORD, 12, "sha256");
		Assert.True(result.HasValue);
		Assert.False(result.IsEmpty);
		Assert.Equal("SHA256", result.Algorithm);
		Assert.Equal(12, result.Factor);
		Assert.False(result.Nonce.IsEmpty);
		Assert.False(result.Value.IsEmpty);
		Assert.Equal(32, result.Value.Length);

		Assert.True(result.Verify(PASSWORD));
		Assert.False(result.Verify(null));
		Assert.False(result.Verify(string.Empty));
		Assert.False(result.Verify(PASSWORD + "!"));
	}

	[Fact]
	public void TestGenerateSHA384()
	{
		const string PASSWORD = "123456";

		var nonce = new byte[30];
		Random.Shared.NextBytes(nonce);

		var result = Password.Generate(PASSWORD, nonce, 12, "sha384");
		Assert.True(result.HasValue);
		Assert.False(result.IsEmpty);
		Assert.Equal("SHA384", result.Algorithm);
		Assert.Equal(12, result.Factor);
		Assert.False(result.Nonce.IsEmpty);
		Assert.False(result.Value.IsEmpty);
		Assert.Equal(48, result.Value.Length);
		Assert.Equal(nonce, result.Nonce);

		Assert.True(result.Verify(PASSWORD));
		Assert.False(result.Verify(null));
		Assert.False(result.Verify(string.Empty));
		Assert.False(result.Verify(PASSWORD + "!"));
	}

	[Fact]
	public void TestGenerateSHA512()
	{
		const string PASSWORD = "123456";

		var result = Password.Generate(PASSWORD, 12, "sha512");
		Assert.True(result.HasValue);
		Assert.False(result.IsEmpty);
		Assert.Equal("SHA512", result.Algorithm);
		Assert.Equal(12, result.Factor);
		Assert.False(result.Nonce.IsEmpty);
		Assert.False(result.Value.IsEmpty);
		Assert.Equal(64, result.Value.Length);

		Assert.True(result.Verify(PASSWORD));
		Assert.False(result.Verify(null));
		Assert.False(result.Verify(string.Empty));
		Assert.False(result.Verify(PASSWORD + "!"));
	}

	[Fact]
	public void TestVerify()
	{
		var empty = default(Password);
		Assert.True(empty.Verify(null));
		Assert.True(empty.Verify(string.Empty));

		string password = Common.Randomizer.GenerateString();
		var result = Password.Generate(password, 0, "SHA1");
		Assert.True(result.Verify(password));
		Assert.False(result.Verify(null));
		Assert.False(result.Verify(" "));
		Assert.False(result.Verify(string.Empty));
		Assert.False(result.Verify(password + '!'));

		result = Password.Generate(password, 0, "SHA256");
		Assert.True(result.Verify(password));
		Assert.False(result.Verify(null));
		Assert.False(result.Verify(" "));
		Assert.False(result.Verify(string.Empty));
		Assert.False(result.Verify(password + '!'));

		result = Password.Generate(password, 0, "SHA384");
		Assert.True(result.Verify(password));
		Assert.False(result.Verify(null));
		Assert.False(result.Verify(" "));
		Assert.False(result.Verify(string.Empty));
		Assert.False(result.Verify(password + '!'));

		result = Password.Generate(password, 0, "SHA512");
		Assert.True(result.Verify(password));
		Assert.False(result.Verify(null));
		Assert.False(result.Verify(" "));
		Assert.False(result.Verify(string.Empty));
		Assert.False(result.Verify(password + '!'));
	}

	[Fact]
	public void TestTryPaseFromText()
	{
		//解析空字符
		Assert.True(Password.TryParse(string.Empty, out var password));
		Assert.True(password.IsEmpty);

		//解析空对象
		Assert.True(Password.TryParse((string)null, out password));
		Assert.True(password.IsEmpty);

		//解析无效文本
		Assert.False(Password.TryParse("1A2B3C4D5E6F7890", out _));

		const string PASSWORD = "ABC1234567890XYZ";
		var older = Password.Generate(PASSWORD, [1, 2, 3, 4, 5, 6, 7, 8], 0, "SHA1");
		Assert.True(Password.TryParse(older.ToString(), out var newer));
		Assert.Equal(older, newer);
		Assert.Equal("SHA1", newer.Algorithm);
		Assert.True(newer.Verify(PASSWORD));

		older = Password.Generate(PASSWORD, 0);
		Assert.True(Password.TryParse(older.ToString(), out newer));
		Assert.Equal(older, newer);

		older = Password.Generate(PASSWORD, 0, "SHA256");
		Assert.True(Password.TryParse(older.ToString(), out newer));
		Assert.Equal(older, newer);

		older = Password.Generate(PASSWORD, 0, "SHA384");
		Assert.True(Password.TryParse(older.ToString(), out newer));
		Assert.Equal(older, newer);

		older = Password.Generate(PASSWORD, 0, "SHA512");
		Assert.True(Password.TryParse(older.ToString(), out newer));
		Assert.Equal(older, newer);
	}

	[Fact]
	public void TestTryParseFromData()
	{
		//解析空数组
		Assert.True(Password.TryParse((byte[])[], out var password));
		Assert.True(password.IsEmpty);

		//解析空对象
		Assert.True(Password.TryParse((byte[])null, out password));
		Assert.True(password.IsEmpty);

		//数据长度不够
		Assert.False(Password.TryParse(new byte[10], out _));

		var data = new byte[100];
		Assert.False(Password.TryParse(data, out _)); //指示符错误

		data[0] = (byte)'Z';
		data[1] = (byte)'s';
		Assert.False(Password.TryParse(data, out _)); //随机数缺失

		data[4] = 1;
		Assert.False(Password.TryParse(data, out _)); //内容不匹配

		const string PASSWORD = "1234567890";

		password = Password.Generate(PASSWORD, 0);
		Assert.True(password.HasValue);
		Assert.False(password.IsEmpty);
		Assert.NotNull(password.Algorithm);

		data = password;
		Assert.NotNull(data);
		Assert.NotEmpty(data);

		Assert.True(Password.TryParse(data, out var result));
		Assert.True(result.HasValue);
		Assert.False(result.IsEmpty);
		Assert.Equal(password.Algorithm, result.Algorithm);
		Assert.Equal(password.Factor, result.Factor);
		Assert.Equal(password, result);
		Assert.Equal((byte[])password, (byte[])result);

		Assert.True(result.Verify(PASSWORD));
		Assert.True(password.Verify(PASSWORD));
	}

	[Fact]
	public void TestTypeConvert()
	{
		const string PASSWORD = "1234567890";

		var text = Common.Convert.ConvertValue<string>(default(Password));
		Assert.Null(text);

		text = Common.Convert.ConvertValue<string>(Password.Generate(PASSWORD, 0));
		Assert.NotNull(text);
		Assert.NotEmpty(text);
		Assert.True(Password.TryParse(text, out var result1));
		Assert.True(result1.HasValue);
		Assert.False(result1.IsEmpty);
		Assert.True(result1.Verify(PASSWORD));
		Assert.False(result1.Verify(null));
		Assert.False(result1.Verify(string.Empty));
		Assert.False(result1.Verify(PASSWORD + '!'));

		var data = Common.Convert.ConvertValue<byte[]>(default(Password));
		Assert.Null(data);

		data = Common.Convert.ConvertValue<byte[]>(Password.Generate(PASSWORD, 0));
		Assert.NotNull(data);
		Assert.NotEmpty(data);
		Assert.True(Password.TryParse(data, out var result2));
		Assert.True(result2.HasValue);
		Assert.False(result2.IsEmpty);
		Assert.True(result2.Verify(PASSWORD));
		Assert.False(result2.Verify(null));
		Assert.False(result2.Verify(string.Empty));
		Assert.False(result2.Verify(PASSWORD + '!'));
	}

	[Fact]
	public void TestJsonConvert()
	{
		const string PASSWORD = "I'm a good man!";

		var cipher = new Cipher()
		{
			UserId = 100,
			UserName = "Administrator",
			Password = Password.Generate(PASSWORD, 0),
		};

		var json = Serialization.Serializer.Json.Serialize(cipher);
		Assert.NotNull(json);
		Assert.NotEmpty(json);

		var result = Serialization.Serializer.Json.Deserialize<Cipher>(json);
		Assert.NotNull(result);
		Assert.True(result.Password.HasValue);
		Assert.False(result.Password.IsEmpty);
		Assert.True(result.Password.Verify(PASSWORD));
	}

	private static void AssertParsingContract<T>(string text, T expected) where T : ISpanParsable<T>
	{
		foreach(var provider in new IFormatProvider[] { null, CultureInfo.GetCultureInfo("tr-TR"), CultureInfo.GetCultureInfo("ar-SA"), new UnusedProvider() })
		{
			Assert.Equal(expected, ParseString<T>(text, provider));
			Assert.True(T.TryParse(text, provider, out var stringResult));
			Assert.Equal(expected, stringResult);
			Assert.Equal(expected, T.Parse(text.AsSpan(), provider));
			Assert.True(T.TryParse(text.AsSpan(), provider, out var spanResult));
			Assert.Equal(expected, spanResult);
		}
	}

	private static void AssertInvalidParsingContract<T>(string text) where T : ISpanParsable<T>
	{
		foreach(var provider in new IFormatProvider[] { null, CultureInfo.GetCultureInfo("tr-TR"), CultureInfo.GetCultureInfo("ar-SA"), new UnusedProvider() })
		{
			Assert.False(T.TryParse(text, provider, out var stringResult));
			Assert.Equal(default, stringResult);
			Assert.False(T.TryParse(text.AsSpan(), provider, out var spanResult));
			Assert.Equal(default, spanResult);
			Assert.Throws<FormatException>(() => ParseString<T>(text, provider));
			Assert.Throws<FormatException>(() => T.Parse(text.AsSpan(), provider));
		}
	}

	private static T ParseString<T>(string text, IFormatProvider provider) where T : IParsable<T> => T.Parse(text, provider);

	private sealed class UnusedProvider : IFormatProvider
	{
		public object GetFormat(Type formatType) => throw new InvalidOperationException();
	}

	private class Cipher
	{
		public int UserId { get; set; }
		public string UserName { get; set; }
		public Password Password { get; set; }
	}
}
