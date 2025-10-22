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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Data;

public readonly struct DataType : IEquatable<DataType>
{
	#region 成员字段
	private readonly int _hashCode;
	#endregion

	#region 构造函数
	public DataType(string name)
	{
		this.Name = string.IsNullOrWhiteSpace(name) ? nameof(System.Data.DbType.String) : name.Trim();
		this.DbType = GetDbType(name);
		_hashCode = this.Name.ToLowerInvariant().GetHashCode();
	}
	#endregion

	#region 公共属性
	public string Name { get; }
	public System.Data.DbType DbType { get; }
	#endregion

	#region 重写方法
	public bool Equals(DataType other) => string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase);
	public override bool Equals(object obj) => base.Equals(obj);
	public override int GetHashCode() => _hashCode;
	public override string ToString() => this.Name;
	#endregion

	#region 符号重载
	public static implicit operator System.Data.DbType(DataType type) => type.DbType;
	public static bool operator ==(DataType left, DataType right) => left.Equals(right);
	public static bool operator !=(DataType left, DataType right) => !(left == right);
	#endregion

	#region 私有方法
	private static System.Data.DbType GetDbType(string type) => type.ToLowerInvariant() switch
	{
		"string" or "nvarchar" => System.Data.DbType.String,
		"nchar" or "stringfixed" or "stringfixedlength" => System.Data.DbType.StringFixedLength,
		"varchar" or "ansistring" => System.Data.DbType.AnsiString,
		"char" or "ansistringfixed" or "ansistringfixedlength" => System.Data.DbType.AnsiStringFixedLength,
		"short" or "int16" or "smallint" => System.Data.DbType.Int16,
		"int" or "int32" or "integer" => System.Data.DbType.Int32,
		"long" or "int64" or "bigint" => System.Data.DbType.Int64,
		"ushort" or "uint16" => System.Data.DbType.UInt16,
		"uint" or "uint32" => System.Data.DbType.UInt32,
		"ulong" or "uint64" => System.Data.DbType.UInt64,
		"byte" or "tiny" or "tinyint" => System.Data.DbType.Byte,
		"sbyte" => System.Data.DbType.SByte,
		"binary" or "byte[]" or "varbinary" => System.Data.DbType.Binary,
		"bool" or "boolean" => System.Data.DbType.Boolean,
		"money" or "currency" => System.Data.DbType.Currency,
		"decimal" => System.Data.DbType.Decimal,
		"double" => System.Data.DbType.Double,
		"float" or "single" => System.Data.DbType.Single,
		"date" => System.Data.DbType.Date,
		"time" => System.Data.DbType.Time,
		"datetime" or "timestamp" => System.Data.DbType.DateTime,
		"datetimeoffset" => System.Data.DbType.DateTimeOffset,
		"guid" or "uuid" => System.Data.DbType.Guid,
		"xml" => System.Data.DbType.Xml,
		"varnumeric" => System.Data.DbType.VarNumeric,
		"object" => System.Data.DbType.Object,
		_ => System.Data.DbType.Object,
	};
	#endregion
}
