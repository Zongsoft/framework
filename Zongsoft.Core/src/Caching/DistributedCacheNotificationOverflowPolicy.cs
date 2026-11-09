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

namespace Zongsoft.Caching;

/// <summary>指定分布式缓存通知订阅队列溢出时的处理策略。</summary>
public enum DistributedCacheNotificationOverflowPolicy
{
	/// <summary>丢弃队列中最旧的通知，为新通知腾出空间。</summary>
	DropOldest,
	/// <summary>丢弃刚收到的新通知。</summary>
	DropNewest,
}
