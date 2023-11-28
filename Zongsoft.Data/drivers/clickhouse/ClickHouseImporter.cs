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
 * Copyright (C) 2010-2022 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Data.ClickHouse library.
 *
 * The Zongsoft.Data.ClickHouse is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data.ClickHouse is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data.ClickHouse library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using ClickHouse.Client;
using ClickHouse.Client.ADO;
using ClickHouse.Client.Copy;

using Zongsoft.Reflection;
using Zongsoft.Data.Common;
using Zongsoft.Data.Metadata;

namespace Zongsoft.Data.ClickHouse
{
	public class ClickHouseImporter : DataImporterBase
	{
		#region 构造函数
		public ClickHouseImporter(DataImportContextBase context) : base(context) { }
		#endregion

		#region 公共方法
		public override void Import(DataImportContext context)
		{
			var bulker = GetBulker(context);
			if(bulker == null)
				return;

			var records = GetRecords(context, this.Members);
			if(records == null)
				return;

			bulker.WriteToServerAsync(records).ConfigureAwait(false).GetAwaiter().GetResult();
		}

		public override async ValueTask ImportAsync(DataImportContext context, CancellationToken cancellation = default)
		{
			var bulker = GetBulker(context);
			if(bulker == null)
				return;

			var records = GetRecords(context, this.Members);
			if(records == null)
				return;

			await bulker.WriteToServerAsync(records, cancellation);
		}
		#endregion

		#region 私有方法
		private static ClickHouseConnection GetConnection(DataImportContext context) => (ClickHouseConnection)context.Source.Driver.CreateConnection(context.Source.ConnectionString);

		private static ClickHouseBulkCopy GetBulker(DataImportContext context)
		{
			var connection = GetConnection(context);
			var bulker = new ClickHouseBulkCopy(connection)
			{
				DestinationTableName = context.Entity.GetTableName(),
				ColumnNames = context.Members,
			};

			return bulker;
		}

		private static IEnumerable<object[]> GetRecords(DataImportContext context, Member[] members)
		{
			if(members == null || members.Length == 0)
				return null;

			//构建导入的数据记录集
			var records = new List<object[]>();

			foreach(var item in context.Data)
			{
				var target = item;
				var record = new object[members.Length];

				for(int i = 0; i < members.Length; i++)
				{
					record[i] = members[i].GetValue(ref target);
				}

				records.Add(record);
			}

			return records;
		}
		#endregion
	}
}
