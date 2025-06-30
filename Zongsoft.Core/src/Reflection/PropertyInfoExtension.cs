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
using System.Reflection.Emit;
using System.Collections.Concurrent;

namespace Zongsoft.Reflection;

public static class PropertyInfoExtension
{
	#region 委托定义
	public delegate object Getter(ref object target, params object[] parameters);
	public delegate object Getter<T>(ref T target, params object[] parameters);

	public delegate void Setter(ref object target, object value, params object[] parameters);
	public delegate void Setter<T>(ref T target, object value, params object[] parameters);
	#endregion

	#region 缓存变量
	private static readonly ConcurrentDictionary<PropertyInfo, PropertyToken> _properties = new();
	private static readonly ConcurrentDictionary<PropertyKey, PropertyTokenGeneric> _genericProperties = new();
	#endregion

	#region 公共方法
	public static bool IsProperty(this MemberInfo member, out PropertyInfo property)
	{
		if(member != null && member.MemberType == MemberTypes.Property)
		{
			property = (PropertyInfo)member;
			return true;
		}

		property = null;
		return false;
	}

	public static bool IsIndexer(this PropertyInfo property) => property != null && property.GetIndexParameters().Length > 0;
	#endregion

	#region 通用目标
	public static Getter GetGetter(this PropertyInfo property)
	{
		if(property == null)
			throw new ArgumentNullException(nameof(property));

		return _properties.GetOrAdd(property, info => new PropertyToken(GenerateGetter(info), GenerateSetter(info))).Getter;
	}

	public static Setter GetSetter(this PropertyInfo property)
	{
		if(property == null)
			throw new ArgumentNullException(nameof(property));

		return _properties.GetOrAdd(property, info => new PropertyToken(GenerateGetter(info), GenerateSetter(info))).Setter;
	}

	public static Getter GenerateGetter(this PropertyInfo property)
	{
		if(property == null)
			throw new ArgumentNullException(nameof(property));

		//如果属性不可读则返回空
		if(!property.CanRead)
			return null;

		var method = new DynamicMethod("dynamic:" + property.DeclaringType.FullName + "!Get" + property.Name,
			typeof(object),
			[typeof(object).MakeByRefType(), typeof(object[])],
			typeof(PropertyInfoExtension),
			true);

		var generator = method.GetILGenerator();

		if(!property.GetMethod.IsStatic)
		{
			generator.DeclareLocal(property.DeclaringType);

			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldind_Ref);
			if(property.DeclaringType.IsValueType)
				generator.Emit(OpCodes.Unbox_Any, property.DeclaringType);
			else
				generator.Emit(OpCodes.Castclass, property.DeclaringType);
			generator.Emit(OpCodes.Stloc_0);

			if(property.DeclaringType.IsValueType)
				generator.Emit(OpCodes.Ldloca_S, 0);
			else
				generator.Emit(OpCodes.Ldloc_0);
		}

		//获取属性的索引器参数
		var parameters = property.GetIndexParameters();
		if(parameters != null && parameters.Length > 0)
		{
			var ERROR_LABEL = generator.DefineLabel();
			var NORMAL_LABEL = generator.DefineLabel();

			//if(parameters == null || ...)
			generator.Emit(OpCodes.Ldarg_1);
			generator.Emit(OpCodes.Brfalse_S, ERROR_LABEL);

			//if(... || parameters.Length < x)
			generator.Emit(OpCodes.Ldarg_1);
			generator.Emit(OpCodes.Ldlen);
			generator.Emit(OpCodes.Conv_I4);
			generator.Emit(OpCodes.Ldc_I4, parameters.Length);
			generator.Emit(OpCodes.Bge_S, NORMAL_LABEL);

			//throw new ArgumentException("...");
			generator.MarkLabel(ERROR_LABEL);
			generator.Emit(OpCodes.Ldstr, $"The count of {property.Name} property indexer parameters is not enough.");
			generator.Emit(OpCodes.Newobj, typeof(ArgumentException).GetConstructor(new Type[] { typeof(string) }));
			generator.Emit(OpCodes.Throw);

			generator.MarkLabel(NORMAL_LABEL);

			//属性索引器获取方法的调用参数
			for(int i = 0; i < parameters.Length; i++)
			{
				generator.Emit(OpCodes.Ldarg_1);
				generator.Emit(OpCodes.Ldc_I4, i);
				generator.Emit(OpCodes.Ldelem_Ref);

				if(parameters[i].ParameterType.IsValueType)
					generator.Emit(OpCodes.Unbox_Any, parameters[i].ParameterType);
				else
					generator.Emit(OpCodes.Castclass, parameters[i].ParameterType);
			}
		}

		//调用属性的获取方法
		if(property.DeclaringType.IsValueType || property.GetMethod.IsStatic)
			generator.Emit(OpCodes.Call, property.GetMethod);
		else
			generator.Emit(OpCodes.Callvirt, property.GetMethod);

		if(property.PropertyType.IsValueType)
			generator.Emit(OpCodes.Box, property.PropertyType);

		generator.Emit(OpCodes.Ret);

		return (Getter)method.CreateDelegate(typeof(Getter));
	}

	public static Setter GenerateSetter(this PropertyInfo property)
	{
		if(property == null)
			throw new ArgumentNullException(nameof(property));

		//如果属性不可写则返回空
		if(!property.CanWrite)
			return null;

		var method = new DynamicMethod("dynamic:" + property.DeclaringType.FullName + "!Set" + property.Name,
			null,
			[typeof(object).MakeByRefType(), typeof(object), typeof(object[])],
			typeof(PropertyInfoExtension),
			true);

		var generator = method.GetILGenerator();

		if(!property.SetMethod.IsStatic)
		{
			generator.DeclareLocal(property.DeclaringType);

			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldind_Ref);
			if(property.DeclaringType.IsValueType)
				generator.Emit(OpCodes.Unbox_Any, property.DeclaringType);
			else
				generator.Emit(OpCodes.Castclass, property.DeclaringType);
			generator.Emit(OpCodes.Stloc_0);

			if(property.DeclaringType.IsValueType)
				generator.Emit(OpCodes.Ldloca_S, 0);
			else
				generator.Emit(OpCodes.Ldloc_0);
		}

		//获取属性的索引器参数
		var parameters = property.GetIndexParameters();
		if(parameters != null && parameters.Length > 0)
		{
			var ERROR_LABEL = generator.DefineLabel();
			var NORMAL_LABEL = generator.DefineLabel();

			//if(parameters == null || ...)
			generator.Emit(OpCodes.Ldarg_2);
			generator.Emit(OpCodes.Brfalse_S, ERROR_LABEL);

			//if(... || parameters.Length < x)
			generator.Emit(OpCodes.Ldarg_2);
			generator.Emit(OpCodes.Ldlen);
			generator.Emit(OpCodes.Conv_I4);
			generator.Emit(OpCodes.Ldc_I4, parameters.Length);
			generator.Emit(OpCodes.Bge_S, NORMAL_LABEL);

			//throw new ArgumentException("...");
			generator.MarkLabel(ERROR_LABEL);
			generator.Emit(OpCodes.Ldstr, $"The count of {property.Name} property indexer parameters is not enough.");
			generator.Emit(OpCodes.Newobj, typeof(ArgumentException).GetConstructor(new Type[] { typeof(string) }));
			generator.Emit(OpCodes.Throw);

			generator.MarkLabel(NORMAL_LABEL);

			//属性索引器获取方法的调用参数
			for(int i = 0; i < parameters.Length; i++)
			{
				generator.Emit(OpCodes.Ldarg_2);
				generator.Emit(OpCodes.Ldc_I4, i);
				generator.Emit(OpCodes.Ldelem_Ref);

				if(parameters[i].ParameterType.IsValueType)
					generator.Emit(OpCodes.Unbox_Any, parameters[i].ParameterType);
				else
					generator.Emit(OpCodes.Castclass, parameters[i].ParameterType);
			}
		}

		var SetValue_Lable = generator.DefineLabel();

		if(property.PropertyType.IsValueType)
		{
			var underlyingType = Nullable.GetUnderlyingType(property.PropertyType);
			var Null_Branch_Label = generator.DefineLabel();

			if(underlyingType != null)
			{
				//定义一个 Nullable<?> 类型的本地变量
				generator.DeclareLocal(property.PropertyType);

				//if(value==null)
				generator.Emit(OpCodes.Ldarg_1);
				generator.Emit(OpCodes.Brfalse_S, Null_Branch_Label);
			}

			//else(value!=null)
			generator.Emit(OpCodes.Ldarg_1);
			generator.Emit(OpCodes.Ldtoken, underlyingType ?? property.PropertyType);
			generator.Emit(OpCodes.Call, typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle), BindingFlags.Public | BindingFlags.Static));
			generator.Emit(OpCodes.Call, typeof(Convert).GetMethod(nameof(Convert.ChangeType), new[] { typeof(object), typeof(Type) }));
			generator.Emit(OpCodes.Unbox_Any, underlyingType ?? property.PropertyType);

			if(underlyingType != null)
			{
				//将基元类型转换为 Nullable<?> 类型
				generator.Emit(OpCodes.Newobj, property.PropertyType.GetConstructor(new[] { underlyingType }));
				generator.Emit(OpCodes.Br_S, SetValue_Lable);

				//标记当value=null的跳转分支
				generator.MarkLabel(Null_Branch_Label);

				//if(value==null) (Nullable<?>)null
				generator.Emit(OpCodes.Ldloca_S, 1);
				generator.Emit(OpCodes.Initobj, property.PropertyType);
				generator.Emit(OpCodes.Ldloc_1);
			}
		}
		else
		{
			generator.Emit(OpCodes.Ldarg_1);
			generator.Emit(OpCodes.Castclass, property.PropertyType);
		}

		generator.MarkLabel(SetValue_Lable);

		//调用属性的设置方法
		if(property.DeclaringType.IsValueType || property.SetMethod.IsStatic)
			generator.Emit(OpCodes.Call, property.SetMethod);
		else
			generator.Emit(OpCodes.Callvirt, property.SetMethod);

		if(property.DeclaringType.IsValueType && (!property.SetMethod.IsStatic))
		{
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldloc_0);
			generator.Emit(OpCodes.Box, property.DeclaringType);
			generator.Emit(OpCodes.Stind_Ref);
		}

		generator.Emit(OpCodes.Ret);

		return (Setter)method.CreateDelegate(typeof(Setter));
	}
	#endregion

	#region 泛型目标
	public static Getter<T> GetGetter<T>(this PropertyInfo property)
	{
		if(property == null)
			throw new ArgumentNullException(nameof(property));

		var token = _genericProperties.GetOrAdd(
			new PropertyKey(typeof(T), property),
			key => new PropertyTokenGeneric(GenerateGetter<T>(property), GenerateSetter<T>(property)));

		return (Getter<T>)token.Getter;
	}

	public static Setter<T> GetSetter<T>(this PropertyInfo property)
	{
		if(property == null)
			throw new ArgumentNullException(nameof(property));

		var token = _genericProperties.GetOrAdd(
			new PropertyKey(typeof(T), property),
			key => new PropertyTokenGeneric(GenerateGetter<T>(property), GenerateSetter<T>(property)));

		return (Setter<T>)token.Setter;
	}

	public static Getter<T> GenerateGetter<T>(this PropertyInfo property)
	{
		if(property == null)
			throw new ArgumentNullException(nameof(property));

		if(!typeof(T).IsAssignableFrom(property.ReflectedType))
			throw new TargetException($"The specified '{typeof(T).FullName}' of the target does not define the '{property.Name}' property.");

		//如果属性不可读则返回空
		if(!property.CanRead)
			return null;

		var method = new DynamicMethod("dynamic:" + typeof(T).FullName + "!Get" + property.Name + "#1",
			typeof(object),
			[typeof(T).MakeByRefType(), typeof(object[])],
			typeof(PropertyInfoExtension),
			true);

		var generator = method.GetILGenerator();

		if(!property.GetMethod.IsStatic)
		{
			generator.Emit(OpCodes.Ldarg_0);

			if(!typeof(T).IsValueType)
				generator.Emit(OpCodes.Ldind_Ref);
		}

		//获取属性的索引器参数
		var parameters = property.GetIndexParameters();
		if(parameters != null && parameters.Length > 0)
		{
			var ERROR_LABEL = generator.DefineLabel();
			var NORMAL_LABEL = generator.DefineLabel();

			//if(parameters == null || ...)
			generator.Emit(OpCodes.Ldarg_1);
			generator.Emit(OpCodes.Brfalse_S, ERROR_LABEL);

			//if(... || parameters.Length < x)
			generator.Emit(OpCodes.Ldarg_1);
			generator.Emit(OpCodes.Ldlen);
			generator.Emit(OpCodes.Conv_I4);
			generator.Emit(OpCodes.Ldc_I4, parameters.Length);
			generator.Emit(OpCodes.Bge_S, NORMAL_LABEL);

			//throw new ArgumentException("...");
			generator.MarkLabel(ERROR_LABEL);
			generator.Emit(OpCodes.Ldstr, $"The count of {property.Name} property indexer parameters is not enough.");
			generator.Emit(OpCodes.Newobj, typeof(ArgumentException).GetConstructor(new Type[] { typeof(string) }));
			generator.Emit(OpCodes.Throw);

			generator.MarkLabel(NORMAL_LABEL);

			//属性索引器获取方法的调用参数
			for(int i = 0; i < parameters.Length; i++)
			{
				generator.Emit(OpCodes.Ldarg_1);
				generator.Emit(OpCodes.Ldc_I4, i);
				generator.Emit(OpCodes.Ldelem_Ref);

				if(parameters[i].ParameterType.IsValueType)
					generator.Emit(OpCodes.Unbox_Any, parameters[i].ParameterType);
				else
					generator.Emit(OpCodes.Castclass, parameters[i].ParameterType);
			}
		}

		//调用属性的获取方法
		if(property.DeclaringType.IsValueType || property.GetMethod.IsStatic)
			generator.Emit(OpCodes.Call, property.GetMethod);
		else
			generator.Emit(OpCodes.Callvirt, property.GetMethod);

		if(property.PropertyType.IsValueType)
			generator.Emit(OpCodes.Box, property.PropertyType);

		generator.Emit(OpCodes.Ret);

		return (Getter<T>)method.CreateDelegate(typeof(Getter<T>));
	}

	public static Setter<T> GenerateSetter<T>(this PropertyInfo property)
	{
		if(property == null)
			throw new ArgumentNullException(nameof(property));

		if(!typeof(T).IsAssignableFrom(property.ReflectedType))
			throw new TargetException($"The specified '{typeof(T).FullName}' of the target does not define the '{property.Name}' property.");

		//如果属性不可写则返回空
		if(!property.CanWrite)
			return null;

		var method = new DynamicMethod("dynamic:" + typeof(T).FullName + "!Set" + property.Name + "#1",
			null,
			[typeof(T).MakeByRefType(), typeof(object), typeof(object[])],
			typeof(PropertyInfoExtension),
			true);

		var generator = method.GetILGenerator();

		if(!property.SetMethod.IsStatic)
		{
			generator.Emit(OpCodes.Ldarg_0);

			if(!typeof(T).IsValueType)
				generator.Emit(OpCodes.Ldind_Ref);
		}

		//获取属性的索引器参数
		var parameters = property.GetIndexParameters();
		if(parameters != null && parameters.Length > 0)
		{
			var ERROR_LABEL = generator.DefineLabel();
			var NORMAL_LABEL = generator.DefineLabel();

			//if(parameters == null || ...)
			generator.Emit(OpCodes.Ldarg_2);
			generator.Emit(OpCodes.Brfalse_S, ERROR_LABEL);

			//if(... || parameters.Length < x)
			generator.Emit(OpCodes.Ldarg_2);
			generator.Emit(OpCodes.Ldlen);
			generator.Emit(OpCodes.Conv_I4);
			generator.Emit(OpCodes.Ldc_I4, parameters.Length);
			generator.Emit(OpCodes.Bge_S, NORMAL_LABEL);

			//throw new ArgumentException("...");
			generator.MarkLabel(ERROR_LABEL);
			generator.Emit(OpCodes.Ldstr, $"The count of {property.Name} property indexer parameters is not enough.");
			generator.Emit(OpCodes.Newobj, typeof(ArgumentException).GetConstructor(new Type[] { typeof(string) }));
			generator.Emit(OpCodes.Throw);

			generator.MarkLabel(NORMAL_LABEL);

			//属性索引器获取方法的调用参数
			for(int i = 0; i < parameters.Length; i++)
			{
				generator.Emit(OpCodes.Ldarg_2);
				generator.Emit(OpCodes.Ldc_I4, i);
				generator.Emit(OpCodes.Ldelem_Ref);

				if(parameters[i].ParameterType.IsValueType)
					generator.Emit(OpCodes.Unbox_Any, parameters[i].ParameterType);
				else
					generator.Emit(OpCodes.Castclass, parameters[i].ParameterType);
			}
		}

		var SetValue_Lable = generator.DefineLabel();

		if(property.PropertyType.IsValueType)
		{
			var underlyingType = Nullable.GetUnderlyingType(property.PropertyType);
			var Null_Branch_Label = generator.DefineLabel();

			if(underlyingType != null)
			{
				//定义一个 Nullable<?> 类型的本地变量
				generator.DeclareLocal(property.PropertyType);

				//if(value==null)
				generator.Emit(OpCodes.Ldarg_1);
				generator.Emit(OpCodes.Brfalse_S, Null_Branch_Label);
			}

			//else(value!=null)
			generator.Emit(OpCodes.Ldarg_1);
			generator.Emit(OpCodes.Ldtoken, underlyingType ?? property.PropertyType);
			generator.Emit(OpCodes.Call, typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle), BindingFlags.Public | BindingFlags.Static));
			generator.Emit(OpCodes.Call, typeof(Convert).GetMethod(nameof(Convert.ChangeType), new[] { typeof(object), typeof(Type) }));
			generator.Emit(OpCodes.Unbox_Any, underlyingType ?? property.PropertyType);

			if(underlyingType != null)
			{
				//将基元类型转换为 Nullable<?> 类型
				generator.Emit(OpCodes.Newobj, property.PropertyType.GetConstructor(new[] { underlyingType }));
				generator.Emit(OpCodes.Br_S, SetValue_Lable);

				//标记当value=null的跳转分支
				generator.MarkLabel(Null_Branch_Label);

				//if(value==null) (Nullable<?>)null
				generator.Emit(OpCodes.Ldloca_S, 0);
				generator.Emit(OpCodes.Initobj, property.PropertyType);
				generator.Emit(OpCodes.Ldloc_0);
			}
		}
		else
		{
			generator.Emit(OpCodes.Ldarg_1);
			generator.Emit(OpCodes.Castclass, property.PropertyType);
		}

		generator.MarkLabel(SetValue_Lable);

		//调用属性的设置方法
		if(property.DeclaringType.IsValueType || property.SetMethod.IsStatic)
			generator.Emit(OpCodes.Call, property.SetMethod);
		else
			generator.Emit(OpCodes.Callvirt, property.SetMethod);

		generator.Emit(OpCodes.Ret);

		return (Setter<T>)method.CreateDelegate(typeof(Setter<T>));
	}
	#endregion

	#region 内部方法
	internal static bool TryGetValue(PropertyInfo property, ref object target, object[] parameters, out object value)
	{
		if(property != null && property.CanRead && property.GetMethod.GetParameters().Length == (parameters == null ? 0 : parameters.Length))
		{
			try
			{
				value = property.GetGetter().Invoke(ref target, parameters);
				return true;
			}
			catch
			{
				value = default;
				return false;
			}
		}

		value = default;
		return false;
	}

	internal static bool TryGetValue<T>(PropertyInfo property, ref T target, object[] parameters, out object value)
	{
		if(property != null && property.CanRead && property.GetMethod.GetParameters().Length == (parameters == null ? 0 : parameters.Length))
		{
			try
			{
				value = property.GetGetter<T>().Invoke(ref target, parameters);
				return true;
			}
			catch
			{
				value = default;
				return false;
			}
		}

		value = default;
		return false;
	}

	internal static bool TrySetValue(PropertyInfo property, ref object target, object value, object[] parameters)
	{
		if(property != null && property.CanWrite && property.SetMethod.GetParameters().Length == (parameters == null ? 0 : parameters.Length) + 1)
		{
			try
			{
				property.GetSetter().Invoke(ref target, value, parameters);
				return true;
			}
			catch
			{
				return false;
			}
		}

		return false;
	}

	internal static bool TrySetValue<T>(PropertyInfo property, ref T target, object value, object[] parameters)
	{
		if(property != null && property.CanWrite && property.SetMethod.GetParameters().Length == (parameters == null ? 0 : parameters.Length) + 1)
		{
			try
			{
				property.GetSetter<T>().Invoke(ref target, value, parameters);
				return true;
			}
			catch
			{
				return false;
			}
		}

		return false;
	}
	#endregion

	#region 嵌套子类
	private readonly struct PropertyKey(Type type, PropertyInfo property) : IEquatable<PropertyKey>
	{
		public readonly Type Type = type;
		public readonly PropertyInfo Property = property;

		public bool Equals(PropertyKey key) => object.ReferenceEquals(this.Type, key.Type) && object.ReferenceEquals(this.Property, key.Property);
		public override bool Equals(object obj) => obj is PropertyKey key && this.Equals(key);
		public override int GetHashCode() => HashCode.Combine(this.Type, this.Property);
		public override string ToString() => this.Type.FullName + ":" + this.Property.Name;
	}

	private readonly struct PropertyToken(Getter getter, Setter setter)
	{
		public readonly Getter Getter = getter;
		public readonly Setter Setter = setter;
	}

	private readonly struct PropertyTokenGeneric(Delegate getter, Delegate setter)
	{
		public readonly Delegate Getter = getter;
		public readonly Delegate Setter = setter;
	}
	#endregion
}
