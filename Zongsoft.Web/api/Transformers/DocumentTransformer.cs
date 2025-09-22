﻿/*
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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Zongsoft.Services;
using Zongsoft.Configuration;

namespace Zongsoft.Web.OpenApi.Transformers;

public class DocumentTransformer : IOpenApiDocumentTransformer
{
	public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellation)
	{
		var servers = GetServers(context.ApplicationServices.Resolve<IConfiguration>() ?? ApplicationContext.Current.Configuration);

		foreach(var server in servers)
		{
			var openApiServer = new OpenApiServer()
			{
				Url = server.Url,
				Description = server.Name
			};

			foreach(var variable in server.Variables)
			{
				openApiServer.Variables.Add(variable.Name, new OpenApiServerVariable()
				{
					Default = variable.Default,
					Enum = [.. variable.Values],
				});
			}

			document.Servers.Add(openApiServer);
		}

		return Task.CompletedTask;
	}

	static IEnumerable<Configuration.ServerOption> GetServers(IConfiguration configuration)
	{
		if(configuration == null)
			throw new ArgumentNullException(nameof(configuration));

		var servers = configuration.GetOption<Configuration.ServerOptionCollection>("/Web/OpenAPI");

		foreach(var server in servers)
			yield return server;
	}
}
