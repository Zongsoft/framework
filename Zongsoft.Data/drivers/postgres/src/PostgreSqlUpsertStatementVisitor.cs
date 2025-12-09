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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Data.PostgreSql library.
 *
 * The Zongsoft.Data.PostgreSql is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data.PostgreSql is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data.PostgreSql library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

using Zongsoft.Data.Metadata;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.PostgreSql;

public class PostgreSqlUpsertStatementVisitor : UpsertStatementVisitor
{
	#region 单例字段
	public static readonly PostgreSqlUpsertStatementVisitor Instance = new();
	#endregion

	#region 构造函数
	private PostgreSqlUpsertStatementVisitor() { }
	#endregion

	#region 重写方法
	protected override void OnVisit(ExpressionVisitorContext context, UpsertStatement statement)
	{
		if(statement.Fields == null || statement.Fields.Count == 0)
			throw new DataException("Missing required fields in the upsert statment.");

		var index = 0;

		context.Write("INSERT INTO ");
		context.Write(context.Dialect.GetIdentifier(statement.Table));
		context.Write(" (");

		foreach(var field in statement.Fields)
		{
			if(index++ > 0)
				context.Write(",");

			context.Write(context.Dialect.GetIdentifier(field.Name));
		}

		index = 0;
		context.WriteLine(") VALUES ");

		foreach(var value in statement.Values)
		{
			if(index++ > 0)
				context.Write(",");

			if(index % statement.Fields.Count == 1)
				context.Write("(");

			var parenthesisRequired = value is IStatementBase;

			if(parenthesisRequired)
				context.Write("(");

			context.Visit(value);

			if(parenthesisRequired)
				context.Write(")");

			if(index % statement.Fields.Count == 0)
				context.Write(")");
		}

		context.Write(" ON CONFLICT ");

		if(statement.Options.ConstraintIgnored || !statement.Table.Entity.HasKey)
		{
			context.WriteLine("DO NOTHING;");
			return;
		}

		context.Write("(");

		for(int i = 0; i < statement.Table.Entity.Key.Length; i++)
		{
			if(i > 0)
				context.Write(',');

			context.Write(context.Dialect.GetIdentifier(statement.Table.Entity.Key[i].GetFieldName()));
		}

		context.WriteLine(") DO UPDATE SET");

		index = 0;
		if(statement.Updation.Count > 0)
		{
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
					context.Write("EXCLUDED.");
					context.Write(context.Dialect.GetIdentifier(item.Field.Name));
				}

				if(parenthesisRequired)
					context.Write(")");
			}
		}
		else
		{
			foreach(var field in statement.Fields)
			{
				//忽略修改序列字段
				if(field.Token.Property is Metadata.IDataEntitySimplexProperty simplex && simplex.Sequence != null)
					continue;

				if(index++ > 0)
					context.Write(",");

				var fieldName = context.Dialect.GetIdentifier(field.Name);
				context.Write($"{fieldName}=EXCLUDED.{fieldName}");
			}
		}
	}
	#endregion
}
