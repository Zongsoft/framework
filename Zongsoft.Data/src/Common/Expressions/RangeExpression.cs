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

public readonly struct RangeExpression : IExpression
{
	#region 公共字段
	public readonly IExpression Minimum;
	public readonly IExpression Maximum;
	#endregion

	#region 构造函数
	public RangeExpression(IExpression minimum, IExpression maximum)
	{
		this.Minimum = minimum;
		this.Maximum = maximum;
	}
	#endregion

	#region 公共方法
	public string Accept(IExpressionVisitor visitor) => visitor.Visit(this);
	#endregion

	#region 重写方法
	public override string ToString() => $"{Minimum} ~ {Maximum}";
	#endregion
}
