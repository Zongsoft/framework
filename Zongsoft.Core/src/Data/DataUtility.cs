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

namespace Zongsoft.Data;

public static class DataUtility
{
	public static Type AsType(this DbType dbType) => dbType switch
	{
		DbType.String or DbType.StringFixedLength or DbType.AnsiString or DbType.AnsiStringFixedLength or DbType.Xml => typeof(string),
		DbType.Int16 => typeof(short),
		DbType.Int32 => typeof(int),
		DbType.Int64 => typeof(long),
		DbType.UInt16 => typeof(ushort),
		DbType.UInt32 => typeof(uint),
		DbType.UInt64 => typeof(ulong),
		DbType.Byte => typeof(byte),
		DbType.SByte => typeof(sbyte),
		DbType.Binary => typeof(byte[]),
		DbType.Boolean => typeof(bool),
		DbType.Currency or DbType.Decimal => typeof(decimal),
		DbType.Double or DbType.VarNumeric => typeof(double),
		DbType.Single => typeof(float),
		DbType.Date or DbType.Time or DbType.DateTime or DbType.DateTime2 => typeof(DateTime),
		DbType.DateTimeOffset => typeof(DateTimeOffset),
		DbType.Guid => typeof(Guid),
		DbType.Object => typeof(object),
		_ => throw new NotSupportedException($"Invalid DbType value:'{dbType}'."),
	};

	public static bool IsNumeric(this DbType dbType) => IsInteger(dbType) || IsFloating(dbType);

	public static bool IsFloating(this DbType dbType) => dbType switch
	{
		DbType.Single => true,
		DbType.Double => true,
		DbType.Decimal => true,
		DbType.Currency => true,
		_ => false,
	};

	public static bool IsInteger(this DbType dbType) => dbType switch
	{
		DbType.Byte => true,
		DbType.SByte => true,
		DbType.Int16 => true,
		DbType.Int32 => true,
		DbType.Int64 => true,
		DbType.UInt16 => true,
		DbType.UInt32 => true,
		DbType.UInt64 => true,
		_ => false,
	};
}
