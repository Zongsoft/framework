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

namespace Zongsoft.Data.Common.Expressions;

public enum SequenceMethod
{
	Next,
	Current,
}

public class SequenceExpression : MethodExpression
{
	#region 构造函数
	private SequenceExpression(string name, SequenceMethod method) : base(name, MethodType.Function)
	{
		this.Method = method;
	}
	#endregion

	#region 公共属性
	public SequenceMethod Method { get; }
	#endregion

	#region 静态方法
	public static SequenceExpression Next(string name, string alias = null) => new SequenceExpression(name, SequenceMethod.Next) { Alias = alias };
	public static SequenceExpression Current(string name, string alias = null) => new SequenceExpression(name, SequenceMethod.Current) { Alias = alias };
	#endregion
}
