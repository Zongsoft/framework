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
