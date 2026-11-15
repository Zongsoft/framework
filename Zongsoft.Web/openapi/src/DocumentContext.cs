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
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;

using Zongsoft.Services;
using Zongsoft.Configuration;

namespace Zongsoft.Web.OpenApi;

public class DocumentContext
{
	#region 构造函数
	internal DocumentContext(DocumentFormat format, IApplicationContext application)
	{
		if(format == null)
			throw new ArgumentNullException(nameof(format));

		this.Format = format;
		this.Application = application ?? throw new ArgumentNullException(nameof(application));
		this.Services = application.Services;
		this.Configuration = application.Services.Resolve<IConfiguration>() ?? application.Configuration;
	}
	#endregion

	#region 公共属性
	public OpenApiDocument Document { get; internal set; }
	public DocumentFormat Format { get; }
	public IServiceProvider Services { get; }
	public IConfiguration Configuration { get; }
	internal IApplicationContext Application { get; }
	internal string PathBase { get; set; }
	internal Configuration.EnvironmentOption Environment { get; init; }

	/// <summary>获取运行时路由表（用于解析约定式路由的接口路径）。</summary>
	public EndpointDataSource Routing { get; internal set; }
	#endregion
}

public static partial class DocumentContextExtension
{
	#region 常量定义
	internal const string SERVERS_PATH = "Web:OpenAPI:Servers";
	internal const string ENVIRONMENTS_PATH = "Web:OpenAPI:Environments";
	internal const string AUTHENTICATION_PATH = "Web:OpenAPI:Authentication";
	#endregion

	#region 公共方法
	public static IReadOnlyCollection<Configuration.HeaderOption> GetHeaders(this IConfiguration configuration)
	{
		return configuration.GetOption<Configuration.HeaderOptionCollection>("/Web/OpenAPI/Headers") ?? [];
	}

	public static IReadOnlyCollection<Configuration.ServerOption> GetServers(this IConfiguration configuration)
	{
		return configuration?.GetOption<Configuration.ServerOptionCollection>(SERVERS_PATH) ?? [];
	}

	public static IReadOnlyCollection<Configuration.EnvironmentOption> GetEnvironments(this IConfiguration configuration)
	{
		return configuration?.GetOption<Configuration.EnvironmentOptionCollection>(ENVIRONMENTS_PATH) ?? [];
	}
	#endregion

	#region 内部方法
	internal static IEnumerable<Configuration.EnvironmentOption> GetEnvironmentsInDeclarationOrder(this IConfiguration configuration)
	{
		if(configuration == null)
			return [];

		var section = configuration.GetSection(ENVIRONMENTS_PATH);
		return configuration.GetOptionsInDeclarationOrder(section, configuration.GetEnvironments(), environment => environment.Name)
			.Select(entry => entry.Option);
	}

	internal static IEnumerable<(T Option, IConfigurationSection Section)> GetOptionsInDeclarationOrder<T>(this IConfiguration configuration, IConfigurationSection section, IEnumerable<T> options, Func<T, string> getName)
	{
		var remaining = options.ToDictionary(getName, StringComparer.OrdinalIgnoreCase);
		if(remaining.Count == 0)
			yield break;

		foreach(var child in configuration.GetSectionsInDeclarationOrder(section))
		{
			if(remaining.Remove(child["name"] ?? child.Key, out var option))
				yield return (option, child);
		}

		foreach(var option in remaining.Values)
			yield return (option, null);
	}

	internal static IEnumerable<IConfigurationSection> GetSectionsInDeclarationOrder(this IConfiguration configuration, IConfigurationSection section)
	{
		if(section == null)
			yield break;

		var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		if(configuration is IConfigurationRoot root)
		{
			foreach(var key in root.GetDeclaredOptionKeys(section.Path))
			{
				keys.Add(key);
				yield return section.GetSection(key);
			}
		}

		foreach(var child in section.GetChildren())
		{
			if(keys.Add(child.Key))
				yield return child;
		}
	}
	#endregion
}
