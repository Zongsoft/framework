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
 * Copyright (C) 2010-2022 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Data.SQLite library.
 *
 * The Zongsoft.Data.SQLite is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data.SQLite is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data.SQLite library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Text;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.Data.Sqlite;

using Zongsoft.Data.Common;
using Zongsoft.Data.Metadata;

namespace Zongsoft.Data.SQLite;

public class SQLiteImporter : DataImporterBase
{
	#region 公共方法
	protected override void OnImport(DataImportContext context, MemberCollection members)
	{
		var command = GetCommand(context, members);

		if(command == null || command.Connection == null)
			return;

		try
		{
			command.Connection.Open();

			foreach(var item in context.Data)
			{
				var target = item;

				for(int i = 0; i < members.Count; i++)
				{
					command.Parameters[i].Value = members[i].GetValue(ref target);
				}

				command.ExecuteNonQuery();
			}

			command.Transaction.Commit();
		}
		catch
		{
			if(command.Transaction != null)
				command.Transaction.Rollback();
		}
		finally
		{
			command.Connection.Dispose();
		}
	}

	protected override async ValueTask OnImportAsync(DataImportContext context, MemberCollection members, CancellationToken cancellation = default)
	{
		var command = GetCommand(context, members);

		if(command == null || command.Connection == null)
			return;

		try
		{
			await command.Connection.OpenAsync(cancellation);

			foreach(var item in context.Data)
			{
				var target = item;

				for(int i = 0; i < members.Count; i++)
				{
					command.Parameters[i].Value = members[i].GetValue(ref target);
				}

				await command.ExecuteNonQueryAsync(cancellation);
			}

			await command.Transaction.CommitAsync(cancellation);
		}
		catch
		{
			if(command.Transaction != null)
				await command.Transaction.RollbackAsync(cancellation);
		}
		finally
		{
			await command.Connection.DisposeAsync();
		}
	}
	#endregion

	#region 私有方法
	private static DbCommand GetCommand(DataImportContext context, MemberCollection members)
	{
		var connection = context.Source.Driver.CreateConnection(context.Source.ConnectionString);
		var command = connection.CreateCommand();

		var fields = new StringBuilder();
		var values = new StringBuilder();
		var parameters = new List<DbParameter>();

		foreach(var member in members)
		{
			if(!member.IsSimplex(out var property))
				continue;

			if(fields.Length > 0)
				fields.Append(',');

			fields.Append(member.Property.GetFieldName(out var alias));

			if(!string.IsNullOrEmpty(alias))
				fields.Append($" AS '{alias}'");

			if(values.Length > 0)
				values.Append(',');

			values.Append($"@p_{member.Name}");

			var parameter = command.CreateParameter();
			parameter.ParameterName = $"@p_{member.Name}";
			parameter.DbType = property.Type;
			parameters.Add(parameter);
		}

		command.Transaction = connection.BeginTransaction();
		command.CommandType = System.Data.CommandType.Text;
		command.CommandText = $"INSERT INTO `{context.Entity.GetTableName()}` ({fields}) VALUES ({values});";

		return command;
	}
	#endregion
}
