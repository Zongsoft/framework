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
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using Zongsoft.Security;

namespace Zongsoft.Externals.Wechat;

internal static class Json
{
	public static readonly JsonSerializerOptions Options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
	{
		IncludeFields = true,
		NumberHandling = JsonNumberHandling.AllowReadingFromString,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
	};

	internal static JsonSerializerOptions GetOptions(Authority.VerificationKey certificate)
	{
		var resolver = new DefaultJsonTypeInfoResolver();

		resolver.Modifiers.Add(info =>
		{
			foreach(var property in info.Properties)
			{
				if(property.CustomConverter is CryptographyConverter)
					property.CustomConverter = new CryptographyConverter(certificate);
			}
		});

		return new JsonSerializerOptions(Options) { TypeInfoResolver = resolver };
	}

	internal class CryptographyConverter : JsonConverter<string>
	{
		private readonly Authority.VerificationKey _certificate;
		public CryptographyConverter() { }
		public CryptographyConverter(Authority.VerificationKey certificate) => _certificate = certificate;

		public override string Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) => reader.GetString();
		public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
		{
			var certificate = _certificate ?? throw new InvalidOperationException(Properties.Resources.Wechat_CertificateNotFound_Message);
			writer.WriteBase64StringValue(certificate.Encrypt(value));
		}
	}

	internal class DateConverter : JsonConverter<DateTime?>
	{
		public override bool HandleNull => true;

		public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			var raw = reader.GetString();

			if(raw != null && raw.Length > 0 && DateTime.TryParse(raw, out var date))
				return date;

			return null;
		}

		public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
		{
			if(value == null)
				writer.WriteStringValue(Properties.Resources.Wechat_CertificatePermanentValue);
			else
				writer.WriteStringValue(value.Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
		}
	}
}
