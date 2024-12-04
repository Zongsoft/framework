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
 * This file is part of Zongsoft.Data.MsSql library.
 *
 * The Zongsoft.Data.MsSql is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data.MsSql is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data.MsSql library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;

using Zongsoft.Data.Common;
using Zongsoft.Data.Metadata;

namespace Zongsoft.Data.MsSql
{
	public class MsSqlImporter : DataImporterBase
	{
		#region 公共方法
		protected override void OnImport(DataImportContext context, MemberCollection members)
		{
			//将数据填充到数据表中
			var table = GetTable(context.Entity.GetTableName(), context.Data, members);

			var bulker = GetBulker(table.TableName, (SqlConnection)context.Source.Driver.CreateConnection(context.Source.ConnectionString));
			bulker.WriteToServer(table);

			//设置返回值
			context.Count = table.Rows.Count;
			//清空数据表中的数据
			table.Rows.Clear();
		}

		protected override ValueTask OnImportAsync(DataImportContext context, MemberCollection members, CancellationToken cancellation = default)
		{
			//将数据填充到数据表中
			var table = GetTable(context.Entity.GetTableName(), context.Data, members);

			var bulker = GetBulker(table.TableName, (SqlConnection)context.Source.Driver.CreateConnection(context.Source.ConnectionString));
			var task = bulker.WriteToServerAsync(table, cancellation);

			//设置返回值
			context.Count = table.Rows.Count;
			//清空数据表中的数据
			table.Rows.Clear();

			return new ValueTask(task);
		}
		#endregion

		#region 私有方法
		private static SqlBulkCopy GetBulker(string name, SqlConnection connection) => new SqlBulkCopy(connection)
		{
			DestinationTableName = name,
		};

		private static DataTable GetTable(string name, IEnumerable data, MemberCollection members)
		{
			var table = new DataTable(name);

			foreach(var member in members)
			{
				if(member.IsSimplex(out var property))
					table.Columns.Add(member.Property.GetFieldName(out _), property.Type.AsType());
			}

			foreach(var item in data)
			{
				var row = table.NewRow();

				for(int i = 0; i < members.Count; i++)
				{
					var target = item;
					row[members[i].Name] = members[i].GetValue(ref target);
				}

				table.Rows.Add(row);
				table.AcceptChanges();
			}

			return table;
		}
		#endregion
	}
}
