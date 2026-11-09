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
	public static IOpenApiExtension Variable(Configuration.VariableOption variable) => new VariableExtension(variable);
}

public class VariableExtension(Configuration.VariableOption variable) : IOpenApiExtension
{
	public void Write(IOpenApiWriter writer, OpenApiSpecVersion version)
	{
		writer.WritePropertyName(variable.Name);
		writer.WriteStartObject();
		writer.WriteProperty("default", variable.Default);

		if(!string.IsNullOrEmpty(variable.Description))
			writer.WriteProperty("description", variable.Description);

		if(variable.Values != null && variable.Values.Count > 0)
		{
			writer.WritePropertyName("enum");
			writer.WriteStartArray();
			foreach(var value in variable.Values)
				writer.WriteValue(value);
			writer.WriteEndArray();
		}

		writer.WriteEndObject();
	}
}
