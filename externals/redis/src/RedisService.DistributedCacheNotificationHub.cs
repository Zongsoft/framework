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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.Redis library.
 *
 * The Zongsoft.Externals.Redis is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Redis is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Redis library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

using StackExchange.Redis;

namespace Zongsoft.Externals.Redis;

partial class RedisService
{
	internal sealed class DistributedCacheNotificationHub
	{
		private static readonly ConditionalWeakTable<ConnectionMultiplexer, ConcurrentDictionary<Scope, DistributedCacheNotificationHub>> _registries = new();

		private readonly Scope _scope;
		private readonly ISubscriber _subscriber;
		private readonly ConcurrentDictionary<Scope, DistributedCacheNotificationHub> _registry;
		private readonly ConcurrentDictionary<DistributedCacheSubscription, byte> _subscriptions = new();
		private readonly SemaphoreSlim _gate = new(1, 1);
		private ChannelMessageQueue _queue;
		private bool _closed;

		private DistributedCacheNotificationHub(ConnectionMultiplexer connection, int database, string @namespace, ConcurrentDictionary<Scope, DistributedCacheNotificationHub> registry)
		{
			_scope = new Scope(database, @namespace ?? string.Empty);
			_registry = registry;
			_subscriber = connection.GetSubscriber();
		}

		internal static async ValueTask<DistributedCacheNotificationHub> GetAsync(ConnectionMultiplexer connection, int database, string @namespace, CancellationToken cancellation)
		{
			ArgumentNullException.ThrowIfNull(connection);
			var registry = _registries.GetValue(connection, static _ => new());
			var scope = new Scope(database, @namespace ?? string.Empty);

			while(true)
			{
				var hub = registry.GetOrAdd(scope, static (scope, state) => new DistributedCacheNotificationHub(state, scope.Database, scope.Namespace, _registries.GetValue(state, static _ => new())), connection);
				if(await hub.EnsureSubscribedAsync(cancellation))
					return hub;

				registry.TryRemove(new(scope, hub));
			}
		}

		internal async ValueTask<bool> AddAsync(DistributedCacheSubscription subscription, CancellationToken cancellation)
		{
			ArgumentNullException.ThrowIfNull(subscription);
			await _gate.WaitAsync(cancellation);

			try
			{
				if(_closed || _queue == null)
					return false;

				if(!_subscriptions.TryAdd(subscription, 0))
					throw new InvalidOperationException(Properties.Resources.CacheNotificationSubscriptionAlreadyRegistered_Message);

				return true;
			}
			finally
			{
				_gate.Release();
			}
		}

		internal async ValueTask RemoveAsync(DistributedCacheSubscription subscription)
		{
			ChannelMessageQueue queue = null;
			await _gate.WaitAsync();

			try
			{
				_subscriptions.TryRemove(subscription, out _);
				if(!_subscriptions.IsEmpty || _closed)
					return;

				_closed = true;
				queue = Interlocked.Exchange(ref _queue, null);
				_registry.TryRemove(new(_scope, this));
			}
			finally
			{
				_gate.Release();
			}

			if(queue != null)
				await queue.UnsubscribeAsync();
		}

		private async ValueTask<bool> EnsureSubscribedAsync(CancellationToken cancellation)
		{
			await _gate.WaitAsync(cancellation);

			try
			{
				if(_closed)
					return false;
				if(_queue != null)
					return true;

				//注：命名空间为空时不能使用 RedisChannel.KeySpacePrefix，StackExchange.Redis 3.x 会拒绝空前缀
				RedisKey prefix = _scope.Namespace;
				var operation = string.IsNullOrEmpty(_scope.Namespace) ?
					_subscriber.SubscribeAsync(RedisChannel.Pattern($"__keyspace@{_scope.Database}__:*")) :
					_subscriber.SubscribeAsync(RedisChannel.KeySpacePrefix(in prefix, _scope.Database));

				try
				{
					_queue = await operation.WaitAsync(cancellation);
					_queue.OnMessage(this.OnMessage);
					return true;
				}
				catch
				{
					_closed = true;
					_registry.TryRemove(new(_scope, this));

					if(operation.IsCompletedSuccessfully)
						await operation.Result.UnsubscribeAsync();
					else if(!operation.IsFaulted)
						_ = UnsubscribeWhenCompletedAsync(operation);

					throw;
				}
			}
			finally
			{
				_gate.Release();
			}

			static async Task UnsubscribeWhenCompletedAsync(Task<ChannelMessageQueue> operation)
			{
				try
				{
					var queue = await operation;
					await queue.UnsubscribeAsync();
				}
				catch { }
			}
		}

		private void OnMessage(ChannelMessage message)
		{
			if(!message.TryParseKeyNotification(out var source) || !RedisService.DistributedCacheSubscription.TryGetNotificationKind(source.Type, out var kind))
				return;

			var key = (string)source.GetKey();
			if(string.IsNullOrEmpty(key) || !key.StartsWith(_scope.Namespace, StringComparison.Ordinal))
				return;

			key = key[_scope.Namespace.Length..];
			if(string.IsNullOrEmpty(key))
				return;

			foreach(var subscription in _subscriptions.Keys)
				subscription.Enqueue(kind, key);
		}

		private readonly record struct Scope(int Database, string Namespace);
	}
}
