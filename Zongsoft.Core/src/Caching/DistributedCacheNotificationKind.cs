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

using System;

namespace Zongsoft.Caching;

/// <summary>表示分布式缓存通知的种类。</summary>
[Flags]
public enum DistributedCacheNotificationKind
{
	/// <summary>无通知。</summary>
	None = 0,
	/// <summary>缓存项被创建、覆盖或其数据内容发生变化。</summary>
	Updated = 1,
	/// <summary>缓存项被显式移除。</summary>
	Removed = 2,
	/// <summary>缓存项因过期而被移除。</summary>
	Expired = 4,
	/// <summary>缓存项因容量或缓存策略而被淘汰。</summary>
	Evicted = 8,
	/// <summary>所有通知种类。</summary>
	All = Updated | Removed | Expired | Evicted,
}
