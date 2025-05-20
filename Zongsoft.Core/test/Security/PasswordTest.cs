using System;

using Xunit;

namespace Zongsoft.Security.Tests;

public class PasswordTest
{
	[Fact]
	public void TestGenerateEmpty()
	{
		var empty = default(Password);
		Assert.True(empty.IsEmpty);
		Assert.False(empty.HasValue);
		Assert.Null(empty.Algorithm);
		Assert.Equal(0, empty.Exponent);
		Assert.True(empty.Nonce.IsEmpty);
		Assert.True(empty.Value.IsEmpty);

		var result = Password.Generate(null);
		Assert.Equal(result, empty);
		result = Password.Generate(string.Empty);
		Assert.Equal(result, empty);
	}

	[Fact]
	public void TestGenerateSHA1()
	{
		const string PASSWORD = "123456";

		var result = Password.Generate(PASSWORD, 0, "sha1");
		Assert.True(result.HasValue);
		Assert.False(result.IsEmpty);
		Assert.Equal("SHA1", result.Algorithm);
		Assert.Equal(0, result.Exponent);
		Assert.False(result.Nonce.IsEmpty);
		Assert.False(result.Value.IsEmpty);
		Assert.Equal(20, result.Value.Length);

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
		Assert.Equal(12, result.Exponent);
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

		var result = Password.Generate(PASSWORD, 12, "sha384");
		Assert.True(result.HasValue);
		Assert.False(result.IsEmpty);
		Assert.Equal("SHA384", result.Algorithm);
		Assert.Equal(12, result.Exponent);
		Assert.False(result.Nonce.IsEmpty);
		Assert.False(result.Value.IsEmpty);
		Assert.Equal(48, result.Value.Length);

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
		Assert.Equal(12, result.Exponent);
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

		string PASSWORD = Common.Randomizer.GenerateString();
		var result = Password.Generate(PASSWORD, "SHA1");
		Assert.True(result.Verify(PASSWORD));
		Assert.False(result.Verify(null));
		Assert.False(result.Verify(" "));
		Assert.False(result.Verify(string.Empty));
		Assert.False(result.Verify(PASSWORD + '!'));

		result = Password.Generate(PASSWORD, "SHA256");
		Assert.True(result.Verify(PASSWORD));
		Assert.False(result.Verify(null));
		Assert.False(result.Verify(" "));
		Assert.False(result.Verify(string.Empty));
		Assert.False(result.Verify(PASSWORD + '!'));

		result = Password.Generate(PASSWORD, "SHA384");
		Assert.True(result.Verify(PASSWORD));
		Assert.False(result.Verify(null));
		Assert.False(result.Verify(" "));
		Assert.False(result.Verify(string.Empty));
		Assert.False(result.Verify(PASSWORD + '!'));

		result = Password.Generate(PASSWORD, "SHA512");
		Assert.True(result.Verify(PASSWORD));
		Assert.False(result.Verify(null));
		Assert.False(result.Verify(" "));
		Assert.False(result.Verify(string.Empty));
		Assert.False(result.Verify(PASSWORD + '!'));
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

		const string PASSWORD = "ABC1234567890XYZ";
		var older = Password.Generate(PASSWORD, 0);
		Assert.True(Password.TryParse(older.ToString(), out var newer));
		Assert.Equal(older, newer);

		older = Password.Generate(PASSWORD);
		Assert.True(Password.TryParse(older.ToString(), out newer));
		Assert.Equal(older, newer);

		older = Password.Generate(PASSWORD, "SHA256");
		Assert.True(Password.TryParse(older.ToString(), out newer));
		Assert.Equal(older, newer);

		older = Password.Generate(PASSWORD, "SHA384");
		Assert.True(Password.TryParse(older.ToString(), out newer));
		Assert.Equal(older, newer);

		older = Password.Generate(PASSWORD, "SHA512");
		Assert.True(Password.TryParse(older.ToString(), out newer));
		Assert.Equal(older, newer);
	}

	[Fact]
	public void TestTryParseFromData()
	{
		//解析空数组
		Assert.True(Password.TryParse([], out var password));
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

		password = Password.Generate(PASSWORD);
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
		Assert.Equal(password.Exponent, result.Exponent);
		Assert.Equal(password, result);
		Assert.Equal((byte[])password, (byte[])result);

		Assert.True(result.Verify(PASSWORD));
		Assert.True(password.Verify(PASSWORD));
	}
}
