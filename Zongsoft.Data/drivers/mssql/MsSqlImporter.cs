/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
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
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;

using Zongsoft.Reflection;
using Zongsoft.Data.Common;
using Zongsoft.Data.Metadata;

namespace Zongsoft.Data.MsSql
{
	public class MsSqlImporter : DataImporterBase
	{
		#region 成员字段
		private readonly DataTable _table;
		#endregion

		#region 构造函数
		public MsSqlImporter(DataImportContextBase context) : base(context)
		{
			_table = new DataTable(context.Entity.GetTableName());

			foreach(var member in this.Members)
			{
				var property = context.Entity.Properties[member.Name];

				if(property != null && property.IsSimplex)
					_table.Columns.Add(property.GetFieldName(out _), ((IDataEntitySimplexProperty)property).Type.AsType());
			}
		}
		#endregion

		#region 公共方法
		public override void Import(DataImportContext context)
		{
			//将数据填充到数据表中
			FillTable(_table, context.Data, this.Members);

			var bulker = GetBulker(_table.TableName, (SqlConnection)context.Source.Driver.CreateConnection(context.Source.ConnectionString));
			bulker.WriteToServer(_table);

			//设置返回值
			context.Count = _table.Rows.Count;
			//清空数据表中的数据
			_table.Rows.Clear();
		}

		public override ValueTask ImportAsync(DataImportContext context, CancellationToken cancellation = default)
		{
			//将数据填充到数据表中
			FillTable(_table, context.Data, this.Members);

			var bulker = GetBulker(_table.TableName, (SqlConnection)context.Source.Driver.CreateConnection(context.Source.ConnectionString));
			var task = bulker.WriteToServerAsync(_table, cancellation);

			//设置返回值
			context.Count = _table.Rows.Count;
			//清空数据表中的数据
			_table.Rows.Clear();

			return new ValueTask(task);
		}
		#endregion

		#region 私有方法
		private static SqlBulkCopy GetBulker(string name, SqlConnection connection) => new SqlBulkCopy(connection)
		{
			DestinationTableName = name,
		};

		private static void FillTable(DataTable table, IEnumerable data, System.Reflection.MemberInfo[] members)
		{
			foreach(var item in data)
			{
				var row = table.NewRow();

				for(int i = 0; i < members.Length; i++)
				{
					var target = item;
					row[members[i].Name] = Reflector.GetValue(members[i], ref target);
				}

				table.Rows.Add(row);
				table.AcceptChanges();
			}
		}
		#endregion
	}
}
