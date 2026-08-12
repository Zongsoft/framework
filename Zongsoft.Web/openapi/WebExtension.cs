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
 * Copyright (C) 2020-2026 Zongsoft Studio <http://www.zongsoft.com>
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
using System.IO;

using Microsoft.OpenApi;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Zongsoft.Web.OpenApi;

public static partial class WebExtension
{
	private static OpenApiDocument _document;

	public static IEndpointConventionBuilder UseOpenApi(this IEndpointRouteBuilder endpoints, string pattern = null) => endpoints.MapGet(pattern ?? "/openapi/{documentName}.{extension}", async (HttpContext context, string documentName = "v1", string extension = "json") =>
	{
		if(!DocumentFormat.TryParse(extension, out var format))
		{
			context.Response.StatusCode = StatusCodes.Status400BadRequest;
			context.Response.ContentType = "text/plain;charset=utf-8";
			await context.Response.WriteAsync(Properties.Resources.DocumentFormatNotSupported_Message);
			return;
		}

		if(_document == null)
		{
			_document = DocumentGenerator.Generate(new DocumentContext(format));

			if(_document == null)
			{
				context.Response.StatusCode = StatusCodes.Status404NotFound;
				context.Response.ContentType = "text/plain;charset=utf-8";
				await context.Response.WriteAsync(string.Format(Properties.Resources.DocumentNotFound_Message, documentName));
				return;
			}
		}

		using var memory = new MemoryStream();
		using(var streamWriter = new StreamWriter(memory, System.Text.Encoding.UTF8, leaveOpen: true))
		{
			OpenApiWriterBase writer = format == DocumentFormat.Yaml ?
				new OpenApiYamlWriter(streamWriter):
				new OpenApiJsonWriter(streamWriter);

			await _document.SerializeAsync(writer, OpenApiSpecVersion.OpenApi3_1, context.RequestAborted);
			await streamWriter.FlushAsync();
		}

		context.Response.ContentType = format.Type;
		await context.Response.Body.WriteAsync(memory.GetBuffer().AsMemory(0, (int)memory.Length), context.RequestAborted);
	}).ExcludeFromDescription();
}
