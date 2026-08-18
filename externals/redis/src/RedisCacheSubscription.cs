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
using System.Threading.Channels;
using System.Diagnostics;

using Zongsoft.Caching;
using Zongsoft.Components;
using Zongsoft.Communication;

using StackExchange.Redis;

namespace Zongsoft.Externals.Redis;

internal sealed class RedisCacheSubscription : ChannelBase, IDistributedCacheSubscription
{
	#region 成员字段
	private readonly RedisService _cache;
	private readonly RedisCacheNotificationHub _hub;
	private readonly string _prefix;
	private readonly DistributedCacheNotificationKind _kind;
	private readonly DistributedCacheSubscriptionOptions _options;
	private readonly CancellationTokenSource _lifetime;
	private readonly object _queueLock = new();
	private readonly Channel<DistributedCacheNotification> _queue;
	private readonly Task _dispatchTask;
	private long _pending;
	private long _dropped;
	private int _closing;
	#endregion

	#region 构造函数
	public RedisCacheSubscription(RedisService cache, RedisCacheNotificationHub hub, IHandler<DistributedCacheNotification> handler, DistributedCacheSubscriptionOptions options)
	{
		_cache = cache ?? throw new ArgumentNullException(nameof(cache));
		_hub = hub ?? throw new ArgumentNullException(nameof(hub));
		this.Handler = handler ?? throw new ArgumentNullException(nameof(handler));

		ArgumentNullException.ThrowIfNull(options);

		_options = options.Snapshot();
		_prefix = options.Prefix ?? string.Empty;
		_kind = options.Kind;
		_lifetime = new CancellationTokenSource();
		_queue = Channel.CreateBounded<DistributedCacheNotification>(new BoundedChannelOptions(options.Capacity)
		{
			SingleReader = true,
			SingleWriter = false,
			FullMode = BoundedChannelFullMode.Wait,
		});
		_dispatchTask = this.DispatchAsync();
	}
	#endregion

	#region 公共属性
	public IDistributedCache Cache => _cache;
	public IHandler<DistributedCacheNotification> Handler { get; }
	public DistributedCacheSubscriptionOptions Options => _options;
	public long PendingCount => Interlocked.Read(ref _pending);
	public long DroppedCount => Interlocked.Read(ref _dropped);
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
	internal ValueTask<bool> SubscribeAsync(CancellationToken cancellation)
	{
		cancellation.ThrowIfCancellationRequested();
		return _hub.AddAsync(this, cancellation);
	}

	internal void Enqueue(DistributedCacheNotificationKind kind, string key)
	{
		if(Volatile.Read(ref _closing) != 0 || (_kind & kind) == 0 || !key.StartsWith(_prefix, StringComparison.Ordinal))
			return;

		var notification = new DistributedCacheNotification(kind, key);
		lock(_queueLock)
		{
			if(_closing != 0)
				return;

			if(_queue.Writer.TryWrite(notification))
			{
				Interlocked.Increment(ref _pending);
				RedisDiagnostics.PendingNotifications.Add(1);
				return;
			}

			if(_options.OverflowPolicy == DistributedCacheNotificationOverflowPolicy.DropOldest && _queue.Reader.TryRead(out _))
			{
				Interlocked.Decrement(ref _pending);
				Interlocked.Increment(ref _dropped);
				RedisDiagnostics.PendingNotifications.Add(-1);
				RedisDiagnostics.DroppedNotifications.Add(1);
				if(_queue.Writer.TryWrite(notification))
				{
					Interlocked.Increment(ref _pending);
					RedisDiagnostics.PendingNotifications.Add(1);
				}
				else
				{
					Interlocked.Increment(ref _dropped);
					RedisDiagnostics.DroppedNotifications.Add(1);
				}
			}
			else
			{
				Interlocked.Increment(ref _dropped);
				RedisDiagnostics.DroppedNotifications.Add(1);
			}
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

		if(Interlocked.Exchange(ref _closing, 1) != 0)
			return;

		_lifetime.Cancel();
		_queue.Writer.TryComplete();

		try
		{
			await _hub.RemoveAsync(this);
			await _dispatchTask;
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
	private async Task DispatchAsync()
	{
		try
		{
			while(await _queue.Reader.WaitToReadAsync())
			{
				DistributedCacheNotification notification;
				lock(_queueLock)
				{
					if(!_queue.Reader.TryRead(out notification))
						continue;
					Interlocked.Decrement(ref _pending);
					RedisDiagnostics.PendingNotifications.Add(-1);
				}

				if(Volatile.Read(ref _closing) != 0)
					continue;

				try
				{
					using var activity = RedisDiagnostics.ActivitySource.StartActivity("redis.cache.notification.handle", ActivityKind.Consumer);
					var started = Stopwatch.GetTimestamp();
					try
					{
						await this.Handler.HandleAsync(notification, _lifetime.Token);
					}
					finally
					{
						RedisDiagnostics.NotificationDuration.Record(Stopwatch.GetElapsedTime(started).TotalMilliseconds);
					}
				}
				catch(OperationCanceledException) when (_lifetime.IsCancellationRequested)
				{
				}
				catch(Exception exception)
				{
					Zongsoft.Diagnostics.Logging.GetLogging(typeof(RedisCacheSubscription)).Error(exception);
				}
			}
		}
		catch(Exception exception)
		{
			Zongsoft.Diagnostics.Logging.GetLogging(typeof(RedisCacheSubscription)).Error(exception);
		}
	}
	#endregion
}
