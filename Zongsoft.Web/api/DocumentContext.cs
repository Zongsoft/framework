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