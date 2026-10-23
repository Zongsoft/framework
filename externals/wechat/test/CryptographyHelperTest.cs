using System;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

using Xunit;

namespace Zongsoft.Externals.Wechat.Tests;

public class CryptographyHelperTest
{
	private const string KEY = "0123456789abcdef0123456789abcdef";
	private const string NONCE = "0123456789ab";
	private const string ASSOCIATED_DATA = "transaction";
	private const string PLAINTEXT = "{\"message\":\"离线测试\",\"amount\":1234}";
	//使用 Python cryptography AESGCM 独立生成的固定向量，输入全部为合成数据。
	private const string CIPHERTEXT = "4ouKXzC5+XyQJxmfVKLHwPg5MnqcIMX+BqPteKSX7MdeXV1IwMERuOWF9bJVlz3auih/9e1McnY=";
	private const string EMPTY_ASSOCIATED_DATA_CIPHERTEXT = "4ouKXzC5+XyQJxmfVKLHwPg5MnqcIMX+BqPteKSX7MdeXV1IwMERuFT3Fk/I9TfuaO/ORszVKJQ=";

	[Theory]
	[InlineData(ASSOCIATED_DATA, CIPHERTEXT)]
	[InlineData("", EMPTY_ASSOCIATED_DATA_CIPHERTEXT)]
	[InlineData(null, EMPTY_ASSOCIATED_DATA_CIPHERTEXT)]
	public void Decrypt_FixedVectorPreservesUtf8AndRemovesTag(string associatedData, string ciphertext)
	{
		var expected = Encoding.UTF8.GetBytes(PLAINTEXT);
		Assert.Equal(expected, CryptographyHelper.Decrypt1(KEY, NONCE, associatedData, ciphertext));
		Assert.Equal(expected, CryptographyHelper.Decrypt2(KEY, NONCE, associatedData, ciphertext));

		var key = Encoding.UTF8.GetBytes(KEY);
		var nonce = Encoding.UTF8.GetBytes(NONCE);
		var data = associatedData == null ? null : Encoding.UTF8.GetBytes(associatedData);
		var encrypted = Convert.FromBase64String(ciphertext);
		Assert.Equal(expected, CryptographyHelper.Decrypt1(key, nonce, data, encrypted));
		Assert.Equal(expected, CryptographyHelper.Decrypt2(key, nonce, data, encrypted));
	}

	[Fact]
	public void Decrypt_AuthenticatedEmptyPlaintext()
	{
		//NIST AES-256-GCM test case 13: zero key/nonce, empty plaintext/AAD.
		var tag = Convert.FromHexString("530F8AFBC74536B9A963B4F1C4CB738B");
		Assert.Empty(CryptographyHelper.Decrypt1(new byte[32], new byte[12], null, tag));
		Assert.Empty(CryptographyHelper.Decrypt2(new byte[32], new byte[12], [], tag));
	}

	[Theory]
	[InlineData("key")]
	[InlineData("nonce")]
	[InlineData("associatedData")]
	[InlineData("ciphertext")]
	[InlineData("tag")]
	public void Decrypt_RejectsTampering(string field)
	{
		var key = Encoding.UTF8.GetBytes(KEY);
		var nonce = Encoding.UTF8.GetBytes(NONCE);
		var associatedData = Encoding.UTF8.GetBytes(ASSOCIATED_DATA);
		var ciphertext = Convert.FromBase64String(CIPHERTEXT);

		switch(field)
		{
			case "key":
				key[0] ^= 1;
				break;
			case "nonce":
				nonce[0] ^= 1;
				break;
			case "associatedData":
				associatedData[0] ^= 1;
				break;
			case "ciphertext":
				ciphertext[0] ^= 1;
				break;
			case "tag":
				ciphertext[^1] ^= 1;
				break;
		}

		Assert.Throws<AuthenticationTagMismatchException>(() => CryptographyHelper.Decrypt1(key, nonce, associatedData, ciphertext));
		Assert.Throws<AuthenticationTagMismatchException>(() => CryptographyHelper.Decrypt2(key, nonce, associatedData, ciphertext));
		Assert.Equal(PLAINTEXT, Encoding.UTF8.GetString(CryptographyHelper.Decrypt1(KEY, NONCE, ASSOCIATED_DATA, CIPHERTEXT)));
	}

	[Theory]
	[InlineData(0)]
	[InlineData(16)]
	[InlineData(24)]
	[InlineData(31)]
	[InlineData(33)]
	public void Decrypt_Requires256BitKey(int length)
	{
		Assert.Throws<ArgumentException>("key", () => CryptographyHelper.Decrypt1(new byte[length], new byte[12], [], new byte[16]));
		Assert.Throws<ArgumentException>("key", () => CryptographyHelper.Decrypt2(new byte[length], new byte[12], [], new byte[16]));
	}

	[Theory]
	[InlineData(0)]
	[InlineData(11)]
	[InlineData(13)]
	public void Decrypt_Requires96BitNonce(int length)
	{
		Assert.Throws<ArgumentException>("nonce", () => CryptographyHelper.Decrypt1(new byte[32], new byte[length], [], new byte[16]));
		Assert.Throws<ArgumentException>("nonce", () => CryptographyHelper.Decrypt2(new byte[32], new byte[length], [], new byte[16]));
	}

	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(15)]
	public void Decrypt_RequiresCompleteTag(int length)
	{
		Assert.Throws<ArgumentException>("ciphertext", () => CryptographyHelper.Decrypt1(new byte[32], new byte[12], [], new byte[length]));
		Assert.Throws<ArgumentException>("ciphertext", () => CryptographyHelper.Decrypt2(new byte[32], new byte[12], [], new byte[length]));
	}

	[Theory]
	[InlineData(16)]
	[InlineData(17)]
	[InlineData(55)]
	public void Decrypt_RejectsTruncatedAuthenticatedResource(int length)
	{
		var ciphertext = Convert.FromBase64String(CIPHERTEXT)[..length];
		var key = Encoding.UTF8.GetBytes(KEY);
		var nonce = Encoding.UTF8.GetBytes(NONCE);
		var associatedData = Encoding.UTF8.GetBytes(ASSOCIATED_DATA);
		Assert.Throws<AuthenticationTagMismatchException>(() => CryptographyHelper.Decrypt1(key, nonce, associatedData, ciphertext));
		Assert.Throws<AuthenticationTagMismatchException>(() => CryptographyHelper.Decrypt2(key, nonce, associatedData, ciphertext));
	}

	[Fact]
	public void Decrypt_RejectsInvalidBase64()
	{
		Assert.Throws<FormatException>(() => CryptographyHelper.Decrypt1(KEY, NONCE, ASSOCIATED_DATA, "not-base64!"));
		Assert.Throws<FormatException>(() => CryptographyHelper.Decrypt2(KEY, NONCE, ASSOCIATED_DATA, "not-base64!"));
	}

	[Fact]
	public void Decrypt_RejectsNullRequiredInputs()
	{
		Assert.Throws<ArgumentNullException>("key", () => CryptographyHelper.Decrypt1(null, new byte[12], [], new byte[16]));
		Assert.Throws<ArgumentNullException>("nonce", () => CryptographyHelper.Decrypt1(new byte[32], null, [], new byte[16]));
		Assert.Throws<ArgumentNullException>("ciphertext", () => CryptographyHelper.Decrypt1(new byte[32], new byte[12], [], null));
	}

	[Fact]
	public void Decrypt_ConcurrentAccountsDoNotShareCipherState()
	{
		var accounts = new Resource[32];

		for(int i = 0; i < accounts.Length; i++)
		{
			var key = SHA256.HashData(Encoding.UTF8.GetBytes($"offline-account-{i}"));
			var nonce = new byte[12];
			nonce[^1] = (byte)i;
			var associatedData = Encoding.UTF8.GetBytes($"offline-resource-{i}");
			var plaintext = Encoding.UTF8.GetBytes($"账户 {i} 的离线资源：{new string('x', i * 127)}");
			var ciphertext = new byte[plaintext.Length + 16];
			using var cipher = new AesGcm(key, 16);
			cipher.Encrypt(nonce, plaintext, ciphertext.AsSpan(0, plaintext.Length), ciphertext.AsSpan(plaintext.Length), associatedData);
			accounts[i] = new(key, nonce, associatedData, ciphertext, plaintext);
		}

		Parallel.For(0, 2048, new ParallelOptions { MaxDegreeOfParallelism = 8, CancellationToken = TestContext.Current.CancellationToken }, index =>
		{
			var account = accounts[index % accounts.Length];
			var result = index % 2 == 0 ?
				CryptographyHelper.Decrypt1(account.Key, account.Nonce, account.AssociatedData, account.Ciphertext) :
				CryptographyHelper.Decrypt2(account.Key, account.Nonce, account.AssociatedData, account.Ciphertext);
			Assert.Equal(account.Plaintext, result);
		});
	}

	private sealed record Resource(byte[] Key, byte[] Nonce, byte[] AssociatedData, byte[] Ciphertext, byte[] Plaintext);
}
