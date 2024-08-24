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
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using Zongsoft.Security;

namespace Zongsoft.Externals.Wechat
{
	internal static class Json
	{
		public static readonly JsonSerializerOptions Options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
		{
			IncludeFields = true,
			NumberHandling = JsonNumberHandling.AllowReadingFromString,
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
			Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
		};

		internal class CryptographyConverter : JsonConverter<string>
		{
			private Lazy<ICertificate> _certificate;

			public CryptographyConverter()
			{
				_certificate = new Lazy<ICertificate>(() => AuthorityUtility.GetAuthority().GetCertificateAsync().AsTask().ConfigureAwait(false).GetAwaiter().GetResult());
			}

			public override string Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
			{
				return reader.GetString();
			}

			public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
			{
				writer.WriteBase64StringValue(_certificate.Value.Encrypt(value));
			}
		}

		internal class DateConverter : JsonConverter<DateTime?>
		{
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
					writer.WriteStringValue("长期");
				else
					writer.WriteStringValue(value.Value.ToString("yyyy-MM-dd"));
			}
		}
	}
}
