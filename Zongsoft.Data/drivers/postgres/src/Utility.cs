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
using System.Collections;
using System.Collections.Generic;

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

	public static bool IsArrayType(this NpgsqlDbType dbType, out NpgsqlDbType elementType)
	{
		if((dbType & NpgsqlDbType.Array) == NpgsqlDbType.Array)
		{
			elementType = dbType & ~NpgsqlDbType.Array;
			return true;
		}

		elementType = dbType;
		return false;
	}

	public static bool IsRangeType(this NpgsqlDbType dbType, out NpgsqlDbType elementType)
	{
		if((dbType & NpgsqlDbType.Range) == NpgsqlDbType.Range)
		{
			elementType = dbType & ~NpgsqlDbType.Range;
			return true;
		}

		elementType = dbType;
		return false;
	}

	public static bool IsMultirangeType(this NpgsqlDbType dbType, out NpgsqlDbType elementType)
	{
		if((dbType & NpgsqlDbType.Multirange) == NpgsqlDbType.Multirange)
		{
			elementType = dbType & ~NpgsqlDbType.Multirange;
			return true;
		}

		elementType = dbType;
		return false;
	}

	public static Type AsType(NpgsqlDbType dbType)
	{
		if(IsArrayType(dbType, out var elementType))
		{
			var elementClrType = AsType(elementType);
			return elementClrType.MakeArrayType();
		}

		if(IsRangeType(dbType, out elementType))
		{
			var elementClrType = AsType(elementType);
			return typeof(NpgsqlRange<>).MakeGenericType(elementClrType);
		}

		if(IsMultirangeType(dbType, out elementType))
		{
			var elementClrType = AsType(elementType);
			return typeof(NpgsqlRange<>).MakeGenericType(elementClrType).MakeArrayType();
		}

		var type = GetTypeFormDbType(dbType);

		if(type != null)
			return type;

		if(dbType == NpgsqlDbType.Cidr)
		{
			#if NET8_0_OR_GREATER
			return typeof(NpgsqlCidr);
			#else
			return typeof(ValueTuple<System.Net.IPAddress, int>);
			#endif
		}

		/*
		 * Refcursor, Int2Vector, Regtype, PgLsn
		 * Geometry, Geography
		 * LTree, LQuery, LTxtQuery
		 */
		return typeof(object);

		static Type GetTypeFormDbType(NpgsqlDbType dbType) => dbType switch
		{
			NpgsqlDbType.Bigint => typeof(long),
			NpgsqlDbType.Double => typeof(double),
			NpgsqlDbType.Integer => typeof(int),
			NpgsqlDbType.Numeric => typeof(decimal),
			NpgsqlDbType.Real => typeof(float),
			NpgsqlDbType.Smallint => typeof(short),
			NpgsqlDbType.Money => typeof(decimal),
			NpgsqlDbType.Boolean => typeof(bool),
			NpgsqlDbType.Bytea => typeof(byte[]),

			NpgsqlDbType.Bit => typeof(BitArray),
			NpgsqlDbType.Varbit => typeof(BitArray),

			NpgsqlDbType.Box => typeof(NpgsqlBox),
			NpgsqlDbType.Circle => typeof(NpgsqlCircle),
			NpgsqlDbType.Line => typeof(NpgsqlLine),
			NpgsqlDbType.LSeg => typeof(NpgsqlLSeg),
			NpgsqlDbType.Path => typeof(NpgsqlPath),
			NpgsqlDbType.Point => typeof(NpgsqlPoint),
			NpgsqlDbType.Polygon => typeof(NpgsqlPolygon),

			NpgsqlDbType.Char => typeof(string),
			NpgsqlDbType.Text => typeof(string),
			NpgsqlDbType.Varchar => typeof(string),
			NpgsqlDbType.Name => typeof(string),
			NpgsqlDbType.Citext => typeof(string),   // Extension type
			NpgsqlDbType.InternalChar => typeof(byte),

			NpgsqlDbType.Date => typeof(DateTime),
			NpgsqlDbType.Time => typeof(TimeOnly),
			NpgsqlDbType.Timestamp => typeof(DateTime),
			NpgsqlDbType.TimestampTz => typeof(DateTimeOffset),
			NpgsqlDbType.Interval => typeof(TimeSpan),
			NpgsqlDbType.TimeTz => typeof(DateTimeOffset),

			NpgsqlDbType.Uuid => typeof(Guid),
			NpgsqlDbType.Xml => typeof(string),

			NpgsqlDbType.Json => typeof(string),
			NpgsqlDbType.Jsonb => typeof(string),
			NpgsqlDbType.JsonPath => typeof(string),

			NpgsqlDbType.Inet => typeof(System.Net.IPAddress),
			NpgsqlDbType.MacAddr => typeof(System.Net.NetworkInformation.PhysicalAddress),
			NpgsqlDbType.MacAddr8 => typeof(System.Net.NetworkInformation.PhysicalAddress),

			NpgsqlDbType.TsVector => typeof(NpgsqlTsVector),
			NpgsqlDbType.TsQuery => typeof(NpgsqlTsQuery),
			NpgsqlDbType.Regconfig => typeof(object),

			NpgsqlDbType.Oid => typeof(uint),
			NpgsqlDbType.Xid => typeof(uint),
			NpgsqlDbType.Xid8 => typeof(ulong),
			NpgsqlDbType.Cid => typeof(uint),
			NpgsqlDbType.Tid => typeof(NpgsqlTid),
			NpgsqlDbType.Oidvector => typeof(uint[]),

			NpgsqlDbType.Hstore => typeof(Dictionary<string, string>), // Extension type
			NpgsqlDbType.Unknown => typeof(object),
			_ => null,
		};
	}
}
