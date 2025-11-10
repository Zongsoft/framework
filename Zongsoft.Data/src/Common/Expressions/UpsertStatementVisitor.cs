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

namespace Zongsoft.Data.Common.Expressions;

public class UpsertStatementVisitor : StatementVisitorBase<UpsertStatement>
{
	#region 构造函数
	protected UpsertStatementVisitor() { }
	#endregion

	#region 重写方法
	protected override void OnVisit(ExpressionVisitorContext context, UpsertStatement statement)
	{
		const string SOURCE_ALIAS = "SRC";

		if(statement.Fields == null || statement.Fields.Count == 0)
			throw new DataException("Missing required fields in the upsert statment.");

		if(statement.With != null && statement.With.Count > 0)
			this.VisitWith(context, statement.With);

		context.Write("MERGE INTO ");
		context.Visit(statement.Table);
		context.WriteLine(" USING (SELECT ");

		for(int i = 0; i < statement.Values.Count; i++)
		{
			if(i > 0)
				context.Write(",");

			context.Visit(statement.Values[i]);
		}

		context.WriteLine(") AS " + SOURCE_ALIAS + " (");

		for(int i = 0; i < statement.Fields.Count; i++)
		{
			if(i > 0)
				context.Write(",");

			context.Write(statement.Fields[i].Name);
		}

		context.WriteLine(") ON");

		for(int i = 0; i < statement.Entity.Key.Length; i++)
		{
			var field = Metadata.DataEntityPropertyExtension.GetFieldName(statement.Entity.Key[i], out _);

			if(i > 0)
				context.Write(" AND ");

			if(string.IsNullOrEmpty(statement.Table.Alias))
				context.Write($"{field}={SOURCE_ALIAS}.{field}");
			else
				context.Write($"{statement.Table.Alias}.{field}={SOURCE_ALIAS}.{field}");
		}

		if(statement.Updation.Count > 0)
		{
			context.WriteLine();
			context.Write("WHEN MATCHED");

			if(statement.Where != null)
			{
				context.Write(" AND ");
				context.Visit(statement.Where);
			}

			context.WriteLine(" THEN");
			context.Write("\tUPDATE SET ");

			int index = 0;

			foreach(var item in statement.Updation)
			{
				if(index++ > 0)
					context.Write(",");

				context.Visit(item.Field);
				context.Write("=");

				var parenthesisRequired = item.Value is IStatementBase;

				if(parenthesisRequired)
					context.Write("(");

				context.Visit(item.Value);

				if(parenthesisRequired)
					context.Write(")");
			}
		}

		context.WriteLine();
		context.WriteLine("WHEN NOT MATCHED THEN");
		context.Write("\tINSERT (");

		for(int i = 0; i < statement.Fields.Count; i++)
		{
			if(i > 0)
				context.Write(",");

			context.Write(context.Dialect.GetIdentifier(statement.Fields[i]));
		}

		context.Write(") VALUES (");

		for(int i = 0; i < statement.Fields.Count; i++)
		{
			if(i > 0)
				context.Write(",");

			context.Write(SOURCE_ALIAS + "." + statement.Fields[i].Name);
		}

		context.Write(")");
	}

	protected override void OnVisiting(ExpressionVisitorContext context, UpsertStatement statement)
	{
		if(statement.Returning != null && statement.Returning.Table != null)
			context.Visit(statement.Returning.Table);
	}

	protected override void OnVisited(ExpressionVisitorContext context, UpsertStatement statement)
	{
		if(statement.Returning != null)
			this.VisitReturning(context, statement.Returning);

		context.WriteLine(";");
	}
	#endregion

	#region 虚拟方法
	protected virtual void VisitWith(ExpressionVisitorContext context, CommonTableExpressionCollection expressions)
	{
		context.Write("WITH ");
		context.Visit(expressions);
	}

	protected virtual void VisitReturning(ExpressionVisitorContext context, ReturningClause clause)
	{
		context.Write(" RETURNING ");

		if(clause.Members == null || clause.Members.Count == 0)
			context.Write("*");
		else
		{
			int index = 0;

			foreach(var member in clause.Members)
			{
				if(index++ > 0)
					context.Write(",");

				context.Visit(member.Field);
			}
		}

		if(clause.Table != null)
		{
			context.Write(" INTO ");
			context.Write(context.Dialect.GetIdentifier(clause.Table.Identifier()));
		}
	}
	#endregion
}
