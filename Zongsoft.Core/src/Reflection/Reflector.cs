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

		public static object GetValue(object target, string name, params object[] parameters)
		{
			if(target == null)
				throw new ArgumentNullException(nameof(target));

			return TryGetValue(target, name, parameters, out var value) ? value :
			       throw new ArgumentException($"A member named '{name}' does not exist in the '{target.ToString()}'.");
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

		public static object GetValue<T>(T target, string name, params object[] parameters)
		{
			if(target == null)
				throw new ArgumentNullException(nameof(target));

			return TryGetValue<T>(target, name, parameters, out var value) ? value :
			       throw new ArgumentException($"A member named '{name}' does not exist in the '{target.ToString()}'.");
		}

		public static bool TryGetValue(this MemberInfo member, ref object target, out object value)
		{
			return TryGetValue(member, ref target, Array.Empty<object>(), out value);
		}

		public static bool TryGetValue(this MemberInfo member, ref object target, object[] parameters, out object value)
		{
			if(member == null)
				throw new ArgumentNullException(nameof(member));

			switch(member.MemberType)
			{
				case MemberTypes.Field:
					value = GetValue((FieldInfo)member, ref target);
					return true;
				case MemberTypes.Property:
					value = GetValue((PropertyInfo)member, ref target, parameters);
					return true;
			}

			value = null;
			return false;
		}

		public static bool TryGetValue(object target, string name, out object value)
		{
			return TryGetValue(target, name, Array.Empty<object>(), out value);
		}

		public static bool TryGetValue(object target, string name, object[] parameters, out object value)
		{
			if(target == null)
				throw new ArgumentNullException(nameof(target));

			value = null;

			var type = (target as Type) ?? target.GetType();
			var members = string.IsNullOrEmpty(name) ?
				type.GetDefaultMembers() :
				type.GetMember(name, MemberTypes.Property | MemberTypes.Field, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

			if(members == null || members.Length == 0)
				return false;

			value = GetValue(members[0], ref target, parameters);
			return true;
		}

		public static bool TryGetValue<T>(this MemberInfo member, ref T target, out object value)
		{
			return TryGetValue<T>(member, ref target, Array.Empty<object>(), out value);
		}

		public static bool TryGetValue<T>(this MemberInfo member, ref T target, object[] parameters, out object value)
		{
			if(member == null)
				throw new ArgumentNullException(nameof(member));

			switch(member.MemberType)
			{
				case MemberTypes.Field:
					value = GetValue<T>((FieldInfo)member, ref target);
					return true;
				case MemberTypes.Property:
					value = GetValue<T>((PropertyInfo)member, ref target, parameters);
					return true;
			}

			value = null;
			return false;
		}

		public static bool TryGetValue<T>(T target, string name, out object value)
		{
			return TryGetValue<T>(target, name, Array.Empty<object>(), out value);
		}

		public static bool TryGetValue<T>(T target, string name, object[] parameters, out object value)
		{
			if(target == null)
				throw new ArgumentNullException(nameof(target));

			value = null;

			var type = typeof(T);
			var members = string.IsNullOrEmpty(name) ?
				type.GetDefaultMembers() :
				type.GetMember(name, MemberTypes.Property | MemberTypes.Field, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

			if(members == null || members.Length == 0)
				return false;

			value = GetValue<T>(members[0], ref target, parameters);
			return true;
		}
		#endregion

		#region 设置方法
		public static void SetValue(this FieldInfo field, ref object target, object value)
		{
			if(field == null)
				throw new ArgumentNullException(nameof(field));

			if(field.IsInitOnly)
				throw new InvalidOperationException($"The '{field.Name}' field does not support writing.");

			field.GetSetter().Invoke(ref target, value);
		}

		public static void SetValue(this PropertyInfo property, ref object target, object value, params object[] parameters)
		{
			if(property == null)
				throw new ArgumentNullException(nameof(property));

			if(!property.CanWrite)
				throw new InvalidOperationException($"The '{property.Name}' property does not support writing.");

			property.GetSetter().Invoke(ref target, value, parameters);
		}

		public static void SetValue(this MemberInfo member, ref object target, object value, params object[] parameters)
		{
			if(member == null)
				throw new ArgumentNullException(nameof(member));

			switch(member.MemberType)
			{
				case MemberTypes.Field:
					var field = (FieldInfo)member;

					if(field.IsInitOnly)
						throw new InvalidOperationException($"The '{field.Name}' field does not support writing.");

					field.GetSetter().Invoke(ref target, value);

					break;
				case MemberTypes.Property:
					var property = (PropertyInfo)member;

					if(!property.CanWrite)
						throw new InvalidOperationException($"The '{property.Name}' property does not support writing.");

					property.GetSetter().Invoke(ref target, value, parameters);

					break;
				default:
					throw new NotSupportedException($"The {member.MemberType.ToString()} of member that is not supported.");
			}
		}

		public static void SetValue(ref object target, string name, object value, params object[] parameters)
		{
			if(target == null)
				throw new ArgumentNullException(nameof(target));

			if(!TrySetValue(ref target, name, value, parameters))
				throw new ArgumentException($"A member named '{name}' does not exist in the '{target.ToString()}'.");
		}

		public static void SetValue<T>(this FieldInfo field, ref T target, object value)
		{
			if(field == null)
				throw new ArgumentNullException(nameof(field));

			if(field.IsInitOnly)
				throw new InvalidOperationException($"The '{field.Name}' field does not support writing.");

			field.GetSetter<T>().Invoke(ref target, value);
		}

		public static void SetValue<T>(this PropertyInfo property, ref T target, object value, params object[] parameters)
		{
			if(property == null)
				throw new ArgumentNullException(nameof(property));

			if(!property.CanWrite)
				throw new InvalidOperationException($"The '{property.Name}' property does not support writing.");

			property.GetSetter<T>().Invoke(ref target, value, parameters);
		}

		public static void SetValue<T>(this MemberInfo member, ref T target, object value, params object[] parameters)
		{
			if(member == null)
				throw new ArgumentNullException(nameof(member));

			if(!TrySetValue<T>(member, ref target, value, parameters))
				throw new NotSupportedException($"The {member.MemberType.ToString()} of member that is not supported.");
		}

		public static void SetValue<T>(ref T target, string name, object value, params object[] parameters)
		{
			if(target == null)
				throw new ArgumentNullException(nameof(target));

			if(!TrySetValue<T>(ref target, name, value, parameters))
				throw new ArgumentException($"A member named '{name}' does not exist in the '{target.ToString()}'.");
		}

		public static bool TrySetValue(this MemberInfo member, ref object target, object value, params object[] parameters)
		{
			if(member == null)
				throw new ArgumentNullException(nameof(member));

			switch(member.MemberType)
			{
				case MemberTypes.Field:
					SetValue((FieldInfo)member, ref target, value);
					return true;
				case MemberTypes.Property:
					SetValue((PropertyInfo)member, ref target, value, parameters);
					return true;
			}

			return false;
		}

		public static bool TrySetValue(ref object target, string name, object value, params object[] parameters)
		{
			if(target == null)
				throw new ArgumentNullException(nameof(target));

			var type = (target as Type) ?? target.GetType();
			var members = string.IsNullOrEmpty(name) ?
				type.GetDefaultMembers() :
				type.GetMember(name, MemberTypes.Property | MemberTypes.Field, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

			if(members == null || members.Length == 0)
				return false;

			return TrySetValue(members[0], ref target, value, parameters);
		}

		public static bool TrySetValue<T>(this MemberInfo member, ref T target, object value, params object[] parameters)
		{
			if(member == null)
				throw new ArgumentNullException(nameof(member));

			switch(member.MemberType)
			{
				case MemberTypes.Field:
					SetValue<T>((FieldInfo)member, ref target, value);
					return true;
				case MemberTypes.Property:
					SetValue<T>((PropertyInfo)member, ref target, value, parameters);
					return true;
			}

			return false;
		}

		public static bool TrySetValue<T>(ref T target, string name, object value, params object[] parameters)
		{
			if(target == null)
				throw new ArgumentNullException(nameof(target));

			var type = typeof(T);
			var members = string.IsNullOrEmpty(name) ?
				type.GetDefaultMembers() :
				type.GetMember(name, MemberTypes.Property | MemberTypes.Field, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

			if(members == null || members.Length == 0)
				return false;

			return TrySetValue<T>(members[0], ref target, value, parameters);
		}
		#endregion
	}
}
