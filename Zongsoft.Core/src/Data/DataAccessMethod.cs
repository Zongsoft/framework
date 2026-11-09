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

namespace Zongsoft.Data;

/// <summary>表示数据访问的方法名的枚举。</summary>
public enum DataAccessMethod
{
	/// <summary>执行方法</summary>
	Execute,

	/// <summary>是否存在</summary>
	Exists,

	/// <summary>聚合方法</summary>
	Aggregate,

	/// <summary>导入方法</summary>
	Import,

	/// <summary>查询方法</summary>
	Select,

	/// <summary>删除方法</summary>
	Delete,

	/// <summary>新增方法</summary>
	Insert,

	/// <summary>更新方法</summary>
	Update,

	/// <summary>增改方法</summary>
	Upsert,
}
