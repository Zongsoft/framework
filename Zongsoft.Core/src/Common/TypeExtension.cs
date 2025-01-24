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
using System.Linq;
using System.Reflection;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Common
{
	public static class TypeExtension
	{
		public static bool IsAssignableFrom(this Type type, Type instanceType) => IsAssignableFrom(type, instanceType, null);
		public static bool IsAssignableFrom(this Type type, Type instanceType, out IReadOnlyList<Type> genericTypes)
		{
			var list = new List<Type>();

			if(IsAssignableFrom(type, instanceType, t =>
			{
				list.Add(t);
				return true;
			}))
			{
				genericTypes = list;
				return true;
			}
			else
			{
				genericTypes = list;
				return false;
			}
		}

		/// <summary>
		/// 提供比<see cref="System.Type.IsAssignableFrom"/>加强的功能，支持对泛型定义接口或类的匹配。
		/// </summary>
		/// <param name="type">指定的接口或基类的类型。</param>
		/// <param name="instanceType">指定的实例类型。</param>
		/// <param name="genericMatch">当<paramref name="type"/>参数为泛型原型，则该委托表示找到的实现者的泛化类型，返回空表示继续后续匹配，否则结束匹配并将该委托的返回作为方法的结果。</param>
		/// <returns>如果当满足如下条件之一则返回真(true)：
		/// <list type="bullet">
		///		<item>
		///			<term>如果 <paramref name="type"/> 为泛型定义类型，则 <paramref name="instanceType"/> 实现的接口或基类中有从 <paramref name="type"/> 指定的泛型定义中泛化的版本。</term>
		///		</item>
		///		<item>
		///			<term>如果 <paramref name="type"/> 和当前 <paramref name="instanceType"/> 表示同一类型；</term>
		///		</item>
		///		<item>
		///			<term>当前 <paramref name="instanceType"/> 位于 <paramref name="type"/> 的继承层次结构中；</term>
		///		</item>
		///		<item>
		///			<term>当前 <paramref name="instanceType"/> 是 <paramref name="type"/> 实现的接口；</term>
		///		</item>
		///		<item>
		///			<term><paramref name="type"/> 是泛型类型参数且当前 <paramref name="instanceType"/> 表示 <paramref name="type"/> 的约束之一。</term>
		///		</item>
		/// </list>
		/// </returns>
		/// <remarks>
		///		<para>除了 <see cref="System.Type.IsAssignableFrom"/> 支持的特性外，增加了如下特性：</para>
		///		<example>
		///		<code>
		///		TypeExtension.IsAssignableFrom(typeof(IDictionary&lt;,&gt;), typeof(IDictionary&lt;string, object&gt;));	// true
		///		TypeExtension.IsAssignableFrom(typeof(IDictionary&lt;,&gt;), typeof(Dictionary&lt;string, object&gt;));	// true
		///		TypeExtension.IsAssignableFrom(typeof(Dictionary&lt;,&gt;), typeof(Dictioanry&lt;string, int&gt;));		//true
		///		
		///		public class MyNamedCollection&lt;T&gt; : Collection&lt;T&gt;, IDictionary&lt;string, T&gt;
		///		{
		///		}
		///		
		///		TypeExtension.IsAssignableFrom(typeof(IDictionary&lt;,&gt;), typeof(MyNamedCollection&lt;string, object&gt;)); //true
		///		</code>
		///		</example>
		/// </remarks>
		public static bool IsAssignableFrom(this Type type, Type instanceType, Func<Type, bool?> genericMatch)
		{
			if(type == null)
				throw new ArgumentNullException(nameof(type));

			if(instanceType == null)
				throw new ArgumentNullException(nameof(instanceType));

			if(type.IsGenericType && type.IsGenericTypeDefinition)
			{
				IEnumerable<Type> baseTypes;

				if(type.IsInterface)
				{
					if(instanceType.IsInterface)
					{
						baseTypes = new List<Type>(new Type[] { instanceType });
						((List<Type>)baseTypes).AddRange(instanceType.GetInterfaces());
					}
					else
					{
						baseTypes = instanceType.GetInterfaces();
					}
				}
				else
				{
					baseTypes = new List<Type>();

					var currentType = instanceType;

					while(currentType != typeof(object) &&
						  currentType != typeof(Enum) &&
						  currentType != typeof(Delegate) &&
						  currentType != typeof(ValueType))
					{
						((List<Type>)baseTypes).Add(currentType);
						currentType = currentType.BaseType;
					}
				}

				foreach(var baseType in baseTypes)
				{
					if(baseType.IsGenericType && baseType.GetGenericTypeDefinition() == type)
					{
						//如果没有指定泛型匹配回调则返回成功
						if(genericMatch == null)
							return true;

						//调用匹配委托
						var match = genericMatch(baseType);

						//如果匹配回调有确定的结果，则返回该结果，否则忽略该次回调并等待下一轮匹配
						if(match.HasValue)
							return match.Value;
					}
				}

				return false;
			}

			return type.IsAssignableFrom(instanceType);
		}

		public static string GetExplicitImplementationName(this Type interfaceType, string memberName)
		{
			if(interfaceType == null)
				throw new ArgumentNullException(nameof(interfaceType));

			if(string.IsNullOrEmpty(memberName))
				throw new ArgumentNullException(nameof(memberName));

			if(interfaceType.IsGenericTypeDefinition)
				return null;

			if(!interfaceType.IsInterface)
				return memberName;

			if(interfaceType.IsGenericType)
			{
				var text = new System.Text.StringBuilder(interfaceType.Namespace + "." + interfaceType.Name.Substring(0, interfaceType.Name.Length - 2) + "<", 128);
				var argumentTypes = interfaceType.GetGenericArguments();

				for(int i = 0; i < argumentTypes.Length; i++)
				{
					text.Append(argumentTypes[i].FullName);

					if(i < argumentTypes.Length - 1)
						text.Append(",");
				}

				text.Append(">." + memberName);
				return text.ToString();
			}

			return interfaceType.Namespace + "." + interfaceType.Name + "." + memberName;
		}

		public static bool IsGenericDefinition(this Type type, Type definitionType)
		{
			if(type == null || definitionType == null)
				return false;

			if(type.IsGenericType)
				return definitionType.IsGenericTypeDefinition && (type.IsGenericTypeDefinition ? type : type.GetGenericTypeDefinition()) == definitionType;

			return false;
		}

		public static bool IsGenericDefinition(this Type type, params Type[] definitionTypes)
		{
			if(type == null || definitionTypes == null || definitionTypes.Length == 0)
				return false;

			if(type.IsGenericType)
			{
				var definitionType = type.IsGenericTypeDefinition ? type : type.GetGenericTypeDefinition();

				for(var i = 0; i < definitionTypes.Length; i++)
				{
					if(definitionTypes[i].IsGenericTypeDefinition && definitionType == definitionTypes[i])
						return true;
				}
			}

			return false;
		}

		public static bool IsGenericDefinition(this Type type, IEnumerable<Type> definitionTypes)
		{
			if(type == null || definitionTypes == null)
				return false;

			if(type.IsGenericType)
			{
				var definitionType = type.IsGenericTypeDefinition ? type : type.GetGenericTypeDefinition();

				foreach(var definition in definitionTypes)
				{
					if(definition.IsGenericTypeDefinition && definitionType == definition)
						return true;
				}
			}

			return false;
		}

		public static bool IsEnumerable(this Type type) => typeof(IEnumerable).IsAssignableFrom(type) || IsAssignableFrom(typeof(IEnumerable<>), type);
		public static bool IsCollection(this Type type) => typeof(ICollection).IsAssignableFrom(type) || IsAssignableFrom(typeof(ICollection<>), type);
		public static bool IsList(this Type type) => typeof(IList).IsAssignableFrom(type) || IsAssignableFrom(typeof(IList<>), type);
		public static bool IsHashset(this Type type) => IsAssignableFrom(typeof(ISet<>), type);

		public static bool IsDictionary(this Type type) => typeof(IDictionary).IsAssignableFrom(type) || IsAssignableFrom(typeof(IDictionary<,>), type);
		public static bool IsDictionary(object instance, out IEnumerable<DictionaryEntry> entries)
		{
			entries = null;

			if(instance == null)
				return false;

			if(IsAssignableFrom(typeof(IDictionary<,>), instance.GetType(), out var argumentTypes))
			{
				var iteratorType = typeof(GenericDictionaryEnumerable<,>).MakeGenericType(argumentTypes.ToArray());
				entries = (IEnumerable<DictionaryEntry>)Activator.CreateInstance(iteratorType, new object[] { instance });
				return true;
			}

			if(instance is IDictionary dictionary)
			{
				entries = new ClassicDictionaryEnumerable(dictionary);
				return true;
			}

			return false;
		}

		public static bool IsScalarType(this Type type)
		{
			if(type == null)
				throw new ArgumentNullException(nameof(type));

			if(type.IsArray)
				return IsScalarType(type.GetElementType());

			if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
				return IsScalarType(type.GetGenericArguments()[0]);

			var result = type.IsPrimitive || type.IsEnum ||
			             type == typeof(string) || type == typeof(decimal) ||
			             type == typeof(DateTime) || type == typeof(TimeSpan) ||
			             type == typeof(DateTimeOffset) || type == typeof(Guid) || type == typeof(DBNull);

			if(result)
				return result;

			var converter = TypeDescriptor.GetConverter(type);
			return (converter != null && converter.CanConvertFrom(typeof(string)) && converter.CanConvertTo(typeof(string)));
		}

		public static bool IsInteger(this Type type)
		{
			if(type == null)
				throw new ArgumentNullException(nameof(type));

			var code = Type.GetTypeCode(type);

			return code == TypeCode.Byte || code == TypeCode.SByte ||
			       code == TypeCode.Int16 || code == TypeCode.UInt16 ||
			       code == TypeCode.Int32 || code == TypeCode.UInt32 ||
			       code == TypeCode.Int64 || code == TypeCode.UInt64;
		}

		public static bool IsNumeric(this Type type)
		{
			if(type == null)
				throw new ArgumentNullException(nameof(type));

			var code = Type.GetTypeCode(type);

			return TypeExtension.IsInteger(type) ||
				   code == TypeCode.Single || code == TypeCode.Double ||
				   code == TypeCode.Decimal || code == TypeCode.Char;
		}

		public static bool IsNullable(this Type type) => type != null && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
		public static bool IsNullable(this Type type, out Type underlyingType)
		{
			if(type != null && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
			{
				underlyingType = Nullable.GetUnderlyingType(type);
				return true;
			}

			underlyingType = null;
			return false;
		}

		public static Type GetListElementType(this Type type) => GetElementType(type, typeof(IList<>), typeof(IList));
		public static Type GetCollectionElementType(this Type type) => GetElementType(type, typeof(ICollection<>), typeof(ICollection));
		public static Type GetElementType(this Type type) => GetElementType(type, typeof(IEnumerable<>), typeof(IEnumerable));
		private static Type GetElementType(Type type, Type genericDefinitionInterface, Type collectionInterface)
		{
			if(type == null)
				throw new ArgumentNullException(nameof(type));

			if(type.IsArray)
				return type.GetElementType();

			if(type.IsInterface && type.IsGenericType && type.GetGenericTypeDefinition() == genericDefinitionInterface)
				return type.GetGenericArguments()[0];

			var collectionType = type.GetInterfaces().FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == genericDefinitionInterface);

			if(collectionType != null)
				return collectionType.GetGenericArguments()[0];

			//如果指定的类型是一个非泛型集合接口的实现则返回其元素类型为object类型
			if(collectionInterface.IsAssignableFrom(type))
				return typeof(object);

			return null;
		}

		public static object GetDefaultValue(this Type type)
		{
			if(type == typeof(DBNull))
				return DBNull.Value;

			if(type == null || type.IsClass || type.IsInterface || type.IsNullable())
				return null;

			if(type.IsEnum)
			{
				var attribute = (DefaultValueAttribute)Attribute.GetCustomAttribute(type, typeof(DefaultValueAttribute), true);

				if(attribute != null && attribute.Value != null)
				{
					if(attribute.Value is string)
						return Enum.Parse(type, attribute.Value.ToString(), true);
					else
						return Enum.ToObject(type, attribute.Value);
				}

				Array values = Enum.GetValues(type);

				if(values.Length > 0)
					return System.Convert.ChangeType(values.GetValue(0), type);
			}

			return Type.GetTypeCode(type) switch
			{
				TypeCode.Boolean => false,
				TypeCode.Char => '\0',
				TypeCode.Byte => (byte)0,
				TypeCode.SByte => (sbyte)0,
				TypeCode.Int16 => (short)0,
				TypeCode.Int32 => 0,
				TypeCode.Int64 => 0L,
				TypeCode.UInt16 => (ushort)0,
				TypeCode.UInt32 => 0U,
				TypeCode.UInt64 => 0UL,
				TypeCode.Single => 0f,
				TypeCode.Double => 0d,
				TypeCode.Decimal => 0m,
				TypeCode.DateTime => DateTime.MinValue,
				TypeCode.DBNull => DBNull.Value,
				TypeCode.String => null,
				_ => Activator.CreateInstance(type),
			};
		}

		public static MethodInfo GetMethod(this Type type, string name, Type[] parameterTypes) => GetMethod(type, name, parameterTypes, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
		public static MethodInfo GetMethod(this Type type, string name, Type[] parameterTypes, BindingFlags bindingFlags)
		{
			var members = type.FindMembers(MemberTypes.Method, bindingFlags, (m, _) =>
			{
				if(parameterTypes == null || parameterTypes.Length == 0)
					return m.Name == name;

				if(m.Name == name)
				{
					var parameters = ((MethodInfo)m).GetParameters();

					if(parameters.Length == parameterTypes.Length)
					{
						for(int i = 0; i < parameters.Length; i++)
						{
							if(parameters[i].ParameterType != parameterTypes[i])
								return false;
						}

						return true;
					}
				}

				return false;
			}, null);

			if(members == null || members.Length == 0)
				return null;

			if(members.Length > 1)
				throw new System.Reflection.AmbiguousMatchException();

			return (MethodInfo)members[0];
		}

		#region 嵌套子类
		private class ClassicDictionaryEnumerable : IEnumerable<DictionaryEntry>
		{
			private readonly IDictionary _dictionary;

			public ClassicDictionaryEnumerable(IDictionary dictionary)
			{
				_dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
			}

			public IEnumerator<DictionaryEntry> GetEnumerator()
			{
				return new DictionaryIterator(_dictionary.GetEnumerator());
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return this.GetEnumerator();
			}

			public class DictionaryIterator : IEnumerator<DictionaryEntry>
			{
				private readonly IDictionaryEnumerator _enumerator;

				public DictionaryIterator(IDictionaryEnumerator enumerator)
				{
					_enumerator = enumerator;
				}

				public DictionaryEntry Current
				{
					get => _enumerator.Entry;
				}

				object IEnumerator.Current
				{
					get => _enumerator.Current;
				}

				public void Dispose()
				{
					if(_enumerator is IDisposable disposable)
						disposable.Dispose();
				}

				public bool MoveNext()
				{
					return _enumerator.MoveNext();
				}

				public void Reset()
				{
					_enumerator.Reset();
				}
			}
		}

		private class GenericDictionaryEnumerable<TKey, TValue> : IEnumerable<DictionaryEntry>
		{
			private readonly IDictionary<TKey, TValue> _dictionary;

			public GenericDictionaryEnumerable(object dictionary)
			{
				_dictionary = (IDictionary<TKey, TValue>)dictionary;
			}

			public IEnumerator<DictionaryEntry> GetEnumerator()
			{
				return new DictionaryIterator(_dictionary.GetEnumerator());
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return this.GetEnumerator();
			}

			public class DictionaryIterator : IEnumerator<DictionaryEntry>
			{
				private readonly IEnumerator<KeyValuePair<TKey, TValue>> _enumerator;

				public DictionaryIterator(IEnumerator<KeyValuePair<TKey, TValue>> enumerator)
				{
					_enumerator = enumerator;
				}

				public DictionaryEntry Current
				{
					get
					{
						var current = _enumerator.Current;
						return new DictionaryEntry(current.Key, current.Value);
					}
				}

				object IEnumerator.Current
				{
					get => _enumerator.Current;
				}

				public void Dispose()
				{
					_enumerator.Dispose();
				}

				public bool MoveNext()
				{
					return _enumerator.MoveNext();
				}

				public void Reset()
				{
					_enumerator.Reset();
				}
			}
		}
		#endregion
	}
}
