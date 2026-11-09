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

public class BinaryExpression : Expression
{
	#region 构造函数
	public BinaryExpression(Operator @operator, IExpression left, IExpression right)
	{
		this.Operator = @operator;
		this.Left = left;
		this.Right = right;
	}
	#endregion

	#region 公共属性
	public IExpression Left { get; }
	public Operator Operator { get; internal set; }
	public IExpression Right { get; }
	#endregion
}
