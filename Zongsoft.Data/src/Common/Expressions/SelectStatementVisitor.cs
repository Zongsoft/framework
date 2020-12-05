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

namespace Zongsoft.Data.Common.Expressions
{
	public class SelectStatementVisitor : SelectStatementVisitorBase<SelectStatement>
	{
		#region 构造函数
		protected SelectStatementVisitor() { }
		#endregion

		#region 重写方法
		protected override void OnVisit(ExpressionVisitorContext context, SelectStatement statement)
		{
			if(statement.Select == null || statement.Select.Members.Count == 0)
			{
				if(string.IsNullOrEmpty(statement.Alias))
					throw new DataException("Missing select-members clause in the select statement.");
				else
					throw new DataException($"Missing select-members clause in the '{statement.Alias}' select statement.");
			}

			this.VisitSelect(context, statement.Select);
			this.VisitInto(context, statement.Into);
			this.VisitFrom(context, statement.From);
			this.VisitWhere(context, statement.Where);
			this.VisitGroupBy(context, statement.GroupBy);
			this.VisitOrderBy(context, statement.OrderBy);
		}

		protected override void OnVisiting(ExpressionVisitorContext context, SelectStatement statement)
		{
			if(!string.IsNullOrEmpty(statement.Alias))
				context.WriteLine($"/* {statement.Alias} */");

			//调用基类同名方法
			base.OnVisiting(context, statement);
		}

		protected override void OnVisited(ExpressionVisitorContext context, SelectStatement statement)
		{
			if(context.Depth == 0)
				context.WriteLine(";");

			if(statement.Paging != null && statement.Paging.Enabled)
			{
				context.WriteLine("SELECT COUNT(*)");

				this.VisitFrom(context, statement.From);
				this.VisitWhere(context, statement.Where);
			}

			//调用基类同名方法
			base.OnVisited(context, statement);
		}
		#endregion

		#region 虚拟方法
		protected virtual void VisitInto(ExpressionVisitorContext context, IIdentifier into)
		{
			if(into == null)
				return;

			context.Write(" INTO ");
			context.Visit(into);
		}

		protected virtual void VisitGroupBy(ExpressionVisitorContext context, GroupByClause clause)
		{
			if(clause == null || clause.Keys.Count == 0)
				return;

			if(context.Output.Length > 0)
				context.WriteLine();

			context.Write("GROUP BY ");

			int index = 0;

			foreach(var key in clause.Keys)
			{
				if(index++ > 0)
					context.Write(",");

				context.Visit(key);
			}

			if(clause.Having != null)
			{
				context.WriteLine();
				context.Write("HAVING ");
				context.Visit(clause.Having);
			}
		}

		protected virtual void VisitOrderBy(ExpressionVisitorContext context, OrderByClause clause)
		{
			if(clause == null || clause.Members.Count == 0)
				return;

			if(context.Output.Length > 0)
				context.WriteLine();

			context.Write("ORDER BY ");

			int index = 0;

			foreach(var member in clause.Members)
			{
				if(index++ > 0)
					context.Write(",");

				context.Visit(member.Field);

				if(member.Mode == SortingMode.Descending)
					context.Write(" DESC");
			}
		}
		#endregion
	}
}
