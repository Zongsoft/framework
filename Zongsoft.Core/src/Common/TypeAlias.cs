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
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Common;

public static partial class TypeAlias
{
	#region 静态字段
	private static readonly AliasCollection _aliases = new();
	#endregion

	#region 静态属性
	public static AliasCollection Aliases => _aliases;
	#endregion

	#region 别名方法
	public static string GetAlias(this Type type)
	{
		if(type == null)
			return null;

		var elementType = type.IsArray ? type.GetElementType() : type;
		elementType = elementType.IsNullable(out var underlyingType) ? underlyingType : elementType;
		var code = Type.GetTypeCode(elementType);

		string alias;
		bool aliased;

		if(!elementType.IsEnum && code != TypeCode.Object)
		{
			alias = code.ToString();
			aliased = true;
		}
		else
		{
			var prototype = elementType.IsGenericType ? elementType.GetGenericTypeDefinition() : elementType;
			aliased = _aliases.TryGet(prototype, out alias);

			if(!aliased)
			{
				var typeName = elementType.IsGenericType ?
					elementType.Name[..(elementType.Name.Length - GetDigits(elementType.GenericTypeArguments.Length) - 1)] :
					elementType.Name;

				aliased = string.Equals(elementType.Namespace, nameof(System));
				alias = aliased ? typeName : $"{elementType.Namespace}.{typeName}";
			}

			if(elementType.IsGenericType)
			{
				alias += '<';

				var arguments = elementType.GenericTypeArguments;
				if(arguments != null && arguments.Length > 0)
				{
					for(int i = 0; i < arguments.Length; i++)
					{
						if(i > 0)
							alias += ", ";

						alias += GetAlias(arguments[i]);
					}
				}

				alias += '>';
			}
		}

		if(underlyingType != null)
			alias += '?';

		if(type.IsArray)
			alias += "[]";

		return aliased ? alias : $"{alias}@{elementType.Assembly.GetName().Name}";
	}

	/// <summary>获取指定整数的十进制数的位数。</summary>
	/// <param name="integer">指定的整数值。</param>
	/// <returns>返回指定整数的十进制的位数。</returns>
	private static int GetDigits(int integer)
	{
		if(integer < 10)
			return 1;
		if(integer < 100)
			return 2;
		if(integer < 1000)
			return 3;
		if(integer < 10000)
			return 4;
		if(integer < 100000)
			return 5;
		if(integer < 1000000)
			return 6;
		if(integer < 10000000)
			return 7;
		if(integer < 100000000)
			return 8;
		if(integer < 1000000000)
			return 9;

		return 10;
	}
	#endregion

	#region 解析方法
	public static bool TryParse(string typeName, out Type result) => TryParse(typeName, true, out result);
	public static bool TryParse(string typeName, bool ignoreCase, out Type result)
	{
		result = Parse(typeName, false, ignoreCase);
		return result != null;
	}

	public static Type Parse(string typeName, bool throwException = true, bool ignoreCase = true) => Parse(typeName, null, null, throwException, ignoreCase);
	public static Type Parse(string typeName, Func<AssemblyName, Assembly> assemblyResolver, Func<Assembly, string, bool, Type> typeResolver, bool throwException = true, bool ignoreCase = true)
	{
		if(string.IsNullOrEmpty(typeName))
			return throwException ? throw new ArgumentNullException(nameof(typeName)) : null;

		if(_aliases.TryGet(typeName, out var type) && !type.ContainsGenericParameters)
			return type;

		if(typeName.IndexOfAny(['`', '=']) > 0)
			return Type.GetType(typeName, assemblyResolver, typeResolver, throwException, ignoreCase);

		var token = throwException ? ParseCore(typeName, message => throw new InvalidOperationException(message)) : ParseCore(typeName);
		return string.IsNullOrEmpty(token.Type) ? null : token.ToType(assemblyResolver, typeResolver, throwException);
	}
	#endregion

	#region 嵌套子类
	public sealed class AliasCollection : ICollection<KeyValuePair<string, TypeInfo>>
	{
		#region 成员字段
		private readonly Dictionary<string, TypeInfo> _types = new(StringComparer.OrdinalIgnoreCase)
		{
			{ nameof(Object), typeof(object).GetTypeInfo() },
			{ nameof(DBNull), typeof(DBNull).GetTypeInfo() },
			{ nameof(String), typeof(string).GetTypeInfo() },
			{ nameof(Boolean), typeof(bool).GetTypeInfo() },
			{ nameof(Guid), typeof(Guid).GetTypeInfo() },
			{ nameof(Char), typeof(char).GetTypeInfo() },
			{ nameof(Byte), typeof(byte).GetTypeInfo() },
			{ nameof(SByte), typeof(sbyte).GetTypeInfo() },
			{ nameof(Int16), typeof(short).GetTypeInfo() },
			{ nameof(UInt16), typeof(ushort).GetTypeInfo() },
			{ nameof(Int32), typeof(int).GetTypeInfo() },
			{ nameof(UInt32), typeof(uint).GetTypeInfo() },
			{ nameof(Int64), typeof(long).GetTypeInfo() },
			{ nameof(UInt64), typeof(ulong).GetTypeInfo() },
			{ nameof(Single), typeof(float).GetTypeInfo() },
			{ nameof(Double), typeof(double).GetTypeInfo() },
			{ nameof(Decimal), typeof(decimal).GetTypeInfo() },
			{ nameof(TimeSpan), typeof(TimeSpan).GetTypeInfo() },
			{ nameof(DateOnly), typeof(DateOnly).GetTypeInfo() },
			{ nameof(TimeOnly), typeof(TimeOnly).GetTypeInfo() },
			{ nameof(DateTime), typeof(DateTime).GetTypeInfo() },
			{ nameof(DateTimeOffset), typeof(DateTimeOffset).GetTypeInfo() },

			{ "void", typeof(void).GetTypeInfo() },
			{ "bool", typeof(bool).GetTypeInfo() },
			{ "uuid", typeof(Guid).GetTypeInfo() },
			{ "float", typeof(float).GetTypeInfo() },
			{ "short", typeof(short).GetTypeInfo() },
			{ "ushort", typeof(ushort).GetTypeInfo() },
			{ "int", typeof(int).GetTypeInfo() },
			{ "integer", typeof(int).GetTypeInfo() },
			{ "uint", typeof(uint).GetTypeInfo() },
			{ "long", typeof(long).GetTypeInfo() },
			{ "ulong", typeof(ulong).GetTypeInfo() },
			{ "money", typeof(decimal).GetTypeInfo() },
			{ "currency", typeof(decimal).GetTypeInfo() },
			{ "date", typeof(DateOnly).GetTypeInfo() },
			{ "time", typeof(TimeOnly).GetTypeInfo() },
			{ "timestamp", typeof(DateTimeOffset).GetTypeInfo() },
			{ "binary", typeof(byte[]).GetTypeInfo() },
			{ "range", typeof(Zongsoft.Data.Range<>).GetTypeInfo() },
			{ "mixture", typeof(Zongsoft.Data.Mixture<>).GetTypeInfo() },

			{ nameof(IList), typeof(IList<>).GetTypeInfo() },
			{ nameof(IEnumerable), typeof(IEnumerable<>).GetTypeInfo() },
			{ nameof(ICollection), typeof(ICollection<>).GetTypeInfo() },
			{ nameof(IDictionary), typeof(IDictionary<,>).GetTypeInfo() },

			{ "List", typeof(List<>).GetTypeInfo() },
			{ "ISet", typeof(ISet<>).GetTypeInfo() },
			{ "Hashset", typeof(HashSet<>).GetTypeInfo() },
			{ "Dictionary", typeof(Dictionary<,>).GetTypeInfo() },

			{ "IReadOnlySet", typeof(IReadOnlySet<>).GetTypeInfo() },
			{ "IReadOnlyList", typeof(IReadOnlyList<>).GetTypeInfo() },
			{ "IReadOnlyCollection", typeof(IReadOnlyCollection<>).GetTypeInfo() },
			{ "IReadOnlyDictionary", typeof(IReadOnlyDictionary<,>).GetTypeInfo() },
		};

		private readonly Dictionary<Type, string> _aliases = new()
		{
			{ typeof(void), "void" },
			{ typeof(object), nameof(Object) },
			{ typeof(Guid), nameof(Guid) },
			{ typeof(TimeSpan), nameof(TimeSpan) },
			{ typeof(DateOnly), "Date" },
			{ typeof(TimeOnly), "Time" },
			{ typeof(DateTimeOffset), "Timestamp" },
			{ typeof(Zongsoft.Data.Range<>), "Range" },
			{ typeof(Zongsoft.Data.Mixture<>), "Mixture" },

			{ typeof(IList<>), nameof(IList) },
			{ typeof(IEnumerable<>), nameof(IEnumerable) },
			{ typeof(ICollection<>), nameof(ICollection) },
			{ typeof(IDictionary<,>), nameof(IDictionary) },

			{ typeof(List<>), "List" },
			{ typeof(ISet<>), "ISet" },
			{ typeof(HashSet<>), "Hashset" },
			{ typeof(Dictionary<,>), "Dictionary" },

			{ typeof(IReadOnlySet<>), "IReadOnlySet" },
			{ typeof(IReadOnlyList<>), "IReadOnlyList" },
			{ typeof(IReadOnlyCollection<>), "IReadOnlyCollection" },
			{ typeof(IReadOnlyDictionary<,>), "IReadOnlyDictionary" },
		};
		#endregion

		#region 公共属性
		public int Count => _types.Count;
		public TypeInfo this[string name] => name != null && _types.TryGetValue(name, out var type) ? type : null;
		public string this[Type type] => type != null && _aliases.TryGetValue(type, out var alias) ? alias : null;
		#endregion

		#region 公共方法
		public bool Map(string name, Type type)
		{
			if(string.IsNullOrEmpty(name) || type == null || type.IsPrimitive)
				return false;

			return _types.TryAdd(name, type.GetTypeInfo()) && _aliases.TryAdd(type, name);
		}

		internal bool TryGet(string name, out TypeInfo type) => _types.TryGetValue(name, out type);
		internal bool TryGet(Type type, out string alias) => _aliases.TryGetValue(type, out alias);
		#endregion

		#region 显式实现
		bool ICollection<KeyValuePair<string, TypeInfo>>.IsReadOnly => false;
		void ICollection<KeyValuePair<string, TypeInfo>>.Add(KeyValuePair<string, TypeInfo> item) => this.Map(item.Key, item.Value);
		void ICollection<KeyValuePair<string, TypeInfo>>.Clear() => throw new NotSupportedException();
		bool ICollection<KeyValuePair<string, TypeInfo>>.Remove(KeyValuePair<string, TypeInfo> item) => throw new NotSupportedException();
		bool ICollection<KeyValuePair<string, TypeInfo>>.Contains(KeyValuePair<string, TypeInfo> item) => _types.ContainsKey(item.Key);
		void ICollection<KeyValuePair<string, TypeInfo>>.CopyTo(KeyValuePair<string, TypeInfo>[] array, int arrayIndex) => ((ICollection<KeyValuePair<string, TypeInfo>>)_types).CopyTo(array, arrayIndex);
		#endregion

		#region 枚举遍历
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		public IEnumerator<KeyValuePair<string, TypeInfo>> GetEnumerator() => _types.GetEnumerator();
		#endregion
	}
	#endregion
}
