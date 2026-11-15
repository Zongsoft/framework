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
using System.Linq;
using System.Collections.Generic;

using Microsoft.OpenApi;
using Microsoft.Extensions.Configuration;

using Zongsoft.Configuration;

namespace Zongsoft.Web.OpenApi;

partial class DocumentGenerator
{
	internal static void GenerateServers(this DocumentContext context)
	{
		if(context.Environment != null)
		{
			var environment = context.Environment;
			context.Document.Servers.Add(new OpenApiServer()
			{
				Name = environment.Name,
				Description = environment.Description ?? environment.Name,
				Url = environment.Url,
			});
			return;
		}

		var configuration = context.Configuration;
		var section = configuration.GetSection(DocumentContextExtension.SERVERS_PATH);
		var servers = configuration.GetOptionsInDeclarationOrder(section, configuration.GetServers(), server => server.Name).ToArray();
		var (current, currentSection) = servers.FirstOrDefault(server => string.Equals(server.Option.Name, "current", StringComparison.OrdinalIgnoreCase));
		var currentServer = CreateServer(configuration, currentSection, current ?? new Configuration.ServerOption("current", null));
		currentServer.Description = current?.Description ?? Properties.Resources.ServerCurrent_Description;

		if(string.IsNullOrEmpty(currentServer.Url))
			currentServer.Url = string.IsNullOrEmpty(context.PathBase) ? "/" : context.PathBase;

		context.Document.Servers.Add(currentServer);

		foreach(var (server, serverSection) in servers)
		{
			if(server == current)
				continue;

			context.Document.Servers.Add(CreateServer(configuration, serverSection, server));
		}
	}

	private static OpenApiServer CreateServer(IConfiguration configuration, IConfigurationSection section, Configuration.ServerOption server)
	{
		var apiServer = new OpenApiServer()
		{
			Name = server.Name,
			Url = server.Url,
			Description = server.Description ?? server.Name,
		};

		//按 URL 模板中的首次出现位置排列变量，未使用的变量放在最后
		var variables = configuration.GetOptionsInDeclarationOrder(section, server.Variables, variable => variable.Name)
			.OrderBy(variable => GetPosition(server.Url, variable.Option.Name));

		foreach(var (variable, variableSection) in variables)
		{
			var allowed = variable.Values;

			if(allowed is { Count: > 0 } && !allowed.Contains(variable.Default ?? string.Empty))
				throw new InvalidOperationException(string.Format(Properties.Resources.ServerVariableDefaultInvalid_Message, server.Name, variable.Name));

			if(allowed is { Count: > 1 } && variableSection != null)
			{
				allowed = configuration.GetSectionsInDeclarationOrder(variableSection.GetSection("Value"))
					.Where(value => value.IsCollection())
					.Select(value => value.Value).ToList();
			}

			apiServer.Variables ??= new Dictionary<string, OpenApiServerVariable>();
			apiServer.Variables.Add(variable.Name, new OpenApiServerVariable()
			{
				Default = variable.Default ?? string.Empty,
				Description = variable.Description,
				Enum = allowed is { Count: > 0 } ? [.. allowed] : null,
			});
		}

		return apiServer;

		static int GetPosition(string url, string name)
		{
			var position = url?.IndexOf($"{{{name}}}", StringComparison.Ordinal) ?? -1;
			return position < 0 ? int.MaxValue : position;
		}
	}
}
