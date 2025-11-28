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

using Zongsoft.Configuration;

namespace Zongsoft.Web.OpenApi;

partial class DocumentGenerator
{
	private static readonly HashSet<string> HTTP_SCHEMES = new(StringComparer.OrdinalIgnoreCase)
	{
		"Basic",
		"Bearer",
		"Digest",
		"NTLM",
		"Negotiate",
	};

	internal static void GenerateSecuritySchemes(this DocumentContext context)
	{
		var authentication = context.Configuration.GetOption<Configuration.AuthenticationOption>("/Web/OpenAPI/Authentication");

		if(authentication == null || authentication.Authenticators.Count == 0)
			return;

		context.Document.Security = [new OpenApiSecurityRequirement()];
		context.Document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>(StringComparer.OrdinalIgnoreCase);

		foreach(var authenticator in authentication.Authenticators)
		{
			var scheme = new OpenApiSecurityScheme()
			{
				Name = authenticator.Name,
				Type = authenticator.Kind.ToType(),
				In = authenticator.Location.ToLocation(),
				Scheme = authenticator.Scheme,
			};

			//处理特殊安全方案
			Specialize(scheme, authenticator.Properties);

			foreach(var property in authenticator.Properties)
				scheme.AddExtension($"x-{property.Key}", Extensions.Helper.String(property.Value));

			context.Document.Components.SecuritySchemes[authenticator.Name] = scheme;
		}
	}

	private static void Specialize(OpenApiSecurityScheme scheme, IDictionary<string, string> properties)
	{
		if(scheme.Type == SecuritySchemeType.Http)
		{
			scheme.In = ParameterLocation.Header;

			if(string.IsNullOrWhiteSpace(scheme.Scheme))
				scheme.Scheme = "Basic";

			if(HTTP_SCHEMES.Contains(scheme.Scheme))
				return;

			scheme.Name = "Authorization";
			scheme.Type = SecuritySchemeType.ApiKey;
		}
		else if(scheme.Type == SecuritySchemeType.ApiKey)
		{
			if(!string.IsNullOrEmpty(scheme.Scheme))
				scheme.Name = scheme.Scheme;
		}
	}

	private static SecuritySchemeType ToType(this Configuration.AuthenticationOption.AuthenticatorKind kind) => kind switch
	{
		Configuration.AuthenticationOption.AuthenticatorKind.Http => SecuritySchemeType.Http,
		Configuration.AuthenticationOption.AuthenticatorKind.Custom => SecuritySchemeType.ApiKey,
		Configuration.AuthenticationOption.AuthenticatorKind.OAuth2 => SecuritySchemeType.OAuth2,
		Configuration.AuthenticationOption.AuthenticatorKind.OpenID => SecuritySchemeType.OpenIdConnect,
		_ => SecuritySchemeType.Http,
	};

	private static ParameterLocation ToLocation(this Configuration.AuthenticationOption.AuthenticatorLocation location) => location switch
	{
		Configuration.AuthenticationOption.AuthenticatorLocation.Path => ParameterLocation.Path,
		Configuration.AuthenticationOption.AuthenticatorLocation.Query => ParameterLocation.Query,
		Configuration.AuthenticationOption.AuthenticatorLocation.Header => ParameterLocation.Header,
		Configuration.AuthenticationOption.AuthenticatorLocation.Cookie => ParameterLocation.Cookie,
		_ => ParameterLocation.Header,
	};
}
