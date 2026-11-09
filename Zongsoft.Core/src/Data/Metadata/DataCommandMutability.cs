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

namespace Zongsoft.Data.Metadata;

/// <summary>表示数据命令变化性的枚举。</summary>
[Flags]
public enum DataCommandMutability
{
	/// <summary>无变化，即只读。</summary>
	None = 0,

	/// <summary>删除。</summary>
	Delete = 1,

	/// <summary>新增。</summary>
	Insert = 2,

	/// <summary>更新。</summary>
	Update = 4,

	/// <summary>增改，即新增和更新。</summary>
	Upsert = 6,
}
