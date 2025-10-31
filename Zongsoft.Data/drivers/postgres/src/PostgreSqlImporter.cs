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
	protected override void OnImport(DataImportContext context, MemberCollection members)
	{
		using var bulker = GetBulker(
			context.Entity.GetTableName(),
			members,
			(NpgsqlConnection)context.Source.Driver.CreateConnection(context.Source.ConnectionString),
			context.Options);

		foreach(var item in context.Data)
		{
			var target = item;
			if(target is null)
				continue;

			bulker.StartRow();

			for(int i = 0; i < members.Count; i++)
			{
				if(members[i].Property.IsSimplex(out var simplex))
				{
					var value = members[i].GetValue(ref target);

					if(value is null)
						bulker.WriteNull();
					else
						bulker.Write(value, Utility.GetDataType(simplex.Type));
				}
			}
		}

		bulker.Complete();
	}

	protected override async ValueTask OnImportAsync(DataImportContext context, MemberCollection members, CancellationToken cancellation = default)
	{
		using var bulker = await GetBulkerAsync(
			context.Entity.GetTableName(),
			members,
			(NpgsqlConnection)context.Source.Driver.CreateConnection(context.Source.ConnectionString),
			context.Options, cancellation);

		foreach(var item in context.Data)
		{
			var target = item;
			if(target is null)
				continue;

			bulker.StartRow();

			for(int i = 0; i < members.Count; i++)
			{
				if(members[i].Property.IsSimplex(out var simplex))
				{
					var value = members[i].GetValue(ref target);

					if(value is null)
						await bulker.WriteNullAsync(cancellation);
					else
						await bulker.WriteAsync(value, Utility.GetDataType(simplex.Type), cancellation);
				}
			}
		}

		await bulker.CompleteAsync(cancellation);
	}
	#endregion

	#region 私有方法
	private static NpgsqlBinaryImporter GetBulker(string name, MemberCollection members, NpgsqlConnection connection, IDataImportOptions options)
	{
		var fields = string.Join(',', members.Select(member => $"\"{member.Name}\""));
		return connection.BeginBinaryImport($"COPY \"{name}\" ({fields}) FROM STDIN (FORMAT Binary)");
	}

	private static Task<NpgsqlBinaryImporter> GetBulkerAsync(string name, MemberCollection members, NpgsqlConnection connection, IDataImportOptions options, CancellationToken cancellation)
	{
		var fields = string.Join(',', members.Select(member => $"\"{member.Name}\""));
		return connection.BeginBinaryImportAsync($"COPY \"{name}\" ({fields}) FROM STDIN (FORMAT Binary)", cancellation);
	}
	#endregion
}