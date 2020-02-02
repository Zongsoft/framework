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
	public class SelectStatementVisitor : SelectStatementVisitorBase<SelectStatement>
	{
		#region 构造函数
		protected SelectStatementVisitor()
		{
		}
		#endregion

		#region 重写方法
		protected override void OnVisit(IExpressionVisitor visitor, SelectStatement statement)
		{
			if(statement.Select == null || statement.Select.Members.Count == 0)
			{
				if(string.IsNullOrEmpty(statement.Alias))
					throw new DataException("Missing select-members clause in the select statement.");
				else
					throw new DataException($"Missing select-members clause in the '{statement.Alias}' select statement.");
			}

			this.VisitSelect(visitor, statement.Select);
			this.VisitInto(visitor, statement.Into);
			this.VisitFrom(visitor, statement.From);
			this.VisitWhere(visitor, statement.Where);
			this.VisitGroupBy(visitor, statement.GroupBy);
			this.VisitOrderBy(visitor, statement.OrderBy);
		}

		protected override void OnVisiting(IExpressionVisitor visitor, SelectStatement statement)
		{
			if(!string.IsNullOrEmpty(statement.Alias))
				visitor.Output.AppendLine($"/* {statement.Alias} */");

			//调用基类同名方法
			base.OnVisiting(visitor, statement);
		}

		protected override void OnVisited(IExpressionVisitor visitor, SelectStatement statement)
		{
			if(visitor.Depth == 0)
				visitor.Output.AppendLine(";");

			if(statement.Paging != null && statement.Paging.PageSize > 0)
			{
				visitor.Output.AppendLine("SELECT COUNT(*)");

				this.VisitFrom(visitor, statement.From);
				this.VisitWhere(visitor, statement.Where);
			}

			//调用基类同名方法
			base.OnVisited(visitor, statement);
		}
		#endregion

		#region 虚拟方法
		protected virtual void VisitInto(IExpressionVisitor visitor, IIdentifier into)
		{
			if(into == null)
				return;

			visitor.Output.Append(" INTO ");
			visitor.Visit(into);
		}

		protected virtual void VisitGroupBy(IExpressionVisitor visitor, GroupByClause clause)
		{
			if(clause == null || clause.Keys.Count == 0)
				return;

			if(visitor.Output.Length > 0)
				visitor.Output.AppendLine();

			visitor.Output.Append("GROUP BY ");

			int index = 0;

			foreach(var key in clause.Keys)
			{
				if(index++ > 0)
					visitor.Output.Append(",");

				visitor.Visit(key);
			}

			if(clause.Having != null)
			{
				visitor.Output.AppendLine();
				visitor.Output.Append("HAVING ");
				visitor.Visit(clause.Having);
			}
		}

		protected virtual void VisitOrderBy(IExpressionVisitor visitor, OrderByClause clause)
		{
			if(clause == null || clause.Members.Count == 0)
				return;

			if(visitor.Output.Length > 0)
				visitor.Output.AppendLine();

			visitor.Output.Append("ORDER BY ");

			int index = 0;

			foreach(var member in clause.Members)
			{
				if(index++ > 0)
					visitor.Output.Append(",");

				visitor.Visit(member.Field);

				if(member.Mode == SortingMode.Descending)
					visitor.Output.Append(" DESC");
			}
		}
		#endregion
	}
}
