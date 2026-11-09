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

public class AggregateExpression : MethodExpression
{
	#region 构造函数
	public AggregateExpression(DataAggregateFunction function, bool distinct, params IExpression[] arguments) : base(function.ToString(), MethodType.Function, arguments)
	{
		this.Function = function;
		this.Distinct = distinct;
	}

	public AggregateExpression(DataAggregate aggregate, params IExpression[] arguments) : base(aggregate.Function.ToString(), MethodType.Function, arguments)
	{
		this.Function = aggregate.Function;
		this.Distinct = aggregate.Distinct;

		if(string.IsNullOrEmpty(aggregate.Alias))
		{
			if(string.IsNullOrEmpty(aggregate.Name) || aggregate.Name == "*")
				this.Alias = aggregate.Function.ToString();
			else
				this.Alias = $"{aggregate.Name}{aggregate.Function}";
		}
		else
			this.Alias = aggregate.Alias;
	}
	#endregion

	#region 公共属性
	public bool Distinct { get; }
	public new DataAggregateFunction Function { get; }
	#endregion

	#region 静态方法
	public static AggregateExpression Aggregate(DataAggregate aggregate, IExpression argument)
	{
		if(argument is FieldIdentifier field)
			field.Alias = null;

		return new AggregateExpression(aggregate, argument == null ? [] : [argument]);
	}
	#endregion
}
