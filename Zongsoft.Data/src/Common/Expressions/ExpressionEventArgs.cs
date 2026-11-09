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
using System.Text;

namespace Zongsoft.Data.Common.Expressions;

public class ExpressionEventArgs : EventArgs
{
	#region 构造函数
	public ExpressionEventArgs(StringBuilder output, IExpression expression)
	{
		this.Output = output;
		this.Expression = expression;
		this.Result = expression;
	}
	#endregion

	#region 公共属性
	public StringBuilder Output { get; }
	public IExpression Expression { get; }
	public IExpression Result { get; set; }
	#endregion
}
