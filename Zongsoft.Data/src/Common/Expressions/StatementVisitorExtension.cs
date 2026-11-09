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

public static class StatementVisitorExtension
{
	public static void VisitFrom(this ExpressionVisitorContext context, ICollection<ISource> sources, Action<ExpressionVisitorContext, JoinClause, int> join)
	{
		if(sources == null || sources.Count == 0)
			return;

		int index = 0;
		context.Write(" FROM ");

		foreach(var source in sources)
		{
			switch(source)
			{
				case TableIdentifier table:
					context.Visit(table);

					break;
				case SelectStatement subquery:
					context.Write("(");

					//递归生成子查询语句
					context.Visit(subquery);

					if(string.IsNullOrEmpty(subquery.Alias))
						context.Write(")");
					else
						context.Write($") AS {subquery.Alias}");

					break;
				case JoinClause joining:
					if(join == null)
						VisitJoin(context, joining);
					else
						join(context, joining, index++);

					break;
			}
		}
	}

	public static void VisitJoin(this ExpressionVisitorContext context, JoinClause joining)
	{
		context.WriteLine();

		switch(joining.Type)
		{
			case JoinType.Inner:
				context.Write("INNER JOIN ");
				break;
			case JoinType.Left:
				context.Write("LEFT JOIN ");
				break;
			case JoinType.Right:
				context.Write("RIGHT JOIN ");
				break;
			case JoinType.Full:
				context.Write("FULL JOIN ");
				break;
		}

		switch(joining.Target)
		{
			case TableIdentifier table:
				context.Visit(table);

				if(string.IsNullOrEmpty(joining.Name))
					context.WriteLine(" ON");
				else
					context.WriteLine(" ON /* " + joining.Name + " */");

				break;
			case SelectStatement subquery:
				context.Write("(");

				//递归生成子查询语句
				context.Visit(subquery);

				if(string.IsNullOrEmpty(subquery.Alias))
					context.WriteLine(") ON");
				else
					context.WriteLine(") AS " + subquery.Alias + " ON");

				break;
		}

		context.Visit(joining.Conditions);
	}

	public static void VisitWhere(this ExpressionVisitorContext context, IExpression where)
	{
		if(where == null)
			return;

		if(context.Output.Length > 0)
			context.WriteLine();

		context.Write("WHERE ");
		context.Visit(where);
	}

	public static void VisitReturning(this ExpressionVisitorContext context, ReturningClause clause)
	{
		int index = 0;

		foreach(var member in clause.Members)
		{
			if(index++ > 0)
				context.Write(",");

			var qualified = context.Dialect.GetIdentifier(member.Kind);
			if(!string.IsNullOrEmpty(qualified))
				context.Write($"{qualified}.");

			context.Write(context.Dialect.GetIdentifier(member.Field.Name));
			if(!string.IsNullOrEmpty(member.Field.Alias))
				context.Write($" AS {context.Dialect.GetIdentifier(member.Field.Alias)}");
		}

		if(clause.Table != null)
		{
			context.Write(" INTO ");
			context.Write(context.Dialect.GetIdentifier(clause.Table.Identifier()));
		}
	}
}
