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
using System.Collections.Generic;

namespace Zongsoft.Reflection.Expressions;

public class MethodExpression : MemberExpression
{
	#region 构造函数
	public MethodExpression(string name, IMemberExpression[] arguments = null)
	{
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		this.Name = name;

		if(arguments == null || arguments.Length == 0)
			this.Arguments = new List<IMemberExpression>();
		else
			this.Arguments = new List<IMemberExpression>(arguments);
	}
	#endregion

	#region 公共属性
	public override MemberExpressionType ExpressionType => MemberExpressionType.Method;
	public string Name { get; }
	public bool HasArguments => this.Arguments != null && this.Arguments.Count > 0;
	public IList<IMemberExpression> Arguments { get; }
	#endregion
}
