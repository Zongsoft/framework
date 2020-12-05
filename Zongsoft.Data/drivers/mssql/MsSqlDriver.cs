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

using Zongsoft.Data.Common;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.MsSql
{
	public class MsSqlDriver : DataDriverBase
	{
		#region 公共常量
		/// <summary>驱动程序的标识：MsSql。</summary>
		public const string Key = "MsSql";
		#endregion

		#region 构造函数
		public MsSqlDriver()
		{
			//添加 MsSql(SQL Server) 支持的功能特性集
			this.Features.Add(Feature.Deletion.Outputting);
			this.Features.Add(Feature.Updation.Outputting);
		}
		#endregion

		#region 公共属性
		public override string Name => Key;
		public override IStatementBuilder Builder => MsSqlStatementBuilder.Default;
		#endregion

		#region 公共方法
		public override Exception OnError(Exception exception)
		{
			if(exception is SqlException error)
			{
				switch(error.Number)
				{
					case 2601:
					case 2627:
						if(this.TryGetConflict(error.Message, out var key, out var value))
							return new DataConflictException(this.Name, error.Number, key, value, Array.Empty<string>());
						else
							return new DataConflictException(this.Name, error.Number, null, null, Array.Empty<string>(), error);
				}
			}

			return exception;
		}

		public override DbCommand CreateCommand()
		{
			return new SqlCommand();
		}

		public override DbCommand CreateCommand(string text, CommandType commandType = CommandType.Text)
		{
			return new SqlCommand(text)
			{
				CommandType = commandType,
			};
		}

		public override DbConnection CreateConnection()
		{
			return new SqlConnection();
		}

		public override DbConnection CreateConnection(string connectionString)
		{
			return new SqlConnection(connectionString);
		}
		#endregion

		#region 保护方法
		protected override ExpressionVisitorBase CreateVisitor()
		{
			return new MsSqlExpressionVisitor();
		}

		protected override void SetParameter(DbParameter parameter, ParameterExpression expression)
		{
			switch(expression.DbType)
			{
				case DbType.SByte:
					parameter.DbType = DbType.Byte;
					break;
				case DbType.UInt16:
					parameter.DbType = DbType.Int16;
					break;
				case DbType.UInt32:
					parameter.DbType = DbType.Int32;
					break;
				case DbType.UInt64:
					parameter.DbType = DbType.Int64;
					break;
				default:
					parameter.DbType = expression.DbType;
					break;
			}

			if(expression.Schema != null && expression.Schema.Token.Property.IsSimplex)
				((SqlParameter)parameter).IsNullable = ((Metadata.IDataEntitySimplexProperty)expression.Schema.Token.Property).Nullable;
			else
				((SqlParameter)parameter).IsNullable = false;
		}
		#endregion

		#region 私有方法
		private bool TryGetConflict(string message, out string key, out string value)
		{
			key = null;
			value = null;

			if(string.IsNullOrEmpty(message))
				return false;

			var end = message.LastIndexOf('\'');
			var start = end > 0 ? message.LastIndexOf('\'', end - 1) : -1;

			if(start > 0 && end > 0)
			{
				key = message.Substring(start + 1, end - start - 1);

				end = message.LastIndexOf('\'', start - 1);
				start = message.IndexOf('\'');

				if(end > 0 && start > 0 && start < end)
					value = message.Substring(start + 1, end - start - 1);

				return true;
			}

			return false;
		}
		#endregion
	}
}
