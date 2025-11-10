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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Data.PostgreSql library.
 *
 * The Zongsoft.Data.PostgreSql is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data.PostgreSql is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data.PostgreSql library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;

using Zongsoft.Data.Common;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.PostgreSql;

public class PostgreSqlDeleteStatementVisitor : DeleteStatementVisitor
{
	#region 单例字段
	public static readonly PostgreSqlDeleteStatementVisitor Instance = new PostgreSqlDeleteStatementVisitor();
	#endregion

	#region 构造函数
	private PostgreSqlDeleteStatementVisitor() { }
	#endregion

	#region 重写方法
	protected override void OnVisiting(ExpressionVisitorContext context, DeleteStatement statement) { }
	protected override void VisitTables(ExpressionVisitorContext context, DeleteStatement statement, IList<TableIdentifier> tables) { }
	protected override void VisitJoin(ExpressionVisitorContext context, DeleteStatement statement, JoinClause joining, int index)
	{
		if(index == 0)
			context.Write(" USING ");

		if(index > 0)
			context.Write(',');

		switch(joining.Target)
		{
			case TableIdentifier table:
				context.Visit(table);
				break;
			case SelectStatement subquery:
				context.Write('(');

				//递归生成子查询语句
				context.Visit(subquery);

				if(string.IsNullOrEmpty(subquery.Alias))
					context.WriteLine(")");
				else
					context.WriteLine($") AS {subquery.Alias}");

				break;
		}
	}

	protected override void VisitWhere(ExpressionVisitorContext context, DeleteStatement statement, IExpression where)
	{
		var conditions = ConditionExpression.And(where);

		if(statement.From.Count > 1)
		{
			for(int i = 1; i < statement.From.Count; i++)
			{
				if(statement.From[i] is JoinClause joining)
					conditions.Add(joining.Conditions);
			}
		}

		base.VisitWhere(context, statement, conditions);
	}
	#endregion
}
