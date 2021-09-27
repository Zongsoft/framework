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
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Collections.Generic;

namespace Zongsoft.Messaging
{
	public static class MessageQueueExtension
	{
		private static Dictionary<Type, Delegate> _dequeues = new Dictionary<Type, Delegate>();

		public static object Dequeue(this IMessageQueue queue, MessageDequeueOptions options = null)
		{
			if(queue == null)
				throw new ArgumentNullException(nameof(queue));

			var queueType = queue.GetType().GetTypeInfo();

			if(!_dequeues.TryGetValue(queueType, out var invoker))
			{
				var contract = queueType.ImplementedInterfaces.FirstOrDefault(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IMessageQueue<>));

				if(contract == null)
					return null;

				var messageType = contract.GenericTypeArguments[0];

				lock(_dequeues)
				{
					if(!_dequeues.TryGetValue(messageType, out invoker))
					{
						invoker = contract.GetMethod("Dequeue", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).CreateDelegate(GetMethodType(messageType), queue);
						_dequeues.Add(queueType, invoker);
					}
				}
			}

			return invoker == null ? null : invoker.DynamicInvoke(options);
		}

		private static Type GetMethodType(Type resultType)
		{
			return typeof(Func<,>).MakeGenericType(typeof(MessageDequeueOptions), resultType);
		}
	}
}
