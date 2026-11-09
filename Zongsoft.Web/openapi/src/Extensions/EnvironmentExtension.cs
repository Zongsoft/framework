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

		writer.WritePropertyName("variables");
		writer.WriteStartObject();

		for(int i = 0; i < environment.Variables.Count; i++)
			Helper.Variable(environment.Variables[i]).Write(writer, version);

		writer.WriteEndObject();
		writer.WriteEndObject();
	}
}
