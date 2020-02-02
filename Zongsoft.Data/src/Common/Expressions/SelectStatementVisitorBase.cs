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
	public class SelectStatementVisitorBase<TStatement> : StatementVisitorBase<TStatement> where TStatement : SelectStatementBase
	{
		#region 构造函数
		protected SelectStatementVisitorBase()
		{
		}
		#endregion

		#region 重写方法
		protected override void OnVisit(IExpressionVisitor visitor, TStatement statement)
		{
			if(statement.Select == null || statement.Select.Members.Count == 0)
			{
				if(string.IsNullOrEmpty(statement.Alias))
					throw new DataException("Missing select-members clause in the select statement.");
				else
					throw new DataException($"Missing select-members clause in the '{statement.Alias}' select statement.");
			}

			this.VisitSelect(visitor, statement.Select);
			this.VisitFrom(visitor, statement.From);
			this.VisitWhere(visitor, statement.Where);
		}
		#endregion

		#region 虚拟方法
		protected virtual void VisitSelect(IExpressionVisitor visitor, SelectClause clause)
		{
			if(visitor.Output.Length > 0)
				visitor.Output.AppendLine();

			visitor.Output.Append("SELECT ");

			if(clause.IsDistinct)
				visitor.Output.Append("DISTINCT ");

			int index = 0;

			foreach(var member in clause.Members)
			{
				if(index++ > 0)
					visitor.Output.AppendLine(",");

				visitor.Visit(member);
			}
		}

		protected virtual void VisitFrom(IExpressionVisitor visitor, ICollection<ISource> sources)
		{
			visitor.VisitFrom(sources, (v, j) => this.VisitJoin(v, j));
		}

		protected virtual void VisitJoin(IExpressionVisitor visitor, JoinClause joining)
		{
			visitor.VisitJoin(joining);
		}

		protected virtual void VisitWhere(IExpressionVisitor visitor, IExpression where)
		{
			visitor.VisitWhere(where);
		}
		#endregion
	}
}
