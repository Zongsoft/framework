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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Zongsoft.Externals.Redis;

/// <summary>公开 Redis 扩展的诊断源名称和标准诊断对象。</summary>
public static class RedisDiagnostics
{
	public const string Name = "Zongsoft.Externals.Redis";
	public static readonly ActivitySource ActivitySource = new(Name);
	public static readonly Meter Meter = new(Name);

	internal static readonly UpDownCounter<long> ActiveConnections = Meter.CreateUpDownCounter<long>("redis.connections.active", "{connection}");
	internal static readonly Counter<long> ConnectionFailures = Meter.CreateCounter<long>("redis.connections.failures", "{failure}");
	internal static readonly Counter<long> ConnectionRestorations = Meter.CreateCounter<long>("redis.connections.restorations", "{restoration}");
	internal static readonly Counter<long> ConnectionErrors = Meter.CreateCounter<long>("redis.connections.errors", "{error}");
	internal static readonly UpDownCounter<long> PendingNotifications = Meter.CreateUpDownCounter<long>("redis.cache.notifications.pending", "{notification}");
	internal static readonly Counter<long> DroppedNotifications = Meter.CreateCounter<long>("redis.cache.notifications.dropped", "{notification}");
	internal static readonly Histogram<double> NotificationDuration = Meter.CreateHistogram<double>("redis.cache.notification.duration", "ms");
	internal static readonly Counter<long> DeadLetters = Meter.CreateCounter<long>("redis.queue.deadletters", "{message}");
	internal static readonly Counter<long> LockRenewalFailures = Meter.CreateCounter<long>("redis.lock.renewal.failures", "{failure}");
}
