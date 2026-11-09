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

namespace Zongsoft.Collections;

/// <summary>表示集合元素被删除的原因。</summary>
public enum CollectionRemovedReason
{
	/// <summary>通过删除方法。</summary>
	Remove,

	/// <summary>因为集合溢出而激发的自动删除。</summary>
	Overflow,

	/// <summary>因为缓存项过期而被删除。</summary>
	Expired,

	/// <summary>其他原因。</summary>
	Other,
}
