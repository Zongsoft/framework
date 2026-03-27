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
 * Copyright (C) 2020-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.Velopack library.
 *
 * The Zongsoft.Externals.Velopack is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Velopack is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Velopack library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

using Velopack;

using Zongsoft.Services;

namespace Zongsoft.Externals.Velopack.Web;

[Service<IApplicationInitializer>(Members = nameof(Initializer))]
public class NugetVersionConverter : JsonConverter<SemanticVersion>
{
	public override SemanticVersion Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
	{
		return reader.TokenType switch
		{
			JsonTokenType.Null => null,
			JsonTokenType.String => SemanticVersion.Parse(reader.GetString()),
			_ => null,
		};
	}

	public override void Write(Utf8JsonWriter writer, SemanticVersion value, JsonSerializerOptions options)
	{
		if(value == null)
			writer.WriteNullValue();
		else
			writer.WriteStringValue(value.ToFullString());
	}

	public static readonly IApplicationInitializer Initializer = new JsonInitializer();

	public sealed class JsonInitializer : IApplicationInitializer
	{
		public void Initialize(IApplicationContext context)
		{
			Serialization.Serializer.Json.Options.Converters.Add(new NugetVersionConverter());
		}
	}
}
