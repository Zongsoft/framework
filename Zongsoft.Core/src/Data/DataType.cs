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
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Zongsoft.Data;

public sealed partial class DataType : IEquatable<DataType>
{
	#region 单例字段
	public static readonly DataType AnsiString = new(System.Data.DbType.AnsiString);
	public static readonly DataType AnsiStringFixedLength = new(System.Data.DbType.AnsiStringFixedLength);
	public static readonly DataType String = new(System.Data.DbType.String);
	public static readonly DataType StringFixedLength = new(System.Data.DbType.StringFixedLength);
	public static readonly DataType Binary = new(System.Data.DbType.Binary);
	public static readonly DataType Byte = new(System.Data.DbType.Byte);
	public static readonly DataType Boolean = new(System.Data.DbType.Boolean);
	public static readonly DataType Currency = new(System.Data.DbType.Currency);
	public static readonly DataType Date = new(System.Data.DbType.Date);
	public static readonly DataType DateTime = new(System.Data.DbType.DateTime);
	public static readonly DataType Decimal = new(System.Data.DbType.Decimal);
	public static readonly DataType Double = new(System.Data.DbType.Double);
	public static readonly DataType Guid = new(System.Data.DbType.Guid);
	public static readonly DataType Int16 = new(System.Data.DbType.Int16);
	public static readonly DataType Int32 = new(System.Data.DbType.Int32);
	public static readonly DataType Int64 = new(System.Data.DbType.Int64);
	public static readonly DataType SByte = new(System.Data.DbType.SByte);
	public static readonly DataType Single = new(System.Data.DbType.Single);
	public static readonly DataType Time = new(System.Data.DbType.Time);
	public static readonly DataType UInt16 = new(System.Data.DbType.UInt16);
	public static readonly DataType UInt32 = new(System.Data.DbType.UInt32);
	public static readonly DataType UInt64 = new(System.Data.DbType.UInt64);
	public static readonly DataType DateTime2 = new(System.Data.DbType.DateTime2);
	public static readonly DataType DateTimeOffset = new(System.Data.DbType.DateTimeOffset);
	public static readonly DataType Xml = new(System.Data.DbType.Xml);
	public static readonly DataType Object = new(System.Data.DbType.Object);
	public static readonly DataType VarNumeric = new(System.Data.DbType.VarNumeric);
	public static readonly DataType Json = new(nameof(Json), System.Data.DbType.String, false);
	#endregion

	#region 静态变量
	#if NET7_0_OR_GREATER
	[GeneratedRegex(@"(?<name>\w+)\s*(?<array>\[\s*\])?", RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace)]
	private static partial Regex GetRegex();
	private static readonly Regex _regex = GetRegex();
	#else
	private static readonly Regex _regex = new(@"(?<name>\w+)\s*(?<array>\[\s*\])?", RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);
	#endif

	private static readonly Dictionary<string, DataType> _types = new(StringComparer.OrdinalIgnoreCase)
	{
		{ "string", String },
		{ "nvarchar", String },

		{ "nchar", StringFixedLength },
		{ "stringfixed", StringFixedLength },
		{ "stringfixedlength", StringFixedLength },

		{ "varchar", AnsiString },
		{ "ansistring", AnsiString },

		{ "char", AnsiStringFixedLength },
		{ "ansistringfixed", AnsiStringFixedLength },
		{ "ansistringfixedlength", AnsiStringFixedLength },

		{ "short", Int16 },
		{ "int16", Int16 },
		{ "smallint", Int16 },

		{ "int", Int32 },
		{ "int32", Int32 },
		{ "integer", Int32 },

		{ "long", Int64 },
		{ "int64", Int64 },
		{ "bigint", Int64 },

		{ "ushort", UInt16 },
		{ "uint16", UInt16 },

		{ "uint", UInt32 },
		{ "uint32", UInt32 },

		{ "ulong", UInt64 },
		{ "uint64", UInt64 },

		{ "byte", Byte },
		{ "tinyint", Byte },
		{ "sbyte", SByte },

		{ "byte[]", Binary },
		{ "binary", Binary },
		{ "varbinary", Binary },

		{ "bool", Boolean },
		{ "boolean", Boolean },

		{ "money", Currency },
		{ "currency", Currency },

		{ "float", Single },
		{ "single", Single },
		{ "double", Double },
		{ "decimal", Decimal },

		{ "date", Date },
		{ "time", Time },
		{ "datetime", DateTime },
		{ "timestamp", DateTime },
		{ "datetimeoffset", DateTimeOffset },

		{ "guid", Guid },
		{ "uuid", Guid },

		{ "xml", Xml },
		{ "json", Json },
		{ "varnumeric", VarNumeric },
		{ "object", Object },
	};
	#endregion

	#region 私有构造
	private DataType(System.Data.DbType type)
	{
		this.DbType = type;
		this.Name = type.ToString().ToLowerInvariant();
		this.IsArray = false;
	}

	private DataType(string name, System.Data.DbType dbType, bool isArray)
	{
		this.Name = name.ToLowerInvariant();
		this.DbType = dbType;
		this.IsArray = isArray;
	}
	#endregion

	#region 公共属性
	public string Name { get; }
	public System.Data.DbType DbType { get; }
	public bool IsArray { get; }
	#endregion

	#region 静态方法
	public static DataType Get(string name)
	{
		if(string.IsNullOrWhiteSpace(name))
			return String;

		var isArray = IsArrayType(name.Trim(), out name);
		var key = isArray ? $"{name}[]" : name;

		if(_types.TryGetValue(key, out var type))
			return type;

		lock(_types)
		{
			if(!_types.TryGetValue(key, out type))
				_types.Add(key, type = new(name, GetDbType(name), isArray));

			return type;
		}
	}
	#endregion

	#region 重写方法
	public bool Equals(DataType other) => other is not null && this.IsArray == other.IsArray && string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase);
	public override bool Equals(object obj) => this.Equals(obj is DataType);
	public override int GetHashCode() => HashCode.Combine(this.Name, this.IsArray);
	public override string ToString() => this.IsArray ? $"{this.Name}[]" : this.Name;
	#endregion

	#region 符号重载
	public static bool operator ==(DataType left, DataType right) => left.Equals(right);
	public static bool operator !=(DataType left, DataType right) => !(left == right);

	public static implicit operator System.Data.DbType(DataType type) => type.DbType;
	public static implicit operator DataType(System.Data.DbType type) => type switch
	{
		System.Data.DbType.AnsiString => AnsiString,
		System.Data.DbType.AnsiStringFixedLength => AnsiStringFixedLength,
		System.Data.DbType.String => String,
		System.Data.DbType.StringFixedLength => StringFixedLength,
		System.Data.DbType.Binary => Binary,
		System.Data.DbType.Byte => Byte,
		System.Data.DbType.Boolean => Boolean,
		System.Data.DbType.Currency => Currency,
		System.Data.DbType.Date => Date,
		System.Data.DbType.DateTime => DateTime,
		System.Data.DbType.Decimal => Decimal,
		System.Data.DbType.Double => Double,
		System.Data.DbType.Guid => Guid,
		System.Data.DbType.Int16 => Int16,
		System.Data.DbType.Int32 => Int32,
		System.Data.DbType.Int64 => Int64,
		System.Data.DbType.SByte => SByte,
		System.Data.DbType.Single => Single,
		System.Data.DbType.Time => Time,
		System.Data.DbType.UInt16 => UInt16,
		System.Data.DbType.UInt32 => UInt32,
		System.Data.DbType.UInt64 => UInt64,
		System.Data.DbType.DateTime2 => DateTime2,
		System.Data.DbType.DateTimeOffset => DateTimeOffset,
		System.Data.DbType.Xml => Xml,
		System.Data.DbType.Object => Object,
		System.Data.DbType.VarNumeric => VarNumeric,
		_ => throw new ArgumentException($"Invalid DbType value: '{type}'."),
	};
	#endregion

	#region 私有方法
	private static bool IsArrayType(string text, out string name)
	{
		if(string.IsNullOrEmpty(text))
		{
			name = null;
			return false;
		}

		var match = _regex.Match(text);
		name = match.Groups["name"].Value;
		return match.Success && match.Groups["array"].Success;
	}

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
		"byte" or "tinyint" => System.Data.DbType.Byte,
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
