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

using Zongsoft.Caching;
using Zongsoft.Components;
using Zongsoft.Communication;

using StackExchange.Redis;

namespace Zongsoft.Externals.Redis;

internal sealed class RedisCacheSubscription : ChannelBase, IDistributedCacheSubscription
{
	#region 成员字段
	private readonly RedisService _cache;
	private readonly ISubscriber _subscriber;
	private readonly int _database;
	private readonly string _namespace;
	private readonly string _prefix;
	private readonly DistributedCacheNotificationKind _kind;
	private readonly CancellationTokenSource _lifetime;
	private readonly object _dispatchLock = new();
	private ChannelMessageQueue _queue;
	private int _closing;
	#endregion

	#region 构造函数
	public RedisCacheSubscription(RedisService cache, ConnectionMultiplexer connection, int database, string @namespace, IHandler<DistributedCacheNotification> handler, DistributedCacheSubscriptionOptions options)
	{
		_cache = cache ?? throw new ArgumentNullException(nameof(cache));
		_subscriber = connection?.GetSubscriber() ?? throw new ArgumentNullException(nameof(connection));
		this.Handler = handler ?? throw new ArgumentNullException(nameof(handler));

		ArgumentNullException.ThrowIfNull(options);

		_database = database;
		_namespace = @namespace ?? string.Empty;
		_prefix = options.Prefix ?? string.Empty;
		_kind = options.Kind;
		_lifetime = new CancellationTokenSource();
	}
	#endregion

	#region 公共属性
	public IDistributedCache Cache => _cache;
	public IHandler<DistributedCacheNotification> Handler { get; }
	public DistributedCacheSubscriptionOptions Options => new(_prefix, _kind);
	#endregion

	#region 公共方法
	public ValueTask UnsubscribeAsync(CancellationToken cancellation = default)
	{
		if(this.IsClosed || this.IsDisposed)
			return ValueTask.CompletedTask;

		return this.CloseAsync(cancellation);
	}
	#endregion

	#region 内部方法
	internal async ValueTask SubscribeAsync(CancellationToken cancellation)
	{
		cancellation.ThrowIfCancellationRequested();

		RedisKey prefix = _namespace + _prefix;
		var channel = RedisChannel.KeySpacePrefix(in prefix, _database);
		var task = _subscriber.SubscribeAsync(channel);

		try
		{
			_queue = await task.WaitAsync(cancellation);
			_queue.OnMessage(this.OnMessageAsync);
		}
		catch
		{
			if(task.IsCompletedSuccessfully)
				await task.Result.UnsubscribeAsync();
			else if(!task.IsFaulted)
			{
				try
				{
					var queue = await task;
					await queue.UnsubscribeAsync();
				}
				catch
				{
				}
			}

			throw;
		}
	}

	internal static bool TryGetNotificationKind(KeyNotificationType type, out DistributedCacheNotificationKind kind)
	{
		kind = type switch
		{
			KeyNotificationType.Expired => DistributedCacheNotificationKind.Expired,
			KeyNotificationType.Evicted => DistributedCacheNotificationKind.Evicted,
			KeyNotificationType.Del or
			KeyNotificationType.RenameFrom or
			KeyNotificationType.MoveFrom => DistributedCacheNotificationKind.Removed,
			KeyNotificationType.Append or
			KeyNotificationType.Copy or
			KeyNotificationType.HDel or
			KeyNotificationType.HExpired or
			KeyNotificationType.HIncrByFloat or
			KeyNotificationType.HIncrBy or
			KeyNotificationType.HSet or
			KeyNotificationType.IncrByFloat or
			KeyNotificationType.IncrBy or
			KeyNotificationType.LInsert or
			KeyNotificationType.LPop or
			KeyNotificationType.LPush or
			KeyNotificationType.LRem or
			KeyNotificationType.LSet or
			KeyNotificationType.LTrim or
			KeyNotificationType.MoveTo or
			KeyNotificationType.RenameTo or
			KeyNotificationType.Restore or
			KeyNotificationType.RPop or
			KeyNotificationType.RPush or
			KeyNotificationType.SAdd or
			KeyNotificationType.Set or
			KeyNotificationType.SetRange or
			KeyNotificationType.SortStore or
			KeyNotificationType.SRem or
			KeyNotificationType.SPop or
			KeyNotificationType.XAdd or
			KeyNotificationType.XDel or
			KeyNotificationType.XTrim or
			KeyNotificationType.ZAdd or
			KeyNotificationType.ZDiffStore or
			KeyNotificationType.ZInterStore or
			KeyNotificationType.ZUnionStore or
			KeyNotificationType.ZIncr or
			KeyNotificationType.ZRemByRank or
			KeyNotificationType.ZRemByScore or
			KeyNotificationType.ZRem or
			KeyNotificationType.ArDel or
			KeyNotificationType.ArDelRange or
			KeyNotificationType.ArSet => DistributedCacheNotificationKind.Updated,
			_ => DistributedCacheNotificationKind.None,
		};

		return kind != DistributedCacheNotificationKind.None;
	}
	#endregion

	#region 重写方法
	protected override async ValueTask OnCloseAsync(CancellationToken cancellation)
	{
		cancellation.ThrowIfCancellationRequested();

		lock(_dispatchLock)
		{
			if(_closing != 0)
				return;

			_closing = 1;
			_lifetime.Cancel();
		}

		var queue = Interlocked.Exchange(ref _queue, null);

		try
		{
			if(queue != null)
				await queue.UnsubscribeAsync();
		}
		catch(Exception exception)
		{
			Zongsoft.Diagnostics.Logging.GetLogging(typeof(RedisCacheSubscription)).Error(exception);
		}
		finally
		{
			_cache.Unregister(this);
		}
	}

	protected override async ValueTask DisposeAsync(bool disposing)
	{
		if(this.IsDisposed)
			return;

		await base.DisposeAsync(disposing);

		if(disposing)
			_lifetime.Dispose();
	}
	#endregion

	#region 私有方法
	private async Task OnMessageAsync(ChannelMessage message)
	{
		if(Volatile.Read(ref _closing) != 0 || !message.TryParseKeyNotification(out var source))
			return;

		if(!TryGetNotificationKind(source.Type, out var kind) || (_kind & kind) == 0)
			return;

		var key = (string)source.GetKey();
		if(string.IsNullOrEmpty(key) || !key.StartsWith(_namespace, StringComparison.Ordinal))
			return;

		key = key[_namespace.Length..];
		if(string.IsNullOrEmpty(key) || !key.StartsWith(_prefix, StringComparison.Ordinal) || Volatile.Read(ref _closing) != 0)
			return;

		try
		{
			ValueTask operation;

			lock(_dispatchLock)
			{
				if(_closing != 0)
					return;

				operation = this.Handler.HandleAsync(new DistributedCacheNotification(kind, key), _lifetime.Token);
			}

			await operation;
		}
		catch(OperationCanceledException) when (_lifetime.IsCancellationRequested)
		{
		}
		catch(Exception exception)
		{
			Zongsoft.Diagnostics.Logging.GetLogging(typeof(RedisCacheSubscription)).Error(exception);
		}
	}
	#endregion
}
