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
using System.Linq;
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

	protected override void VisitReturning(ExpressionVisitorContext context, ReturningClause clause)
	{
		var members = clause.Members.Where(member => member.Kind == ReturningKind.Newer).ToArray();
		base.VisitReturning(context, clause.Table == null ? new ReturningClause(members) : new ReturningClause(clause.Table, members));
	}
	#endregion
}
