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
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;

using Zongsoft.Services;
using Zongsoft.Configuration;

namespace Zongsoft.Web.OpenApi;

public class DocumentContext
{
	#region 构造函数
	internal DocumentContext(DocumentFormat format)
	{
		if(format == null)
			throw new ArgumentNullException(nameof(format));

		this.Format = format;
		this.Services = ApplicationContext.Current.Services;
		this.Configuration = ApplicationContext.Current.Services.Resolve<IConfiguration>() ?? ApplicationContext.Current.Configuration;
	}
	#endregion

	#region 公共属性
	public OpenApiDocument Document { get; internal set; }
	public DocumentFormat Format { get; }
	public IServiceProvider Services { get; }
	public IConfiguration Configuration { get; }

	/// <summary>获取运行时路由表（用于解析约定式路由的接口路径）。</summary>
	public EndpointDataSource Routing { get; internal set; }
	#endregion
}

public static partial class DocumentContextExtension
{
	public static IReadOnlyCollection<Configuration.HeaderOption> GetHeaders(this IConfiguration configuration)
	{
		return configuration.GetOption<Configuration.HeaderOptionCollection>("/Web/OpenAPI/Headers") ?? [];
	}

	public static IReadOnlyCollection<Configuration.ServerOption> GetServers(this IConfiguration configuration)
	{
		return configuration?.GetOption<Configuration.ServerOptionCollection>("/Web/OpenAPI/Servers") ?? [];
	}

	public static IReadOnlyCollection<Configuration.EnvironmentOption> GetEnvironments(this IConfiguration configuration)
	{
		return configuration?.GetOption<Configuration.EnvironmentOptionCollection>("/Web/OpenAPI/Environments") ?? [];
	}
}