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
using System.Data.Common;

using ClickHouse.Client;
using ClickHouse.Client.ADO;

using Zongsoft.Data.Common;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.ClickHouse
{
	public class ClickHouseDriver : DataDriverBase
	{
		#region 公共常量
		/// <summary>驱动程序的标识：ClickHouse。</summary>
		public const string Key = "ClickHouse";
		#endregion

		#region 构造函数
		public ClickHouseDriver() { }
		#endregion

		#region 公共属性
		public override string Name => Key;
		public override IStatementBuilder Builder => ClickHouseStatementBuilder.Default;
		#endregion

		#region 公共方法
		public override Exception OnError(Exception exception)
		{
			if(exception is DbException error)
			{
				switch(error.ErrorCode)
				{
					case 0:
						return error;
				}
			}

			return exception;
		}

		public override DbCommand CreateCommand() => new ClickHouseCommand();
		public override DbCommand CreateCommand(string text, CommandType commandType = CommandType.Text) => new ClickHouseCommand { CommandText = text, CommandType = commandType };

		public override DbConnection CreateConnection() => new ClickHouseConnection();
		public override DbConnection CreateConnection(string connectionString) => new ClickHouseConnection(connectionString);

		public override IDataImporter CreateImporter(DataImportContextBase context) => new ClickHouseImporter(context);
		#endregion

		#region 保护方法
		protected override ExpressionVisitorBase CreateVisitor() => new ClickHouseExpressionVisitor();
		#endregion
	}
}
