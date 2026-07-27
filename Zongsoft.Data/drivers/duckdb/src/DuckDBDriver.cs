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
using System.IO;
using System.Data;
using System.Data.Common;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;

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

	public override DbCommand CreateCommand() => new DuckDBCommandAdapter();
	public override DbCommand CreateCommand(string text, CommandType commandType = CommandType.Text) => new DuckDBCommandAdapter(text)
	{
		CommandType = commandType,
	};

	public override DbConnection CreateConnection(string connectionString = null) =>
		new DuckDBConnectionAdapter(Configuration.DuckDBConnectionSettingsDriver.Instance.GetSettings(connectionString).GetOptions().ConnectionString);
	public override DbConnectionStringBuilder CreateConnectionBuilder(string connectionString = null) =>
		Configuration.DuckDBConnectionSettingsDriver.Instance.GetSettings(connectionString).GetOptions();
	#endregion

	#region 保护方法
	protected override IDataImporter CreateImporter() => new DuckDBImporter();
	protected override ExpressionVisitorBase CreateVisitor() => new DuckDBExpressionVisitor();
	#endregion

	#region 嵌套子类
	private sealed class DuckDBCommandAdapter : DuckDBCommand
	{
		public DuckDBCommandAdapter() { }
		public DuckDBCommandAdapter(string text) : base(text) { }

		protected override DbParameter CreateDbParameter() => new Parameter();

		public override object ExecuteScalar()
		{
			using var reader = base.ExecuteDbDataReader(CommandBehavior.SingleResult | CommandBehavior.SingleRow);
			return reader.Read() && reader.FieldCount > 0 ? reader.GetValue(0) : null;
		}

		protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => new DataReader(base.ExecuteDbDataReader(behavior));

		private sealed class Parameter : DuckDBParameter
		{
			public override string ParameterName
			{
				get => base.ParameterName;
				set => base.ParameterName = string.IsNullOrEmpty(value) || value[0] is not ('@' or '$') ? value : value[1..];
			}
		}

		private sealed class DataReader(DbDataReader reader) : DbDataReader
		{
			private readonly DbDataReader _reader = reader ?? throw new ArgumentNullException(nameof(reader));
			private int _count;

			public override object this[int ordinal] => _reader[ordinal];
			public override object this[string name] => _reader[name];
			public override int Depth => _reader.Depth;
			public override int FieldCount => _reader.FieldCount;
			public override bool HasRows => _reader.HasRows;
			public override bool IsClosed => _reader.IsClosed;
			public override int RecordsAffected => _reader.RecordsAffected < 0 ? _count : _reader.RecordsAffected;
			public override int VisibleFieldCount => _reader.VisibleFieldCount;

			public override bool GetBoolean(int ordinal) => _reader.GetBoolean(ordinal);
			public override byte GetByte(int ordinal) => _reader.GetByte(ordinal);
			public override long GetBytes(int ordinal, long offset, byte[] buffer, int bufferOffset, int length) => _reader.GetBytes(ordinal, offset, buffer, bufferOffset, length);
			public override char GetChar(int ordinal) => _reader.GetChar(ordinal);
			public override long GetChars(int ordinal, long offset, char[] buffer, int bufferOffset, int length) => _reader.GetChars(ordinal, offset, buffer, bufferOffset, length);
			public override DateTime GetDateTime(int ordinal) => _reader.GetDateTime(ordinal);
			public override decimal GetDecimal(int ordinal) => _reader.GetDecimal(ordinal);
			public override double GetDouble(int ordinal) => _reader.GetDouble(ordinal);
			public override float GetFloat(int ordinal) => _reader.GetFloat(ordinal);
			public override Guid GetGuid(int ordinal) => _reader.GetGuid(ordinal);
			public override short GetInt16(int ordinal) => _reader.GetInt16(ordinal);
			public override int GetInt32(int ordinal) => _reader.GetInt32(ordinal);
			public override long GetInt64(int ordinal) => _reader.GetInt64(ordinal);
			public override string GetString(int ordinal) => _reader.GetString(ordinal);
			public override Stream GetStream(int ordinal) => _reader.GetStream(ordinal);
			public override object GetValue(int ordinal) => _reader.GetValue(ordinal);
			public override int GetValues(object[] values) => _reader.GetValues(values);
			public override string GetName(int ordinal) => _reader.GetName(ordinal);
			public override int GetOrdinal(string name) => _reader.GetOrdinal(name);
			public override string GetDataTypeName(int ordinal) => _reader.GetDataTypeName(ordinal);
			public override Type GetFieldType(int ordinal) => _reader.GetFieldType(ordinal);
			public override T GetFieldValue<T>(int ordinal) => _reader.GetFieldValue<T>(ordinal);
			public override Task<T> GetFieldValueAsync<T>(int ordinal, CancellationToken cancellation) => _reader.GetFieldValueAsync<T>(ordinal, cancellation);
			public override TextReader GetTextReader(int ordinal) => _reader.GetTextReader(ordinal);
			public override IEnumerator GetEnumerator() => new DbEnumerator(this, false);
			public override DataTable GetSchemaTable() => _reader.GetSchemaTable();
			public override bool IsDBNull(int ordinal) => _reader.IsDBNull(ordinal);
			public override Task<bool> IsDBNullAsync(int ordinal, CancellationToken cancellation) => _reader.IsDBNullAsync(ordinal, cancellation);

			public override bool NextResult() => _reader.NextResult();
			public override Task<bool> NextResultAsync(CancellationToken cancellation) => _reader.NextResultAsync(cancellation);

			public override bool Read()
			{
				if(!_reader.Read())
					return false;

				_count++;
				return true;
			}

			public override async Task<bool> ReadAsync(CancellationToken cancellation)
			{
				if(!await _reader.ReadAsync(cancellation).ConfigureAwait(false))
					return false;

				_count++;
				return true;
			}

			public override void Close() => _reader.Close();
			public override Task CloseAsync() => _reader.CloseAsync();
			protected override void Dispose(bool disposing)
			{
				if(disposing)
					_reader.Dispose();

				base.Dispose(disposing);
			}
		}
	}

	private sealed class DuckDBConnectionAdapter(string connectionString) : DuckDBConnection(connectionString)
	{
		//DuckDB仅支持单一事务隔离模式，其驱动不接受显式指定的ADO.NET隔离级别
		protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => base.BeginTransaction();
	}
	#endregion
}
