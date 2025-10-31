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
using System.Collections.Generic;

using Zongsoft.Data.Common;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.MsSql
{
	public class MsSqlDeleteStatementVisitor : DeleteStatementVisitor
	{
		#region 单例字段
		public static readonly MsSqlDeleteStatementVisitor Instance = new MsSqlDeleteStatementVisitor();
		#endregion

		#region 构造函数
		private MsSqlDeleteStatementVisitor() { }
		#endregion

		#region 重写方法
		protected override void VisitFrom(ExpressionVisitorContext context, DeleteStatement statement, ICollection<ISource> sources)
		{
			//生成OUTPUT(RETURNING)子句
			this.VisitOutput(context, statement.Returning);

			//调用基类同名方法
			base.VisitFrom(context, statement, sources);
		}
		#endregion

		#region 私有方法
		private void VisitOutput(ExpressionVisitorContext context, ReturningClause returning)
		{
			if(returning == null)
				return;

			context.WriteLine();
			context.Write("OUTPUT ");

			if(returning.Members == null || returning.Members.Count == 0)
				context.Write("DELETED.*");
			else
			{
				int index = 0;

				foreach(var member in returning.Members)
				{
					if(index++ > 0)
						context.Write(",");

					context.Write((member.Mode == ReturningClause.ReturningMode.Deleted ? "DELETED." : "INSERTED.") + member.Field.Name);
				}
			}

			if(returning.Table != null)
			{
				context.Write(" INTO ");
				context.Write(context.Dialect.GetIdentifier(returning.Table.Identifier()));
			}
		}
		#endregion
	}
}
