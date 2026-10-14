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

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

using Scalar.AspNetCore;
using Zongsoft.Services;

namespace Zongsoft.Web.OpenApi;

[Service<IApplicationInitializer<IApplicationBuilder>>]
public class WebInitializer : IApplicationInitializer<IApplicationBuilder>
{
	#region 初始方法
	public void Initialize(IApplicationBuilder builder)
	{
		if(builder is IEndpointRouteBuilder app)
		{
			app.UseOpenApi();
			app.MapScalarApiReference(options =>
			{
				options.PersistentAuthentication = true;
				options.WithDefaultHttpClient(ScalarTarget.Http, ScalarClient.Http11);
				options.Authentication = new() { PreferredSecuritySchemes = ["credential"] };
				options.AddApiKeyAuthentication("credential", scheme =>
				{
					scheme.WithValue($"Credential {Common.Randomizer.GenerateString(18)}");
				});
			});
		}
	}
	#endregion
}
