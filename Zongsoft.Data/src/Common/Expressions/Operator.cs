/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Data library.
 *
 * The Zongsoft.Data is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.ComponentModel;

namespace Zongsoft.Data.Common.Expressions
{
	/// <summary>
	/// 表示操作符的枚举。
	/// </summary>
	public enum Operator
	{
		/// <summary>加号或正号(+)</summary>
		Plus,
		/// <summary>减号或负号(-)</summary>
		Minus,
		/// <summary>乘号(*)</summary>
		Multiply,
		/// <summary>除号(/)</summary>
		Divide,
		/// <summary>取模(%)</summary>
		Modulo,
		/// <summary>赋值(=)</summary>
		Assign,

		/// <summary>逻辑非</summary>
		Not,
		/// <summary>逻辑与</summary>
		AndAlso,
		/// <summary>逻辑或</summary>
		OrElse,

		/// <summary>如果一组的比较都为真，则为真。</summary>
		All,
		/// <summary>如果一组的比较中任何一个为真，则为真</summary>
		Any,
		/// <summary>如果操作数在某个范围之内，那么就为真。</summary>
		Between,
		/// <summary>如果子查询包含一些行，那么就为真。</summary>
		Exists,
		/// <summary>如果子查询不包含一些行，那么就为真。</summary>
		NotExists,
		/// <summary>如果操作数等于表达式列表中的一个，那么就为真。</summary>
		In,
		/// <summary>如果操作数不等于表达式列表中的一个，那么就为真。</summary>
		NotIn,
		/// <summary>如果操作数与一种模式相匹配，那么就为真。</summary>
		Like,

		/// <summary>是(IS)</summary>
		Is,
		/// <summary>不是(NOT IS)</summary>
		NotIs,

		/// <summary>等于号(==)</summary>
		Equal,
		/// <summary>不等于(!=)</summary>
		NotEqual,
		/// <summary>小于(&lt;)</summary>
		LessThan,
		/// <summary>小于等于(&lt;=)</summary>
		LessThanOrEqual,
		/// <summary>大于(>)</summary>
		GreaterThan,
		/// <summary>大于等于(>=)</summary>
		GreaterThanOrEqual,
	}
}
