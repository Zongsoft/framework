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

public enum ConditionOperator
{
	/// <summary>等于</summary>
	[Components.Alias("==")]
	Equal,

	/// <summary>不等于</summary>
	[Components.Alias("!=")]
	NotEqual,

	/// <summary>大于</summary>
	[Components.Alias(">")]
	GreaterThan,

	/// <summary>大于或等于</summary>
	[Components.Alias(">=")]
	GreaterThanEqual,

	/// <summary>小于</summary>
	[Components.Alias("<")]
	LessThan,

	/// <summary>小于或等于</summary>
	[Components.Alias("<=")]
	LessThanEqual,

	/// <summary>模糊匹配</summary>
	[Components.Alias("*=")]
	Like,

	/// <summary>介于</summary>
	Between,

	/// <summary>范围</summary>
	In,

	/// <summary>排除范围</summary>
	NotIn,

	/// <summary>存在(单目运算符)</summary>
	Exists,

	/// <summary>不存在(单目运算符)</summary>
	NotExists,
}
