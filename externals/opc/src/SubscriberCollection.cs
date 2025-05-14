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
 * This file is part of Zongsoft.Externals.Opc library.
 *
 * The Zongsoft.Externals.Opc is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Opc is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Opc library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Opc.Ua;
using Opc.Ua.Client;

namespace Zongsoft.Externals.Opc;

public class SubscriberCollection : IReadOnlyCollection<Subscriber>
{
	#region 成员字段
	private readonly ConcurrentDictionary<uint, Subscriber> _subscribers = new();
	#endregion

	#region 公共方法
	public int Count => _subscribers.Count;
	public bool TryGetValue(int id, out Subscriber value) => _subscribers.TryGetValue((uint)id, out value);
	public bool TryGetValue(uint id, out Subscriber value) => _subscribers.TryGetValue(id, out value);

	public async ValueTask UnsubscribeAsync(CancellationToken cancellation = default)
	{
		if(_subscribers.IsEmpty || cancellation.IsCancellationRequested)
			return;

		foreach(var key in _subscribers.Keys)
		{
			if(_subscribers.TryRemove(key, out var subscriber))
				await subscriber.DisposeAsync();
		}
	}
	#endregion

	#region 内部方法
	internal async ValueTask<bool> RegisterAsync(Subscriber subscriber, CancellationToken cancellation)
	{
		if(subscriber == null)
			throw new ArgumentNullException(nameof(subscriber));

		if(subscriber.Subscription is not Subscription subscription)
			throw new ArgumentException(nameof(subscriber));

		if(subscription.Created)
			throw new InvalidOperationException($"The specified '{subscription.Id}' subscription has been registered.");

		//从服务器中创建指定的订阅
		await subscription.CreateAsync(cancellation);

		if(_subscribers.TryAdd(subscriber.Identifier, subscriber))
		{
			//当订阅者被注销则将其从订阅者集合中删除
			subscriber.Disposed.RegisterChangeCallback(state => _subscribers.TryRemove((uint)state, out _), subscriber.Identifier);

			return true;
		}

		return false;
	}
	#endregion

	#region 枚举遍历
	IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
	public IEnumerator<Subscriber> GetEnumerator() => _subscribers.Values.GetEnumerator();
	#endregion
}
