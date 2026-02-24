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

using Zongsoft.Data.Common;

namespace Zongsoft.Data.SQLite;

partial class SQLiteDriver
{
	private sealed class SQLiteGetter : IDataRecordGetter
	{
		public T GetValue<T>(IDataRecord record, int ordinal)
		{
			switch(Type.GetTypeCode(typeof(T)))
			{
				case TypeCode.Byte:
					var valueByte = (byte)record.GetInt16(ordinal);
					return System.Runtime.CompilerServices.Unsafe.As<byte, T>(ref valueByte);
				case TypeCode.SByte:
					var valueSByte = (sbyte)record.GetInt16(ordinal);
					return System.Runtime.CompilerServices.Unsafe.As<sbyte, T>(ref valueSByte);
				case TypeCode.UInt16:
					var valueUInt16 = (ushort)record.GetInt16(ordinal);
					return System.Runtime.CompilerServices.Unsafe.As<ushort, T>(ref valueUInt16);
				case TypeCode.UInt32:
					var valueUInt32 = (uint)record.GetInt32(ordinal);
					return System.Runtime.CompilerServices.Unsafe.As<uint, T>(ref valueUInt32);
				case TypeCode.UInt64:
					var valueUInt64 = (ulong)record.GetInt64(ordinal);
					return System.Runtime.CompilerServices.Unsafe.As<ulong, T>(ref valueUInt64);
				default:
					return record.GetValue<T>(ordinal);
			}
		}
	}
}
