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
 * This file is part of Zongsoft.Intelligences.Web library.
 *
 * The Zongsoft.Intelligences.Web is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Intelligences.Web is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Intelligences.Web library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.ServerSentEvents;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Zongsoft.Intelligences;

public static class WebUtility
{
	public static async ValueTask EnumerableAsync<T>(this HttpResponse response, IAsyncEnumerable<T> source, CancellationToken cancellation = default)
	{
		response.ContentType = "text/event-stream";
		response.Headers.Pragma = "no-cache";
		response.Headers.CacheControl = "no-cache,no-store";
		response.Headers.ContentEncoding = "identity";

		var context = response.HttpContext;
		var feature = context.Features.Get<IHttpResponseBodyFeature>() ??
			throw new InvalidOperationException($"The {typeof(IHttpResponseBodyFeature)} feature is not present.");

		feature.DisableBuffering();

		var events = Wrap(source);

		if(events is IAsyncEnumerable<SseItem<string>> stringEvents)
		{
			await SseFormatter.WriteAsync(stringEvents, response.Body, cancellation);
			return;
		}

		var jsonOptions = context.RequestServices.GetService<IOptions<JsonOptions>>()?.Value ?? new JsonOptions();

		await SseFormatter.WriteAsync(
			events,
			response.Body,
			(item, writer) => FormatSseItem(item, writer, jsonOptions),
			cancellation);

		static async IAsyncEnumerable<SseItem<T>> Wrap(IAsyncEnumerable<T> source, string eventType = null)
		{
			await foreach(var item in source)
				yield return new SseItem<T>(item, eventType);
		}

		static void FormatSseItem(SseItem<T> item, IBufferWriter<byte> writer, JsonOptions jsonOptions)
		{
			if(item.Data is null)
			{
				writer.Write([]);
				return;
			}

			if(item.Data is byte[] bytes)
			{
				writer.Write(bytes);
				return;
			}

			var runtimeType = item.Data.GetType();
			var jsonTypeInfo = jsonOptions.JsonSerializerOptions.GetTypeInfo(typeof(T));

			var typeInfo = jsonTypeInfo.ShouldUseWith(runtimeType)
				? jsonTypeInfo
				: jsonOptions.JsonSerializerOptions.GetTypeInfo(typeof(object));

			var json = JsonSerializer.SerializeToUtf8Bytes(item.Data, typeInfo);
			writer.Write(json);
		}
	}

	internal static bool HasKnownPolymorphism(this JsonTypeInfo jsonTypeInfo)
		=> jsonTypeInfo.Type.IsSealed || jsonTypeInfo.Type.IsValueType || jsonTypeInfo.PolymorphismOptions is not null;

	internal static bool ShouldUseWith(this JsonTypeInfo jsonTypeInfo, Type runtimeType)
		=> runtimeType is null || jsonTypeInfo.Type == runtimeType || jsonTypeInfo.HasKnownPolymorphism();
}
