/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
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
		public static void VisitFrom(this IExpressionVisitor visitor, ICollection<ISource> sources, Action<IExpressionVisitor, JoinClause> join)
		{
			if(sources == null || sources.Count == 0)
				return;

			visitor.Output.Append(" FROM ");

			foreach(var source in sources)
			{
				switch(source)
				{
					case TableIdentifier table:
						visitor.Visit(table);

						break;
					case SelectStatement subquery:
						visitor.Output.Append("(");

						//递归生成子查询语句
						visitor.Visit(subquery);

						if(string.IsNullOrEmpty(subquery.Alias))
							visitor.Output.Append(")");
						else
							visitor.Output.Append(") AS " + subquery.Alias);

						break;
					case JoinClause joining:
						if(join == null)
							VisitJoin(visitor, joining);
						else
							join(visitor, joining);

						break;
				}
			}
		}

		public static void VisitJoin(this IExpressionVisitor visitor, JoinClause joining)
		{
			visitor.Output.AppendLine();

			switch(joining.Type)
			{
				case JoinType.Inner:
					visitor.Output.Append("INNER JOIN ");
					break;
				case JoinType.Left:
					visitor.Output.Append("LEFT JOIN ");
					break;
				case JoinType.Right:
					visitor.Output.Append("RIGHT JOIN ");
					break;
				case JoinType.Full:
					visitor.Output.Append("FULL JOIN ");
					break;
			}

			switch(joining.Target)
			{
				case TableIdentifier table:
					visitor.Visit(table);

					if(string.IsNullOrEmpty(joining.Name))
						visitor.Output.AppendLine(" ON");
					else
						visitor.Output.AppendLine(" ON /* " + joining.Name + " */");

					break;
				case SelectStatement subquery:
					visitor.Output.Append("(");

					//递归生成子查询语句
					visitor.Visit(subquery);

					if(string.IsNullOrEmpty(subquery.Alias))
						visitor.Output.AppendLine(") ON");
					else
						visitor.Output.AppendLine(") AS " + subquery.Alias + " ON");

					break;
			}

			visitor.Visit(joining.Condition);
		}

		public static void VisitWhere(this IExpressionVisitor visitor, IExpression where)
		{
			if(where == null)
				return;

			if(visitor.Output.Length > 0)
				visitor.Output.AppendLine();

			visitor.Output.Append("WHERE ");
			visitor.Visit(where);
		}
	}
}
