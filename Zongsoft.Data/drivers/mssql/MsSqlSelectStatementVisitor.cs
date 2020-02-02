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
using System.Collections.Generic;

using Zongsoft.Data.Common;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.MsSql
{
	public class MsSqlSelectStatementVisitor : SelectStatementVisitor
	{
		#region 单例字段
		public static readonly MsSqlSelectStatementVisitor Instance = new MsSqlSelectStatementVisitor();
		#endregion

		#region 构造函数
		private MsSqlSelectStatementVisitor()
		{
		}
		#endregion

		#region 重写方法
		protected override void OnVisit(IExpressionVisitor visitor, SelectStatement statement)
		{
			//由于分页子句必须依赖于排序(OrderBy)子句，所以在没有指定排序子句的情况下默认以主键进行排序
			if(statement.Paging != null && statement.Paging.PageSize > 0 && statement.OrderBy == null && statement.Table != null)
			{
				statement.OrderBy = new OrderByClause();

				foreach(var key in statement.Table.Entity.Key)
					statement.OrderBy.Add(statement.Table.CreateField(key));
			}

			//调用基类同名方法
			base.OnVisit(visitor, statement);

			if(statement.Paging != null && statement.Paging.PageSize > 0 && statement.OrderBy != null)
				this.VisitPaging(visitor, statement.Paging);
		}
		#endregion

		#region 虚拟方法
		protected virtual void VisitPaging(IExpressionVisitor visitor, Paging paging)
		{
			if(visitor.Output.Length > 0)
				visitor.Output.AppendLine();

			visitor.Output.Append("OFFSET " + ((paging.PageIndex - 1) * paging.PageSize).ToString() + " ROWS ");
			visitor.Output.Append("FETCH NEXT " + paging.PageSize.ToString() + " ROWS ONLY");
		}
		#endregion
	}
}
