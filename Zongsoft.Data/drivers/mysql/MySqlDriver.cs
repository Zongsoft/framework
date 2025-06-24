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
 * This file is part of Zongsoft.Data.MySql library.
 *
 * The Zongsoft.Data.MySql is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data.MySql is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data.MySql library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Data;
using System.Data.Common;

using MySqlConnector;

using Zongsoft.Data.Common;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.MySql
{
	public class MySqlDriver : DataDriverBase
	{
		#region 公共常量
		/// <summary>驱动程序的标识：MySql。</summary>
		public const string NAME = "MySql";
		#endregion

		#region 单例字段
		public static readonly MySqlDriver Instance = new();
		#endregion

		#region 私有构造
		private MySqlDriver()
		{
			//添加 MySQL 支持的功能特性集
			this.Features.Add(Feature.Deletion.Multitable);
			this.Features.Add(Feature.Updation.Multitable);
		}
		#endregion

		#region 公共属性
		public override string Name => NAME;
		public override IStatementBuilder Builder => MySqlStatementBuilder.Default;
		#endregion

		#region 公共方法
		public override Exception OnError(IDataAccessContext context, Exception exception)
		{
			if(exception is MySqlException error)
			{
				switch(error.Number)
				{
					case 1406:
						/*
						 * 参考文档：https://dev.mysql.com/doc/mysql-errors/8.0/en/server-error-reference.html#error_er_data_too_long
						 */
						var index = error.Message.IndexOf('\'');

						if(index >= 0)
						{
							var last = error.Message.IndexOf('\'', index + 1);
							var name = error.Message.Substring(index + 1, last - index);
							return new DataArgumentException(name, error.Message);
						}

						break;
					case 1062:
					case 1586:
						/*
						 * 参考文档：唯一约束冲突
						 * https://dev.mysql.com/doc/mysql-errors/8.0/en/server-error-reference.html#error_er_dup_entry
						 * https://dev.mysql.com/doc/mysql-errors/8.0/en/server-error-reference.html#error_er_dup_entry_with_key_name
						 */
						if(TryGetConstraint(error.Message, out var table, out var key, out var value))
						{
							(var kind, var fields) = GetIndexer(context, table, key);
							return new DataConstraintException(NAME, error.Number, kind, key, value, table, fields);
						}

						return new DataConflictException(NAME, error.Number, null, null, error);
					case 1216:
					case 1217:
					case 1451:
					case 1452:
						/*
						 * 参考文档：外键引用约束
						 * https://dev.mysql.com/doc/mysql-errors/8.0/en/server-error-reference.html#error_er_no_referenced_row
						 * https://dev.mysql.com/doc/mysql-errors/8.0/en/server-error-reference.html#error_er_row_is_referenced
						 * https://dev.mysql.com/doc/mysql-errors/8.0/en/server-error-reference.html#error_er_row_is_referenced_2
						 * https://dev.mysql.com/doc/mysql-errors/8.0/en/server-error-reference.html#error_er_no_referenced_row_2
						 */
						return new DataConflictException(NAME, error.Number, null, null, error);
				}
			}

			return exception;
		}

		public override DbCommand CreateCommand() => new MySqlCommand();
		public override DbCommand CreateCommand(string text, CommandType commandType = CommandType.Text) => new MySqlCommand(text)
		{
			CommandType = commandType,
		};

		public override DbConnection CreateConnection(string connectionString = null) => new MySqlConnection(connectionString);
		public override DbConnectionStringBuilder CreateConnectionBuilder(string connectionString = null) => new MySqlConnectionStringBuilder(connectionString);

		public override IDataImporter CreateImporter() => new MySqlImporter();
		#endregion

		#region 保护方法
		protected override ExpressionVisitorBase CreateVisitor() => new MySqlExpressionVisitor();
		#endregion

		#region 私有方法
		private static bool TryGetConstraint(string message, out string table, out string name, out string value)
		{
			table = null;
			name = null;
			value = null;

			if(string.IsNullOrEmpty(message))
				return false;

			var end = message.LastIndexOf('\'');
			var start = end > 0 ? message.LastIndexOf('\'', end - 1) : -1;

			if(start > 0 && end > 0)
			{
				var constraint = message.Substring(start + 1, end - start - 1);

				if(constraint != null)
				{
					var index = constraint.IndexOf('.');

					if(index > 0)
					{
						table = constraint[..index];
						name = constraint[(index + 1)..];
					}
					else
					{
						table = null;
						name = constraint;
					}
				}

				end = message.LastIndexOf('\'', start - 1);
				start = message.IndexOf('\'');

				if(end > 0 && start > 0 && start < end)
					value = message.Substring(start + 1, end - start - 1);

				return true;
			}

			return false;
		}

		private static (DataConstraintKind, string[]) GetIndexer(IDataAccessContext context, string tableName, string indexName)
		{
			if(context == null || context.Source == null)
				return default;

			if(string.IsNullOrEmpty(tableName) || string.IsNullOrEmpty(indexName))
				return default;

			try
			{
				var table = context.Source.GetSchema("Indexes");
				var rows = table.Select($"TABLE_NAME='{tableName}' AND INDEX_NAME='{indexName}'");
				if(rows == null || rows.Length == 0)
					return default;

				var isUnique = rows[0].Field<bool>("UNIQUE");
				var isPrimary = rows[0].Field<bool>("PRIMARY");

				table = context.Source.GetSchema("IndexColumns");
				rows = table.Select($"TABLE_NAME='{tableName}' AND INDEX_NAME='{indexName}'", "ORDINAL_POSITION");

				var result = new string[rows.Length];
				for(int i = 0; i < rows.Length; i++)
				{
					result[i] = rows[i].Field<string>("COLUMN_NAME");
				}

				return (isPrimary ? DataConstraintKind.PrimaryKey : DataConstraintKind.Unique, result);
			}
			catch
			{
				return default;
			}
		}

		private static (string, string[], string, string[]) GetForeignKey(IDataAccessContext context, string tableName, string foreignKey)
		{
			if(context == null || context.Source == null)
				return default;

			if(string.IsNullOrEmpty(tableName) || string.IsNullOrEmpty(foreignKey))
				return default;

			try
			{
				var table = context.Source.GetSchema("KeyColumnUsage");
				var rows = table.Select($"TABLE_NAME='{tableName}' AND CONSTRAINT_NAME='{foreignKey}'", "ORDINAL_POSITION");
				if(rows == null || rows.Length == 0)
					return default;

				var principalFields = new string[rows.Length];
				var foreignerFields = new string[rows.Length];

				for(int i = 0; i < rows.Length; i++)
				{
					principalFields[i] = rows[i].Field<string>("COLUMN_NAME");
					foreignerFields[i] = rows[i].Field<string>("REFERENCED_COLUMN_NAME");
				}

				return new
				(
					rows[0].Field<string>("TABLE_NAME"),
					principalFields,
					rows[0].Field<string>("REFERENCED_TABLE_NAME"),
					foreignerFields
				);
			}
			catch
			{
				return default;
			}
		}
		#endregion
	}
}
