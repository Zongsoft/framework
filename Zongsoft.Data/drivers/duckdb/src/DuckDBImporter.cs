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
 * Copyright (C) 2020-2026 Zongsoft Studio <http://zongsoft.com>
 *
 * This file is part of Zongsoft.Data.DuckDB library.
 *
 * The Zongsoft.Data.DuckDB is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data.DuckDB is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data.DuckDB library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Text;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Data.Common;
using Zongsoft.Data.Metadata;

namespace Zongsoft.Data.DuckDB;

public class DuckDBImporter : DataImporterBase
{
	#region 公共方法
	protected override void OnImport(DataImportContext context, MemberCollection members)
	{
		using var connection = context.Source.Driver.CreateConnection(context.Source.ConnectionString);
		using var command = GetCommand(context, members, connection);

		connection.Open();
		using var transaction = connection.BeginTransaction();
		command.Transaction = transaction;

		try
		{
			foreach(var item in context.Data)
			{
				var target = item;

				for(int i = 0; i < members.Count; i++)
				{
					command.Parameters[i].Value = members[i].GetValue(ref target) ?? DBNull.Value;
				}

				context.Count += command.ExecuteNonQuery();
			}

			transaction.Commit();
		}
		catch
		{
			transaction.Rollback();
			throw;
		}
	}

	protected override async ValueTask OnImportAsync(DataImportContext context, MemberCollection members, CancellationToken cancellation = default)
	{
		await using var connection = context.Source.Driver.CreateConnection(context.Source.ConnectionString);
		await using var command = GetCommand(context, members, connection);

		await connection.OpenAsync(cancellation);
		await using var transaction = await connection.BeginTransactionAsync(cancellation);
		command.Transaction = transaction;

		try
		{
			foreach(var item in context.Data)
			{
				var target = item;

				for(int i = 0; i < members.Count; i++)
				{
					command.Parameters[i].Value = members[i].GetValue(ref target) ?? DBNull.Value;
				}

				context.Count += await command.ExecuteNonQueryAsync(cancellation);
			}

			await transaction.CommitAsync(cancellation);
		}
		catch
		{
			await transaction.RollbackAsync(CancellationToken.None);
			throw;
		}
	}
	#endregion

	#region 私有方法
	private static DbCommand GetCommand(DataImportContext context, MemberCollection members, DbConnection connection)
	{
		var command = connection.CreateCommand();

		var fields = new StringBuilder();
		var values = new StringBuilder();

		foreach(var member in members)
		{
			if(!member.IsSimplex(out var property))
				continue;

			if(fields.Length > 0)
				fields.Append(',');

			fields.Append($"\"{member.Property.GetFieldName()}\"");

			if(values.Length > 0)
				values.Append(',');

			values.Append('?');

			var parameter = command.CreateParameter();
			parameter.DbType = property.Type;
			command.Parameters.Add(parameter);
		}

		command.CommandType = System.Data.CommandType.Text;
		var keyword = context.Options.ConstraintIgnored ? "INSERT OR IGNORE" : "INSERT";
		command.CommandText = $"{keyword} INTO \"{context.Entity.GetTableName()}\" ({fields}) VALUES ({values});";

		return command;
	}
	#endregion
}
