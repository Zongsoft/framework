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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Microsoft.OpenApi;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

using Zongsoft.Services;

namespace Zongsoft.Web.OpenApi;

public static partial class WebExtension
{
	public static IEndpointConventionBuilder UseOpenApi(this IEndpointRouteBuilder endpoints, string pattern = null) => UseOpenApi(endpoints, pattern, null, new SemaphoreSlim(1));

	internal static IEndpointConventionBuilder UseOpenApi(this IEndpointRouteBuilder endpoints, string pattern, IReadOnlyDictionary<string, Configuration.EnvironmentOption> environments, SemaphoreSlim gate)
	{
		var application = ApplicationContext.Current;
		var snapshots = new ConcurrentDictionary<(string PathBase, string Environment), DocumentSnapshot>();

		return endpoints.MapGet(pattern ?? "/openapi/{documentName}.{extension}", async (HttpContext context, string documentName = "v1", string extension = "json") =>
		{
			if(!DocumentFormat.TryParse(extension, out var format))
			{
				context.Response.StatusCode = StatusCodes.Status400BadRequest;
				context.Response.ContentType = "text/plain;charset=utf-8";
				await context.Response.WriteAsync(Properties.Resources.DocumentFormatNotSupported_Message, context.RequestAborted);
				return;
			}

			Configuration.EnvironmentOption environment = null;
			DocumentSnapshot document = null;

			if(environments == null || environments.TryGetValue(documentName, out environment))
			{
				var key = (context.Request.PathBase.ToUriComponent(), environment?.Name);
				document = snapshots.TryGetValue(key, out var cached) ? cached : await GetSnapshot(context, key, environment);
			}

			if(document == null)
			{
				context.Response.StatusCode = StatusCodes.Status404NotFound;
				context.Response.ContentType = "text/plain;charset=utf-8";
				await context.Response.WriteAsync(string.Format(Properties.Resources.DocumentNotFound_Message, documentName), context.RequestAborted);
				return;
			}

			context.Response.ContentType = format.Type;
			await context.Response.Body.WriteAsync(format == DocumentFormat.Yaml ? document.Yaml : document.Json, context.RequestAborted);
		}).ExcludeFromDescription();

		async Task<DocumentSnapshot> GetSnapshot(HttpContext context, (string PathBase, string Environment) key, Configuration.EnvironmentOption environment)
		{
			await gate.WaitAsync(context.RequestAborted);

			try
			{
				if(snapshots.TryGetValue(key, out var snapshot))
					return snapshot;

				var owner = application ?? ApplicationContext.Current;
				if(owner == null)
					return null;

				var document = DocumentGenerator.Generate(new DocumentContext(DocumentFormat.Json, owner)
				{
					Routing = endpoints.ServiceProvider.GetService<EndpointDataSource>(),
					PathBase = key.PathBase,
					Environment = environment,
				});

				if(document == null)
					return null;

				var result = new DocumentSnapshot(Serialize(document, DocumentFormat.Json), Serialize(document, DocumentFormat.Yaml));
				snapshots[key] = result;
				return result;
			}
			finally
			{
				gate.Release();
			}
		}
	}

	private static byte[] Serialize(OpenApiDocument document, DocumentFormat format)
	{
		using var buffer = new MemoryStream();
		//使用默认编码和缓冲区大小，释放写入器时保留底层流
		using(var text = new StreamWriter(buffer, null, -1, true))
		{
			IOpenApiWriter writer = format == DocumentFormat.Yaml ? new VersionedYamlWriter(text) : new VersionedJsonWriter(text);
			document.SerializeAsV32(writer);
		}

		return buffer.ToArray();
	}

	private sealed record DocumentSnapshot(ReadOnlyMemory<byte> Json, ReadOnlyMemory<byte> Yaml);
}
