/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.WeChat library.
 *
 * The Zongsoft.Externals.WeChat is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.WeChat is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.WeChat library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Zongsoft.Common;
using Zongsoft.Components;

using Microsoft.AspNetCore.Http;

namespace Zongsoft.Externals.Wechat.Daemon
{
	public static class FallbackHandlerFactory
	{
		#region 常量定义
		internal const string ERROR_NOTFOUND = "NotFound";
		internal const string ERROR_UNSUPPORTED = "Unsupported";
		internal const string ERROR_CANNOTHANDLE = "CannotHandle";
		#endregion

		#region 私有变量
		private static readonly ConcurrentDictionary<Type, Type> _cache = new ConcurrentDictionary<Type, Type>();
		#endregion

		#region 公共字段
		public static readonly IDictionary<string, IHandler> Handlers = new Dictionary<string, IHandler>(StringComparer.OrdinalIgnoreCase);
		#endregion

		#region 公共方法
		public static async ValueTask<OperationResult> HandleAsync(HttpContext context, string name, string key, CancellationToken cancellation = default)
		{
			if(name != null && Handlers.TryGetValue(name, out var handler) && handler != null)
			{
				if(context.Request.HasFormContentType)
					return await handler.HandleAsync(
						key,
						context.Request.Form == null || context.Request.Form.Count == 0 ? null : new Dictionary<string, string>(context.Request.Form.Select(entry => new KeyValuePair<string, string>(entry.Key, entry.Value)), StringComparer.OrdinalIgnoreCase),
						cancellation);

				Type requestType = _cache.GetOrAdd(handler.GetType(), type => GetHandlerRequestType(type));
				if(requestType == null)
					return await handler.HandleAsync(key, context.Request.Body, cancellation);

				object request = null;
				var converter = TypeDescriptor.GetConverter(requestType);

				if(converter != null && converter.GetType() != typeof(TypeConverter))
				{
					if(converter.CanConvertFrom(typeof(HttpRequest)))
						request = converter.ConvertFrom(context.Request);
					else if(converter.CanConvertFrom(typeof(HttpContext)))
						request = converter.ConvertFrom(context);
					else if(converter.CanConvertFrom(typeof(Stream)))
						request = converter.ConvertFrom(context.Request.Body);
					else
						return OperationResult.Fail(ERROR_UNSUPPORTED, $"The '{converter.GetType().FullName}' fallback converter does not support conversion.");
				}
				else if(context.Request.ContentLength > 0)
				{
					if(context.Request.ContentType != null && context.Request.ContentType.EndsWith("json", StringComparison.OrdinalIgnoreCase))
						request = await JsonSerializer.DeserializeAsync(context.Request.Body, requestType, null, cancellation);
					else
						request = context.Request.Body;
				}

				return handler.CanHandle(request) ?
					await handler.HandleAsync(key, request, cancellation) :
					OperationResult.Fail(ERROR_CANNOTHANDLE);
			}

			return OperationResult.Fail(ERROR_NOTFOUND);
		}
		#endregion

		#region 私有方法
		private static Type GetHandlerRequestType(Type type)
		{
			var contracts = type.GetInterfaces();

			if(contracts == null || contracts.Length == 0)
				return null;

			for(int i = 0; i < contracts.Length; i++)
			{
				var contract = contracts[i];

				if(contract.IsGenericType)
				{
					var prototype = contract.GetGenericTypeDefinition();

					if(prototype == typeof(IHandler<>) || prototype == typeof(IHandler<,>))
						return contract.GenericTypeArguments[0];
				}
			}

			return null;
		}
		#endregion
	}
}
