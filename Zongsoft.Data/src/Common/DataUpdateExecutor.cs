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
using System.Linq;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.Common;

public class DataUpdateExecutor : DataMutateExecutor<UpdateStatement>
{
	protected override async ValueTask<bool> OnExecuteAsync(IDataMutateContext context, UpdateStatement statement, CancellationToken cancellation)
	{
		if(statement.Links.Count == 0)
			return await base.OnExecuteAsync(context, statement, cancellation);

		try
		{
			if(!await base.OnExecuteAsync(context, statement, cancellation))
				return false;

			await this.ExecuteSlavesAsync(context, statement, cancellation);
			return false;
		}
		finally
		{
			foreach(var link in statement.Links)
				link.Keys.Clear();
		}
	}

	private async ValueTask ExecuteSlavesAsync(IDataMutateContext context, UpdateStatement statement, CancellationToken cancellation)
	{
		if(!statement.HasSlaves)
			return;

		var data = context.Data;
		if(statement.Schema != null)
			context.Data = statement.Schema.Token.GetValue(data);

		try
		{
			foreach(var slave in statement.Slaves)
			{
				var link = statement.Links.Find(link => link.Statement == slave);
				if(link == null)
				{
					await context.Provider.Executor.ExecuteAsync(context, slave, cancellation);
					continue;
				}

				var update = link.Statement;
				var where = update.Where;
				var parameters = update.Parameters.ToArray();
				var size = Math.Min(128, (900 - parameters.Length) / link.Fields.Count);

				if(size <= 0)
					throw new NotSupportedException();

				try
				{
					var continued = false;
					foreach(var keys in link.Keys.Chunk(size))
					{
						cancellation.ThrowIfCancellationRequested();
						update.Parameters.Clear();
						foreach(var parameter in parameters)
							update.Parameters.Add(parameter);

						var criteria = ConditionExpression.Or();
						foreach(var key in keys)
						{
							var condition = ConditionExpression.And();
							for(int i = 0; i < key.Length; i++)
							{
								var field = link.Fields[i].Value;
								var parameter = Expression.Parameter(field, key[i]);
								update.Parameters.Add(parameter);
								condition.Add(Expression.Equal(field, parameter));
							}

							criteria.Add(condition);
						}

						update.Where = ConditionExpression.And(where, criteria);
						continued |= await base.OnExecuteAsync(context, update, cancellation);
					}

					if(continued)
						await this.ExecuteSlavesAsync(context, update, cancellation);
				}
				finally
				{
					update.Where = where;
					update.Parameters.Clear();
					foreach(var parameter in parameters)
						update.Parameters.Add(parameter);

					foreach(var child in update.Links)
						child.Keys.Clear();
				}
			}
		}
		finally
		{
			context.Data = data;
		}
	}

	protected override async ValueTask<bool> OnMutatedAsync(IDataMutateContext context, UpdateStatement statement, DbDataReader reader, CancellationToken cancellation)
	{
		var count = 0;
		while(await reader.ReadAsync(cancellation))
		{
			cancellation.ThrowIfCancellationRequested();
			count++;
			this.Populate(context, statement, reader);

			foreach(var link in statement.Links)
			{
				var keys = new object[link.Fields.Count];
				for(int i = 0; i < keys.Length; i++)
					keys[i] = reader.GetValue(reader.GetOrdinal(link.Fields[i].Key.Alias));

				if(!Array.Exists(keys, key => key == null || key == DBNull.Value))
					link.Keys.Add(keys);
			}
		}

		if(reader.RecordsAffected > 0)
			context.Count += reader.RecordsAffected;

		return count > 0;
	}
}
