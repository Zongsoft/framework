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
 * This file is part of Zongsoft.Data library.
 *
 * The Zongsoft.Data is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Data;
using System.Data.Common;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Data.Common;

partial class DataSession
{
	private class SessionReader : DbDataReader
	{
		#region 成员字段
		private int _closed;
		private ConnectionLease _lease;
		private readonly DbDataReader _reader;
		#endregion

		#region 构造函数
		internal SessionReader(DbDataReader reader, ConnectionLease lease)
		{
			_reader = reader ?? throw new ArgumentNullException(nameof(reader));
			_lease = lease ?? throw new ArgumentNullException(nameof(lease));
		}
		#endregion

		#region 重写属性
		public override object this[int ordinal] => _reader[ordinal];
		public override object this[string name] => _reader[name];
		public override int Depth => _reader.Depth;
		public override int FieldCount => _reader.FieldCount;
		public override bool HasRows => _reader.HasRows;
		public override bool IsClosed => _reader.IsClosed;
		public override int RecordsAffected => _reader.RecordsAffected;
		public override int VisibleFieldCount => _reader.VisibleFieldCount;
		#endregion

		#region 重写方法
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
		public override Task<T> GetFieldValueAsync<T>(int ordinal, CancellationToken cancellationToken) => _reader.GetFieldValueAsync<T>(ordinal, cancellationToken);
		public override TextReader GetTextReader(int ordinal) => _reader.GetTextReader(ordinal);
		public override IEnumerator GetEnumerator() => _reader.GetEnumerator();
		public override bool IsDBNull(int ordinal) => _reader.IsDBNull(ordinal);
		public override bool NextResult() => _reader.NextResult();
		public override bool Read() => _reader.Read();
		#endregion

		#region 关闭方法
		public override void Close()
		{
			if(Interlocked.Exchange(ref _closed, 1) != 0)
				return;

			try
			{
				//关闭数据读取器
				if(!_reader.IsClosed)
					_reader.Close();
			}
			finally
			{
				//获取并清空当前读取器持有的数据连接租约
				var lease = Interlocked.Exchange(ref _lease, null);

				//释放当前读取器持有的数据连接租约
				lease?.Dispose();
			}
		}

		public override async Task CloseAsync()
		{
			if(Interlocked.Exchange(ref _closed, 1) != 0)
				return;

			try
			{
				//关闭数据读取器
				if(!_reader.IsClosed)
					await _reader.CloseAsync();
			}
			finally
			{
				//获取并清空当前读取器持有的数据连接租约
				var lease = Interlocked.Exchange(ref _lease, null);

				//释放当前读取器持有的数据连接租约
				if(lease != null)
					await lease.DisposeAsync().ConfigureAwait(false);
			}
		}
		#endregion
	}
}
