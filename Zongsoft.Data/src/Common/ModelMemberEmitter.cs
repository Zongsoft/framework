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

namespace Zongsoft.Data.Common;

public static class ModelMemberEmitter
{
	#region 委托定义
	public delegate void PopulatorWithGetter(ref object target, IDataRecord record, int ordinal, IDataRecordGetter getter);
	public delegate void PopulatorWithGetter<T>(ref T target, IDataRecord record, int ordinal, IDataRecordGetter getter);
	public delegate void PopulatorWithConverter(ref object target, IDataRecord record, int ordinal, Func<object, Type, object> converter);
	public delegate void PopulatorWithConverter<T>(ref T target, IDataRecord record, int ordinal, Func<object, Type, object> converter);
	#endregion

	#region 私有变量
	private static readonly MethodInfo __IsDBNull__ = typeof(IDataRecord).GetMethod(nameof(IDataRecord.IsDBNull), [typeof(int)]);
	private static readonly MethodInfo __GetValueOfRecord__ = typeof(IDataRecord).GetMethod(nameof(IDataRecord.GetValue), [typeof(int)]);
	private static readonly MethodInfo __GetValueOfGetterInstance__ = typeof(DataRecordGetter).GetMethod(nameof(DataRecordGetter.GetValue), [typeof(IDataRecord), typeof(int)]);
	private static readonly MethodInfo __GetValueOfGetterInterface__ = typeof(IDataRecordGetter).GetMethod(nameof(IDataRecordGetter.GetValue), [typeof(IDataRecord), typeof(int)]);
	private static readonly MethodInfo __GetTypeFromHandler__ = typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle), [typeof(RuntimeTypeHandle)]);
	private static readonly MethodInfo __Invoke__ = typeof(Func<object, Type, object>).GetMethod(nameof(Func<object, Type, object>.Invoke), [typeof(object), typeof(Type)]);
	private static readonly FieldInfo __GetterDefaultField__ = typeof(DataRecordGetter).GetField(nameof(DataRecordGetter.Default), BindingFlags.Public | BindingFlags.Static);
	#endregion

	#region 公共方法
	/*
	 * 动态生成的字段设置方法的代码大致如下：
	 * static void SetFieldX(
	 *      ref TModel target,
	 *      IDataRecord record,
	 *      int ordinal,
	 *      IDataRecordGetter getter)
	 * {
	 *     if(record.IsDBNull(ordinal))
	 *         return;
	 * 
	 *     target.FieldX = getter != null ?
	 *         getter.GetValue<TField>(record, ordinal) :
	 *         DataRecordGetter.Default.GetValue<TField>(record, ordinal);
	 * }
	 */
	public static PopulatorWithGetter<TModel> GenerateFieldSetter<TModel>(FieldInfo field)
	{
		var method = new DynamicMethod($"{field.DeclaringType.FullName}$Set{field.Name}", null,
			[typeof(TModel).MakeByRefType(), typeof(IDataRecord), typeof(int), typeof(IDataRecordGetter)],
			typeof(ModelMemberEmitter), true);

		var generator = method.GetILGenerator();
		var fieldType = field.FieldType;

		if(Zongsoft.Common.TypeExtension.IsNullable(fieldType, out var underlyingType))
			fieldType = underlyingType;

		if(fieldType.IsEnum)
			fieldType = Enum.GetUnderlyingType(fieldType);

		var endingLabel = generator.DefineLabel();
		var getterLabel = generator.DefineLabel();
		var setterLabel = generator.DefineLabel();

		//if(record.IsDBNull(ordinal))
		generator.Emit(OpCodes.Ldarg_1);
		generator.Emit(OpCodes.Ldarg_2);
		generator.Emit(OpCodes.Callvirt, __IsDBNull__);
		generator.Emit(OpCodes.Brtrue, endingLabel);

		//target.~~~
		generator.Emit(OpCodes.Ldarg_0);
		if(!field.DeclaringType.IsValueType)
			generator.Emit(OpCodes.Ldind_Ref);

		//getter??
		generator.Emit(OpCodes.Ldarg_3); //getter
		generator.Emit(OpCodes.Brfalse_S, getterLabel); //getter is null then goto ...

		//getter.GetValue<T>(record, ordinal)
		generator.Emit(OpCodes.Ldarg_3); //getter
		generator.Emit(OpCodes.Ldarg_1); //record
		generator.Emit(OpCodes.Ldarg_2); //ordinal
		generator.Emit(OpCodes.Callvirt, __GetValueOfGetterInterface__.MakeGenericMethod(fieldType));
		generator.Emit(OpCodes.Br_S, setterLabel);

		generator.MarkLabel(getterLabel);

		//DataRecordGetter.Default.GetValue<T>(record, ordinal)
		generator.Emit(OpCodes.Ldsfld, __GetterDefaultField__);
		generator.Emit(OpCodes.Ldarg_1); //record
		generator.Emit(OpCodes.Ldarg_2); //ordinal
		generator.Emit(OpCodes.Callvirt, __GetValueOfGetterInstance__.MakeGenericMethod(fieldType));

		generator.MarkLabel(setterLabel);

		if(underlyingType != null)
			generator.Emit(OpCodes.Newobj, typeof(Nullable<>).MakeGenericType(underlyingType).GetConstructor([underlyingType]));

		generator.Emit(OpCodes.Stfld, field);

		generator.MarkLabel(endingLabel);
		generator.Emit(OpCodes.Ret);

		return (PopulatorWithGetter<TModel>)method.CreateDelegate(typeof(PopulatorWithGetter<TModel>));
	}

	/*
	 * 动态生成的字段设置方法的代码大致如下：
	 * static void SetFieldX(
	 *      ref TModel target,
	 *      IDataRecord record,
	 *      int ordinal,
	 *      Func<object, Type, object> converter)
	 * {
	 *     if(record.IsDBNull(ordinal))
	 *         return;
	 * 
	 *     return target.FieldX = (TField)converter(record.GetValue(ordinal), typeof(TField));
	 * }
	 */
	public static PopulatorWithConverter<TModel> GenerateFieldSetter<TModel>(FieldInfo field, Func<object, Type, object> converter)
	{
		var method = new DynamicMethod($"{field.DeclaringType.FullName}$Set{field.Name}", null,
			[typeof(TModel).MakeByRefType(), typeof(IDataRecord), typeof(int), typeof(Func<object, Type, object>)],
			typeof(ModelMemberEmitter), true);

		var generator = method.GetILGenerator();
		var fieldType = field.FieldType;

		if(Zongsoft.Common.TypeExtension.IsNullable(fieldType, out var underlyingType))
			fieldType = underlyingType;

		if(fieldType.IsEnum)
			fieldType = Enum.GetUnderlyingType(fieldType);

		var endingLabel = generator.DefineLabel();

		//if(record.IsDBNull(ordinal))
		generator.Emit(OpCodes.Ldarg_1);
		generator.Emit(OpCodes.Ldarg_2);
		generator.Emit(OpCodes.Callvirt, __IsDBNull__);
		generator.Emit(OpCodes.Brtrue, endingLabel);

		//target.~~~
		generator.Emit(OpCodes.Ldarg_0);
		if(!field.DeclaringType.IsValueType)
			generator.Emit(OpCodes.Ldind_Ref);

		//converter
		generator.Emit(OpCodes.Ldarg_3);

		//record.GetValue(ordinal)
		generator.Emit(OpCodes.Ldarg_1);
		generator.Emit(OpCodes.Ldarg_2);
		generator.Emit(OpCodes.Callvirt, __GetValueOfRecord__);

		//typeof(TField)
		generator.Emit(OpCodes.Ldtoken, field.FieldType);
		generator.Emit(OpCodes.Call, __GetTypeFromHandler__);

		//converter.Invoke(...)
		generator.Emit(OpCodes.Callvirt, __Invoke__);

		//(TField)...
		if(field.FieldType.IsValueType)
			generator.Emit(OpCodes.Unbox_Any, field.FieldType);
		else
			generator.Emit(OpCodes.Castclass, field.FieldType);

		//if(underlyingType != null)
		//	generator.Emit(OpCodes.Newobj, typeof(Nullable<>).MakeGenericType(underlyingType).GetConstructor([underlyingType]));

		//target.FieldX = ...
		generator.Emit(OpCodes.Stfld, field);

		generator.MarkLabel(endingLabel);
		generator.Emit(OpCodes.Ret);

		return (PopulatorWithConverter<TModel>)method.CreateDelegate(typeof(PopulatorWithConverter<TModel>));
	}

	/*
	 * 动态生成的属性设置方法的代码大致如下：
	 * static void SetPropertyX(
	 *      ref TModel target,
	 *      IDataRecord record,
	 *      int ordinal,
	 *      IDataRecordGetter getter)
	 * {
	 *     if(record.IsDBNull(ordinal))
	 *         return;
	 * 
	 *     target.PropertyX = getter != null ?
	 *         getter.GetValue<TProperty>(record, ordinal) :
	 *         DataRecordGetter.Default.GetValue<TProperty>(record, ordinal);
	 * }
	 */
	public static PopulatorWithGetter<TModel> GeneratePropertySetter<TModel>(PropertyInfo property)
	{
		var method = new DynamicMethod($"{property.DeclaringType.FullName}$Set{property.Name}", null,
			[typeof(TModel).MakeByRefType(), typeof(IDataRecord), typeof(int), typeof(IDataRecordGetter)],
			typeof(ModelMemberEmitter), true);

		var generator = method.GetILGenerator();
		var propertyType = property.PropertyType;

		if(Zongsoft.Common.TypeExtension.IsNullable(propertyType, out var underlyingType))
			propertyType = underlyingType;

		if(propertyType.IsEnum)
			propertyType = Enum.GetUnderlyingType(propertyType);

		var endingLabel = generator.DefineLabel();
		var getterLabel = generator.DefineLabel();
		var setterLabel = generator.DefineLabel();

		//if(record.IsDBNull(ordinal))
		generator.Emit(OpCodes.Ldarg_1);
		generator.Emit(OpCodes.Ldarg_2);
		generator.Emit(OpCodes.Callvirt, __IsDBNull__);
		generator.Emit(OpCodes.Brtrue, endingLabel);

		//target.~~~
		generator.Emit(OpCodes.Ldarg_0);
		if(!property.DeclaringType.IsValueType)
			generator.Emit(OpCodes.Ldind_Ref);

		//getter??
		generator.Emit(OpCodes.Ldarg_3); //getter
		generator.Emit(OpCodes.Brfalse_S, getterLabel); //getter is null then goto ...

		//getter.GetValue<T>(record, ordinal)
		generator.Emit(OpCodes.Ldarg_3); //getter
		generator.Emit(OpCodes.Ldarg_1); //record
		generator.Emit(OpCodes.Ldarg_2); //ordinal
		generator.Emit(OpCodes.Callvirt, __GetValueOfGetterInterface__.MakeGenericMethod(propertyType));
		generator.Emit(OpCodes.Br_S, setterLabel);

		generator.MarkLabel(getterLabel);

		//DataRecordGetter.Default.GetValue<T>(record, ordinal)
		generator.Emit(OpCodes.Ldsfld, __GetterDefaultField__);
		generator.Emit(OpCodes.Ldarg_1); //record
		generator.Emit(OpCodes.Ldarg_2); //ordinal
		generator.Emit(OpCodes.Callvirt, __GetValueOfGetterInstance__.MakeGenericMethod(propertyType));

		generator.MarkLabel(setterLabel);

		if(underlyingType != null)
			generator.Emit(OpCodes.Newobj, typeof(Nullable<>).MakeGenericType(underlyingType).GetConstructor([underlyingType]));

		generator.Emit(OpCodes.Callvirt, property.SetMethod);

		generator.MarkLabel(endingLabel);
		generator.Emit(OpCodes.Ret);

		return (PopulatorWithGetter<TModel>)method.CreateDelegate(typeof(PopulatorWithGetter<TModel>));
	}

	/*
	 * 动态生成的属性设置方法的代码大致如下：
	 * static void SetPropertyX(
	 *      ref TModel target,
	 *      IDataRecord record,
	 *      int ordinal,
	 *      Func<object, Type, object> converter)
	 * {
	 *     if(record.IsDBNull(ordinal))
	 *         return;
	 * 
	 *     return target.PropertyX = (TProperty)converter(record.GetValue(ordinal), typeof(TProperty));
	 * }
	 */
	public static PopulatorWithConverter<TModel> GeneratePropertySetter<TModel>(PropertyInfo property, Func<object, Type, object> converter)
	{
		var method = new DynamicMethod($"{property.DeclaringType.FullName}$Set{property.Name}", null,
			[typeof(TModel).MakeByRefType(), typeof(IDataRecord), typeof(int), typeof(Func<object, Type, object>)],
			typeof(ModelMemberEmitter), true);

		var generator = method.GetILGenerator();
		var propertyType = property.PropertyType;

		if(Zongsoft.Common.TypeExtension.IsNullable(propertyType, out var underlyingType))
			propertyType = underlyingType;

		if(propertyType.IsEnum)
			propertyType = Enum.GetUnderlyingType(propertyType);

		var endingLabel = generator.DefineLabel();

		//if(record.IsDBNull(ordinal))
		generator.Emit(OpCodes.Ldarg_1);
		generator.Emit(OpCodes.Ldarg_2);
		generator.Emit(OpCodes.Callvirt, __IsDBNull__);
		generator.Emit(OpCodes.Brtrue, endingLabel);

		//target.~~~
		generator.Emit(OpCodes.Ldarg_0);
		if(!property.DeclaringType.IsValueType)
			generator.Emit(OpCodes.Ldind_Ref);

		//converter
		generator.Emit(OpCodes.Ldarg_3);

		//record.GetValue(ordinal)
		generator.Emit(OpCodes.Ldarg_1);
		generator.Emit(OpCodes.Ldarg_2);
		generator.Emit(OpCodes.Callvirt, __GetValueOfRecord__);

		//typeof(TProperty)
		generator.Emit(OpCodes.Ldtoken, property.PropertyType);
		generator.Emit(OpCodes.Call, __GetTypeFromHandler__);

		//converter.Invoke(...)
		generator.Emit(OpCodes.Callvirt, __Invoke__);

		//(TProperty)...
		if(property.PropertyType.IsValueType)
			generator.Emit(OpCodes.Unbox_Any, property.PropertyType);
		else
			generator.Emit(OpCodes.Castclass, property.PropertyType);

		//if(underlyingType != null)
		//	generator.Emit(OpCodes.Newobj, typeof(Nullable<>).MakeGenericType(underlyingType).GetConstructor([underlyingType]));

		//target.PropertyX = ...
		generator.Emit(OpCodes.Callvirt, property.SetMethod);

		generator.MarkLabel(endingLabel);
		generator.Emit(OpCodes.Ret);

		return (PopulatorWithConverter<TModel>)method.CreateDelegate(typeof(PopulatorWithConverter<TModel>));
	}
	#endregion
}
