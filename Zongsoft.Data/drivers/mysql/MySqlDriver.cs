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

using MySql.Data.MySqlClient;

using Zongsoft.Data.Common;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.MySql
{
	public class MySqlDriver : DataDriverBase
	{
		#region 公共常量
		/// <summary>驱动程序的标识：MySql。</summary>
		public const string Key = "MySql";
		#endregion

		#region 构造函数
		public MySqlDriver()
		{
			//添加 MySQL 支持的功能特性集
			this.Features.Add(Feature.Deletion.Multitable);
			this.Features.Add(Feature.Updation.Multitable);
		}
		#endregion

		#region 公共属性
		public override string Name
		{
			get
			{
				return Key;
			}
		}

		public override IStatementBuilder Builder
		{
			get
			{
				return MySqlStatementBuilder.Default;
			}
		}
		#endregion

		#region 公共方法
		public override Exception OnError(Exception exception)
		{
			if(exception is MySqlException error)
			{
				switch(error.Number)
				{
					case 1062:
						if(this.TryGetConflict(error.Message, out var key, out var value))
							return new DataConflictException(this.Name, error.Number, key, value, Array.Empty<string>());
						else
							return new DataConflictException(this.Name, error.Number, null, null, Array.Empty<string>(), error);
					default:
						return new DataAccessException(this.Name, error.Number, error);
				}
			}

			return exception;
		}

		public override DbCommand CreateCommand()
		{
			return new MySqlCommand();
		}

		public override DbCommand CreateCommand(string text, CommandType commandType = CommandType.Text)
		{
			return new MySqlCommand(text)
			{
				CommandType = commandType,
			};
		}

		public override DbConnection CreateConnection()
		{
			return new MySqlConnection();
		}

		public override DbConnection CreateConnection(string connectionString)
		{
			return new MySqlConnection(connectionString);
		}
		#endregion

		#region 保护方法
		protected override IExpressionVisitor CreateVisitor()
		{
			return new MySqlExpressionVisitor();
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
