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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Data.PostgreSql library.
 *
 * The Zongsoft.Data.PostgreSql is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data.PostgreSql is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data.PostgreSql library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

using Npgsql;

using Zongsoft.Data.Common;
using Zongsoft.Data.Features;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.PostgreSql;

public partial class PostgreSqlDriver : DataDriverBase
{
	#region 公共常量
	/// <summary>驱动程序的标识：PostgreSql。</summary>
	public const string NAME = "PostgreSql";
	#endregion

	#region 单例字段
	public static readonly PostgreSqlDriver Instance = new();
	#endregion

	#region 私有构造
	private PostgreSqlDriver()
	{
		this.Getter = new PostgreSqlGetter();
		this.Setter = new PostgreSqlSetter();
		this.Features.Set<IReturningFeature>(ReturningFeature.Instance);
	}
	#endregion

	#region 公共属性
	public override string Name => NAME;
	public override IStatementBuilder Builder => PostgreSqlStatementBuilder.Default;
	#endregion

	#region 公共方法
	public override async ValueTask<Exception> OnErrorAsync(IDataAccessContext context, Exception exception, CancellationToken cancellation = default)
	{
		if(exception is PostgresException error)
		{
			switch(error.SqlState)
			{
				case PostgresErrorCodes.StringDataRightTruncation:
					return new DataArgumentException(error.ColumnName, error);
				case PostgresErrorCodes.UniqueViolation:
					(var kind, var fields) = await GetIndexerAsync(context, error.SchemaName, error.TableName, error.ConstraintName, cancellation).ConfigureAwait(false);
					return new DataConstraintException(NAME, 23505, kind, error.ConstraintName, null, error.TableName, fields, error);
				case PostgresErrorCodes.ForeignKeyViolation:
					return new DataConflictException(NAME, 23503, error.ConstraintName, null, error);
			}
		}

		return exception;
	}

	public override DbCommand CreateCommand() => new NpgsqlCommand();
	public override DbCommand CreateCommand(string text, CommandType commandType = CommandType.Text) => new NpgsqlCommand(text)
	{
		CommandType = commandType,
	};

	public override DbConnection CreateConnection(string connectionString = null) =>
		new NpgsqlConnection(Configuration.PostgreSqlConnectionSettingsDriver.Instance.GetSettings(connectionString).GetOptions().ConnectionString);
	public override DbConnectionStringBuilder CreateConnectionBuilder(string connectionString = null) =>
		Configuration.PostgreSqlConnectionSettingsDriver.Instance.GetSettings(connectionString).GetOptions();
	#endregion

	#region 保护方法
	protected override IDataImporter CreateImporter() => new PostgreSqlImporter();
	protected override ExpressionVisitorBase CreateVisitor() => new PostgreSqlExpressionVisitor();
	#endregion

	#region 私有方法
	private static async ValueTask<(DataConstraintKind, string[])> GetIndexerAsync(IDataAccessContext context, string schemaName, string tableName, string indexName, CancellationToken cancellation)
	{
		if(context?.Source == null || string.IsNullOrEmpty(schemaName) || string.IsNullOrEmpty(tableName) || string.IsNullOrEmpty(indexName))
			return default;

		try
		{
			await using var connection = await DataConnectorManager.GetConnector(context.Source).ConnectAsync(cancellation).ConfigureAwait(false);
			await using var command = connection.CreateCommand();

			command.CommandText = """
				SELECT x.indisprimary,
					CASE WHEN x.indexprs IS NULL THEN ARRAY(
						SELECT a.attname::text
						FROM pg_catalog.unnest(x.indkey) WITH ORDINALITY AS k(number, position)
						JOIN pg_catalog.pg_attribute AS a ON a.attrelid = x.indrelid AND a.attnum = k.number
						WHERE k.position <= x.indnkeyatts
						ORDER BY k.position
					) ELSE ARRAY[]::text[] END
				FROM pg_catalog.pg_index AS x
				JOIN pg_catalog.pg_class AS t ON t.oid = x.indrelid
				JOIN pg_catalog.pg_namespace AS n ON n.oid = t.relnamespace
				JOIN pg_catalog.pg_class AS i ON i.oid = x.indexrelid
				WHERE n.nspname = @schema AND t.relname = @table AND i.relname = @index AND x.indisunique
				""";

			command.Parameters.Add(new NpgsqlParameter("schema", schemaName));
			command.Parameters.Add(new NpgsqlParameter("table", tableName));
			command.Parameters.Add(new NpgsqlParameter("index", indexName));

			await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellation).ConfigureAwait(false);
			if(await reader.ReadAsync(cancellation).ConfigureAwait(false))
				return (reader.GetBoolean(0) ? DataConstraintKind.PrimaryKey : DataConstraintKind.Unique, reader.GetFieldValue<string[]>(1));

			return default;
		}
		catch(OperationCanceledException) when(cancellation.IsCancellationRequested)
		{
			throw;
		}
		catch
		{
			//元数据获取失败时保留服务器已提供的约束信息。
			return default;
		}
	}
	#endregion
}
