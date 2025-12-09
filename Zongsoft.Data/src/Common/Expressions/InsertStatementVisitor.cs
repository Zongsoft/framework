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

namespace Zongsoft.Data.Common.Expressions;

public class InsertStatementVisitor : StatementVisitorBase<InsertStatement>
{
	#region 构造函数
	protected InsertStatementVisitor() { }
	#endregion

	#region 重写方法
	protected override void OnVisit(ExpressionVisitorContext context, InsertStatement statement)
	{
		if(statement.Fields == null || statement.Fields.Count == 0)
			throw new DataException("Missing required fields in the insert statment.");

		if(statement.With != null && statement.With.Count > 0)
			this.VisitWith(context, statement.With);

		this.VisitInsert(context, statement);
		context.Visit(statement.Table);
		this.VisitFields(context, statement, statement.Fields);
		this.VisitValues(context, statement, statement.Values, statement.Fields.Count);
	}

	protected override void OnVisiting(ExpressionVisitorContext context, InsertStatement statement)
	{
		if(statement.Returning != null && statement.Returning.Table != null)
			context.Visit(statement.Returning.Table);
	}

	protected override void OnVisited(ExpressionVisitorContext context, InsertStatement statement)
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

	protected virtual void VisitInsert(ExpressionVisitorContext context, InsertStatement statement)
	{
		context.Write("INSERT INTO ");
	}

	protected virtual void VisitFields(ExpressionVisitorContext context, InsertStatement statement, ICollection<FieldIdentifier> fields)
	{
		int index = 0;

		context.Write(" (");

		foreach(var field in fields)
		{
			if(index++ > 0)
				context.Write(",");

			context.Visit(field);
		}

		context.Write(")");
	}

	protected virtual void VisitValues(ExpressionVisitorContext context, InsertStatement statement, ICollection<IExpression> values, int rounds)
	{
		int index = 0;

		context.WriteLine(" VALUES");

		foreach(var value in values)
		{
			if(index > 0)
				context.Write(",");

			if(index % rounds == 0)
				context.Write("(");

			var parenthesisRequired = value is IStatementBase;

			if(parenthesisRequired)
				context.Write("(");

			context.Visit(value);

			if(parenthesisRequired)
				context.Write(")");

			if(++index % rounds == 0)
				context.Write(")");
		}
	}

	protected virtual void VisitReturning(ExpressionVisitorContext context, ReturningClause clause)
	{
		if(clause.Members == null || clause.Members.Count == 0)
			return;

		this.OnVisiteReturning(context, clause);

		int index = 0;
		foreach(var member in clause.Members)
		{
			if(index++ > 0)
				context.Write(",");

			var qualified = context.Dialect.GetIdentifier(member.Kind);
			if(!string.IsNullOrEmpty(qualified))
				context.Write($"{qualified}.");

			context.Visit(member.Field);
		}

		if(clause.Table != null)
		{
			context.Write(" INTO ");
			context.Write(context.Dialect.GetIdentifier(clause.Table.Identifier()));
		}
	}

	protected virtual void OnVisiteReturning(ExpressionVisitorContext context, ReturningClause clause) => context.Write(" RETURNING ");
	#endregion
}
