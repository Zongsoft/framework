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
using System.Threading;
using System.Threading.Tasks;

using Npgsql;

using Zongsoft.Data.Common;
using Zongsoft.Data.Metadata;

namespace Zongsoft.Data.PostgreSql;

public class PostgreSqlImporter : DataImporterBase
{
	#region 公共方法
	protected override async ValueTask OnImportAsync(DataImportContext context, MemberCollection members, CancellationToken cancellation = default)
	{
		await using var lease = await context.Session.AcquireLeaseAsync(context.Options.TransactionSuppressed, cancellation);

		await using var bulker = await GetBulkerAsync(
			context.Entity.GetTableName(),
			members,
			(NpgsqlConnection)lease.Connection,
			context.Options, cancellation);

		await foreach(var item in GetRecordsAsync(context).WithCancellation(cancellation).ConfigureAwait(false))
		{
			var target = item;
			if(target is null)
				continue;

			await bulker.StartRowAsync(cancellation).ConfigureAwait(false);

			for(int i = 0; i < members.Count; i++)
			{
				if(members[i].Property.IsSimplex(out var simplex))
				{
					var prepared = await members[i].GetValueAsync(target, cancellation).ConfigureAwait(false);
					target = prepared.Target;
					var value = prepared.Value;

					if(value is null || Convert.IsDBNull(value))
						await bulker.WriteNullAsync(cancellation);
					else
					{
						var pgType = PostgreSqlUtility.GetDataType(simplex.Type);
						await bulker.WriteAsync(Zongsoft.Common.Convert.ConvertValue(value, pgType.AsType()), pgType, cancellation);
					}
				}
			}
		}

		context.Count = (int)await bulker.CompleteAsync(cancellation);
	}
	#endregion

	#region 私有方法
	private static async Task<NpgsqlBinaryImporter> GetBulkerAsync(string name, MemberCollection members, NpgsqlConnection connection, IDataImportOptions options, CancellationToken cancellation)
	{
		var fields = string.Join(',', members.Select(member => $"\"{member.Name}\""));

		return await connection.BeginBinaryImportAsync($"COPY \"{name}\" ({fields}) FROM STDIN (FORMAT Binary)", cancellation);
	}
	#endregion
}
