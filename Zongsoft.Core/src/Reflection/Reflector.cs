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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Reflection
{
	public static class Reflector
	{
		#region 获取方法
		public static object GetValue(this FieldInfo field, ref object target)
		{
			if(field == null)
				throw new ArgumentNullException(nameof(field));

			return field.GetGetter().Invoke(ref target);
		}

		public static object GetValue(this PropertyInfo property, ref object target, params object[] parameters)
		{
			if(property == null)
				throw new ArgumentNullException(nameof(property));

			if(!property.CanRead)
				throw new InvalidOperationException($"The '{property.Name}' property does not support reading.");

			return property.GetGetter().Invoke(ref target, parameters);
		}

		public static object GetValue(this MemberInfo member, ref object target, params object[] parameters)
		{
			if(member == null)
				throw new ArgumentNullException(nameof(member));

			switch(member.MemberType)
			{
				case MemberTypes.Field:
					return ((FieldInfo)member).GetGetter().Invoke(ref target);
				case MemberTypes.Property:
					var property = (PropertyInfo)member;

					if(!property.CanRead)
						throw new InvalidOperationException($"The '{property.Name}' property does not support reading.");

					return property.GetGetter().Invoke(ref target, parameters);
			}

			throw new NotSupportedException($"The {member.MemberType.ToString()} of member that is not supported.");
		}

		public static object GetValue(ref object target, string name, params object[] parameters)
		{
			if(target == null)
				throw new ArgumentNullException(nameof(target));

			var type = (target as Type) ?? target.GetType();
			var members = string.IsNullOrEmpty(name) ?
				type.GetDefaultMembers() :
				type.GetMember(name, MemberTypes.Property | MemberTypes.Field, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.IgnoreCase);

			if(members == null || members.Length == 0)
				throw new ArgumentException($"A member named '{name}' does not exist in the '{type.FullName}'.");

			return GetValue(members[0], ref target, parameters);
		}

		public static object GetValue<T>(this FieldInfo field, ref T target)
		{
			if(field == null)
				throw new ArgumentNullException(nameof(field));

			return field.GetGetter<T>().Invoke(ref target);
		}

		public static object GetValue<T>(this PropertyInfo property, ref T target, params object[] parameters)
		{
			if(property == null)
				throw new ArgumentNullException(nameof(property));

			if(!property.CanRead)
				throw new InvalidOperationException($"The '{property.Name}' property does not support reading.");

			return property.GetGetter<T>().Invoke(ref target, parameters);
		}

		public static object GetValue<T>(this MemberInfo member, ref T target, params object[] parameters)
		{
			if(member == null)
				throw new ArgumentNullException(nameof(member));

			switch(member.MemberType)
			{
				case MemberTypes.Field:
					return ((FieldInfo)member).GetGetter<T>().Invoke(ref target);
				case MemberTypes.Property:
					var property = (PropertyInfo)member;

					if(!property.CanRead)
						throw new InvalidOperationException($"The '{property.Name}' property does not support reading.");

					return property.GetGetter<T>().Invoke(ref target, parameters);
			}

			throw new NotSupportedException($"The {member.MemberType.ToString()} of member that is not supported.");
		}

		public static object GetValue<T>(ref T target, string name, params object[] parameters)
		{
			if(target == null)
				throw new ArgumentNullException(nameof(target));

			var type = target.GetType();
			var members = string.IsNullOrEmpty(name) ?
				type.GetDefaultMembers() :
				type.GetMember(name, MemberTypes.Property | MemberTypes.Field, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.IgnoreCase);

			if(members == null || members.Length == 0)
				throw new ArgumentException($"A member named '{name}' does not exist in the '{type.FullName}'.");

			return GetValue(members[0], ref target, parameters);
		}

		public static bool TryGetValue(this FieldInfo field, ref object target, out object value)
		{
			if(field != null)
			{
				value = field.GetGetter().Invoke(ref target);
				return true;
			}

			value = null;
			return false;
		}

		public static bool TryGetValue(this PropertyInfo property, ref object target, out object value)
		{
			return TryGetValue(property, ref target, Array.Empty<object>(), out value);
		}

		public static bool TryGetValue(this PropertyInfo property, ref object target, object[] parameters, out object value)
		{
			return PropertyInfoExtension.TryGetValue(property, ref target, parameters, out value);
		}

		public static bool TryGetValue(this MemberInfo member, ref object target, out object value)
		{
			return TryGetValue(member, ref target, Array.Empty<object>(), out value);
		}

		public static bool TryGetValue(this MemberInfo member, ref object target, object[] parameters, out object value)
		{
			value = null;

			if(member == null)
				return false;

			switch(member.MemberType)
			{
				case MemberTypes.Field:
					value = ((FieldInfo)member).GetGetter().Invoke(ref target);
					return true;
				case MemberTypes.Property:
					var property = (PropertyInfo)member;
					return PropertyInfoExtension.TryGetValue(property, ref target, parameters, out value);
			}

			return false;
		}

		public static bool TryGetValue(ref object target, string name, out object value)
		{
			return TryGetValue(ref target, name, Array.Empty<object>(), out value);
		}

		public static bool TryGetValue(ref object target, string name, object[] parameters, out object value)
		{
			value = null;

			if(target == null)
				return false;

			var type = (target as Type) ?? target.GetType();
			var members = string.IsNullOrEmpty(name) ?
				type.GetDefaultMembers() :
				type.GetMember(name, MemberTypes.Property | MemberTypes.Field, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.IgnoreCase);

			if(members == null || members.Length == 0)
				return false;

			return TryGetValue(members[0], ref target, parameters, out value);
		}

		public static bool TryGetValue<T>(this FieldInfo field, ref T target, out object value)
		{
			if(field != null)
			{
				value = field.GetGetter<T>().Invoke(ref target);
				return true;
			}

			value = null;
			return false;
		}

		public static bool TryGetValue<T>(this PropertyInfo property, ref T target, out object value)
		{
			return TryGetValue(property, ref target, Array.Empty<object>(), out value);
		}

		public static bool TryGetValue<T>(this PropertyInfo property, ref T target, object[] parameters, out object value)
		{
			return PropertyInfoExtension.TryGetValue<T>(property, ref target, parameters, out value);
		}

		public static bool TryGetValue<T>(this MemberInfo member, ref T target, out object value)
		{
			return TryGetValue<T>(member, ref target, Array.Empty<object>(), out value);
		}

		public static bool TryGetValue<T>(this MemberInfo member, ref T target, object[] parameters, out object value)
		{
			value = null;

			if(member == null)
				return false;

			switch(member.MemberType)
			{
				case MemberTypes.Field:
					value = ((FieldInfo)member).GetGetter<T>().Invoke(ref target);
					return true;
				case MemberTypes.Property:
					var property = (PropertyInfo)member;
					return PropertyInfoExtension.TryGetValue(property, ref target, parameters, out value);
			}

			return false;
		}

		public static bool TryGetValue<T>(ref T target, string name, out object value)
		{
			return TryGetValue<T>(ref target, name, Array.Empty<object>(), out value);
		}

		public static bool TryGetValue<T>(ref T target, string name, object[] parameters, out object value)
		{
			if(target == null)
				throw new ArgumentNullException(nameof(target));

			value = null;

			var type = target.GetType();
			var members = string.IsNullOrEmpty(name) ?
				type.GetDefaultMembers() :
				type.GetMember(name, MemberTypes.Property | MemberTypes.Field, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.IgnoreCase);

			if(members == null || members.Length == 0)
				return false;

			return TryGetValue<T>(members[0], ref target, parameters, out value);
		}
		#endregion

		#region 设置方法
		public static bool SetValue(this FieldInfo field, ref object target, object value) => SetValue(field, ref target, _ => value);
		public static bool SetValue(this FieldInfo field, ref object target, Func<Type, object> valueFactory)
		{
			if(field == null)
				throw new ArgumentNullException(nameof(field));

			if(field.IsInitOnly)
				return false;

			field.GetSetter().Invoke(ref target, valueFactory?.Invoke(field.FieldType));
			return true;
		}

		public static bool SetValue(this PropertyInfo property, ref object target, object value, params object[] parameters) => SetValue(property, ref target, _ => value, parameters);
		public static bool SetValue(this PropertyInfo property, ref object target, Func<Type, object> valueFactory, params object[] parameters)
		{
			if(property == null)
				throw new ArgumentNullException(nameof(property));

			if(property.CanWrite)
				property.GetSetter().Invoke(ref target, valueFactory?.Invoke(property.PropertyType), parameters);

			return property.CanWrite;
		}

		public static bool SetValue(this MemberInfo member, ref object target, object value, params object[] parameters) => SetValue(member, ref target, _ => value, parameters);
		public static bool SetValue(this MemberInfo member, ref object target, Func<Type, object> valueFactory, params object[] parameters)
		{
			if(member == null)
				throw new ArgumentNullException(nameof(member));

			switch(member.MemberType)
			{
				case MemberTypes.Field:
					var field = (FieldInfo)member;

					if(field.IsInitOnly)
						return false;

					field.GetSetter().Invoke(ref target, valueFactory?.Invoke(field.FieldType));
					return true;
				case MemberTypes.Property:
					var property = (PropertyInfo)member;

					if(property.CanWrite)
						property.GetSetter().Invoke(ref target, valueFactory?.Invoke(property.PropertyType), parameters);

					return property.CanWrite;
				default:
					throw new NotSupportedException($"The {member.MemberType} of member that is not supported.");
			}
		}

		public static bool SetValue(ref object target, string name, object value, params object[] parameters) => SetValue(ref target, name, _ => value, parameters);
		public static bool SetValue(ref object target, string name, Func<Type, object> valueFactory, params object[] parameters)
		{
			if(target == null)
				throw new ArgumentNullException(nameof(target));

			var type = (target as Type) ?? target.GetType();
			var members = string.IsNullOrEmpty(name) ?
				type.GetDefaultMembers() :
				type.GetMember(name, MemberTypes.Property | MemberTypes.Field, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.IgnoreCase);

			if(members == null || members.Length == 0)
				throw new ArgumentException($"A member named '{name}' does not exist in the '{type.FullName}'.");

			return SetValue(members[0], ref target, valueFactory?.Invoke(members[0].GetMemberType()), parameters);
		}

		public static bool SetValue<T>(this FieldInfo field, ref T target, object value) => SetValue<T>(field, ref target, _ => value);
		public static bool SetValue<T>(this FieldInfo field, ref T target, Func<Type, object> valueFactory)
		{
			if(field == null)
				throw new ArgumentNullException(nameof(field));

			if(field.IsInitOnly)
				return false;

			field.GetSetter<T>().Invoke(ref target, valueFactory?.Invoke(field.FieldType));
			return true;
		}

		public static bool SetValue<T>(this PropertyInfo property, ref T target, object value, params object[] parameters) => SetValue<T>(property, ref target, _ => value, parameters);
		public static bool SetValue<T>(this PropertyInfo property, ref T target, Func<Type, object> valueFactory, params object[] parameters)
		{
			if(property == null)
				throw new ArgumentNullException(nameof(property));

			if(property.CanWrite)
				property.GetSetter<T>().Invoke(ref target, valueFactory?.Invoke(property.PropertyType), parameters);

			return property.CanWrite;
		}

		public static bool SetValue<T>(this MemberInfo member, ref T target, object value, params object[] parameters) => SetValue<T>(member, ref target, _ => value, parameters);
		public static bool SetValue<T>(this MemberInfo member, ref T target, Func<Type, object> valueFactory, params object[] parameters)
		{
			if(member == null)
				throw new ArgumentNullException(nameof(member));

			switch(member.MemberType)
			{
				case MemberTypes.Field:
					var field = (FieldInfo)member;

					if(field.IsInitOnly)
						return false;

					field.GetSetter<T>().Invoke(ref target, valueFactory?.Invoke(field.FieldType));
					return true;
				case MemberTypes.Property:
					var property = (PropertyInfo)member;

					if(property.CanWrite)
						property.GetSetter<T>().Invoke(ref target, valueFactory?.Invoke(property.PropertyType), parameters);

					return property.CanWrite;
				default:
					throw new NotSupportedException($"The {member.MemberType} of member that is not supported.");
			}
		}

		public static bool SetValue<T>(ref T target, string name, object value, params object[] parameters) => SetValue<T>(ref target, name, _ => value, parameters);
		public static bool SetValue<T>(ref T target, string name, Func<Type, object> valueFactory, params object[] parameters)
		{
			if(target == null)
				throw new ArgumentNullException(nameof(target));

			var type = target.GetType();
			var members = string.IsNullOrEmpty(name) ?
				type.GetDefaultMembers() :
				type.GetMember(name, MemberTypes.Property | MemberTypes.Field, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.IgnoreCase);

			if(members == null || members.Length == 0)
				throw new ArgumentException($"A member named '{name}' does not exist in the '{type.FullName}'.");

			return SetValue(members[0], ref target, valueFactory?.Invoke(members[0].GetMemberType()), parameters);
		}

		public static bool TrySetValue(this FieldInfo field, ref object target, object value) => TrySetValue(field, ref target, _ => value);
		public static bool TrySetValue(this FieldInfo field, ref object target, Func<Type, object> valueFactory)
		{
			if(field == null || field.IsInitOnly)
				return false;

			field.GetSetter().Invoke(ref target, valueFactory?.Invoke(field.FieldType));
			return true;
		}

		public static bool TrySetValue(this PropertyInfo property, ref object target, object value, params object[] parameters) => TrySetValue(property, ref target, _ => value, parameters);
		public static bool TrySetValue(this PropertyInfo property, ref object target, Func<Type, object> valueFactory, params object[] parameters)
		{
			if(property == null || !property.CanWrite)
				return false;

			return PropertyInfoExtension.TrySetValue(property, ref target, valueFactory?.Invoke(property.PropertyType), parameters);
		}

		public static bool TrySetValue(this MemberInfo member, ref object target, object value, params object[] parameters) => TrySetValue(member, ref target, _ => value, parameters);
		public static bool TrySetValue(this MemberInfo member, ref object target, Func<Type, object> valueFactory, params object[] parameters)
		{
			if(member == null)
				return false;

			switch(member.MemberType)
			{
				case MemberTypes.Field:
					var field = (FieldInfo)member;

					if(field.IsInitOnly)
						return false;

					field.GetSetter().Invoke(ref target, valueFactory?.Invoke(field.FieldType));

					return true;
				case MemberTypes.Property:
					var property = (PropertyInfo)member;

					if(!property.CanWrite)
						return false;

					return PropertyInfoExtension.TrySetValue(property, ref target, valueFactory?.Invoke(property.PropertyType), parameters);
			}

			return false;
		}

		public static bool TrySetValue(ref object target, string name, object value, params object[] parameters) => TrySetValue(ref target, name, _ => value, parameters);
		public static bool TrySetValue(ref object target, string name, Func<Type, object> valueFactory, params object[] parameters)
		{
			if(target == null)
				return false;

			var type = (target as Type) ?? target.GetType();
			var members = string.IsNullOrEmpty(name) ?
				type.GetDefaultMembers() :
				type.GetMember(name, MemberTypes.Property | MemberTypes.Field, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.IgnoreCase);

			if(members == null || members.Length == 0)
				return false;

			return TrySetValue(members[0], ref target, valueFactory?.Invoke(members[0].GetMemberType()), parameters);
		}

		public static bool TrySetValue<T>(this FieldInfo field, ref T target, object value) => TrySetValue<T>(field, ref target, _ => value);
		public static bool TrySetValue<T>(this FieldInfo field, ref T target, Func<Type, object> valueFactory)
		{
			if(field == null || field.IsInitOnly)
				return false;

			field.GetSetter<T>().Invoke(ref target, valueFactory?.Invoke(field.FieldType));
			return true;
		}

		public static bool TrySetValue<T>(this PropertyInfo property, ref T target, object value, params object[] parameters) => TrySetValue<T>(property, ref target, _ => value, parameters);
		public static bool TrySetValue<T>(this PropertyInfo property, ref T target, Func<Type, object> valueFactory, params object[] parameters)
		{
			if(property == null || !property.CanWrite)
				return false;

			return PropertyInfoExtension.TrySetValue(property, ref target, valueFactory?.Invoke(property.PropertyType), parameters);
		}

		public static bool TrySetValue<T>(this MemberInfo member, ref T target, object value, params object[] parameters) => TrySetValue<T>(member, ref target, _ => value, parameters);
		public static bool TrySetValue<T>(this MemberInfo member, ref T target, Func<Type, object> valueFactory, params object[] parameters)
		{
			if(member == null)
				return false;

			switch(member.MemberType)
			{
				case MemberTypes.Field:
					var field = (FieldInfo)member;

					if(field.IsInitOnly)
						return false;

					field.GetSetter<T>().Invoke(ref target, valueFactory?.Invoke(field.FieldType));

					return true;
				case MemberTypes.Property:
					var property = (PropertyInfo)member;

					if(!property.CanWrite)
						return false;

					return PropertyInfoExtension.TrySetValue(property, ref target, valueFactory?.Invoke(property.PropertyType), parameters);
			}

			return false;
		}

		public static bool TrySetValue<T>(ref T target, string name, object value, params object[] parameters) => TrySetValue<T>(ref target, name, _ => value, parameters);
		public static bool TrySetValue<T>(ref T target, string name, Func<Type, object> valueFactory, params object[] parameters)
		{
			if(target == null)
				return false;

			var type = target.GetType();
			var members = string.IsNullOrEmpty(name) ?
				type.GetDefaultMembers() :
				type.GetMember(name, MemberTypes.Property | MemberTypes.Field, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.IgnoreCase);

			if(members == null || members.Length == 0)
				return false;

			return TrySetValue<T>(members[0], ref target, valueFactory?.Invoke(members[0].GetMemberType()), parameters);
		}
		#endregion

		#region 辅助方法
		private static Type GetMemberType(this MemberInfo member) => member.MemberType switch
		{
			MemberTypes.Field => ((FieldInfo)member).FieldType,
			MemberTypes.Property => ((PropertyInfo)member).PropertyType,
			MemberTypes.Method => ((MethodInfo)member).ReturnType,
			MemberTypes.Event => ((EventInfo)member).EventHandlerType,
			MemberTypes.TypeInfo => member as Type,
			_ => null,
		};
		#endregion
	}
}
