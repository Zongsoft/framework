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
