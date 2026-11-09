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

[Flags]
public enum ConditionBehaviors
{
	/// <summary>无特性</summary>
	None = 0,

	/// <summary>忽略值为空(<c>null</c>)的查询项</summary>
	IgnoreNull = 1,

	/// <summary>忽略值为空字符串或全空白字符串的查询项</summary>
	IgnoreEmpty = 2,

	/// <summary>忽略值为空或全空白字符串的查询项</summary>
	IgnoreNullOrEmpty = 3,
}
