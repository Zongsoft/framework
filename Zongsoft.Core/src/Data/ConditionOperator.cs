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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
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
