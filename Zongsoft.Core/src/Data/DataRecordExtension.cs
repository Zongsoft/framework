﻿/*
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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Data;
using System.Collections.Generic;

namespace Zongsoft.Data;

/// <summary>
/// 关于数据记录接口的扩展方法类。
/// </summary>
public static class DataRecordExtension
{
	#region 静态构造
	static DataRecordExtension()
	{
		RecordGetterTemplate<bool>.Get = new Func<IDataRecord, int, bool>((record, ordinal) => record.GetBoolean(ordinal));
		RecordGetterTemplate<bool?>.Get = new Func<IDataRecord, int, bool?>((record, ordinal) => record.IsDBNull(ordinal) ? null : (bool?)record.GetBoolean(ordinal));

		RecordGetterTemplate<char>.Get = new Func<IDataRecord, int, char>((record, ordinal) => record.GetChar(ordinal));
		RecordGetterTemplate<char?>.Get = new Func<IDataRecord, int, char?>((record, ordinal) => record.IsDBNull(ordinal) ? null : (char?)record.GetChar(ordinal));
		RecordGetterTemplate<char[]>.Get = new Func<IDataRecord, int, char[]>((record, ordinal) => record.IsDBNull(ordinal) ? null : record.GetValue(ordinal) as char[]);
		RecordGetterTemplate<IEnumerable<char>>.Get = new Func<IDataRecord, int, IEnumerable<char>>((record, ordinal) => record.IsDBNull(ordinal) ? null : record.GetValue(ordinal) as IEnumerable<char>);

		RecordGetterTemplate<byte>.Get = new Func<IDataRecord, int, byte>((record, ordinal) => record.GetByte(ordinal));
		RecordGetterTemplate<byte?>.Get = new Func<IDataRecord, int, byte?>((record, ordinal) => record.IsDBNull(ordinal) ? null : (byte?)record.GetByte(ordinal));
		RecordGetterTemplate<byte[]>.Get = new Func<IDataRecord, int, byte[]>((record, ordinal) => record.IsDBNull(ordinal) ? null : record.GetValue(ordinal) as byte[]);
		RecordGetterTemplate<IEnumerable<byte>>.Get = new Func<IDataRecord, int, IEnumerable<byte>>((record, ordinal) => record.IsDBNull(ordinal) ? null : record.GetValue(ordinal) as IEnumerable<byte>);

		RecordGetterTemplate<sbyte>.Get = new Func<IDataRecord, int, sbyte>((record, ordinal) => (sbyte)record.GetValue(ordinal));
		RecordGetterTemplate<sbyte?>.Get = new Func<IDataRecord, int, sbyte?>((record, ordinal) => record.IsDBNull(ordinal) ? null : (sbyte?)record.GetValue(ordinal));
		RecordGetterTemplate<short>.Get = new Func<IDataRecord, int, short>((record, ordinal) => record.GetInt16(ordinal));
		RecordGetterTemplate<short?>.Get = new Func<IDataRecord, int, short?>((record, ordinal) => record.IsDBNull(ordinal) ? null : (short?)record.GetInt16(ordinal));
		RecordGetterTemplate<ushort>.Get = new Func<IDataRecord, int, ushort>((record, ordinal) => (ushort)record.GetValue(ordinal));
		RecordGetterTemplate<ushort?>.Get = new Func<IDataRecord, int, ushort?>((record, ordinal) => record.IsDBNull(ordinal) ? null : (ushort?)record.GetValue(ordinal));
		RecordGetterTemplate<int>.Get = new Func<IDataRecord, int, int>((record, ordinal) => record.GetInt32(ordinal));
		RecordGetterTemplate<int?>.Get = new Func<IDataRecord, int, int?>((record, ordinal) => record.IsDBNull(ordinal) ? null : (int?)record.GetInt32(ordinal));
		RecordGetterTemplate<uint>.Get = new Func<IDataRecord, int, uint>((record, ordinal) => (uint)record.GetValue(ordinal));
		RecordGetterTemplate<uint?>.Get = new Func<IDataRecord, int, uint?>((record, ordinal) => record.IsDBNull(ordinal) ? null : (uint?)record.GetValue(ordinal));
		RecordGetterTemplate<long>.Get = new Func<IDataRecord, int, long>((record, ordinal) => record.GetInt64(ordinal));
		RecordGetterTemplate<long?>.Get = new Func<IDataRecord, int, long?>((record, ordinal) => record.IsDBNull(ordinal) ? null : (long?)record.GetInt64(ordinal));
		RecordGetterTemplate<ulong>.Get = new Func<IDataRecord, int, ulong>((record, ordinal) => (ulong)record.GetValue(ordinal));
		RecordGetterTemplate<ulong?>.Get = new Func<IDataRecord, int, ulong?>((record, ordinal) => record.IsDBNull(ordinal) ? null : (ulong?)record.GetValue(ordinal));

		RecordGetterTemplate<decimal>.Get = new Func<IDataRecord, int, decimal>((record, ordinal) => record.GetDecimal(ordinal));
		RecordGetterTemplate<decimal?>.Get = new Func<IDataRecord, int, decimal?>((record, ordinal) => record.IsDBNull(ordinal) ? null : (decimal?)record.GetDecimal(ordinal));
		RecordGetterTemplate<double>.Get = new Func<IDataRecord, int, double>((record, ordinal) => record.GetDouble(ordinal));
		RecordGetterTemplate<double?>.Get = new Func<IDataRecord, int, double?>((record, ordinal) => record.IsDBNull(ordinal) ? null : (double?)record.GetDouble(ordinal));
		RecordGetterTemplate<float>.Get = new Func<IDataRecord, int, float>((record, ordinal) => record.GetFloat(ordinal));
		RecordGetterTemplate<float?>.Get = new Func<IDataRecord, int, float?>((record, ordinal) => record.IsDBNull(ordinal) ? null : (float?)record.GetFloat(ordinal));

		RecordGetterTemplate<DateTime>.Get = new Func<IDataRecord, int, DateTime>((record, ordinal) => record.GetDateTime(ordinal));
		RecordGetterTemplate<DateTime?>.Get = new Func<IDataRecord, int, DateTime?>((record, ordinal) => record.IsDBNull(ordinal) ? null : (DateTime?)record.GetDateTime(ordinal));
		RecordGetterTemplate<DateTimeOffset>.Get = new Func<IDataRecord, int, DateTimeOffset>((record, ordinal) => (DateTimeOffset)record.GetDateTime(ordinal));
		RecordGetterTemplate<DateTimeOffset?>.Get = new Func<IDataRecord, int, DateTimeOffset?>((record, ordinal) => record.IsDBNull(ordinal) ? null : (DateTimeOffset?)record.GetDateTime(ordinal));

		RecordGetterTemplate<Guid>.Get = new Func<IDataRecord, int, Guid>((record, ordinal) => record.GetGuid(ordinal));
		RecordGetterTemplate<Guid?>.Get = new Func<IDataRecord, int, Guid?>((record, ordinal) => record.IsDBNull(ordinal) ? null : (Guid?)record.GetGuid(ordinal));

		RecordGetterTemplate<string>.Get = new Func<IDataRecord, int, string>((record, ordinal) => record.IsDBNull(ordinal) ? null : record.GetString(ordinal));
		RecordGetterTemplate<object>.Get = new Func<IDataRecord, int, object>((record, ordinal) => record.IsDBNull(ordinal) ? null : record.GetValue(ordinal));
	}
	#endregion

	#region 扩展方法
	public static T GetValue<T>(this IDataRecord record, int ordinal)
	{
		return RecordGetterTemplate<T>.Get(record, ordinal);
	}
	#endregion

	#region 私有子类
	private static class RecordGetterTemplate<T>
	{
		public static Func<IDataRecord, int, T> Get;
	}
	#endregion
}
