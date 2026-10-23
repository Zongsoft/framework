using System;
using System.Text;
using System.Buffers.Binary;
using System.Security.Cryptography;

using Xunit;

namespace Zongsoft.Externals.Wechat.Tests;

public class PlatformCryptographyTest
{
	#region 常量定义
	private const string KEY = "MDEyMzQ1Njc4OWFiY2RlZjAxMjM0NTY3ODlhYmNkZWY";
	private const string APP_ID = "wx-offline-app";
	//微信官方消息推送文档中的公开示例，不是真实凭据。
	private const string OFFICIAL_CIPHERTEXT = "+qdx1OKCy+5JPCBFWw70tm0fJGb2Jmeia4FCB7kao+/Q5c/ohsOzQHi8khUOb05JCpj0JB4RvQMkUyus8TPxLKJGQqcvZqzDpVzazhZv6JsXUnnR8XGT740XgXZUXQ7vJVnAG+tE8NUd4yFyjPy7GgiaviNrlCTj+l5kdfMuFUPpRSrfMZuMcp3Fn2Pede2IuQrKEYwKSqFIZoNqJ4M8EajAsjLY2km32IIjdf8YL/P50F7mStwntrA2cPDrM1kb6mOcfBgRtWygb3VIYnSeOBrebufAlr7F9mFUPAJGj04=";
	#endregion

	#region 测试方法
	[Fact]
	public void OfficialVectorVerifiesSignatureAndReceiver()
	{
		const string SIGNATURE = "046e02f8204d34f8ba5fa3b1db94908f3df2e9b3";
		Assert.Equal(SIGNATURE, Platforms.CryptographyUtility.Sign("AAAAA", "1714112445", "415670741", OFFICIAL_CIPHERTEXT));
		Assert.True(Platforms.CryptographyUtility.VerifySignature("AAAAA", "1714112445", "415670741", OFFICIAL_CIPHERTEXT, SIGNATURE));
		Assert.False(Platforms.CryptographyUtility.VerifySignature("AAAAA", "1714112446", "415670741", OFFICIAL_CIPHERTEXT, SIGNATURE));
		Assert.False(Platforms.CryptographyUtility.VerifySignature("wrong", "1714112445", "415670741", OFFICIAL_CIPHERTEXT, SIGNATURE));
		Assert.False(Platforms.CryptographyUtility.VerifySignature("AAAAA", "1714112445", "415670742", OFFICIAL_CIPHERTEXT, SIGNATURE));
		Assert.False(Platforms.CryptographyUtility.VerifySignature("AAAAA", "1714112445", "415670741", "changed", SIGNATURE));
		Assert.False(Platforms.CryptographyUtility.VerifySignature("AAAAA", "1714112445", "415670741", OFFICIAL_CIPHERTEXT, new string('g', 40)));
		Assert.False(Platforms.CryptographyUtility.VerifySignature("AAAAA", "1714112445", "415670741", OFFICIAL_CIPHERTEXT, null));

		var plaintext = Platforms.CryptographyUtility.Decrypt(OFFICIAL_CIPHERTEXT, new string('A', 43), "wxba5fad812f8e6fb9");
		Assert.Equal("""{"ToUserName":"gh_97417a04a28d","FromUserName":"o9AgO5Kd5ggOC-bXrbNODIiE3bGY","CreateTime":1714112445,"MsgType":"event","Event":"debug_demo","debug_str":"hello world"}""", plaintext);
		Assert.Throws<CryptographicException>(() => Platforms.CryptographyUtility.Decrypt(OFFICIAL_CIPHERTEXT, new string('A', 43), "other-app"));
		Assert.Throws<CryptographicException>(() => Platforms.CryptographyUtility.Decrypt(OFFICIAL_CIPHERTEXT, KEY, "wxba5fad812f8e6fb9"));
	}

	[Fact]
	public void OfficialThirdPartyVectorUsesComponentReceiver()
	{
		const string CIPHERTEXT = "D7yzvUNAL930rd28wf21s4hvXhz0L6Uit/p2Di6C5DHyYGpEgdBRnKjBec34JwoQXicwaZC7fOVihW80F4VtsdvE//1vr7oAbqjDv8KenVp+ajKYpJnyQ4zMRhIC+a31fCVOMC03FfzV/QuC94kBP55a+Za3sJgvAn+ZbsNqZI5DkyuzkhQN8OBqCzFhizGmy0xpM0MEA4agpvE+RuNO1rhHTtuJB5yltw1FiYzecSXJ+y/D2r81VkRn2eYjh2ltsoyfbDR7Is6ookXIFxTfyeNyeHxMeT4KN5WpCDmSbTcUYrbBUlkGLJ9n/rU8YOywma6G7aTb7KZKOqCxgfoUlYEZPk4FUL/TK7ShriFDMVCLyjQJ15Ob++agDWtcxhfUfe6HIoIpRW8mKNBQiY/Jd1svvuskA1wLef1RTtzKfMpagSCX/laZINmdnX4zrF6kIaR9P4xQrWXbsTFHTYe0Mg==";
		Assert.True(Platforms.CryptographyUtility.VerifySignature("AAAAA", "1715943329", "1590219412", CIPHERTEXT, "6c12a4205838198b8fa631b3220723bb07f1015c"));
		var plaintext = Platforms.CryptographyUtility.Decrypt(CIPHERTEXT, new string('A', 43), "wx134c8103faa5a59e");
		var message = System.Xml.Linq.XDocument.Parse(plaintext);
		Assert.Equal("debug_demo", message.Root.Element("Event").Value);
		Assert.Equal("hello world", message.Root.Element("debug_str").Value);
		Assert.Throws<CryptographicException>(() => Platforms.CryptographyUtility.Decrypt(CIPHERTEXT, new string('A', 43), "wxba5fad812f8e6fb9"));
	}

	[Fact]
	public void IndependentVectorPreservesUtf8()
	{
		//独立 Python AES-CBC 向量，前缀为 00..0F。
		const string CIPHERTEXT = "oChxUuEMxdC3iAHYNh2h5ZyqVzQjjHvd/X+QEbcpzQlZlBEYcGYGVC7E/c0zXoeqPyDUwo3H1jQhUMu8yUwOmA==";
		const string PLAINTEXT = "公众平台离线消息";
		Assert.Equal(PLAINTEXT, Platforms.CryptographyUtility.Decrypt(CIPHERTEXT, KEY, APP_ID));
		var first = Platforms.CryptographyUtility.Encrypt(PLAINTEXT, KEY, APP_ID);
		var second = Platforms.CryptographyUtility.Encrypt(PLAINTEXT, KEY, APP_ID);
		Assert.NotEqual(first, second);
		Assert.Equal(PLAINTEXT, Platforms.CryptographyUtility.Decrypt(first, KEY, APP_ID));
		Assert.Equal(PLAINTEXT, Platforms.CryptographyUtility.Decrypt(second, KEY, APP_ID));
	}

	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(11)]
	[InlineData(12)]
	[InlineData(13)]
	[InlineData(30)]
	[InlineData(31)]
	[InlineData(32)]
	[InlineData(33)]
	[InlineData(1024)]
	public void BinaryPayloadAndPaddingBoundaries(int length)
	{
		var data = new byte[length];

		for(var index = 0; index < length; index++)
			data[index] = (byte)index;

		var ciphertext = Platforms.CryptographyUtility.Encrypt(data, KEY, APP_ID);
		Assert.Equal(data, Platforms.CryptographyUtility.DecryptBytes(ciphertext, KEY, APP_ID));
		using var cipher = Aes.Create();
		cipher.Key = Convert.FromBase64String(KEY + "=");
		var frame = cipher.DecryptCbc(Convert.FromBase64String(ciphertext), cipher.Key.AsSpan(0, 16), PaddingMode.None);
		Assert.Equal(0, frame.Length % 32);
		Assert.Equal(length, BinaryPrimitives.ReadInt32BigEndian(frame.AsSpan(16, 4)));
		Assert.Equal(data, frame.AsSpan(20, length).ToArray());
		Assert.Equal(APP_ID, Encoding.UTF8.GetString(frame.AsSpan(20 + length, APP_ID.Length)));
		Assert.All(frame[(20 + length + APP_ID.Length)..], value => Assert.Equal(frame[^1], value));
		Assert.InRange(frame[^1], (byte)1, (byte)32);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(2)]
	[InlineData(3)]
	[InlineData(4)]
	[InlineData(5)]
	[InlineData(6)]
	public void MalformedFramesAreRejected(int scenario)
	{
		var frame = new byte[64];
		Encoding.UTF8.GetBytes(APP_ID).CopyTo(frame, 20);
		frame.AsSpan(33).Fill(31);

		switch(scenario)
		{
			case 0:
				frame[^1] = 0;
				break;
			case 1:
				frame[^1] = 33;
				break;
			case 2:
				frame[^2] = 30;
				break;
			case 3:
				BinaryPrimitives.WriteInt32BigEndian(frame.AsSpan(16, 4), -1);
				break;
			case 4:
				BinaryPrimitives.WriteInt32BigEndian(frame.AsSpan(16, 4), int.MaxValue);
				break;
			case 5:
				BinaryPrimitives.WriteInt32BigEndian(frame.AsSpan(16, 4), 1);
				break;
			case 6:
				frame.AsSpan().Fill(32);
				frame = frame[..32];
				break;
		}

		Assert.Throws<CryptographicException>(() => Platforms.CryptographyUtility.DecryptBytes(EncryptFrame(frame), KEY, APP_ID));
	}

	[Fact]
	public void InvalidTextIsNotSilentlyReplaced()
	{
		byte[] data = [0xFF, 0x00, 0xC0, 0x80];
		var ciphertext = Platforms.CryptographyUtility.Encrypt(data, KEY, APP_ID);
		Assert.Equal(data, Platforms.CryptographyUtility.DecryptBytes(ciphertext, KEY, APP_ID));
		Assert.Throws<DecoderFallbackException>(() => Platforms.CryptographyUtility.Decrypt(ciphertext, KEY, APP_ID));
		Assert.Throws<EncoderFallbackException>(() => Platforms.CryptographyUtility.Encrypt("\uD800", KEY, APP_ID));
	}

	[Fact]
	public void InvalidKeysAndCiphertextAreRejected()
	{
		foreach(var key in new[] { "short", new string('?', 43), new string('A', 42) + "B", new string('A', 42) + " ", KEY + "=" })
			Assert.Equal("encodingAESKey", Assert.Throws<ArgumentException>(() => Platforms.CryptographyUtility.Encrypt("message", key, APP_ID)).ParamName);

		Assert.Throws<FormatException>(() => Platforms.CryptographyUtility.Decrypt("invalid!", KEY, APP_ID));
		Assert.Throws<CryptographicException>(() => Platforms.CryptographyUtility.Decrypt(Convert.ToBase64String(new byte[16]), KEY, APP_ID));
		Assert.Throws<CryptographicException>(() => Platforms.CryptographyUtility.Decrypt(Convert.ToBase64String(new byte[63]), KEY, APP_ID));
		Assert.Equal("appId", Assert.Throws<ArgumentException>(() => Platforms.CryptographyUtility.Encrypt("message", KEY, "")).ParamName);
	}
	#endregion

	#region 私有方法
	private static string EncryptFrame(byte[] frame)
	{
		using var cipher = Aes.Create();
		cipher.Key = Convert.FromBase64String(KEY + "=");
		return Convert.ToBase64String(cipher.EncryptCbc(frame, cipher.Key.AsSpan(0, 16), PaddingMode.None));
	}
	#endregion
}
