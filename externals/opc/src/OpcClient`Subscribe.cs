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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Opc.Ua;
using Opc.Ua.Client;

namespace Zongsoft.Externals.Opc;

partial class OpcClient
{
	public ValueTask<Subscriber> SubscribeAsync(IEnumerable<string> identifiers, CancellationToken cancellation = default) => this.SubscribeAsync(identifiers, null, null, cancellation);
	public ValueTask<Subscriber> SubscribeAsync(IEnumerable<string> identifiers, Action<Subscriber, Subscriber.Entry, object> consumer, CancellationToken cancellation = default) => this.SubscribeAsync(identifiers, null, consumer, cancellation);
	public ValueTask<Subscriber> SubscribeAsync(IEnumerable<string> identifiers, SubscriberOptions options, CancellationToken cancellation = default) => this.SubscribeAsync(identifiers, options, null, cancellation);
	public ValueTask<Subscriber> SubscribeAsync(IEnumerable<string> identifiers, SubscriberOptions options, Action<Subscriber, Subscriber.Entry, object> consumer, CancellationToken cancellation = default) => this.SubscribeAsync(identifiers?.Select(id => new KeyValuePair<string, object>(id, null)), options, consumer, cancellation);

	public ValueTask<Subscriber> SubscribeAsync(IEnumerable<KeyValuePair<string, object>> identifiers, CancellationToken cancellation = default) => this.SubscribeAsync(identifiers, null, null, cancellation);
	public ValueTask<Subscriber> SubscribeAsync(IEnumerable<KeyValuePair<string, object>> identifiers, Action<Subscriber, Subscriber.Entry, object> consumer, CancellationToken cancellation = default) => this.SubscribeAsync(identifiers, null, consumer, cancellation);
	public ValueTask<Subscriber> SubscribeAsync(IEnumerable<KeyValuePair<string, object>> identifiers, SubscriberOptions options, CancellationToken cancellation = default) => this.SubscribeAsync(identifiers, options, null, cancellation);
	public async ValueTask<Subscriber> SubscribeAsync(IEnumerable<KeyValuePair<string, object>> identifiers, SubscriberOptions options, Action<Subscriber, Subscriber.Entry, object> consumer, CancellationToken cancellation = default)
	{
		if(identifiers == null)
			throw new ArgumentNullException(nameof(identifiers));

		//确保待订阅的条目未被订阅
		var entries = identifiers
			.Where(entry => !string.IsNullOrEmpty(entry.Key) && !Exists(entry.Key))
			.DistinctBy(entry => entry.Key)
			.Select(entry => new Subscriber.Entry(entry.Key, entry.Value))
			.ToArray();

		//如果待订阅的条目为空则退出
		if(entries.Length == 0)
			return null;

		var session = this.GetSession();
		var subscriber = new Subscriber(options, consumer);

		for(int i = 0; i < entries.Length; i++)
			await subscriber.Entries.AddAsync(entries[i], cancellation);

		if(session.AddSubscription((Subscription)subscriber.Subscription) && await _subscribers.RegisterAsync(subscriber, cancellation))
			return subscriber;

		return null;

		bool Exists(string identifier)
		{
			foreach(var subscriber in _subscribers)
			{
				if(subscriber.Entries.Contains(identifier))
					return true;
			}

			return false;
		}
	}
}
