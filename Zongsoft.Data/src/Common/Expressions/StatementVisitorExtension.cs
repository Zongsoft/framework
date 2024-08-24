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

namespace Zongsoft.Data.Common.Expressions
{
	internal static class StatementVisitorExtension
	{
		public static void VisitFrom(this ExpressionVisitorContext context, ICollection<ISource> sources, Action<ExpressionVisitorContext, JoinClause> join)
		{
			if(sources == null || sources.Count == 0)
				return;

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
							context.Write(") AS " + subquery.Alias);

						break;
					case JoinClause joining:
						if(join == null)
							VisitJoin(context, joining);
						else
							join(context, joining);

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
	}
}
