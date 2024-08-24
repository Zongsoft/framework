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
using System.Reflection.Emit;
using System.ComponentModel;

namespace Zongsoft.Data.Common
{
	public static class ModelMemberEmitter
	{
		#region 委托定义
		public delegate void Populator(ref object target, IDataRecord record, int ordinal, TypeConverter converter);
		public delegate void Populator<T>(ref T target, IDataRecord record, int ordinal, TypeConverter converter);
		#endregion

		#region 私有变量
		private static readonly MethodInfo __IsDBNull__ = typeof(IDataRecord).GetMethod(nameof(IDataRecord.IsDBNull), new Type[] { typeof(int) });
		private static readonly MethodInfo __GetValueRecord__ = typeof(IDataRecord).GetMethod(nameof(IDataRecord.GetValue), new Type[] { typeof(int) });
		private static readonly MethodInfo __GetValueExtension__ = typeof(DataRecordExtension).GetMethod(nameof(DataRecordExtension.GetValue), new Type[] { typeof(IDataRecord), typeof(int) });
		private static readonly MethodInfo __ConvertFrom__ = typeof(TypeConverter).GetMethod(nameof(TypeConverter.ConvertFrom), new Type[] { typeof(object) });
		#endregion

		#region 公共方法
		public static Populator GenerateFieldSetter(FieldInfo field, TypeConverter converter)
		{
			var method = new DynamicMethod(field.DeclaringType.FullName + "$Set" + field.Name, null,
				new Type[] { typeof(object).MakeByRefType(), typeof(IDataRecord), typeof(int), typeof(TypeConverter) },
				typeof(ModelMemberEmitter), true);

			var generator = method.GetILGenerator();
			var fieldType = field.FieldType;

			if(Zongsoft.Common.TypeExtension.IsNullable(fieldType, out var underlyingType))
				fieldType = underlyingType;

			if(fieldType.IsEnum)
				fieldType = Enum.GetUnderlyingType(fieldType);

			var ending = generator.DefineLabel();

			generator.DeclareLocal(field.DeclaringType);

			//if(record.IsDBNull(ordinal))
			generator.Emit(OpCodes.Ldarg_1);
			generator.Emit(OpCodes.Ldarg_2);
			generator.Emit(OpCodes.Callvirt, __IsDBNull__);
			generator.Emit(OpCodes.Brtrue, ending);

			//local_0 = (T)target;
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldind_Ref);
			if(field.DeclaringType.IsValueType)
				generator.Emit(OpCodes.Unbox_Any, field.DeclaringType);
			else
				generator.Emit(OpCodes.Castclass, field.DeclaringType);
			generator.Emit(OpCodes.Stloc_0);

			//local_0 = ...
			if(field.DeclaringType.IsValueType)
				generator.Emit(OpCodes.Ldloca_S, 0);
			else
				generator.Emit(OpCodes.Ldloc_0);

			if(converter == null)
			{
				//local_0.FieldX = DataRecordExtension.GetValue<T>(record, ordinal)
				generator.Emit(OpCodes.Ldarg_1);
				generator.Emit(OpCodes.Ldarg_2);
				generator.Emit(OpCodes.Call, __GetValueExtension__.MakeGenericMethod(fieldType));

				if(underlyingType != null)
					generator.Emit(OpCodes.Newobj, typeof(Nullable<>).MakeGenericType(underlyingType).GetConstructor(new[] { underlyingType }));

				generator.Emit(OpCodes.Stfld, field);
			}
			else
			{
				generator.Emit(OpCodes.Ldarg_3);

				//record.GetValue(ordinal)
				generator.Emit(OpCodes.Ldarg_1);
				generator.Emit(OpCodes.Ldarg_2);
				generator.Emit(OpCodes.Callvirt, __GetValueRecord__);

				//local_0.FieldX = (TField)converter(...)
				generator.Emit(OpCodes.Callvirt, __ConvertFrom__);

				if(field.FieldType.IsValueType)
					generator.Emit(OpCodes.Unbox_Any, field.FieldType);
				else
					generator.Emit(OpCodes.Castclass, field.FieldType);

				generator.Emit(OpCodes.Stfld, field);
			}

			//target = local_0
			if(field.DeclaringType.IsValueType)
			{
				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldloc_0);
				generator.Emit(OpCodes.Box, field.DeclaringType);
				generator.Emit(OpCodes.Stind_Ref);
			}

			generator.MarkLabel(ending);
			generator.Emit(OpCodes.Ret);

			return (Populator)method.CreateDelegate(typeof(Populator));
		}

		public static Populator<TModel> GenerateFieldSetter<TModel>(FieldInfo field, TypeConverter converter)
		{
			var method = new DynamicMethod(field.DeclaringType.FullName + "$Set" + field.Name, null,
				new Type[] { typeof(TModel).MakeByRefType(), typeof(IDataRecord), typeof(int), typeof(TypeConverter) },
				typeof(ModelMemberEmitter), true);

			var generator = method.GetILGenerator();
			var fieldType = field.FieldType;

			if(Zongsoft.Common.TypeExtension.IsNullable(fieldType, out var underlyingType))
				fieldType = underlyingType;

			if(fieldType.IsEnum)
				fieldType = Enum.GetUnderlyingType(fieldType);

			var ending = generator.DefineLabel();

			//if(record.IsDBNull(ordinal))
			generator.Emit(OpCodes.Ldarg_1);
			generator.Emit(OpCodes.Ldarg_2);
			generator.Emit(OpCodes.Callvirt, __IsDBNull__);
			generator.Emit(OpCodes.Brtrue, ending);

			//target.~~~
			generator.Emit(OpCodes.Ldarg_0);
			if(!field.DeclaringType.IsValueType)
				generator.Emit(OpCodes.Ldind_Ref);

			if(converter == null)
			{
				//target.FieldX = DataRecordExtension.GetValue<T>(record, ordinal)
				generator.Emit(OpCodes.Ldarg_1);
				generator.Emit(OpCodes.Ldarg_2);
				generator.Emit(OpCodes.Call, __GetValueExtension__.MakeGenericMethod(fieldType));

				if(underlyingType != null)
					generator.Emit(OpCodes.Newobj, typeof(Nullable<>).MakeGenericType(underlyingType).GetConstructor(new[] { underlyingType }));

				generator.Emit(OpCodes.Stfld, field);
			}
			else
			{
				generator.Emit(OpCodes.Ldarg_3);

				//record.GetValue(ordinal)
				generator.Emit(OpCodes.Ldarg_1);
				generator.Emit(OpCodes.Ldarg_2);
				generator.Emit(OpCodes.Callvirt, __GetValueRecord__);

				//target.FieldX = (TField)converter(...)
				generator.Emit(OpCodes.Callvirt, __ConvertFrom__);

				if(field.FieldType.IsValueType)
					generator.Emit(OpCodes.Unbox_Any, field.FieldType);
				else
					generator.Emit(OpCodes.Castclass, field.FieldType);

				generator.Emit(OpCodes.Stfld, field);
			}

			generator.MarkLabel(ending);
			generator.Emit(OpCodes.Ret);

			return (Populator<TModel>)method.CreateDelegate(typeof(Populator<TModel>));
		}

		public static Populator GeneratePropertySetter(PropertyInfo property, TypeConverter converter)
		{
			var method = new DynamicMethod(property.DeclaringType.FullName + "$Set" + property.Name, null,
				new Type[] { typeof(object).MakeByRefType(), typeof(IDataRecord), typeof(int), typeof(TypeConverter) },
				typeof(ModelMemberEmitter), true);

			var generator = method.GetILGenerator();
			var propertyType = property.PropertyType;

			if(Zongsoft.Common.TypeExtension.IsNullable(propertyType, out var underlyingType))
				propertyType = underlyingType;

			if(propertyType.IsEnum)
				propertyType = Enum.GetUnderlyingType(propertyType);

			var ending = generator.DefineLabel();

			generator.DeclareLocal(property.DeclaringType);

			//if(record.IsDBNull(ordinal))
			generator.Emit(OpCodes.Ldarg_1);
			generator.Emit(OpCodes.Ldarg_2);
			generator.Emit(OpCodes.Callvirt, __IsDBNull__);
			generator.Emit(OpCodes.Brtrue, ending);

			//local_0 = (T)target;
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldind_Ref);
			if(property.DeclaringType.IsValueType)
				generator.Emit(OpCodes.Unbox_Any, property.DeclaringType);
			else
				generator.Emit(OpCodes.Castclass, property.DeclaringType);
			generator.Emit(OpCodes.Stloc_0);

			//local_0 = ...
			if(property.DeclaringType.IsValueType)
				generator.Emit(OpCodes.Ldloca_S, 0);
			else
				generator.Emit(OpCodes.Ldloc_0);

			if(converter == null)
			{
				//local_0.PropertyX = DataRecordExtension.GetValue<T>(record, ordinal)
				generator.Emit(OpCodes.Ldarg_1);
				generator.Emit(OpCodes.Ldarg_2);
				generator.Emit(OpCodes.Call, __GetValueExtension__.MakeGenericMethod(propertyType));

				if(underlyingType != null)
					generator.Emit(OpCodes.Newobj, typeof(Nullable<>).MakeGenericType(underlyingType).GetConstructor(new[] { underlyingType }));

				generator.Emit(OpCodes.Callvirt, property.SetMethod);
			}
			else
			{
				generator.Emit(OpCodes.Ldarg_3);

				//record.GetValue(ordinal)
				generator.Emit(OpCodes.Ldarg_1);
				generator.Emit(OpCodes.Ldarg_2);
				generator.Emit(OpCodes.Callvirt, __GetValueRecord__);

				//local_0.PropertyX = (TProperty)converter(...)
				generator.Emit(OpCodes.Callvirt, __ConvertFrom__);

				if(property.PropertyType.IsValueType)
					generator.Emit(OpCodes.Unbox_Any, property.PropertyType);
				else
					generator.Emit(OpCodes.Castclass, property.PropertyType);

				generator.Emit(OpCodes.Callvirt, property.SetMethod);
			}

			//target = local_0
			if(property.DeclaringType.IsValueType)
			{
				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldloc_0);
				generator.Emit(OpCodes.Box, property.DeclaringType);
				generator.Emit(OpCodes.Stind_Ref);
			}

			generator.MarkLabel(ending);
			generator.Emit(OpCodes.Ret);

			return (Populator)method.CreateDelegate(typeof(Populator));
		}

		public static Populator<TModel> GeneratePropertySetter<TModel>(PropertyInfo property, TypeConverter converter)
		{
			var method = new DynamicMethod(property.DeclaringType.FullName + "$Set" + property.Name, null,
				new Type[] { typeof(TModel).MakeByRefType(), typeof(IDataRecord), typeof(int), typeof(TypeConverter) },
				typeof(ModelMemberEmitter), true);

			var generator = method.GetILGenerator();
			var propertyType = property.PropertyType;

			if(Zongsoft.Common.TypeExtension.IsNullable(propertyType, out var underlyingType))
				propertyType = underlyingType;

			if(propertyType.IsEnum)
				propertyType = Enum.GetUnderlyingType(propertyType);

			var ending = generator.DefineLabel();

			//if(record.IsDBNull(ordinal))
			generator.Emit(OpCodes.Ldarg_1);
			generator.Emit(OpCodes.Ldarg_2);
			generator.Emit(OpCodes.Callvirt, __IsDBNull__);
			generator.Emit(OpCodes.Brtrue, ending);

			//target.~~~
			generator.Emit(OpCodes.Ldarg_0);
			if(!property.DeclaringType.IsValueType)
				generator.Emit(OpCodes.Ldind_Ref);

			if(converter == null)
			{
				//target.PropertyX = DataRecordExtension.GetValue<T>(record, ordinal)
				generator.Emit(OpCodes.Ldarg_1);
				generator.Emit(OpCodes.Ldarg_2);
				generator.Emit(OpCodes.Call, __GetValueExtension__.MakeGenericMethod(propertyType));

				if(underlyingType != null)
					generator.Emit(OpCodes.Newobj, typeof(Nullable<>).MakeGenericType(underlyingType).GetConstructor(new[] { underlyingType }));

				generator.Emit(OpCodes.Callvirt, property.SetMethod);
			}
			else
			{
				generator.Emit(OpCodes.Ldarg_3);

				//record.GetValue(ordinal)
				generator.Emit(OpCodes.Ldarg_1);
				generator.Emit(OpCodes.Ldarg_2);
				generator.Emit(OpCodes.Callvirt, __GetValueRecord__);

				//target.PropertyX = (TProperty)converter(...)
				generator.Emit(OpCodes.Callvirt, __ConvertFrom__);

				if(property.PropertyType.IsValueType)
					generator.Emit(OpCodes.Unbox_Any, property.PropertyType);
				else
					generator.Emit(OpCodes.Castclass, property.PropertyType);

				generator.Emit(OpCodes.Callvirt, property.SetMethod);
			}

			generator.MarkLabel(ending);
			generator.Emit(OpCodes.Ret);

			return (Populator<TModel>)method.CreateDelegate(typeof(Populator<TModel>));
		}
		#endregion
	}
}
