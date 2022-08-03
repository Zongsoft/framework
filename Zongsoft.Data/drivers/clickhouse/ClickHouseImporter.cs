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

using ClickHouse.Ado;

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
			var command = GetCommand(context);
			if(command == null || command.Connection == null)
				return;

			try
			{
				var count = command.ExecuteNonQuery();
			}
			finally
			{
				var connection = command.Connection;

				if(connection != null)
					connection.Dispose();
			}
		}

		public override ValueTask ImportAsync(DataImportContext context, CancellationToken cancellation = default)
		{
			var command = GetCommand(context);
			if(command == null || command.Connection == null)
				return ValueTask.CompletedTask;

			try
			{
				var count = command.ExecuteNonQuery();
				return ValueTask.CompletedTask;
			}
			finally
			{
				var connection = command.Connection;

				if(connection != null)
					connection.Dispose();
			}
		}
		#endregion

		#region 私有方法
		private IDbCommand GetCommand(DataImportContext context)
		{
			if(this.Members.Length == 0)
				return null;

			//构建批量导入的命令脚本
			var text = new System.Text.StringBuilder($"INSERT INTO {context.Entity.GetTableName()} (", 256);
			for(int i = 0; i < this.Members.Length; i++)
			{
				if(i > 0)
					text.Append(',');

				text.Append(this.Members[i].Property.GetFieldName(out _));
			}
			text.AppendLine(") VALUES (@bulk);");

			//构建导入的数据记录集
			var records = new List<object[]>();
			foreach(var item in context.Data)
			{
				var target = item;
				var record = new object[this.Members.Length];

				for(int i = 0; i < this.Members.Length; i++)
				{
					record[i] = this.Members[i].GetValue(ref target);
				}

				records.Add(record);
			}

			//如果导入的数据记录为空则返回失败
			if(records == null || records.Count == 0)
				return null;

			var connection = context.Source.Driver.CreateConnection(context.Source.ConnectionString);
			var command = connection.CreateCommand();
			command.CommandText = text.ToString();
			command.CommandType = CommandType.Text;
			command.AddParameter("bulk", DbType.Object, records);

			return command;
		}
		#endregion
	}
}
