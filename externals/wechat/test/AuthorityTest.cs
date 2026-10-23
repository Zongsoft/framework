using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using Microsoft.Extensions.Configuration;

using Xunit;

using Zongsoft.Configuration;
using Zongsoft.Externals.Wechat.Options;
using Zongsoft.Externals.Wechat.Paying;

namespace Zongsoft.Externals.Wechat.Tests;

public class AuthorityTest
{
	[Fact]
	public void Configuration_BindsExplicitKeyRolesFromPluginOptions()
	{
		const string OPTIONS = "<options><option path='/Externals/Wechat'><authorities default='offline'><authority authority.name='offline' code='merchant' apiV2Key='22222222222222222222222222222222' apiV3Key='33333333333333333333333333333333'><signing serialNumber='0102' privateKeyFile='merchant.pem'/><verification mode='PublicKey' encryptionKeyId='PUB_KEY_ID_OFFLINE'><keys><key key.name='PUB_KEY_ID_OFFLINE' file='wechat.pem'/></keys></verification></authority></authorities></option></options>";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(OPTIONS));
		using var configuration = (IDisposable)new ConfigurationBuilder().AddOptionStream(stream).Build();
		var options = Zongsoft.Configuration.ConfigurationBinder.GetOption<AuthorityOptionsCollection>((IConfiguration)configuration, "/Externals/Wechat/Authorities").GetDefault();
		Assert.Equal("merchant", options.Code);
		Assert.Equal("0102", options.Signing.SerialNumber);
		Assert.Equal("merchant.pem", options.Signing.PrivateKeyFile);
		Assert.Equal(VerificationMode.PublicKey, options.Verification.Mode);
		Assert.Equal("wechat.pem", Assert.Single(options.Verification.Keys).File);
		Assert.Equal("PUB_KEY_ID_OFFLINE", options.Verification.EncryptionKeyId);
		Assert.Equal(new string('2', 32), options.ApiV2Key);
		Assert.Equal(new string('3', 32), options.ApiV3Key);
	}

	[Fact]
	public void Authority_DoesNotSerializeApiKeys()
	{
		var authority = new Authority("offline", "merchant", new string('2', 32), new string('3', 32));
		Assert.DoesNotContain(authority.ApiV2Key, System.Text.Json.JsonSerializer.Serialize(authority));
		Assert.DoesNotContain(authority.ApiV3Key, System.Text.Json.JsonSerializer.Serialize<IAuthority>(authority));
	}

	[Theory]
	[InlineData(0)]
	[InlineData(16)]
	[InlineData(31)]
	[InlineData(33)]
	public void ApiKeys_RejectInvalidLengths(int length)
	{
		Assert.Throws<ArgumentException>("apiV2Key", () => new Authority("offline", "merchant", new string('2', length)));
		Assert.Throws<ArgumentException>("apiV3Key", () => new Authority("offline", "merchant", apiV3Key: new string('3', length)));
	}

	[Fact]
	public void Signing_RejectsPublicKeysAndWrongRoles()
	{
		using var rsa = RSA.Create(2048);
		Assert.ThrowsAny<CryptographicException>(() => new Authority.SigningCredential("merchant", "0102", rsa.ExportSubjectPublicKeyInfoPem()));
		Assert.Throws<ArgumentException>("serialNumber", () => new Authority.SigningCredential("merchant", "PUB_KEY_ID_OFFLINE", rsa.ExportPkcs8PrivateKeyPem()));
		var signing = new Authority.SigningCredential("merchant", "0102", rsa.ExportPkcs8PrivateKeyPem());
		Assert.Throws<ArgumentException>("signing", () => new Authority("offline", "other-merchant", signing: signing));
		Assert.Throws<ArgumentException>("identifier", () => Authority.VerificationKey.FromPublicKey("0102", rsa.ExportSubjectPublicKeyInfoPem()));
		Assert.Throws<ArgumentException>("pem", () => Authority.VerificationKey.FromPublicKey("PUB_KEY_ID_OFFLINE", rsa.ExportPkcs8PrivateKeyPem()));
	}

	[Fact]
	public void Signing_CopiesBorrowedCertificateAndUsesAtomicIdentity()
	{
		using var rsa = RSA.Create(2048);
		var certificate = Zongsoft.Security.Certificate.FromPem("0102", Encoding.UTF8.GetBytes(rsa.ExportPkcs8PrivateKeyPem()), default);
		Authority.SigningCredential signing;

		try
		{
			signing = Authority.SigningCredential.FromCertificate("offline-merchant", certificate);
		}
		finally
		{
			((IDisposable)certificate).Dispose();
		}

		var data = Encoding.UTF8.GetBytes("offline signed resource");
		Assert.Equal("offline-merchant", signing.MerchantId);
		Assert.Equal("0102", signing.SerialNumber);
		Assert.True(rsa.VerifyData(data, signing.Sign(data), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
	}

	[Fact]
	public void Signing_RejectsExpiredCredential()
	{
		using var rsa = RSA.Create(2048);
		var signing = new Authority.SigningCredential("offline-merchant", "0102", rsa.ExportPkcs8PrivateKeyPem(), new(DateTime.UtcNow.AddDays(-2), DateTime.UtcNow.AddDays(-1)));
		Assert.Throws<CryptographicException>(() => signing.Sign([1, 2, 3]));
	}

	[Fact]
	public async Task PublicKeys_RequireExplicitSelectionAndRemainIsolated()
	{
		using var first = RSA.Create(2048);
		using var second = RSA.Create(2048);
		var a = Authority.VerificationKey.FromPublicKey("PUB_KEY_ID_A", first.ExportSubjectPublicKeyInfoPem());
		var b = Authority.VerificationKey.FromPublicKey("PUB_KEY_ID_B", second.ExportSubjectPublicKeyInfoPem());
		Assert.Throws<ArgumentException>("encryptionKeyId", () => new Authority.Verifier(VerificationMode.PublicKey, [a, b]));
		var options = new Authority.Verifier(VerificationMode.PublicKey, [a, b], b.Identifier);
		var merchant = new Authority("tenant-a", "same-merchant", verification: options);
		var other = new Authority("tenant-b", "same-merchant", verification: new(VerificationMode.PublicKey, [a]));
		Assert.NotSame(options, merchant.Verification);
		Assert.Same(b, await merchant.GetCertificateAsync(TestContext.Current.CancellationToken));
		Assert.Same(a, await merchant.GetCertificateAsync(a.Identifier, TestContext.Current.CancellationToken));
		Assert.Null(await other.GetCertificateAsync(b.Identifier, TestContext.Current.CancellationToken));
		Assert.Null(await merchant.GetCertificateAsync("untrusted", TestContext.Current.CancellationToken));
		var ciphertext = b.Encrypt("合成测试");
		Assert.Equal("合成测试", Encoding.UTF8.GetString(second.Decrypt(ciphertext, RSAEncryptionPadding.OaepSHA1)));
	}

	[Fact]
	public void Verification_RejectsMixedModesAndExpiredCertificates()
	{
		using var rsa = RSA.Create(2048);
		using var certificate = new CertificateRequest("CN=offline-platform", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
			.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-2), DateTimeOffset.UtcNow.AddDays(-1));
		var key = Authority.VerificationKey.FromCertificate(certificate.ExportCertificatePem());
		Assert.Throws<ArgumentException>("keys", () => new Authority.Verifier(VerificationMode.PublicKey, [key]));
		Assert.Throws<ArgumentException>("keys", () => new Authority.Verifier(VerificationMode.PlatformCertificate, []));
		Assert.Throws<ArgumentException>("keys", () => new Authority.Verifier(VerificationMode.PlatformCertificate, [key, key]));
		Assert.False(key.Verify([1], [2]));
		Assert.Throws<CryptographicException>(() => key.Encrypt("offline"));
	}

	[Fact]
	public void Managers_KeepMerchantSnapshotsSeparate()
	{
		using var rsa = RSA.Create(2048);
		var first = new Authority("tenant-a", "same-code", signing: new("same-code", "0102", rsa.ExportPkcs8PrivateKeyPem()));
		var second = new Authority("tenant-b", "same-code", signing: new("same-code", "0304", rsa.ExportPkcs8PrivateKeyPem()));
		var a = PaymentManager.Get(first);
		var b = PaymentManager.Get(second);
		Assert.NotSame(a, b);
		Assert.Same(first, a.Payment.Authority);
		Assert.Same(second, b.Payment.Authority);
		Assert.Throws<ArgumentException>("authority", () => Paying.HttpClientFactory.GetHttpClient(first));
	}

	[Fact]
	public async Task Configuration_ReadsExplicitFilesAndPublishesCompleteSnapshot()
	{
		var directory = Directory.CreateTempSubdirectory("wechat-keys-");
		using var first = RSA.Create(2048);
		using var second = RSA.Create(2048);
		using var certificate = new CertificateRequest("CN=offline-merchant", first, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
			.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(5));
		var cancellation = TestContext.Current.CancellationToken;

		try
		{
			var options = new AuthorityOptions
			{
				Name = "offline",
				Code = "offline-merchant",
				ApiV2Key = new string('2', 32),
				ApiV3Key = new string('3', 32),
				Signing = new() { SerialNumber = certificate.SerialNumber, PrivateKeyFile = Path.Combine(directory.FullName, "private.pem"), CertificateFile = Path.Combine(directory.FullName, "merchant.pem") },
				Verification = new() { Mode = VerificationMode.PublicKey },
			};
			var publicFile = Path.Combine(directory.FullName, "wechat.pem");
			options.Verification.Keys.Add(new() { Name = "PUB_KEY_ID_OFFLINE", File = publicFile });
			await File.WriteAllTextAsync(options.Signing.PrivateKeyFile, first.ExportPkcs8PrivateKeyPem(), cancellation);
			await File.WriteAllTextAsync(options.Signing.CertificateFile, certificate.ExportCertificatePem(), cancellation);
			await File.WriteAllTextAsync(publicFile, second.ExportSubjectPublicKeyInfoPem(), cancellation);
			IAuthority snapshot;

			using(var reader = File.Open(publicFile, FileMode.Open, FileAccess.Read, FileShare.Read))
				snapshot = await AuthorityUtility.CreateAsync(options, cancellation);

			Assert.Equal(options.ApiV2Key, snapshot.ApiV2Key);
			Assert.Equal(options.ApiV3Key, snapshot.ApiV3Key);
			Assert.Equal(certificate.SerialNumber, snapshot.Signing.SerialNumber);

			await File.WriteAllTextAsync(options.Signing.PrivateKeyFile, second.ExportPkcs8PrivateKeyPem(), cancellation);
			await Assert.ThrowsAsync<CryptographicException>(() => AuthorityUtility.CreateAsync(options, cancellation).AsTask());
			Assert.True(first.VerifyData(new byte[] { 1, 2, 3 }, snapshot.Signing.Sign([1, 2, 3]), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));

			options.Signing.CertificateFile = null;
			options.Signing.SerialNumber = "0304";
			var replacement = await AuthorityUtility.CreateAsync(options, cancellation);
			Assert.Equal("0304", replacement.Signing.SerialNumber);
			Assert.True(second.VerifyData(new byte[] { 1, 2, 3 }, replacement.Signing.Sign([1, 2, 3]), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
			using var canceled = new CancellationTokenSource();
			canceled.Cancel();
			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => AuthorityUtility.CreateAsync(options, canceled.Token).AsTask());
		}
		finally
		{
			directory.Delete(true);
		}
	}
}
