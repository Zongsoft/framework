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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.Data;

using Zongsoft.Data.Common;

namespace Zongsoft.Data.PostgreSql;

partial class PostgreSqlDriver
{
	private sealed class PostgreSqlGetter : IDataRecordGetter
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
