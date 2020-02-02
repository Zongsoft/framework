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
	public class DeleteStatementVisitor : StatementVisitorBase<DeleteStatement>
	{
		#region 构造函数
		protected DeleteStatementVisitor()
		{
		}
		#endregion

		#region 重写方法
		protected override void OnVisit(IExpressionVisitor visitor, DeleteStatement statement)
		{
			if(statement.Returning != null && statement.Returning.Table != null)
				visitor.Visit(statement.Returning.Table);

			visitor.Output.Append("DELETE ");

			this.VisitTables(visitor, statement, statement.Tables);
			this.VisitFrom(visitor, statement, statement.From);
			this.VisitWhere(visitor, statement, statement.Where);

			visitor.Output.AppendLine(";");
		}
		#endregion

		#region 虚拟方法
		protected virtual void VisitTables(IExpressionVisitor visitor, DeleteStatement statement, IList<TableIdentifier> tables)
		{
			for(int i = 0; i < tables.Count; i++)
			{
				if(i > 0)
					visitor.Output.Append(",");

				if(string.IsNullOrEmpty(tables[i].Alias))
					visitor.Output.Append(tables[i].Name);
				else
					visitor.Output.Append(tables[i].Alias);
			}
		}

		protected virtual void VisitFrom(IExpressionVisitor visitor, DeleteStatement statement, ICollection<ISource> sources)
		{
			visitor.VisitFrom(sources, (v, j) => this.VisitJoin(v, statement, j));
		}

		protected virtual void VisitJoin(IExpressionVisitor visitor, DeleteStatement statement, JoinClause joining)
		{
			visitor.VisitJoin(joining);
		}

		protected virtual void VisitWhere(IExpressionVisitor visitor, DeleteStatement statement, IExpression where)
		{
			visitor.VisitWhere(where);
		}
		#endregion
	}
}
