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
 * This file is part of Zongsoft.Data.MySql library.
 *
 * The Zongsoft.Data.MySql is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data.MySql is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data.MySql library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;

using Zongsoft.Data.Common;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.MySql
{
	public class MySqlUpsertStatementVisitor : UpsertStatementVisitor
	{
		#region 单例字段
		public static readonly MySqlUpsertStatementVisitor Instance = new MySqlUpsertStatementVisitor();
		#endregion

		#region 构造函数
		private MySqlUpsertStatementVisitor()
		{
		}
		#endregion

		#region 重写方法
		protected override void OnVisit(IExpressionVisitor visitor, UpsertStatement statement)
		{
			if(statement.Fields == null || statement.Fields.Count == 0)
				throw new DataException("Missing required fields in the upsert statment.");

			var index = 0;

			visitor.Output.Append("INSERT INTO ");
			visitor.Visit(statement.Table);
			visitor.Output.Append(" (");

			foreach(var field in statement.Fields)
			{
				if(index++ > 0)
					visitor.Output.Append(",");

				visitor.Visit(field);
			}

			index = 0;
			visitor.Output.AppendLine(") VALUES ");

			foreach(var value in statement.Values)
			{
				if(index++ > 0)
					visitor.Output.Append(",");

				if(index % statement.Fields.Count == 1)
					visitor.Output.Append("(");

				visitor.Visit(value);

				if(index % statement.Fields.Count == 0)
					visitor.Output.Append(")");
			}

			if(statement.Updation.Count > 0)
			{
				index = 0;
				visitor.Output.AppendLine(" ON DUPLICATE KEY UPDATE ");

				foreach(var item in statement.Updation)
				{
					if(index++ > 0)
						visitor.Output.Append(",");

					//visitor.Visit(item.Field);
					visitor.Output.Append(visitor.Dialect.GetIdentifier(item.Field));
					visitor.Output.Append("=");
					visitor.Visit(item.Value);
				}
			}

			visitor.Output.AppendLine(";");
		}
		#endregion
	}
}
