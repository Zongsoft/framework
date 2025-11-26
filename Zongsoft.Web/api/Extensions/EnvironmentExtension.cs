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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Web.OpenApi library.
 *
 * The Zongsoft.Web.OpenApi is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Web.OpenApi is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Web.OpenApi library. If not, see <http://www.gnu.org/licenses/>.
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
