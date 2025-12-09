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
 * Copyright (C) 2010-2022 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Data.SQLite library.
 *
 * The Zongsoft.Data.SQLite is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data.SQLite is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data.SQLite library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Data;
using System.Data.Common;

using Microsoft.Data.Sqlite;

using Zongsoft.Data.Common;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.SQLite;

public class SQLiteDriver : DataDriverBase
{
	#region 公共常量
	/// <summary>驱动程序的标识：SQLite。</summary>
	public const string NAME = "SQLite";
	#endregion

	#region 单例字段
	public static readonly SQLiteDriver Instance = new();
	#endregion

	#region 私有构造
	private SQLiteDriver()
	{
		this.Features.Add(Feature.Returning);
	}
	#endregion

	#region 公共属性
	public override string Name => NAME;
	public override IStatementBuilder Builder => SQLiteStatementBuilder.Default;
	#endregion

	#region 公共方法
	public override Exception OnError(IDataAccessContext context, Exception exception)
	{
		if(exception is SqliteException error)
		{
			switch(error.SqliteErrorCode)
			{
				case -1:
					break;
			}
		}

		return exception;
	}

	public override DbCommand CreateCommand() => new SqliteCommand();
	public override DbCommand CreateCommand(string text, CommandType commandType = CommandType.Text) => new SqliteCommand(text)
	{
		CommandType = commandType,
	};

	public override DbConnection CreateConnection(string connectionString = null) => new SqliteConnection(connectionString);
	public override DbConnectionStringBuilder CreateConnectionBuilder(string connectionString = null) => new SqliteConnectionStringBuilder(connectionString);

	public override IDataImporter CreateImporter() => new SQLiteImporter();
	#endregion

	#region 保护方法
	protected override ExpressionVisitorBase CreateVisitor() => new SQLiteExpressionVisitor();
	#endregion
}
