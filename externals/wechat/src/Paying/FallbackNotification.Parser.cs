/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@qq.com>
 *
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.WeChat library.
 *
 * The Zongsoft.Externals.WeChat is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.WeChat is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.WeChat library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;

using Zongsoft.Services;

namespace Zongsoft.Externals.Wechat.Paying;

public sealed partial class FallbackNotification
{
	internal static class Parser
	{
		internal const int MAXIMUM_LENGTH = 2 * 1024 * 1024;

		public static async ValueTask<IAuthority> GetAuthorityAsync(IServiceProvider services, string code, CancellationToken cancellation)
		{
			var options = Utility.GetOptions<Options.AuthorityOptionsCollection>("/Externals/Wechat/Authorities");
			var matches = options?.Where(option => option.Code == code).ToArray();
			if(matches?.Length > 1)
				throw new InvalidOperationException();
			if(matches?.Length == 1)
				return await AuthorityUtility.CreateAsync(matches[0], cancellation);

			foreach(var provider in services.ResolveAll<IServiceProvider<IAuthority>>())
			{
				var authority = provider.GetService(code);
				if(authority != null)
					return authority;
			}

			return null;
		}

		public static async ValueTask<FallbackNotification> ParseAsync(FallbackKind kind, FallbackRequest request, IAuthority authority, CancellationToken cancellation)
		{
			if(authority == null || authority.Verification == null || string.IsNullOrEmpty(authority.ApiV3Key) ||
				!string.Equals(authority.Code, request.Merchant, StringComparison.Ordinal))
				throw new CryptographicException();

			var serial = GetHeader("Wechatpay-Serial");
			var timestamp = GetHeader("Wechatpay-Timestamp");
			var nonce = GetHeader("Wechatpay-Nonce");
			var signature = GetHeader("Wechatpay-Signature");

			if(request.Headers.ContainsKey("Wechatpay-Signature-Type") && GetHeader("Wechatpay-Signature-Type") != "WECHATPAY2-SHA256-RSA2048")
				throw new CryptographicException();

			var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
			if(!long.TryParse(timestamp, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds) || seconds < now - 300 || seconds > now + 300)
				throw new CryptographicException();

			using var buffer = new MemoryStream();
			var block = new byte[8192];
			int count;

			while((count = await request.Body.ReadAsync(block, cancellation)) > 0)
			{
				if(buffer.Length + count > MAXIMUM_LENGTH)
					throw new CryptographicException();
				buffer.Write(block, 0, count);
			}

			var body = buffer.ToArray();
			var prefix = Encoding.UTF8.GetBytes($"{timestamp}\n{nonce}\n");
			var message = new byte[prefix.Length + body.Length + 1];
			prefix.CopyTo(message, 0);
			body.CopyTo(message, prefix.Length);
			message[^1] = (byte)'\n';
			var key = await authority.GetCertificateAsync(serial, cancellation);

			if(key == null || !key.Verify(message, Convert.FromBase64String(signature)))
				throw new CryptographicException();

			using var envelope = JsonDocument.Parse(body);
			ValidateObject(envelope.RootElement);
			var identifier = GetString(envelope.RootElement, "id");
			var eventType = GetString(envelope.RootElement, "event_type");

			if(identifier.Length > 128 || GetString(envelope.RootElement, "resource_type") != "encrypt-resource")
				throw new CryptographicException();

			var resource = envelope.RootElement.GetProperty("resource");
			ValidateObject(resource);
			if(GetString(resource, "algorithm") != "AEAD_AES_256_GCM")
				throw new CryptographicException();

			var originalType = GetString(resource, "original_type");
			var accepted = kind switch
			{
				FallbackKind.Payment => eventType == "TRANSACTION.SUCCESS" && originalType == "transaction",
				FallbackKind.Refundment => eventType is "REFUND.SUCCESS" or "REFUND.ABNORMAL" or "REFUND.CLOSED" && originalType == "refund",
				FallbackKind.OfflineFaceAgreement => eventType == "FACEPAY.USER_STATE_CHANGE" && originalType == "facepay",
				_ => false,
			};

			if(!accepted)
				throw new CryptographicException();

			var ciphertext = GetString(resource, "ciphertext");
			if(ciphertext.Length > 1048576)
				throw new CryptographicException();

			var data = CryptographyHelper.Decrypt1(authority.ApiV3Key, GetString(resource, "nonce"), GetString(resource, "associated_data", false), ciphertext);

			try
			{
				using var document = JsonDocument.Parse(data);
				var payload = document.RootElement;
				ValidateObject(payload);

				var broker = payload.TryGetProperty("sp_mchid", out _);
				var merchant = GetString(payload, kind == FallbackKind.OfflineFaceAgreement ? "mch_id" : broker ? "sp_mchid" : "mchid");

				if(merchant != authority.Code || (broker && payload.TryGetProperty("mchid", out _)) || (kind == FallbackKind.OfflineFaceAgreement && broker))
					throw new CryptographicException();

				var subsidiary = broker ? GetString(payload, "sub_mchid") : null;
				if(kind != FallbackKind.Refundment)
				{
					var applet = GetString(payload, broker ? "sp_appid" : "appid");
					if(authority.Accounts.Default.Code != applet && !authority.Accounts.Any(account => account.Code == applet))
						throw new CryptographicException();
				}

				if(kind == FallbackKind.Payment && GetString(payload, "trade_state") != "SUCCESS" ||
					kind == FallbackKind.Refundment && eventType != $"REFUND.{GetString(payload, "refund_status")}")
					throw new CryptographicException();

				if(kind == FallbackKind.Refundment && payload.TryGetProperty("status", out var status) && status.GetString() != GetString(payload, "refund_status"))
					throw new CryptographicException();

				return new(kind, identifier, eventType, merchant, subsidiary, broker ? "broker" : "direct", payload);
			}
			finally
			{
				CryptographicOperations.ZeroMemory(data);
			}

			string GetHeader(string name)
			{
				if(!request.Headers.TryGetValue(name, out var values) || values.Length != 1 || string.IsNullOrWhiteSpace(values[0]) ||
				   values[0].Length > 1024 || values[0].Contains('\r') || values[0].Contains('\n'))
					throw new CryptographicException();

				return values[0];
			}
		}

		private static string GetString(JsonElement element, string name, bool required = true)
		{
			if(!element.TryGetProperty(name, out var property))
			{
				if(!required)
					return null;

				throw new CryptographicException();
			}

			if(property.ValueKind != JsonValueKind.String || required && string.IsNullOrWhiteSpace(property.GetString()))
				throw new CryptographicException();

			return property.GetString();
		}

		private static void ValidateObject(JsonElement element)
		{
			if(element.ValueKind != JsonValueKind.Object)
				throw new CryptographicException();

			var names = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);

			foreach(var property in element.EnumerateObject())
			{
				if(!names.Add(property.Name))
					throw new CryptographicException();

				if(property.Value.ValueKind == JsonValueKind.Object)
					ValidateObject(property.Value);
			}
		}
	}
}
