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
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using ClickHouse.Driver;
using ClickHouse.Driver.ADO;
using ClickHouse.Driver.Compression;

using Zongsoft.Reflection;
using Zongsoft.Data.Common;
using Zongsoft.Data.Metadata;

namespace Zongsoft.Data.ClickHouse;

public class ClickHouseImporter : DataImporterBase
{
	#region 公共方法
	protected override async ValueTask OnImportAsync(DataImportContext context, MemberCollection members, CancellationToken cancellation = default)
	{
		var records = await GetRecordsAsync(context, members, cancellation).ConfigureAwait(false);
		if(records == null)
			return;

		await using var lease = await context.Session.AcquireLeaseAsync(context.Options.TransactionSuppressed, cancellation);
		var connection = (ClickHouseConnection)lease.Connection;
		using var client = new ClickHouseClient(connection.Settings);
		context.Count = (int)await client.InsertBinaryAsync(context.Entity.GetTableName(), context.Members, records, GetOptions(connection), cancellation);
	}
	#endregion

	#region 私有方法
	private static InsertOptions GetOptions(ClickHouseConnection connection) => new()
	{
		Database = connection.Database,
		Compressor = connection.UseCompression ? ZstdCompressor.Default : null,
	};

	private static async ValueTask<List<object[]>> GetRecordsAsync(DataImportContext context, MemberCollection members, CancellationToken cancellation)
	{
		if(members == null || members.Count == 0)
			return null;

		//构建导入的数据记录集
		var records = new List<object[]>();

		await foreach(var item in GetRecordsAsync(context).WithCancellation(cancellation).ConfigureAwait(false))
		{
			var target = item;
			var record = new object[members.Count];

			for(int i = 0; i < members.Count; i++)
			{
				var prepared = await members[i].GetValueAsync(target, cancellation).ConfigureAwait(false);
				target = prepared.Target;
				record[i] = prepared.Value;
			}

			records.Add(record);
		}

		return records;
	}
	#endregion
}
