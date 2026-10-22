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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Data.PostgreSql library.
 *
 * The Zongsoft.Data.PostgreSql is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data.PostgreSql is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data.PostgreSql library. If not, see <http://www.gnu.org/licenses/>.
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
