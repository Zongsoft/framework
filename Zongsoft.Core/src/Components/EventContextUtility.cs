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
 * Copyright (C) 2020-2023 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Concurrent;

namespace Zongsoft.Components
{
	public static class EventContextUtility
	{
		private static readonly ConcurrentDictionary<Type, Func<EventContext, object>> _accessors = new();

		public static object GetArgument(this EventContext context) => GetArgument(context, out _);
		public static object GetArgument(this EventContext context, out Type argumentType)
		{
			argumentType = null;
			if(context == null)
				return null;

			if(context.GetType().IsGenericType)
			{
				argumentType = context.GetType().GetGenericArguments()[0];

				var accessor = _accessors.GetOrAdd(argumentType, type =>
				{
					var property = context.GetType().GetProperty(nameof(EventContext<object>.Argument), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
					var parameter = Expression.Parameter(typeof(EventContext), "context");
					var converter = Expression.Convert(parameter, typeof(EventContext<>).MakeGenericType(type));
					var invoke = Expression.Call(converter, property.GetMethod);

					var expression = type.IsValueType ?
						Expression.Convert(invoke, typeof(object)) : (Expression)invoke;

					return Expression.Lambda<Func<EventContext, object>>(expression, parameter).Compile();
				});

				return accessor.Invoke(context);
			}

			return null;
		}
	}
}