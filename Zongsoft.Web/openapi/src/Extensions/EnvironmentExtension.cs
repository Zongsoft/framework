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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;

using Microsoft.OpenApi;

namespace Zongsoft.Web.OpenApi.Extensions;

static partial class Helper
{
	public static IOpenApiExtension Environment(Configuration.EnvironmentOption environment) => new EnvironmentExtension(environment);
}

public class EnvironmentExtension(Configuration.EnvironmentOption environment) : IOpenApiExtension
{
	public void Write(IOpenApiWriter writer, OpenApiSpecVersion version)
	{
		writer.WritePropertyName(environment.Name);
		writer.WriteStartObject();

		if(!string.IsNullOrEmpty(environment.Description))
			writer.WriteProperty("description", environment.Description);

		writer.WriteProperty("color", "#FFFFFF");
		writer.WritePropertyName("variables");
		writer.WriteStartArray();

		foreach(var variable in environment.Variables)
		{
			writer.WriteStartObject();
			writer.WriteProperty("name", variable.Name);
			writer.WritePropertyName("value");
			writer.WriteStartObject();
			writer.WriteProperty("default", variable.Default ?? string.Empty);

			if(!string.IsNullOrEmpty(variable.Description))
				writer.WriteProperty("description", variable.Description);

			writer.WriteEndObject();
			writer.WriteEndObject();
		}

		writer.WriteEndArray();
		writer.WriteEndObject();
	}
}
