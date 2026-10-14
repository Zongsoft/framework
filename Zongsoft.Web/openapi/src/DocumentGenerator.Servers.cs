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
using System.Collections.Generic;

using Microsoft.OpenApi;

namespace Zongsoft.Web.OpenApi;

partial class DocumentGenerator
{
	internal static void GenerateServers(this DocumentContext context)
	{
		var servers = context.Configuration.GetServers();

		foreach(var server in servers)
		{
			var apiServer = new OpenApiServer()
			{
				Url = server.Url,
				Description = server.Name
			};

			foreach(var variable in server.Variables)
			{
				apiServer.Variables ??= new Dictionary<string, OpenApiServerVariable>();
				apiServer.Variables.Add(variable.Name, new OpenApiServerVariable()
				{
					Default = variable.Default,
					Enum = [.. variable.Values ?? []],
				});
			}

			context.Document.Servers.Add(apiServer);
		}
	}
}
