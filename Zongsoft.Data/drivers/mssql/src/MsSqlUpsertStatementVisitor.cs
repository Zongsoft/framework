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
 * This file is part of Zongsoft.Data.MsSql library.
 *
 * The Zongsoft.Data.MsSql is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data.MsSql is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data.MsSql library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

using Zongsoft.Data.Common;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.MsSql;

public class MsSqlUpsertStatementVisitor : UpsertStatementVisitor
{
	#region 单例字段
	public static readonly MsSqlUpsertStatementVisitor Instance = new();
	#endregion

	#region 构造函数
	private MsSqlUpsertStatementVisitor() { }
	#endregion

	#region 重写方法
	protected override void OnVisit(ExpressionVisitorContext context, UpsertStatement statement)
	{
		const string TARGET_ALIAS = "T";
		const string SOURCE_ALIAS = "SRC";

		if(statement.Fields == null || statement.Fields.Count == 0)
			throw new DataException(Properties.Resources.ResourceManager.GetString("UpsertStatement.MissingFields.Message"));

		context.Write("MERGE INTO ");
		context.Visit(statement.Table);
		context.WriteLine(" AS " + TARGET_ALIAS);
		context.WriteLine("USING (VALUES");

		for(int i = 0; i < statement.Values.Count; i++)
		{
			if(i > 0)
				context.Write(",");

			if(i % statement.Fields.Count == 0)
				context.Write("(");

			context.Visit(statement.Values[i]);

			if(i % statement.Fields.Count == statement.Fields.Count - 1)
				context.Write(")");
		}

		context.WriteLine(") AS " + SOURCE_ALIAS + " (");

		for(int i = 0; i < statement.Fields.Count; i++)
		{
			if(i > 0)
				context.Write(",");

			context.Write("[" + statement.Fields[i].Name + "]");
		}

		context.WriteLine(") ON");

		for(int i = 0; i < statement.Entity.Key.Length; i++)
		{
			var field = Metadata.DataEntityPropertyExtension.GetFieldName(statement.Entity.Key[i], out _);

			if(i > 0)
				context.Write(" AND ");

			context.Write($"{TARGET_ALIAS}.[{field}]={SOURCE_ALIAS}.[{field}]");
		}

		if(statement.Updation.Count > 0)
		{
			context.WriteLine();
			context.Write("WHEN MATCHED");

			if(statement.Where != null)
			{
				context.WriteLine(" AND ");
				context.Visit(statement.Where);
			}

			context.WriteLine(" THEN");
			context.Write("\tUPDATE SET ");

			int index = 0;

			foreach(var item in statement.Updation)
			{
				if(index++ > 0)
					context.Write(",");

				context.Write(context.Dialect.GetIdentifier(item.Field));
				context.Write("=");

				var parenthesisRequired = item.Value is IStatementBase;

				if(parenthesisRequired)
					context.Write("(");

				if(item.Value != null)
					context.Visit(item.Value);
				else
				{
					context.Write(SOURCE_ALIAS + ".");
					context.Write(context.Dialect.GetIdentifier(item.Field.Name));
				}

				if(parenthesisRequired)
					context.Write(")");
			}
		}
		else
		{
			var index = 0;

			foreach(var field in statement.Fields)
			{
				if(field.Token.Property is Metadata.IDataEntitySimplexProperty simplex && simplex.Sequence != null)
					continue;

				if(index++ == 0)
				{
					context.WriteLine();
					context.WriteLine("WHEN MATCHED THEN");
					context.Write("\tUPDATE SET ");
				}
				else
					context.Write(",");

				var fieldName = context.Dialect.GetIdentifier(field.Name);
				context.Write($"{fieldName}={SOURCE_ALIAS}.{fieldName}");
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

			context.Write(SOURCE_ALIAS + ".[" + statement.Fields[i].Name + "]");
		}

		context.Write(")");

		//生成OUTPUT(RETURNING)子句
		if(statement.Returning != null)
			this.VisitReturning(context, statement.Returning);

		context.WriteLine(";");
	}

	protected override void OnVisited(ExpressionVisitorContext context, UpsertStatement statement) => context.WriteLine(";");
	protected override void OnVisiteReturning(ExpressionVisitorContext context, ReturningClause clause) => context.WriteLine(" OUTPUT");
	#endregion
}
