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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Data.TDengine library.
 *
 * The Zongsoft.Data.TDengine is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data.TDengine is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data.TDengine library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;

using TDengine.Driver;

namespace Zongsoft.Data.TDengine;

partial class TDengineDriver
{
	private sealed class Reader(IRowsAsync rows, Connection connection) : DbDataReader
	{
		#region 成员字段
		private readonly IRowsAsync _rows = rows;
		private readonly Connection _connection = connection;
		private Task _disposing;
		#endregion

		#region 公共成员
		public override object this[int ordinal] => this.GetValue(ordinal);
		public override object this[string name] => this.GetValue(this.GetOrdinal(name));
		public override int Depth => 0;
		public override int FieldCount => _rows.FieldCount;
		public override bool HasRows => _rows.HasRows;
		public override int RecordsAffected => _rows.AffectRows;
		public override bool IsClosed => _disposing != null;

		public override bool Read() => throw new NotSupportedException(Properties.Resources.Driver_AsyncRequired_Message);
		public override Task<bool> ReadAsync(CancellationToken cancellation)
		{
			ObjectDisposedException.ThrowIf(this.IsClosed, this);
			cancellation.ThrowIfCancellationRequested();
			return _rows.ReadAsync(cancellation);
		}

		public override bool NextResult() => throw new NotSupportedException(Properties.Resources.Driver_AsyncRequired_Message);
		public override Task<bool> NextResultAsync(CancellationToken cancellation)
		{
			ObjectDisposedException.ThrowIf(this.IsClosed, this);
			cancellation.ThrowIfCancellationRequested();
			return Task.FromResult(false);
		}

		public override IEnumerator GetEnumerator() => throw new NotSupportedException(Properties.Resources.Driver_AsyncRequired_Message);
		public override string GetName(int ordinal) => _rows.GetName(ordinal);
		public override int GetOrdinal(string name) => _rows.GetOrdinal(name);
		public override Type GetFieldType(int ordinal) => _rows.GetFieldType(ordinal);
		public override string GetDataTypeName(int ordinal) => _rows.GetDataTypeName(ordinal);
		public override object GetValue(int ordinal) => _rows.GetValue(ordinal);
		public override int GetValues(object[] values) => _rows.GetValues(values);
		public override bool IsDBNull(int ordinal) => _rows.IsDBNull(ordinal);
		public override bool GetBoolean(int ordinal) => _rows.GetBoolean(ordinal);
		public override byte GetByte(int ordinal) => _rows.GetByte(ordinal);
		public override char GetChar(int ordinal) => _rows.GetChar(ordinal);
		public override short GetInt16(int ordinal) => _rows.GetInt16(ordinal);
		public override int GetInt32(int ordinal) => _rows.GetInt32(ordinal);
		public override long GetInt64(int ordinal) => _rows.GetInt64(ordinal);
		public override float GetFloat(int ordinal) => _rows.GetFloat(ordinal);
		public override double GetDouble(int ordinal) => _rows.GetDouble(ordinal);
		public override decimal GetDecimal(int ordinal) => _rows.GetDecimal(ordinal);
		public override DateTime GetDateTime(int ordinal) => _rows.GetDateTime(ordinal);
		public override string GetString(int ordinal) => _rows.GetString(ordinal);
		public override Guid GetGuid(int ordinal) => this.GetFieldValue<Guid>(ordinal);
		public override long GetBytes(int ordinal, long offset, byte[] buffer, int bufferOffset, int length) => _rows.GetBytes(ordinal, offset, buffer, bufferOffset, length);
		public override long GetChars(int ordinal, long offset, char[] buffer, int bufferOffset, int length) => _rows.GetChars(ordinal, offset, buffer, bufferOffset, length);

		public override DataTable GetSchemaTable()
		{
			var table = new DataTable();
			if(this.FieldCount == 0)
				return table;

			table.Columns.Add(SchemaTableColumn.ColumnName, typeof(string));
			table.Columns.Add(SchemaTableColumn.ColumnOrdinal, typeof(int));
			table.Columns.Add(SchemaTableColumn.ColumnSize, typeof(int));
			table.Columns.Add(SchemaTableColumn.DataType, typeof(Type));
			table.Columns.Add("DataTypeName", typeof(string));
			table.Columns.Add(SchemaTableColumn.NumericPrecision, typeof(int));
			table.Columns.Add(SchemaTableColumn.NumericScale, typeof(int));

			for(int i = 0; i < this.FieldCount; i++)
				table.Rows.Add(this.GetName(i), i, _rows.GetFieldSize(i), this.GetFieldType(i), this.GetDataTypeName(i), _rows.GetFieldPrecision(i), _rows.GetFieldScale(i));

			return table;
		}

		public override void Close()
		{
			if(_disposing is not { IsCompleted: true })
				throw new NotSupportedException(Properties.Resources.Driver_AsyncRequired_Message);
		}

		public override Task CloseAsync() => _disposing ??= this.DisposeCoreAsync();
		public override ValueTask DisposeAsync() => new(this.CloseAsync());
		private async Task DisposeCoreAsync()
		{
			try
			{
				await _rows.DisposeAsync().ConfigureAwait(false);
			}
			finally
			{
				if(_connection != null)
					await _connection.CloseAsync().ConfigureAwait(false);

				GC.SuppressFinalize(this);
			}
		}

		protected override void Dispose(bool disposing)
		{
			if(disposing)
				this.Close();
		}
		#endregion
	}
}
