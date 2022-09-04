/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2022 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Data.ClickHouse library.
 *
 * The Zongsoft.Data.ClickHouse is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data.ClickHouse is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data.ClickHouse library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Data;

using Zongsoft.Data.Metadata;

namespace Zongsoft.Data.ClickHouse
{
	public static class ClickHouseUtility
	{
		public static string GetDataType(IDataEntityProperty property) => GetDataType(property as IDataEntitySimplexProperty);
		public static string GetDataType(IDataEntitySimplexProperty property) => property == null ? throw new DataAccessException("ClickHouse", -1) : GetDataType(property.Type, property.Length, property.Precision, property.Scale, property.Nullable);
		public static string GetDataType(DbType dbType, int length, int precision, int scale, bool nullable) => dbType switch
		{
			DbType.Byte => nullable ? "Nullable(UInt8)" : "UInt8",
			DbType.SByte => nullable ? "Nullable(Int8)" : "Int8",
			DbType.Int16 => nullable ? "Nullable(Int16)" : "Int16",
			DbType.Int32 => nullable ? "Nullable(Int32)" : "Int32",
			DbType.Int64 => nullable ? "Nullable(Int64)" : "Int64",
			DbType.UInt16 => nullable ? "Nullable(UInt16)" : "UInt16",
			DbType.UInt32 => nullable ? "Nullable(UInt32)" : "UInt32",
			DbType.UInt64 => nullable ? "Nullable(UInt64)" : "UInt64",
			DbType.Single => nullable ? "Nullable(Float32)" : "Float32",
			DbType.Double => nullable ? "Nullable(Float64)" : "Float64",
			DbType.Decimal or DbType.Currency => nullable ? $"Nullable(Decimal({precision},{scale}))" : $"Decimal({precision},{scale})",
			DbType.String => nullable ? "Nullable(String)" : "String",
			DbType.AnsiString => nullable ? "Nullable(String)" : "String",
			DbType.StringFixedLength => nullable ? $"Nullable(FixedString({length}))" : $"FixedString({length})",
			DbType.AnsiStringFixedLength => nullable ? $"Nullable(FixedString({length}))" : $"FixedString({length})",
			DbType.DateTime or DbType.DateTime2 => nullable ? "Nullable(DateTime)" : "DateTime",
			DbType.DateTimeOffset => nullable ? "Nullable(DateTime)" : "DateTime",
			DbType.Date => nullable ? "Nullable(Date)" : "Date",
			DbType.Time => nullable ? "Nullable(Time)" : "Time",
			DbType.Guid => nullable ? "Nullable(UUID)" : "UUID",
			DbType.Boolean => nullable ? "Nullable(UInt8)" : "UInt8",
			DbType.Binary => nullable ? "Nullable(Array(UInt8))" : "Array(UInt8)",
			DbType.Xml => nullable ? "Nullable(String)" : "String",
			_ => nullable ? "Nullable(Object)" : "Object",
		};
	}
}
