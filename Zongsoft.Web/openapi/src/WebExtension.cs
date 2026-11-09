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
using System.IO;

using Microsoft.OpenApi;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

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
			var documentContext = new DocumentContext(format)
			{
				//注入运行时路由表，用于补全约定式路由的接口路径
				Routing = context.RequestServices?.GetService<EndpointDataSource>(),
			};

			_document = DocumentGenerator.Generate(documentContext);

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
