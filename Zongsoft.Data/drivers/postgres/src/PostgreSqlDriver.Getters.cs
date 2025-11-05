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

using Zongsoft.Data.Common;

namespace Zongsoft.Data.PostgreSql;

partial class PostgreSqlDriver
{
	private sealed class ByteGetter : IDataRecordGetter<byte>
	{
		public byte GetValue(IDataRecord record, int ordinal) => (byte)record.GetInt16(ordinal);
	}
	private sealed class SByteGetter : IDataRecordGetter<sbyte>
	{
		public sbyte GetValue(IDataRecord record, int ordinal) => (sbyte)record.GetInt16(ordinal);
	}

	private sealed class UInt16Getter : IDataRecordGetter<ushort>
	{
		public ushort GetValue(IDataRecord record, int ordinal) => (ushort)record.GetInt16(ordinal);
	}
	private sealed class UInt32Getter : IDataRecordGetter<uint>
	{
		public uint GetValue(IDataRecord record, int ordinal) => (uint)record.GetInt32(ordinal);
	}
	private sealed class UInt64Getter : IDataRecordGetter<ulong>
	{
		public ulong GetValue(IDataRecord record, int ordinal) => (ulong)record.GetInt64(ordinal);
	}
}
