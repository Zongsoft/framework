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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Zongsoft.Data.Common.Expressions;

public class AggregateStatementBuilder : IStatementBuilder<DataAggregateContext>
{
	#region 公共方法
	public async IAsyncEnumerable<IStatementBase> BuildAsync(DataAggregateContext context, [EnumeratorCancellation] CancellationToken cancellation = default)
	{
		var statement = this.CreateStatement(context);
		var argument = (IExpression)null;

		if(string.IsNullOrWhiteSpace(context.Aggregate.Name) || context.Aggregate.Name == "*")
			argument = Expression.Literal("*");
		else
			argument = statement.From(context.Aggregate.Name, context.Aliaser, null, out var property).CreateField(property);

		//添加返回的聚合函数成员
		statement.Select.Members.Add(AggregateExpression.Aggregate(context.Aggregate, argument));

		//生成条件子句
		statement.Where = statement.Where(await context.ValidateAsync(cancellation).ConfigureAwait(false), context.Aliaser);

		yield return statement;
	}
	#endregion

	#region 虚拟方法
	protected virtual AggregateStatement CreateStatement(DataAggregateContext context) => new(context.Entity);
	#endregion
}
