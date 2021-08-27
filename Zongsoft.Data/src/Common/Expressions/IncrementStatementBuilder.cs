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
	public class IncrementStatementBuilder : IStatementBuilder<DataIncrementContext>
	{
		public IEnumerable<IStatementBase> Build(DataIncrementContext context)
		{
			var statement = new UpdateStatement(context.Entity);

			var source = statement.From(context.Member, context.Aliaser, null, out var property);
			var field = source.CreateField(property);
			var value = context.Interval > 0 ?
			            Expression.Add(field, Expression.Constant(context.Interval)) :
			            Expression.Subtract(field, Expression.Constant(-context.Interval));

			//添加修改字段
			statement.Fields.Add(new FieldValue(field, value));

			//构建WHERE子句
			statement.Where = statement.Where(context.Validate(), context.Aliaser);

			if(context.Source.Features.Support(Feature.Updation.Outputting))
			{
				statement.Returning = new ReturningClause();
				statement.Returning.Append(field, ReturningClause.ReturningMode.Inserted);
			}
			else
			{
				var slave = new SelectStatement();

				foreach(var from in statement.From)
					slave.From.Add(from);

				slave.Where = statement.Where;
				slave.Select.Members.Add(field);

				//注：由于从属语句的WHERE子句只是简单的指向父语句的WHERE子句，
				//因此必须手动将父语句的参数依次添加到从属语句中。
				foreach(var parameter in statement.Parameters)
				{
					slave.Parameters.Add(parameter);
				}

				statement.Slaves.Add(slave);
			}

			yield return statement;
		}
	}
}
