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

public class SQLiteUpdateStatementVisitor : UpdateStatementVisitor
{
	#region 单例字段
	public static readonly SQLiteUpdateStatementVisitor Instance = new();
	#endregion

	#region 构造函数
	private SQLiteUpdateStatementVisitor() { }
	#endregion

	#region 重写方法
	protected override void VisitTables(ExpressionVisitorContext context, UpdateStatement statement, IList<TableIdentifier> tables)
	{
		for(int i = 0; i < tables.Count; i++)
		{
			if(i > 0)
				context.Write(",");

			context.Visit(tables[i]);
		}
	}

	protected override void VisitFrom(ExpressionVisitorContext context, UpdateStatement statement, ICollection<ISource> sources)
	{
		if(sources.Count <= 1)
			return;

		context.Write(" FROM ");

		foreach(var source in sources)
		{
			switch(source)
			{
				case TableIdentifier table:
					//跳过当前更新表
					if(table != statement.Table)
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
					context.VisitJoin(joining);
					break;
			}
		}
	}

	protected override void VisitFields(ExpressionVisitorContext context, UpdateStatement statement, ICollection<FieldValue> fields)
	{
		var index = 0;

		foreach(var field in fields)
		{
			if(index++ > 0)
				context.WriteLine(",");

			//注意：SQLite 不支持 SET 子句中的字段名前附带有表名或表别名
			context.Write($"{context.Dialect.GetIdentifier(field.Field.Name)}=");

			var parenthesisRequired = field.Value is IStatementBase;

			if(parenthesisRequired)
				context.Write("(");

			context.Visit(field.Value);

			if(parenthesisRequired)
				context.Write(")");
		}
	}

	protected override void OnVisiteReturning(ExpressionVisitorContext context, ReturningClause clause)
	{
		List<ReturningClause.ReturningMember> removables = [];

		foreach(var member in clause.Members)
		{
			if(member.Kind == ReturningKind.Older)
				removables.Add(member);
		}

		foreach(var member in removables)
			clause.Members.Remove(member);

		if(clause.Members.Count > 0)
			base.OnVisiteReturning(context, clause);
	}
	#endregion
}
