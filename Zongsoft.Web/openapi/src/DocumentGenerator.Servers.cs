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
