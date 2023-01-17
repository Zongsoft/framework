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
 * Copyright (C) 2010-2023 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Zongsoft.Components
{
	public static class ExecutorUtility
	{
		#region 私有变量
		private static readonly ConcurrentDictionary<Type, Type> _cache = new ConcurrentDictionary<Type, Type>();
		#endregion

		#region 公共方法
		public static ExecutorContext Context(this IExecutor executor, object value, IEnumerable<KeyValuePair<string, object>> parameters = null) =>
			new ExecutorContext(executor, value, parameters);

		public static async ValueTask<object> ExecuteAsync<TContext>(this IExecutor executor, IHandler handler, TContext context, CancellationToken cancellation) where TContext : IExecutorContext
		{
			if(handler == null) throw new ArgumentNullException(nameof(handler));
			if(context == null) throw new ArgumentNullException(nameof(context));

			//获取指定处理器的请求参数的类型
			var requestType = _cache.GetOrAdd(handler.GetType(), GetHandlerRequestType);

			//如果处理器的请求参数类型是object或执行上下文的传入参数类型
			if(requestType == null || requestType == typeof(object) || (context.Value != null && requestType.IsAssignableFrom(context.Value.GetType())))
			{
				await handler.HandleAsync(executor, context.Value, context.HasParameters ? context.Parameters : null, cancellation);
				return null;
			}

			//如果处理器的请求参数类型是执行上下文类型
			if(requestType.IsAssignableFrom(context.GetType()))
			{
				await handler.HandleAsync(executor, context, context.HasParameters ? context.Parameters : null, cancellation);
				return context.Result;
			}

			//如果执行上下文对象可被转换为处理器的请求参数类型
			if(Common.Convert.TryConvertValue(context, requestType, out var request))
			{
				await handler.HandleAsync(executor, request, context.HasParameters ? context.Parameters : null, cancellation);
				return context.Result;
			}

			//如果执行上下文传入参数值可被转换为处理器的请求参数类型
			if(Common.Convert.TryConvertValue(context.Value, requestType, out request))
			{
				await handler.HandleAsync(executor, request, context.HasParameters ? context.Parameters : null, cancellation);
				return null;
			}

			throw Common.OperationException.Unsupported($"The specified '{handler}' handler cannot process parameters of type '{context.GetType().FullName}'.");
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