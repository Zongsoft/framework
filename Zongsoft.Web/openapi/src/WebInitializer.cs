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
using System.Threading;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;

using Scalar.AspNetCore;

using Zongsoft.Services;
using Zongsoft.Configuration;

namespace Zongsoft.Web.OpenApi;

[Service<IApplicationInitializer<IApplicationBuilder>>]
public class WebInitializer : IApplicationInitializer<IApplicationBuilder>
{
	#region 常量定义
	private const string ENVIRONMENTS_DOCUMENT_ROUTE = "openapi/environments/{documentName}";
	private const string ENVIRONMENT_NONE = "none";
	#endregion

	#region 初始方法
	public void Initialize(IApplicationBuilder builder)
	{
		if(builder is not IEndpointRouteBuilder app)
			return;

		var configuration = builder.ApplicationServices.Resolve<IConfiguration>();
		var authentication = configuration?.GetOption<Configuration.AuthenticationOption>(DocumentContextExtension.AUTHENTICATION_PATH);
		var environments = GetEnvironmentDocuments(configuration);

		//手动文档和各环境文档共享控制器模型
		var gate = new SemaphoreSlim(1);
		app.UseOpenApi(null, null, gate);

		if(environments.Count > 0)
			app.UseOpenApi("/" + ENVIRONMENTS_DOCUMENT_ROUTE + ".{extension}", environments, gate);

		app.MapScalarApiReference(ConfigureScalar);

		void ConfigureScalar(ScalarOptions options)
		{
			options.Layout = ScalarLayout.Modern;
			options.DefaultOpenAllTags = false;
			options.PersistentAuthentication = authentication?.Persisted ?? false;
			options.WithDefaultHttpClient(ScalarTarget.Http, ScalarClient.Http11);
			options.AddDocument("v1", ENVIRONMENT_NONE, "openapi/v1.json", true);

			foreach(var environment in environments.Values)
				options.AddDocument(environment.Name, environment.Name, ENVIRONMENTS_DOCUMENT_ROUTE + ".json");
		}
	}
	#endregion

	#region 私有方法
	private static IReadOnlyDictionary<string, Configuration.EnvironmentOption> GetEnvironmentDocuments(IConfiguration configuration)
	{
		var environments = new OrderedDictionary<string, Configuration.EnvironmentOption>(StringComparer.OrdinalIgnoreCase);

		foreach(var environment in configuration.GetEnvironmentsInDeclarationOrder())
		{
			if(string.IsNullOrWhiteSpace(environment.Url))
				continue;

			if(!IsEnvironmentName(environment.Name) || !TryGetBaseAddress(environment.Url, out var uri))
				throw new InvalidOperationException(string.Format(Properties.Resources.EnvironmentDocumentInvalid_Message, environment.Name));

			if(environment.Name == ENVIRONMENT_NONE)
				throw new InvalidOperationException(string.Format(Properties.Resources.EnvironmentDocumentNameReserved_Message, environment.Name));

			environment.Url = uri.AbsoluteUri.TrimEnd('/');
			environments.Add(environment.Name, environment);
		}

		return environments;

		static bool IsEnvironmentName(string name) => name is { Length: > 0 and <= 255 } && Regex.IsMatch(name, @"\A[a-z0-9]+(?:-[a-z0-9]+)*\z");
		static bool TryGetBaseAddress(string url, out Uri uri)
		{
			if(!Uri.TryCreate(url, UriKind.Absolute, out uri) || !uri.IsWellFormedOriginalString())
				return false;

			if(uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
				return false;

			if(string.IsNullOrEmpty(uri.Host))
				return false;

			return uri.UserInfo.Length == 0 && uri.Query.Length == 0 && uri.Fragment.Length == 0;
		}
	}
	#endregion
}
