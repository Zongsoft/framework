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

using Zongsoft.Data.Common;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.PostgreSql;

public class PostgreSqlDeleteStatementVisitor : DeleteStatementVisitor
{
	#region 单例字段
	public static readonly PostgreSqlDeleteStatementVisitor Instance = new();
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
