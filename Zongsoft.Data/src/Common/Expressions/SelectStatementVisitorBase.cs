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

public class SelectStatementVisitorBase<TStatement> : StatementVisitorBase<TStatement> where TStatement : SelectStatementBase
{
	#region 构造函数
	protected SelectStatementVisitorBase() { }
	#endregion

	#region 重写方法
	protected override void OnVisit(ExpressionVisitorContext context, TStatement statement)
	{
		this.VisitWith(context, statement.With);
		this.VisitSelect(context, statement.Select);
		this.VisitFrom(context, statement.From);
		this.VisitWhere(context, statement.Where);
	}
	#endregion

	#region 虚拟方法
	protected virtual void VisitWith(ExpressionVisitorContext context, CommonTableExpressionCollection expressions)
	{
		if(expressions == null || expressions.Count == 0)
			return;

		context.Write("WITH ");
		context.Visit(expressions);
	}

	protected virtual void VisitSelect(ExpressionVisitorContext context, SelectClause clause)
	{
		if(context.Output.Length > 0)
			context.WriteLine();

		context.Write("SELECT ");

		this.VisitSelectOption(context, clause);

		if(clause == null || clause.Members.Count == 0)
			context.Write('*');
		else
		{
			int index = 0;

			foreach(var member in clause.Members)
			{
				if(index++ > 0)
					context.WriteLine(",");

				context.Visit(member);
			}
		}
	}

	protected virtual void VisitSelectOption(ExpressionVisitorContext context, SelectClause clause)
	{
		if(clause.IsDistinct)
			context.Write("DISTINCT ");
	}

	protected virtual void VisitFrom(ExpressionVisitorContext context, ICollection<ISource> sources)
	{
		context.VisitFrom(sources, this.VisitJoin);
	}

	protected virtual void VisitJoin(ExpressionVisitorContext context, JoinClause joining, int index)
	{
		context.VisitJoin(joining);
	}

	protected virtual void VisitWhere(ExpressionVisitorContext context, IExpression where)
	{
		context.VisitWhere(where);
	}
	#endregion
}
