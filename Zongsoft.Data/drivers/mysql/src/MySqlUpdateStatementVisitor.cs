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

namespace Zongsoft.Data.MySql;

public class MySqlUpdateStatementVisitor : UpdateStatementVisitor
{
	#region 单例字段
	public static readonly MySqlUpdateStatementVisitor Instance = new();
	#endregion

	#region 构造函数
	private MySqlUpdateStatementVisitor() { }
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

		/*
		 * 注意：由于 MySQL 的 UPDATE 语句不支持 FROM 子句，因此必须将其改写为多表修改的语法。
		 */

		if(statement.HasFrom)
		{
			foreach(var source in statement.From)
			{
				switch(source)
				{
					case TableIdentifier table:
						if(!tables.Contains(table))
						{
							context.Write(",");
							context.Visit(table);
						}

						break;
					case JoinClause join:
						if(join.Target is TableIdentifier target)
						{
							if(!tables.Contains(target))
							{
								context.Write(",");
								context.Visit(target);
							}
						}
						else
						{
							throw new DataException(string.Format(Properties.Resources.UpdateStatement_JoinTargetUnsupported_Message, MySqlDriver.NAME, join.Target.GetType().Name));
						}

						break;
					default:
						throw new NotSupportedException(string.Format(Properties.Resources.UpdateStatement_SourceUnsupported_Message, MySqlDriver.NAME, source.GetType().Name));
				}
			}
		}
	}

	protected override void VisitWhere(ExpressionVisitorContext context, UpdateStatement statement, IExpression where)
	{
		/*
		 * 注意：由于 MySQL 的 UPDATE 语句不支持 FROM 子句，因此必须将其改写为多表修改的语法。
		 * 由于 FROM 子句中可能包含 JOIN 类型语句，所以必须将 JOIN 子句中的条件式添加到 UPDATE 语句的 WHERE 子句中。
		 */

		if(statement.HasFrom)
		{
			var conditions = ConditionExpression.And();

			foreach(var source in statement.From)
			{
				if(source is JoinClause join)
					conditions.Add(join.Conditions);
			}

			if(conditions.Count > 0)
			{
				conditions.Add(where);
				where = conditions;
			}
		}

		//调用基类同名方法
		base.VisitWhere(context, statement, where);
	}

	protected override void VisitFrom(ExpressionVisitorContext context, UpdateStatement statement, ICollection<ISource> sources)
	{
		/*
		 * 由于 MySQL 的 UPDATE 语句不支持 FROM 子句，故不输出任何内容，且不调用基类同名方法以避免生成错误的语句。
		 */
	}
	#endregion
}
