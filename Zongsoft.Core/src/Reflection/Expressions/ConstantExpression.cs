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

namespace Zongsoft.Reflection.Expressions;

public class ConstantExpression : MemberExpression
{
	#region 单例字段
	public static readonly ConstantExpression Null = new();
	#endregion

	#region 构造函数
	private ConstantExpression() { }
	public ConstantExpression(object value)
	{
		this.Value = value ?? throw new ArgumentNullException(nameof(value));
	}
	#endregion

	#region 公共属性
	public override MemberExpressionType ExpressionType => MemberExpressionType.Constant;
	public object Value { get; }
	#endregion
}
