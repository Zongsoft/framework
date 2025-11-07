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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Data library.
 *
 * The Zongsoft.Data is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Data;
using System.Reflection;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Zongsoft.Data.Common;

internal static class Utility
{
	#region 静态字段
	private static readonly ConcurrentDictionary<MemberInfo, TypeConverter> _converters = new ConcurrentDictionary<MemberInfo, TypeConverter>();
	#endregion

	public static DbType GetDbType(object value)
	{
		if(value == null)
			return DbType.Object;

		var type = value.GetType();

		if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
			type = Nullable.GetUnderlyingType(type);

		if(type.IsEnum)
			type = Enum.GetUnderlyingType(type);

		switch(Type.GetTypeCode(type))
		{
			case TypeCode.Boolean:
				return DbType.Boolean;
			case TypeCode.Byte:
				return DbType.Byte;
			case TypeCode.SByte:
				return DbType.SByte;
			case TypeCode.Char:
				return DbType.StringFixedLength;
			case TypeCode.DateTime:
				return ((DateTime)value).Kind == DateTimeKind.Utc ? DbType.DateTimeOffset : DbType.DateTime;
			case TypeCode.Decimal:
				return DbType.Decimal;
			case TypeCode.Double:
				return DbType.Double;
			case TypeCode.Int16:
				return DbType.Int16;
			case TypeCode.Int32:
				return DbType.Int32;
			case TypeCode.Int64:
				return DbType.Int64;
			case TypeCode.Single:
				return DbType.Single;
			case TypeCode.String:
				return DbType.String;
			case TypeCode.UInt16:
				return DbType.UInt16;
			case TypeCode.UInt32:
				return DbType.UInt32;
			case TypeCode.UInt64:
				return DbType.UInt64;
		}

		if(type == typeof(DateTimeOffset))
			return DbType.DateTimeOffset;
		else if(type == typeof(Guid))
			return DbType.Guid;
		else if(type == typeof(byte[]) || typeof(System.IO.Stream).IsAssignableFrom(type))
			return DbType.Binary;

		return DbType.Object;
	}

	public static TypeConverter GetConverter(this MemberInfo member)
	{
		if(member == null)
			return null;

		if(_converters.TryGetValue(member, out var converter))
			return converter;

		return _converters.GetOrAdd(member, Zongsoft.Common.Convert.GetTypeConverter(member));
	}

	internal static object GetMemberValue(ref object target, string name)
	{
		if(target is IModel model)
			return model.TryGetValue(name, out var value) ? value : null;

		if(target is IDictionary<string, object> generic)
			return generic.TryGetValue(name, out var value) ? value : null;

		if(target is IDictionary classic)
			return classic.Contains(name) ? classic[name] : null;

		return Reflection.Reflector.GetValue(ref target, name);
	}

	internal static object GetDefaultValue(DbType dbType, bool nullable = false)
	{
		if(nullable)
			return null;

		return dbType switch
		{
			DbType.Byte => (byte)0,
			DbType.SByte => (sbyte)0,
			DbType.Int16 => (short)0,
			DbType.UInt16 => (ushort)0,
			DbType.Int32 => 0,
			DbType.UInt32 => 0,
			DbType.Int64 => 0,
			DbType.UInt64 => 0,
			DbType.Single => 0,
			DbType.Double => 0,
			DbType.Decimal => 0m,
			DbType.Currency => 0m,
			DbType.Boolean => false,
			DbType.Date => DateTime.Today,
			DbType.Time => DateTime.Now,
			DbType.DateTime => DateTime.Now,
			DbType.DateTime2 => DateTime.Now,
			DbType.DateTimeOffset => DateTime.UtcNow,
			DbType.Guid => Guid.Empty,
			_ => null,
		};
	}

	internal static bool TryGetMemberValue(ref object target, string name, out object value)
	{
		if(target is IModel model)
			return model.TryGetValue(name, out value);

		if(target is IDictionary<string, object> generic)
			return generic.TryGetValue(name, out value);

		if(target is IDictionary classic)
		{
			if(classic.Contains(name))
			{
				value = classic[name];
				return true;
			}

			value = null;
			return false;
		}

		return Reflection.Reflector.TryGetValue(ref target, name, out value);
	}

	internal static bool TrySetMemberValue(ref object target, string name, object value)
	{
		if(target == null)
			return false;

		if(target is IModel model)
			return model.TrySetValue(name, value);

		if(target is IDictionary<string, object> generic)
		{
			generic[name] = value;
			return true;
		}

		if(target is IDictionary classic)
		{
			classic[name] = value;
			return true;
		}

		return Reflection.Reflector.TrySetValue(ref target, name, value);
	}

	internal static bool IsGenerateRequired(ref object data, string name)
	{
		//注意：数据为空必须返回真
		if(data == null)
			return true;

		return data switch
		{
			IModel model => model.HasChanges(name),
			IDataDictionary dictionary => dictionary.HasChanges(name),
			IDictionary<string, object> generic => generic.ContainsKey(name),
			IDictionary classic => classic.Contains(name),
			_ => true,
		};
	}

	internal static bool IsLinked(SchemaMember owner, Metadata.IDataEntitySimplexProperty property)
	{
		if(owner == null || owner.Token.Property.IsSimplex)
			return false;

		var links = ((Metadata.IDataEntityComplexProperty)owner.Token.Property).Links;

		for(int i = 0; i < links.Length; i++)
		{
			if(object.Equals(links[i].ForeignKey, property))
				return true;
		}

		return false;
	}
}
