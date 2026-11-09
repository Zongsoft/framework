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

/// <summary>表示缓存设置项的必须条件的枚举。</summary>
public enum CacheRequisite
{
	/// <summary>无约束，即无论缓存项是否存在都能被设置。</summary>
	Always,

	/// <summary>存在，即只有当缓存项存在时才能被更新。</summary>
	Exists,

	/// <summary>不存在，即只有当缓存项不存在时才能被设置。</summary>
	NotExists,
}
