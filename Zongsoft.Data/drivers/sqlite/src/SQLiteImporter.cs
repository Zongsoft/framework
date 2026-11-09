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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.Text;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Data.Common;
using Zongsoft.Data.Metadata;

namespace Zongsoft.Data.SQLite;

public class SQLiteImporter : DataImporterBase
{
	#region 公共方法
	protected override async ValueTask OnImportAsync(DataImportContext context, MemberCollection members, CancellationToken cancellation = default)
	{
		await using var lease = await context.Session.AcquireLeaseAsync(context.Options.TransactionSuppressed, cancellation);
		await using var command = GetCommand(context, members, lease.Connection);

		await using var transaction = lease.Transaction == null ? await lease.Connection.BeginTransactionAsync(cancellation) : null;
		command.Transaction = lease.Transaction ?? transaction;

		try
		{
			await foreach(var item in GetRecordsAsync(context).WithCancellation(cancellation).ConfigureAwait(false))
			{
				var target = item;

				for(int i = 0; i < members.Count; i++)
				{
					var prepared = await members[i].GetValueAsync(target, cancellation).ConfigureAwait(false);
					target = prepared.Target;
					command.Parameters[i].Value = prepared.Value ?? DBNull.Value;
				}

				context.Count += await command.ExecuteNonQueryAsync(cancellation);
			}

			if(transaction != null)
				await transaction.CommitAsync(cancellation);
		}
		catch
		{
			if(transaction != null)
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

			values.Append($"@{member.Name}");

			var parameter = command.CreateParameter();
			parameter.ParameterName = $"@{member.Name}";
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
