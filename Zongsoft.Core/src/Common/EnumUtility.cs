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
using System.Reflection;
using System.ComponentModel;

using Zongsoft.ComponentModel;

namespace Zongsoft.Common
{
	public static class EnumUtility
	{
		public static Type GetEnumType(Type type)
		{
			if(type == null)
				return null;

			if(type.IsEnum)
				return type;

			if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) && type.GetGenericArguments()[0].IsEnum)
				return type.GetGenericArguments()[0];

			return null;
		}

		public static string GetEnumAlias(this Enum enumValue)
		{
			if(TryGetEnumAlias(enumValue, out var alias))
				return alias;

			return null;
		}

		public static string GetEnumDescription(this Enum enumValue)
		{
			if(TryGetEnumDescription(enumValue, out var description))
				return description;

			return null;
		}

		/// <summary>
		/// 获取指定枚举项对应的<see cref="EnumEntry"/>描述对象。
		/// </summary>
		/// <param name="enumValue">要获取的枚举项。</param>
		/// <param name="underlyingType">是否将生成的 <seealso cref="EnumEntry"/> 元素的 <seealso cref="EnumEntry.Value"/> 属性值置为 enumType 参数对应的枚举项基类型值。</param>
		/// <returns>返回指定枚举值对应的<seealso cref="EnumEntry"/>对象。</returns>
		public static EnumEntry GetEnumEntry(this Enum enumValue, bool underlyingType = false)
		{
			if(enumValue == null)
				throw new ArgumentNullException(nameof(enumValue));

			if(TryGetEnumEntry(enumValue, underlyingType, out var entry))
				return entry;

			throw new ArgumentException($"The specified '{enumValue}' enumeration value is undefined.");
		}

		public static bool TryGetEnumAlias(this Enum enumValue, out string alias)
		{
			FieldInfo field = enumValue.GetType().GetField(enumValue.ToString());

			if(field != null)
			{
				var attribute = (AliasAttribute)Attribute.GetCustomAttribute(field, typeof(AliasAttribute));

				if(attribute != null && attribute.Alias != null)
				{
					alias = attribute.Alias;
					return true;
				}
			}

			alias = null;
			return false;
		}

		public static bool TryGetEnumDescription(this Enum enumValue, out string description)
		{
			FieldInfo field = enumValue.GetType().GetField(enumValue.ToString());

			if(field != null)
			{
				description = GetDescription(field);
				return description != null;
			}

			description = null;
			return false;
		}

		public static bool TryGetEnumEntry(this Enum enumValue, out EnumEntry entry)
		{
			return TryGetEnumEntry(enumValue, false, out entry);
		}

		public static bool TryGetEnumEntry(this Enum enumValue, bool underlyingType, out EnumEntry entry)
		{
			FieldInfo field = null;

			if(enumValue != null)
				field = enumValue.GetType().GetField(enumValue.ToString());

			if(field == null)
			{
				entry = new EnumEntry();
				return false;
			}

			entry = new EnumEntry(field.DeclaringType, field.Name,
								underlyingType ? System.Convert.ChangeType(enumValue, Enum.GetUnderlyingType(enumValue.GetType())) : enumValue,
								field.GetCustomAttribute<AliasAttribute>()?.Alias,
								GetDescription(field));

			return true;
		}

		/// <summary>
		/// 获取指定枚举的描述对象数组。
		/// </summary>
		/// <param name="enumType">要获取的枚举类型。</param>
		/// <param name="underlyingType">是否将生成的 <seealso cref="EnumEntry"/> 元素的 <seealso cref="EnumEntry.Value"/> 属性值置为 enumType 参数对应的枚举项基类型值。</param>
		/// <returns>返回的枚举描述对象数组。</returns>
		public static EnumEntry[] GetEnumEntries(Type enumType, bool underlyingType)
		{
			return GetEnumEntries(enumType, underlyingType, null, string.Empty);
		}

		/// <summary>
		/// 获取指定枚举的描述对象数组。
		/// </summary>
		/// <param name="enumType">要获取的枚举类型，可为<seealso cref="System.Nullable"/>类型。</param>
		/// <param name="underlyingType">是否将生成的 <seealso cref="EnumEntry"/> 元素的 <seealso cref="EnumEntry.Value"/> 属性值置为 enumType 参数对应的枚举项基类型值。</param>
		/// <param name="nullValue">如果参数<paramref name="enumType"/>为可空类型时，该空值对应的<seealso cref="EnumEntry.Value"/>属性的值。</param>
		/// <returns>返回的枚举描述对象数组。</returns>
		public static EnumEntry[] GetEnumEntries(Type enumType, bool underlyingType, object nullValue)
		{
			return GetEnumEntries(enumType, underlyingType, nullValue, string.Empty);
		}

		/// <summary>
		/// 获取指定枚举的描述对象数组。
		/// </summary>
		/// <param name="enumType">要获取的枚举类型，可为<seealso cref="System.Nullable"/>类型。</param>
		/// <param name="underlyingType">是否将生成的 <seealso cref="EnumEntry"/> 元素的 <seealso cref="EnumEntry.Value"/> 属性值置为 enumType 参数对应的枚举项基类型值。</param>
		/// <param name="nullValue">如果参数<paramref name="enumType"/>为可空类型时，该空值对应的<seealso cref="EnumEntry.Value"/>属性的值。</param>
		/// <param name="nullText">如果参数<paramref name="enumType"/>为可空类型时，该空值对应的<seealso cref="EnumEntry.Description"/>属性的值。</param>
		/// <returns>返回的枚举描述对象数组。</returns>
		public static EnumEntry[] GetEnumEntries(Type enumType, bool underlyingType, object nullValue, string nullText)
		{
			if(enumType == null)
				throw new ArgumentNullException(nameof(enumType));

			Type underlyingTypeOfNullable = Nullable.GetUnderlyingType(enumType);
			if(underlyingTypeOfNullable != null)
				enumType = underlyingTypeOfNullable;

			EnumEntry[] entries;
			int baseIndex = (underlyingTypeOfNullable == null) ? 0 : 1;
			var fields = enumType.GetFields(BindingFlags.Public | BindingFlags.Static);

			if(underlyingTypeOfNullable == null)
				entries = new EnumEntry[fields.Length];
			else
			{
				entries = new EnumEntry[fields.Length + 1];
				entries[0] = new EnumEntry(enumType, string.Empty, nullValue, nullText, nullText);
			}

			for(int i = 0; i < fields.Length; i++)
			{
				entries[baseIndex + i] = new EnumEntry(enumType, fields[i].Name,
													underlyingType ? System.Convert.ChangeType(fields[i].GetValue(null), Enum.GetUnderlyingType(enumType)) : fields[i].GetValue(null),
													fields[i].GetCustomAttribute<AliasAttribute>()?.Alias,
													GetDescription(fields[i]));
			}

			return entries;
		}

		#region 私有方法
		private static string GetDescription(FieldInfo field)
		{
			if(field == null)
				throw new ArgumentNullException(nameof(field));

			var attribute = field.GetCustomAttribute<DescriptionAttribute>();
			var resourceKey = attribute?.Description ?? $"{field.DeclaringType.Name}.{field.Name}";
			return GetResourceString(resourceKey, field.DeclaringType.Assembly) ?? attribute?.Description;
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private static string GetResourceString(string name, Assembly assembly)
		{
			var names = assembly.GetManifestResourceNames();

			for(int i = 0; i < names.Length; i++)
			{
				using var stream = assembly.GetManifestResourceStream(names[i]);
				using var resource = new System.Resources.ResourceSet(stream);

				var value = resource.GetString(name);

				if(value != null)
					return value;
			}

			return null;
		}
		#endregion
	}
}
