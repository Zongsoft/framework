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

using Microsoft.OpenApi;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Zongsoft.Web.OpenApi;

public static class WebExtension
{
	private static OpenApiDocument _document;

	public static IEndpointConventionBuilder UseOpenApi(this IEndpointRouteBuilder endpoints, string pattern = null) => endpoints.MapGet(pattern ?? "/openapi/{documentName}.json", async (HttpContext context, string documentName = "v1") =>
	{
		_document ??= DocumentGenerator.Generate();

		if(_document == null)
		{
			context.Response.StatusCode = StatusCodes.Status404NotFound;
			context.Response.ContentType = "text/plain;charset=utf-8";
			await context.Response.WriteAsync($"No OpenAPI document with the name '{documentName}' was found.");
			return;
		}

		using var textWriter = new Utf8BufferTextWriter(System.Globalization.CultureInfo.InvariantCulture);
		textWriter.SetWriter(context.Response.BodyWriter);

		OpenApiWriterBase openApiWriter;

		if(IsYamlFormat(context.Request.Path))
		{
			context.Response.ContentType = "text/plain+yaml;charset=utf-8";
			openApiWriter = new OpenApiYamlWriter(textWriter);
		}
		else
		{
			context.Response.ContentType = "application/json;charset=utf-8";
			openApiWriter = new OpenApiJsonWriter(textWriter);
		}

		await context.Response.StartAsync();
		await _document.SerializeAsync(openApiWriter, OpenApiSpecVersion.OpenApi3_1, context.RequestAborted);
		await context.Response.BodyWriter.FlushAsync(context.RequestAborted);
	}).ExcludeFromDescription();

	private static bool IsYamlFormat(string pattern) =>
		pattern.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) ||
		pattern.EndsWith(".yml", StringComparison.OrdinalIgnoreCase);
}
