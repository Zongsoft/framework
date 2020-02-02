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
	public class InsertStatementVisitor : StatementVisitorBase<InsertStatement>
	{
		#region 构造函数
		protected InsertStatementVisitor()
		{
		}
		#endregion

		#region 重写方法
		protected override void OnVisit(IExpressionVisitor visitor, InsertStatement statement)
		{
			if(statement.Fields == null || statement.Fields.Count == 0)
				throw new DataException("Missing required fields in the insert statment.");

			if(statement.Returning != null && statement.Returning.Table != null)
				visitor.Visit(statement.Returning.Table);

			visitor.Output.Append("INSERT INTO ");
			visitor.Visit(statement.Table);

			this.VisitFields(visitor, statement, statement.Fields);
			this.VisitValues(visitor, statement, statement.Values, statement.Fields.Count);

			visitor.Output.AppendLine(";");
		}
		#endregion

		#region 虚拟方法
		protected virtual void VisitFields(IExpressionVisitor visitor, InsertStatement statement, ICollection<FieldIdentifier> fields)
		{
			int index = 0;

			visitor.Output.Append(" (");

			foreach(var field in fields)
			{
				if(index++ > 0)
					visitor.Output.Append(",");

				visitor.Visit(field);
			}

			visitor.Output.Append(")");
		}

		protected virtual void VisitValues(IExpressionVisitor visitor, InsertStatement statement, ICollection<IExpression> values, int rounds)
		{
			int index = 0;

			visitor.Output.AppendLine(" VALUES");

			foreach(var value in values)
			{
				if(index > 0)
					visitor.Output.Append(",");

				if(index % rounds == 0)
					visitor.Output.Append("(");

				visitor.Visit(value);

				if(++index % rounds == 0)
					visitor.Output.Append(")");
			}
		}
		#endregion
	}
}
