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

/// <summary>表示缓存项被移除的原因。</summary>
public enum CacheEvictedReason
{
	/// <summary>系统内部原因。</summary>
	None,

	/// <summary>因为过期被移除。</summary>
	Expired,

	/// <summary>被手动删除的。</summary>
	Removed,

	/// <summary>被手动替换的。</summary>
	Replaced,

	/// <summary>依赖变更触发。</summary>
	Depended,

	/// <summary>溢出，超出容量。</summary>
	Overfull,
}
