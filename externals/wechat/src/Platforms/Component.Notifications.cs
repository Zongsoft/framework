using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Text;
using System.Globalization;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Externals.Wechat.Platforms;

public sealed partial class Component
{
	private async ValueTask<Notification> ReadNotificationAsync(Stream body, string timestamp, string nonce, string signature, CancellationToken cancellation)
	{
		ArgumentNullException.ThrowIfNull(body);
		var now = DateTimeOffset.UtcNow;

		if(string.IsNullOrEmpty(timestamp) || timestamp.Length > 16 || !long.TryParse(timestamp, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds) ||
			Math.Abs(now.ToUnixTimeSeconds() - seconds) > 300 || string.IsNullOrEmpty(nonce) || nonce.Length > 256 || signature?.Length != 40)
			throw new CryptographicException();

		using var buffer = new MemoryStream();
		var chunk = new byte[8192];
		int count;

		while((count = await body.ReadAsync(chunk, cancellation)) != 0)
		{
			if(buffer.Length + count > 256 * 1024)
				throw new FormatException();

			buffer.Write(chunk, 0, count);
		}

		var envelope = ReadXml(new UTF8Encoding(false, true).GetString(buffer.GetBuffer(), 0, (int)buffer.Length));
		var ciphertext = Required(envelope, "Encrypt");

		if(!CryptographyUtility.VerifySignature(_token, timestamp, nonce, ciphertext, signature))
			throw new CryptographicException();
		if(envelope.TryGetValue("AppId", out var outerAppId) && outerAppId != this.AppId)
			throw new CryptographicException();

		var fields = ReadXml(CryptographyUtility.Decrypt(ciphertext, _encodingAESKey, this.AppId));

		if(Required(fields, "AppId") != this.AppId)
			throw new CryptographicException();

		var created = DateTimeOffset.FromUnixTimeSeconds(long.Parse(Required(fields, "CreateTime"), NumberStyles.None, CultureInfo.InvariantCulture));

		if(created > now.AddMinutes(5))
			throw new FormatException();

		var kind = Required(fields, "InfoType") switch
		{
			"component_verify_ticket" => NotificationKind.VerificationTicket,
			"authorized" => NotificationKind.Authorized,
			"updateauthorized" => NotificationKind.Updated,
			"unauthorized" => NotificationKind.Unauthorized,
			_ => throw new FormatException(),
		};
		var authorizer = kind == NotificationKind.VerificationTicket ? null : Required(fields, "AuthorizerAppid");
		var value = kind switch
		{
			NotificationKind.VerificationTicket => Required(fields, "ComponentVerifyTicket"),
			NotificationKind.Authorized or NotificationKind.Updated => Required(fields, "AuthorizationCode"),
			_ => null,
		};
		var expiration = kind is NotificationKind.Authorized or NotificationKind.Updated ?
			(DateTimeOffset?)DateTimeOffset.FromUnixTimeSeconds(long.Parse(Required(fields, "AuthorizationCodeExpiredTime"), NumberStyles.None, CultureInfo.InvariantCulture)) : null;

		if(authorizer?.Length > 64 || value?.Length > 4096 || expiration <= created)
			throw new FormatException();

		fields.TryGetValue("PreAuthCode", out var preauthorizationCode);
		string[] identity = [this.AppId, kind.ToString(), created.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture), authorizer, value, expiration?.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture), preauthorizationCode];
		var canonical = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(identity);

		try
		{
			return new Notification
			{
				Id = Convert.ToHexString(SHA256.HashData(canonical)),
				AppId = this.AppId,
				Kind = kind,
				CreatedTime = created,
				AuthorizerAppId = authorizer,
				Value = value,
				Expiration = expiration,
				PreauthorizationCode = preauthorizationCode,
			};
		}
		finally { CryptographicOperations.ZeroMemory(canonical); }
	}

	private static Dictionary<string, string> ReadXml(string content)
	{
		using var text = new StringReader(content);
		using var reader = XmlReader.Create(text, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 256 * 1024 });
		var document = XDocument.Load(reader, LoadOptions.None);

		if(document.Root?.Name != "xml" || document.Root.HasAttributes)
			throw new FormatException();

		var values = new Dictionary<string, string>(StringComparer.Ordinal);

		foreach(var element in document.Root.Elements())
		{
			if(element.HasElements || element.HasAttributes || element.Name.Namespace != XNamespace.None || !values.TryAdd(element.Name.LocalName, element.Value))
				throw new FormatException();
		}

		return values;
	}

	private static string Required(Dictionary<string, string> values, string name) =>
		values.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value) ? value : throw new FormatException();
}
