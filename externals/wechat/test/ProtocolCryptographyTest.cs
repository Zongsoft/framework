using System;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Cryptography;

using Xunit;

namespace Zongsoft.Externals.Wechat.Tests;

public class ProtocolCryptographyTest
{
	private const string KEY = "0123456789abcdef0123456789abcdef";
	private const string MD5_SIGNATURE = "C30C37CFA4273EC617F4BA314E2904F6";
	private const string HMAC_SIGNATURE = "A2932FB05F5896067EF0AD0D3431258928698ABACF1D2FC17E5BC5C0593F6640";
	//直接绑定内部签名入口，不扩大生产类型的可见性；期望值由 Python hashlib/hmac 独立生成。
	private static readonly Func<IEnumerable<KeyValuePair<string, object>>, string, HashAlgorithm, string> _postmark = typeof(CryptographyHelper).Assembly
		.GetType("Zongsoft.Externals.Wechat.Utility", true)
		.GetMethod("Postmark", BindingFlags.Public | BindingFlags.Static)
		.CreateDelegate<Func<IEnumerable<KeyValuePair<string, object>>, string, HashAlgorithm, string>>();

	[Fact]
	public void V2_PostmarkPreservesMd5AndCallerOwnedHmac()
	{
		var fields = GetFields();
		Assert.Equal(MD5_SIGNATURE, _postmark(fields, KEY, null));

		using var algorithm = new HMACSHA256(Encoding.UTF8.GetBytes(KEY));
		Assert.Equal(HMAC_SIGNATURE, _postmark(fields, KEY, algorithm));
		Assert.Equal(HMAC_SIGNATURE, _postmark(fields, KEY, algorithm));
	}

	[Fact]
	public void V2_CanonicalizationUsesOrdinalFieldsInvariantValuesAndExcludesEmptyValuesAndSignature()
	{
		var previous = System.Globalization.CultureInfo.CurrentCulture;

		try
		{
			System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("fr-FR");
			var fields = new SortedDictionary<string, object>(StringComparer.Create(System.Globalization.CultureInfo.GetCultureInfo("fr-FR"), false))
			{
				["Z"] = "last",
				["a"] = 1.25m,
				["b"] = "value",
				["empty"] = "",
				["missing"] = null,
				["sign"] = "ignored",
			};
			Assert.Equal("2FC6592DBFFDDE0BCF609455BF80C80D", _postmark(fields, KEY, null));
		}
		finally { System.Globalization.CultureInfo.CurrentCulture = previous; }
	}

	[Fact]
	public void V2_ConcurrentPostmarksDoNotShareHashState()
	{
		var fields = GetFields();
		Parallel.For(0, 2048, new ParallelOptions { MaxDegreeOfParallelism = 8, CancellationToken = TestContext.Current.CancellationToken }, index =>
		{
			Assert.Equal(MD5_SIGNATURE, _postmark(fields, KEY, null));
			using var algorithm = new HMACSHA256(Encoding.UTF8.GetBytes(KEY));
			Assert.Equal(HMAC_SIGNATURE, _postmark(fields, KEY, algorithm));
		});
	}

	private static Dictionary<string, object> GetFields() => new()
	{
		["nonce_str"] = "offline-nonce",
		["mch_id"] = "offline-merchant",
		["body"] = "test",
		["appid"] = "offline-app",
	};
}
