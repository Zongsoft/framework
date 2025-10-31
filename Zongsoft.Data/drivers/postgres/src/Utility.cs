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

using Npgsql;
using NpgsqlTypes;

namespace Zongsoft.Data.PostgreSql;

internal static class Utility
{
	public static NpgsqlDbType GetDataType(this DbType dbType) => dbType switch
	{
		DbType.AnsiString => NpgsqlDbType.Varchar,
		DbType.AnsiStringFixedLength => NpgsqlDbType.Char,
		DbType.String => NpgsqlDbType.Varchar,
		DbType.StringFixedLength => NpgsqlDbType.Char,
		DbType.Binary => NpgsqlDbType.Bytea,
		DbType.Boolean => NpgsqlDbType.Boolean,
		DbType.Byte => NpgsqlDbType.Smallint,
		DbType.SByte => NpgsqlDbType.Smallint,
		DbType.Date => NpgsqlDbType.Date,
		DbType.DateTime => NpgsqlDbType.Timestamp,
		DbType.DateTime2 => NpgsqlDbType.TimestampTz,
		DbType.DateTimeOffset => NpgsqlDbType.TimestampTz,
		DbType.Guid => NpgsqlDbType.Uuid,
		DbType.Int16 => NpgsqlDbType.Smallint,
		DbType.Int32 => NpgsqlDbType.Integer,
		DbType.Int64 => NpgsqlDbType.Bigint,
		DbType.Time => NpgsqlDbType.Interval,
		DbType.UInt16 => NpgsqlDbType.Smallint,
		DbType.UInt32 => NpgsqlDbType.Integer,
		DbType.UInt64 => NpgsqlDbType.Bigint,
		DbType.Currency => NpgsqlDbType.Money,
		DbType.Decimal => NpgsqlDbType.Numeric,
		DbType.Double => NpgsqlDbType.Double,
		DbType.Single => NpgsqlDbType.Real,
		DbType.VarNumeric => NpgsqlDbType.Numeric,
		DbType.Xml => NpgsqlDbType.Xml,
		DbType.Object => NpgsqlDbType.Unknown,
		_ => throw new DataException($"Unsupported '{dbType}' data type."),
	};

	public static NpgsqlDbType GetDataType(this DataType type)
	{
		if(type.DbType == DbType.Object)
			return Enum.Parse<NpgsqlDbType>(type.Name, true);

		var dbType = type.DbType.GetDataType();
		return type.IsArray ? NpgsqlDbType.Array | dbType : dbType;
	}
}
