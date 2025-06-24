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
using System.Collections.Generic;

namespace Zongsoft.Data.Common.Expressions;

public class AggregateStatementBuilder : IStatementBuilder<DataAggregateContext>
{
	#region 公共方法
	public IEnumerable<IStatementBase> Build(DataAggregateContext context)
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
		statement.Where = statement.Where(context.Validate(), context.Aliaser);

		yield return statement;
	}
	#endregion

	#region 虚拟方法
	protected virtual AggregateStatement CreateStatement(DataAggregateContext context) => new(context.Entity);
	#endregion
}
