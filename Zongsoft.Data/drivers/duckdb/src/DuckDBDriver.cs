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
 * Copyright (C) 2020-2026 Zongsoft Studio <http://zongsoft.com>
 *
 * This file is part of Zongsoft.Data.DuckDB library.
 *
 * The Zongsoft.Data.DuckDB is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data.DuckDB is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data.DuckDB library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Data;
using System.Data.Common;

using DuckDB.NET.Data;

using Zongsoft.Data.Common;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.DuckDB;

public partial class DuckDBDriver : DataDriverBase
{
	#region 公共常量
	/// <summary>驱动程序的标识：DuckDB。</summary>
	public const string NAME = "DuckDB";
	#endregion

	#region 单例字段
	public static readonly DuckDBDriver Instance = new();
	#endregion

	#region 私有构造
	private DuckDBDriver()
	{
		this.Features.Add(Feature.Returning);
	}
	#endregion

	#region 公共属性
	public override string Name => NAME;
	public override IStatementBuilder Builder => DuckDBStatementBuilder.Default;
	#endregion

	#region 公共方法
	public override Exception OnError(IDataAccessContext context, Exception exception)
	{
		if(exception is DuckDBException error)
		{
			switch(error.ErrorCode)
			{
				case -1:
					break;
			}
		}

		return exception;
	}

	public override DbCommand CreateCommand() => new DuckDBCommand();
	public override DbCommand CreateCommand(string text, CommandType commandType = CommandType.Text) => new DuckDBCommand(text)
	{
		CommandType = commandType,
	};

	public override DbConnection CreateConnection(string connectionString = null) => new DuckDBConnection(connectionString);
	public override DbConnectionStringBuilder CreateConnectionBuilder(string connectionString = null) => new DuckDBConnectionStringBuilder() { ConnectionString = connectionString };
	#endregion

	#region 保护方法
	protected override IDataImporter CreateImporter() => new DuckDBImporter();
	protected override ExpressionVisitorBase CreateVisitor() => new DuckDBExpressionVisitor();
	#endregion
}
