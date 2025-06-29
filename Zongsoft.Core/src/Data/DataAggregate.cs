﻿/*
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

/// <summary>
/// 表示聚合元素的结构。
/// </summary>
public readonly struct DataAggregate
{
	#region 构造函数
	public DataAggregate(DataAggregateFunction function, string name, string alias = null, bool distinct = false)
	{
		this.Function = function;
		this.Name = name;
		this.Alias = alias;
		this.Distinct = distinct;
	}
	#endregion

	#region 公共属性
	/// <summary>获取聚合元素的成员名(字段名或通配符)。</summary>
	public string Name { get; }

	/// <summary>获取聚合元素的别称。</summary>
	public string Alias { get; }

	/// <summary>获取一个值，指示是否开启去重。</summary>
	public bool Distinct { get; }

	/// <summary>获取聚合元素的聚合函数。</summary>
	public DataAggregateFunction Function { get; }
	#endregion
}
