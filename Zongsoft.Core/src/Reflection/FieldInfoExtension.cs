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
using System.Reflection.Emit;
using System.Collections.Concurrent;

namespace Zongsoft.Reflection
{
	public static class FieldInfoExtension
	{
		#region 委托定义
		public delegate object Getter(ref object target);
		public delegate object Getter<T>(ref T target);

		public delegate void Setter(ref object target, object value);
		public delegate void Setter<T>(ref T target, object value);
		#endregion

		#region 缓存变量
		private static readonly ConcurrentDictionary<FieldInfo, FieldToken> _fields = new ConcurrentDictionary<FieldInfo, FieldToken>();
		private static readonly ConcurrentDictionary<FieldKey, FieldTokenGeneric> _genericProperties = new ConcurrentDictionary<FieldKey, FieldTokenGeneric>();
		#endregion

		#region 公共方法

		#region 通用目标
		public static Getter GetGetter(this FieldInfo field)
		{
			if(field == null)
				throw new ArgumentNullException(nameof(field));

			return _fields.GetOrAdd(field, info => new FieldToken(GenerateGetter(info), GenerateSetter(info))).Getter;
		}

		public static Setter GetSetter(this FieldInfo field)
		{
			if(field == null)
				throw new ArgumentNullException(nameof(field));

			return _fields.GetOrAdd(field, info => new FieldToken(GenerateGetter(info), GenerateSetter(info))).Setter;
		}

		public static Getter GenerateGetter(this FieldInfo field)
		{
			if(field == null)
				throw new ArgumentNullException(nameof(field));

			var method = new DynamicMethod("dynamic:" + field.DeclaringType.FullName + "!Get" + field.Name,
				typeof(object),
				new Type[] { typeof(object).MakeByRefType() },
				typeof(FieldInfoExtension),
				true);

			var generator = method.GetILGenerator();

			generator.DeclareLocal(field.DeclaringType);

			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldind_Ref);
			if(field.DeclaringType.IsValueType)
				generator.Emit(OpCodes.Unbox_Any, field.DeclaringType);
			else
				generator.Emit(OpCodes.Castclass, field.DeclaringType);

			generator.Emit(OpCodes.Ldfld, field);

			if(field.FieldType.IsValueType)
				generator.Emit(OpCodes.Box, field.FieldType);

			generator.Emit(OpCodes.Ret);

			return (Getter)method.CreateDelegate(typeof(Getter));
		}

		public static Setter GenerateSetter(this FieldInfo field)
		{
			if(field == null)
				throw new ArgumentNullException(nameof(field));

			//如果字段为只读则返回空
			if(field.IsInitOnly)
				return null;

			var method = new DynamicMethod("dynamic:" + field.DeclaringType.FullName + "!Set" + field.Name,
				null,
				new Type[] { typeof(object).MakeByRefType(), typeof(object) },
				typeof(FieldInfoExtension),
				true);

			var generator = method.GetILGenerator();
			var SetValue_Lable = generator.DefineLabel();

			generator.DeclareLocal(field.DeclaringType);

			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldind_Ref);
			if(field.DeclaringType.IsValueType)
				generator.Emit(OpCodes.Unbox_Any, field.DeclaringType);
			else
				generator.Emit(OpCodes.Castclass, field.DeclaringType);
			generator.Emit(OpCodes.Stloc_0);

			if(field.DeclaringType.IsValueType)
				generator.Emit(OpCodes.Ldloca_S, 0);
			else
				generator.Emit(OpCodes.Ldloc_0);

			if(field.FieldType.IsValueType)
			{
				var underlyingType = Nullable.GetUnderlyingType(field.FieldType);
				var Null_Branch_Label = generator.DefineLabel();

				if(underlyingType != null)
				{
					//定义一个 Nullable<?> 类型的本地变量
					generator.DeclareLocal(field.FieldType);

					//if(value==null)
					generator.Emit(OpCodes.Ldarg_1);
					generator.Emit(OpCodes.Brfalse_S, Null_Branch_Label);
				}

				//else(value!=null)
				generator.Emit(OpCodes.Ldarg_1);
				generator.Emit(OpCodes.Ldtoken, underlyingType ?? field.FieldType);
				generator.Emit(OpCodes.Call, typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle), BindingFlags.Public | BindingFlags.Static));
				generator.Emit(OpCodes.Call, typeof(Convert).GetMethod(nameof(Convert.ChangeType), new[] { typeof(object), typeof(Type) }));
				generator.Emit(OpCodes.Unbox_Any, underlyingType ?? field.FieldType);

				if(underlyingType != null)
				{
					//将基元类型转换为 Nullable<?> 类型
					generator.Emit(OpCodes.Newobj, field.FieldType.GetConstructor(new[] { underlyingType }));
					generator.Emit(OpCodes.Br_S, SetValue_Lable);

					//标记当value=null的跳转分支
					generator.MarkLabel(Null_Branch_Label);

					//if(value==null) (Nullable<?>)null
					generator.Emit(OpCodes.Ldloca_S, 1);
					generator.Emit(OpCodes.Initobj, field.FieldType);
					generator.Emit(OpCodes.Ldloc_1);
				}
			}
			else
			{
				generator.Emit(OpCodes.Ldarg_1);
				generator.Emit(OpCodes.Castclass, field.FieldType);
			}

			generator.MarkLabel(SetValue_Lable);

			//设置字段值
			generator.Emit(OpCodes.Stfld, field);

			if(field.DeclaringType.IsValueType)
			{
				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldloc_0);
				generator.Emit(OpCodes.Box, field.DeclaringType);
				generator.Emit(OpCodes.Stind_Ref);
			}

			generator.Emit(OpCodes.Ret);

			return (Setter)method.CreateDelegate(typeof(Setter));
		}
		#endregion

		#region 泛型目标
		public static Getter<T> GetGetter<T>(this FieldInfo field)
		{
			if(field == null)
				throw new ArgumentNullException(nameof(field));

			var token = _genericProperties.GetOrAdd(
				new FieldKey(typeof(T), field),
				key => new FieldTokenGeneric(GenerateGetter<T>(field), GenerateSetter<T>(field)));

			return (Getter<T>)token.Getter;
		}

		public static Setter<T> GetSetter<T>(this FieldInfo field)
		{
			if(field == null)
				throw new ArgumentNullException(nameof(field));

			var token = _genericProperties.GetOrAdd(
				new FieldKey(typeof(T), field),
				key => new FieldTokenGeneric(GenerateGetter<T>(field), GenerateSetter<T>(field)));

			return (Setter<T>)token.Setter;
		}

		public static Getter<T> GenerateGetter<T>(this FieldInfo field)
		{
			if(field == null)
				throw new ArgumentNullException(nameof(field));

			if(!typeof(T).IsAssignableFrom(field.DeclaringType))
				throw new TargetException($"The specified '{typeof(T).FullName}' of the target does not define the '{field.Name}' field.");

			var method = new DynamicMethod("dynamic:" + typeof(T).FullName + "!Get" + field.Name + "#1",
				typeof(object),
				new Type[] { typeof(T).MakeByRefType() },
				typeof(FieldInfoExtension),
				true);

			var generator = method.GetILGenerator();

			generator.Emit(OpCodes.Ldarg_0);

			if(!typeof(T).IsValueType)
				generator.Emit(OpCodes.Ldind_Ref);

			generator.Emit(OpCodes.Ldfld, field);

			if(field.FieldType.IsValueType)
				generator.Emit(OpCodes.Box, field.FieldType);

			generator.Emit(OpCodes.Ret);

			return (Getter<T>)method.CreateDelegate(typeof(Getter<T>));
		}

		public static Setter<T> GenerateSetter<T>(this FieldInfo field)
		{
			if(field == null)
				throw new ArgumentNullException(nameof(field));

			if(!typeof(T).IsAssignableFrom(field.DeclaringType))
				throw new TargetException($"The specified '{typeof(T).FullName}' of the target does not define the '{field.Name}' field.");

			//如果字段为只读则返回空
			if(field.IsInitOnly)
				return null;

			var method = new DynamicMethod("dynamic:" + field.DeclaringType.FullName + "!Set" + field.Name + "#1",
				null,
				new Type[] { typeof(T).MakeByRefType(), typeof(object) },
				typeof(FieldInfoExtension),
				true);

			var generator = method.GetILGenerator();
			var SetValue_Lable = generator.DefineLabel();

			generator.Emit(OpCodes.Ldarg_0);

			if(!typeof(T).IsValueType)
				generator.Emit(OpCodes.Ldind_Ref);

			if(field.FieldType.IsValueType)
			{
				var underlyingType = Nullable.GetUnderlyingType(field.FieldType);
				var Null_Branch_Label = generator.DefineLabel();

				if(underlyingType != null)
				{
					//定义一个 Nullable<?> 类型的本地变量
					generator.DeclareLocal(field.FieldType);

					//if(value==null)
					generator.Emit(OpCodes.Ldarg_1);
					generator.Emit(OpCodes.Brfalse_S, Null_Branch_Label);
				}

				//else(value!=null)
				generator.Emit(OpCodes.Ldarg_1);
				generator.Emit(OpCodes.Ldtoken, underlyingType ?? field.FieldType);
				generator.Emit(OpCodes.Call, typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle), BindingFlags.Public | BindingFlags.Static));
				generator.Emit(OpCodes.Call, typeof(Convert).GetMethod(nameof(Convert.ChangeType), new[] { typeof(object), typeof(Type) }));
				generator.Emit(OpCodes.Unbox_Any, underlyingType ?? field.FieldType);

				if(underlyingType != null)
				{
					//将基元类型转换为 Nullable<?> 类型
					generator.Emit(OpCodes.Newobj, field.FieldType.GetConstructor(new[] { underlyingType }));
					generator.Emit(OpCodes.Br_S, SetValue_Lable);

					//标记当value=null的跳转分支
					generator.MarkLabel(Null_Branch_Label);

					//if(value==null) (Nullable<?>)null
					generator.Emit(OpCodes.Ldloca_S, 0);
					generator.Emit(OpCodes.Initobj, field.FieldType);
					generator.Emit(OpCodes.Ldloc_0);
				}
			}
			else
			{
				generator.Emit(OpCodes.Ldarg_1);
				generator.Emit(OpCodes.Castclass, field.FieldType);
			}

			generator.MarkLabel(SetValue_Lable);

			//设置字段值
			generator.Emit(OpCodes.Stfld, field);
			generator.Emit(OpCodes.Ret);

			return (Setter<T>)method.CreateDelegate(typeof(Setter<T>));
		}
		#endregion

		#endregion

		#region 嵌套子类
		private readonly struct FieldKey : IEquatable<FieldKey>
		{
			public readonly Type Type;
			public readonly FieldInfo Field;

			public FieldKey(Type type, FieldInfo field)
			{
				this.Type = type;
				this.Field = field;
			}

			public bool Equals(FieldKey key) => object.ReferenceEquals(this.Type, key.Type) && object.ReferenceEquals(this.Field, key.Field);
			public override bool Equals(object obj) => obj is FieldKey key && this.Equals(key);
			public override int GetHashCode() => HashCode.Combine(this.Type, this.Field);
			public override string ToString() => this.Type.FullName + ":" + this.Field.Name;
		}

		private class FieldToken
		{
			public readonly Getter Getter;
			public readonly Setter Setter;

			public FieldToken(Getter getter, Setter setter)
			{
				this.Getter = getter;
				this.Setter = setter;
			}
		}

		private readonly struct FieldTokenGeneric
		{
			public readonly Delegate Getter;
			public readonly Delegate Setter;

			public FieldTokenGeneric(Delegate getter, Delegate setter)
			{
				this.Getter = getter;
				this.Setter = setter;
			}
		}
		#endregion
	}
}
