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
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Zongsoft.Components
{
	public static class EventSubscriptionProviderUtility
	{
		private static readonly ConcurrentDictionary<object, Func<object, EventContextBase, CancellationToken, IAsyncEnumerable<IEventSubscription>>> _invokers = new();

		public static IAsyncEnumerable<IEventSubscription> GetSubscriptionsAsync(object provider, EventContextBase context, CancellationToken cancellation)
		{
			if(provider == null)
				return Zongsoft.Collections.Enumerable.Empty<IEventSubscription>();

			if(provider is IEventSubscriptionProvider subscriptionProvider)
				return subscriptionProvider.GetSubscriptionsAsync(context, cancellation) ?? Zongsoft.Collections.Enumerable.Empty<IEventSubscription>();

			return _invokers.GetOrAdd(provider, CreateInvoker)?.Invoke(provider, context, cancellation) ?? Zongsoft.Collections.Enumerable.Empty<IEventSubscription>();
		}

		/*
		 * 生成对 IEventSubscriptionProvider<T> 提供程序的调用委托，该委托代码如下所示：
		 * IAsyncEnumerable<IEventSubscription> Invoke(object provider, EventContextBase context, CancellationToken cancellation)
		 * {
		 *		return ((IEventSubscriptionProvider<T>)provider).GetSubscriptionsAsync((EventContext<T>)context, cancellation);
		 * }
		 */
		private static Func<object, EventContextBase, CancellationToken, IAsyncEnumerable<IEventSubscription>> CreateInvoker(object provider)
		{
			var contract = provider.GetType()
				.GetInterfaces()
				.FirstOrDefault(contract => contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IEventSubscriptionProvider<>));

			if(contract == null)
				return null;

			var argumentType = contract.GenericTypeArguments[0];
			var method = provider.GetType().GetInterfaceMap(contract).InterfaceMethods[0];
			var providerParameter = Expression.Parameter(typeof(object), "provider");
			var contextParameter = Expression.Parameter(typeof(EventContextBase), "context");
			var cancellationParameter = Expression.Parameter(typeof(CancellationToken), "cancellation");
			var providerConvert = Expression.Convert(providerParameter, typeof(IEventSubscriptionProvider<>).MakeGenericType(argumentType));
			var contextConvert = Expression.Convert(contextParameter, typeof(EventContext<>).MakeGenericType(argumentType));
			var invoker = Expression.Call(providerConvert, method, contextConvert, cancellationParameter);

			return Expression.Lambda<Func<object, EventContextBase, CancellationToken, IAsyncEnumerable<IEventSubscription>>>(invoker, providerParameter, contextParameter, cancellationParameter).Compile();
		}
	}
}