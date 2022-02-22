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
	public class UpdateStatementVisitor : StatementVisitorBase<UpdateStatement>
	{
		#region 构造函数
		protected UpdateStatementVisitor() { }
		#endregion

		#region 重写方法
		protected override void OnVisit(ExpressionVisitorContext context, UpdateStatement statement)
		{
			if(statement.Tables == null || statement.Tables.Count == 0)
				throw new DataException("Missing required tables in the update statement.");

			if(statement.Fields == null || statement.Fields.Count == 0)
				throw new DataException("Missing required fields in the update statment.");

			if(statement.Returning != null && statement.Returning.Table != null)
				context.Visit(statement.Returning.Table);

			this.VisitUpdate(context, statement);
			this.VisitTables(context, statement, statement.Tables);

			context.WriteLine(" SET");
			this.VisitFields(context, statement, statement.Fields);

			this.VisitFrom(context, statement, statement.From);
			this.VisitWhere(context, statement, statement.Where);

			context.WriteLine(";");
		}
		#endregion

		#region 虚拟方法
		protected virtual void VisitUpdate(ExpressionVisitorContext context, UpdateStatement statement)
		{
			context.Write("UPDATE ");
		}

		protected virtual void VisitTables(ExpressionVisitorContext context, UpdateStatement statement, IList<TableIdentifier> tables)
		{
			for(int i = 0; i < tables.Count; i++)
			{
				if(i > 0)
					context.Write(",");

				context.Visit(tables[i]);
			}
		}

		protected virtual void VisitFields(ExpressionVisitorContext context, UpdateStatement statement, ICollection<FieldValue> fields)
		{
			var index = 0;

			foreach(var field in fields)
			{
				if(index++ > 0)
					context.WriteLine(",");

				context.Visit(field.Field);
				context.Write("=");

				var parenthesisRequired = field.Value is IStatementBase;

				if(parenthesisRequired)
					context.Write("(");

				context.Visit(field.Value);

				if(parenthesisRequired)
					context.Write(")");
			}
		}

		protected virtual void VisitFrom(ExpressionVisitorContext context, UpdateStatement statement, ICollection<ISource> sources)
		{
			context.VisitFrom(sources, (ctx, join) => this.VisitJoin(ctx, statement, join));
		}

		protected virtual void VisitJoin(ExpressionVisitorContext context, UpdateStatement statement, JoinClause joining)
		{
			context.VisitJoin(joining);
		}

		protected virtual void VisitWhere(ExpressionVisitorContext context, UpdateStatement statement, IExpression where)
		{
			context.VisitWhere(where);
		}
		#endregion
	}
}
