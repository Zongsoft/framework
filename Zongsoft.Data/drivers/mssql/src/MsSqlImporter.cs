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
using System.Linq;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Microsoft.Data.SqlClient;

using Zongsoft.Data.Common;
using Zongsoft.Data.Metadata;

namespace Zongsoft.Data.MsSql;

public class MsSqlImporter : DataImporterBase
{
	#region 常量定义
	private const int BATCH_SIZE = 1024;
	#endregion

	#region 公共方法
	protected override async ValueTask OnImportAsync(DataImportContext context, MemberCollection members, CancellationToken cancellation = default)
	{
		await using var batches = GetBatchesAsync(context, members, cancellation).GetAsyncEnumerator(cancellation);
		if(!await batches.MoveNextAsync().ConfigureAwait(false))
			return;

		await using var lease = await context.Session.AcquireLeaseAsync(context.Options.TransactionSuppressed, cancellation);
		using var bulker = GetBulker(batches.Current, (SqlConnection)lease.Connection, (SqlTransaction)lease.Transaction);
		var count = 0;

		do
		{
			await bulker.WriteToServerAsync(batches.Current, cancellation);
			count += batches.Current.Rows.Count;
		} while(await batches.MoveNextAsync().ConfigureAwait(false));

		context.Count = count;
	}
	#endregion

	#region 私有方法
	private static SqlBulkCopy GetBulker(DataTable table, SqlConnection connection, SqlTransaction transaction)
	{
		var bulker = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction)
		{
			DestinationTableName = table.TableName,
		};

		foreach(DataColumn column in table.Columns)
			bulker.ColumnMappings.Add(column.ColumnName, column.ColumnName);

		return bulker;
	}

	private static async IAsyncEnumerable<DataTable> GetBatchesAsync(DataImportContext context, MemberCollection members, [EnumeratorCancellation] CancellationToken cancellation)
	{
		using var table = new DataTable(context.Entity.GetTableName());
		var columns = members.Where(member => member.IsSimplex(out _)).ToArray();

		foreach(var member in columns)
			table.Columns.Add(member.Property.GetFieldName(out _), GetDbType(((IDataEntitySimplexProperty)member.Property).Type).AsType());

		await foreach(var item in GetRecordsAsync(context).WithCancellation(cancellation).ConfigureAwait(false))
		{
			cancellation.ThrowIfCancellationRequested();

			if(item == null)
				continue;

			var target = item;
			var row = table.NewRow();

			for(int i = 0; i < columns.Length; i++)
			{
				var prepared = await columns[i].GetValueAsync(target, cancellation).ConfigureAwait(false);
				target = prepared.Target;
				var value = prepared.Value;

				row[i] = value == null || Convert.IsDBNull(value) ? DBNull.Value :
					Zongsoft.Common.Convert.ConvertValue(value, table.Columns[i].DataType);
			}

			table.Rows.Add(row);

			if(table.Rows.Count == BATCH_SIZE)
			{
				yield return table;
				table.Clear();
			}
		}

		if(table.Rows.Count > 0)
			yield return table;
	}

	private static DbType GetDbType(DataType type)
	{
		if(type.DbType == DbType.Object && type.Name.Equals("text", StringComparison.OrdinalIgnoreCase))
			return DbType.String;

		return type.DbType switch
		{
			DbType.SByte => DbType.Byte,
			DbType.UInt16 => DbType.Int16,
			DbType.UInt32 => DbType.Int32,
			DbType.UInt64 => DbType.Int64,
			_ => type,
		};
	}
	#endregion
}
