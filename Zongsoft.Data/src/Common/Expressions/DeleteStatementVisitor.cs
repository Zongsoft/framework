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

namespace Zongsoft.Data.Common.Expressions;

public class DeleteStatementVisitor : StatementVisitorBase<DeleteStatement>
{
	#region 构造函数
	protected DeleteStatementVisitor() { }
	#endregion

	#region 重写方法
	protected override void OnVisit(ExpressionVisitorContext context, DeleteStatement statement)
	{
		this.VisitWith(context, statement, statement.With);
		this.VisitDelete(context, statement);
		this.VisitTables(context, statement, statement.Tables);
		this.VisitFrom(context, statement, statement.From);
		this.VisitWhere(context, statement, statement.Where);
	}

	protected override void OnVisiting(ExpressionVisitorContext context, DeleteStatement statement)
	{
		if(statement.Returning != null && statement.Returning.Table != null)
			context.Visit(statement.Returning.Table);
	}

	protected override void OnVisited(ExpressionVisitorContext context, DeleteStatement statement)
	{
		if(statement.Returning != null)
			this.VisitReturning(context, statement.Returning);

		context.WriteLine(";");
	}
	#endregion

	#region 虚拟方法
	protected virtual void VisitWith(ExpressionVisitorContext context, DeleteStatement statement, CommonTableExpressionCollection expressions)
	{
		if(expressions == null || expressions.Count == 0)
			return;

		context.Write("WITH ");
		context.Visit(expressions);
	}

	protected virtual void VisitDelete(ExpressionVisitorContext context, DeleteStatement statement)
	{
		context.Write("DELETE ");
	}

	protected virtual void VisitTables(ExpressionVisitorContext context, DeleteStatement statement, IList<TableIdentifier> tables)
	{
		for(int i = 0; i < tables.Count; i++)
		{
			if(i > 0)
				context.Write(",");

			if(string.IsNullOrEmpty(tables[i].Alias))
				context.Write(tables[i].Name);
			else
				context.Write(tables[i].Alias);
		}
	}

	protected virtual void VisitFrom(ExpressionVisitorContext context, DeleteStatement statement, ICollection<ISource> sources)
	{
		context.VisitFrom(sources, (ctx, join, index) => this.VisitJoin(ctx, statement, join, index));
	}

	protected virtual void VisitJoin(ExpressionVisitorContext context, DeleteStatement statement, JoinClause joining, int index)
	{
		context.VisitJoin(joining);
	}

	protected virtual void VisitWhere(ExpressionVisitorContext context, DeleteStatement statement, IExpression where)
	{
		context.VisitWhere(where);
	}

	protected virtual void VisitReturning(ExpressionVisitorContext context, ReturningClause clause)
	{
		if(clause.Members == null || clause.Members.Count == 0)
			return;

		this.OnVisiteReturning(context, clause);
		context.VisitReturning(clause);
	}

	protected virtual void OnVisiteReturning(ExpressionVisitorContext context, ReturningClause clause) => context.Write("\nRETURNING ");
	#endregion
}
