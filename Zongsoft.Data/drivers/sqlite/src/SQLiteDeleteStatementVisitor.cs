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
 * Copyright (C) 2020-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Data.SQLite library.
 *
 * The Zongsoft.Data.SQLite is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data.SQLite is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data.SQLite library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;

using Zongsoft.Data.Common;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.SQLite;

/*
 * 由于 SQLite 的删除语句不支持类似 PostgreSQL 中的 USING 子句，因此必须将删除语句中的连接、查询子句转换为 WITH 子句；
 * 并且 SQLite 的删除语句的 WHERE 条件还不支持直接 WITH 子句表名，因此必须使用 EXISTS 将原 WHERE 过滤条件进行包裹。
 *
 * 下面的 PostgreSQL 删除语句：
 * DELETE FROM DepartmentMember AS M
 * USING Department AS D
 * WHERE M.DepartmentId = D.DepartmentId AND 
 *       D.Name LIKE '研发部';
 *
 * 对应的 SQLite 删除语句：
 * WITH D AS (SELECT * FROM Department)
 * DELETE FROM DepartmentMember AS M
 * WHERE EXISTS
 * (
 *     SELECT *
 *     FROM D
 *     WHERE 
 *         D.DepartmentId = M.DepartmentId AND
 *         D.Name LIKE '研发部'
 *     LIMIT 1
 * );
 */
public class SQLiteDeleteStatementVisitor : DeleteStatementVisitor
{
	#region 单例字段
	public static readonly SQLiteDeleteStatementVisitor Instance = new();
	#endregion

	#region 构造函数
	private SQLiteDeleteStatementVisitor() { }
	#endregion

	#region 重写方法
	protected override void VisitTables(ExpressionVisitorContext context, DeleteStatement statement, IList<TableIdentifier> tables) { }
	protected override void VisitJoin(ExpressionVisitorContext context, DeleteStatement statement, JoinClause joining, int index) { }

	protected override void VisitWith(ExpressionVisitorContext context, DeleteStatement statement, CommonTableExpressionCollection expressions)
	{
		/*
		 * 注意：SQLite 的删除语句不支持类似 PostgreSQL 中的 USING 子句，
		 * 因此在这里需要将删除语句中的连接、查询子句转换为 WITH 子句，以便在 WHERE 子句中使用这些表进行条件过滤。
		 */

		if(statement.HasFrom)
		{
			foreach(var item in statement.From)
			{
				if(item is JoinClause joining)
				{
					expressions ??= [];
					expressions.Add(new(joining.Alias, new SelectStatement(joining.Target, joining.Alias)));
				}
				else if(item is SelectStatement select)
				{
					expressions ??= [];
					expressions.Add(new(select.Alias, select));
				}
			}
		}

		base.VisitWith(context, statement, statement.With = expressions);
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

		/*
		 * 注意：SQLite 的删除语句的 WHERE 子句不支持直接引用 WITH 子句定义的表名，而只能通过子查询来引用，
		 * 因此在这里需要将原 WHERE 条件放在一个新的查询子句中并使用 EXISTS 操作包裹。
		 */

		if(statement.With != null && statement.With.Count > 0)
		{
			context.WriteLine();
			context.Write("WHERE EXISTS (SELECT * FROM ");

			int index = 0;
			foreach(var item in statement.With)
			{
				if(index++ > 0)
					context.Write(',');

				context.Write(item.Name);
			}

			base.VisitWhere(context, statement, conditions);

			context.WriteLine(" LIMIT 1)");
			return;
		}

		base.VisitWhere(context, statement, conditions);
	}
	#endregion
}
