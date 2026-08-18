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
