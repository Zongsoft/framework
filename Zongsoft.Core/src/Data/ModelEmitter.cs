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
using System.Threading;
using System.Reflection;
using System.Reflection.Emit;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Data;

internal abstract class ModelEmitterBase
{
	#region 常量定义
	private const string MASK_VARIABLE = "$MASK$";
	private const string PROPERTY_NAMES_VARIABLE = "$$PROPERTY_NAMES";
	private const string PROPERTY_TOKENS_VARIABLE = "$$PROPERTY_TOKENS";
	#endregion

	#region 静态字段
	private static Type PROPERTY_TOKEN_TYPE = null;
	private static FieldInfo PROPERTY_TOKEN_GETTER_FIELD;
	private static FieldInfo PROPERTY_TOKEN_SETTER_FIELD;
	private static FieldInfo PROPERTY_TOKEN_ORDINAL_FIELD;
	private static ConstructorBuilder PROPERTY_TOKEN_CONSTRUCTOR;
	#endregion

	#region 成员字段
	private readonly ModuleBuilder _module;
	#endregion

	#region 构造函数
	protected ModelEmitterBase(ModuleBuilder module)
	{
		_module = module ?? throw new ArgumentNullException(nameof(module));
	}
	#endregion

	#region 保护属性
	protected ModuleBuilder Module => _module;
	#endregion

	#region 公共方法
	public Func<object> Compile(Type type)
	{
		ILGenerator generator;

		//如果是首次编译，则首先生成属性标记类型
		if(PROPERTY_TOKEN_TYPE == null)
			this.GeneratePropertyTokenClass();

		//生成类型构建器
		var builder = this.Build(type, GetClassName(type));

		//生成模型类型的注解
		GenerateModelTypeAnnotation(builder, type);

		//获取数据模型的属性集，以及确认“INotifyPropertyChanged”接口的激发方法或事件字段
		var properties = this.GetProperties(type, builder, out var propertyChanged);

		//获取可写属性的数量
		var countWritable = properties.Count(p => p.CanWrite);

		//定义掩码字段
		FieldBuilder mask = null;

		if(countWritable <= 8)
			mask = builder.DefineField(MASK_VARIABLE, typeof(byte), FieldAttributes.Private);
		else if(countWritable <= 16)
			mask = builder.DefineField(MASK_VARIABLE, typeof(UInt16), FieldAttributes.Private);
		else if(countWritable <= 32)
			mask = builder.DefineField(MASK_VARIABLE, typeof(UInt32), FieldAttributes.Private);
		else if(countWritable <= 64)
			mask = builder.DefineField(MASK_VARIABLE, typeof(UInt64), FieldAttributes.Private);
		else
			mask = builder.DefineField(MASK_VARIABLE, typeof(byte[]), FieldAttributes.Private);

		//生成属性定义以及嵌套子类
		this.GenerateProperties(builder, mask, properties, propertyChanged, out var methods);

		//生成构造函数
		GenerateConstructor(builder, countWritable, mask, properties);

		//生成静态构造函数
		this.GenerateTypeInitializer(builder, properties, methods, out var names, out var tokens);

		//生成“GetCount”方法
		this.GenerateGetCountMethod(builder, mask, countWritable);

		//生成“Reset”方法
		this.GenerateResetMethod(builder, mask, tokens);
		this.GenerateResetManyMethod(builder, mask, tokens);

		//生成“HasChanges”方法
		this.GenerateHasChangesMethod(builder, mask, names, tokens);

		//生成“GetChanges”方法
		this.GenerateGetChangesMethod(builder, mask, names, tokens);

		//生成“TryGetValue”方法
		this.GenerateTryGetValueMethod(builder, mask, tokens);

		//生成“TrySetValue”方法
		this.GenerateTrySetValueMethod(builder, tokens);

		//构建类型
		type = builder.CreateType();

		//生成创建实例的动态方法
		var creator = new DynamicMethod("Create", typeof(object), Type.EmptyTypes);

		generator = creator.GetILGenerator();
		generator.Emit(OpCodes.Newobj, type.GetConstructor(Type.EmptyTypes));
		generator.Emit(OpCodes.Ret);

		//返回实例创建方法的委托
		return (Func<object>)creator.CreateDelegate(typeof(Func<object>));
	}
	#endregion

	#region 抽象方法
	protected abstract TypeBuilder Build(Type type, string name);
	protected abstract IList<PropertyMetadata> GetProperties(Type type, TypeBuilder builder, out MemberInfo propertyChanged);
	protected abstract PropertyBuilder DefineProperty(TypeBuilder builder, PropertyMetadata property);
	#endregion

	#region 虚拟方法
	protected virtual MethodBuilder DefineResetMethod(TypeBuilder builder)
	{
		var method = builder.DefineMethod(typeof(Zongsoft.Data.IModel).FullName + "." + nameof(IModel.Reset),
			MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.NewSlot,
			typeof(bool),
			[typeof(string), typeof(object).MakeByRefType()]);

		//添加方法的实现标记
		builder.DefineMethodOverride(method, typeof(Zongsoft.Data.IModel).GetMethod(nameof(IModel.Reset), new[] { typeof(string), typeof(object).MakeByRefType() }));

		//定义方法参数
		method.DefineParameter(1, ParameterAttributes.None, "name");
		method.DefineParameter(2, ParameterAttributes.Out, "value");

		return method;
	}

	protected virtual MethodBuilder DefineResetManyMethod(TypeBuilder builder)
	{
		var method = builder.DefineMethod(typeof(Zongsoft.Data.IModel).FullName + "." + nameof(IModel.Reset),
			MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.NewSlot,
			null,
			[typeof(string[])]);

		//添加方法的实现标记
		builder.DefineMethodOverride(method, typeof(Zongsoft.Data.IModel).GetMethod(nameof(IModel.Reset), new[] { typeof(string[]) }));

		//定义方法参数
		method.DefineParameter(1, ParameterAttributes.None, "names").SetCustomAttribute(typeof(ParamArrayAttribute).GetConstructor(Type.EmptyTypes), new byte[0]);

		return method;
	}

	protected virtual MethodBuilder DefineGetCountMethod(TypeBuilder builder)
	{
		var method = builder.DefineMethod(typeof(Zongsoft.Data.IModel).FullName + "." + nameof(IModel.GetCount),
			MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.NewSlot,
			typeof(int),
			Type.EmptyTypes);

		//添加方法的实现标记
		builder.DefineMethodOverride(method, typeof(Zongsoft.Data.IModel).GetMethod(nameof(IModel.GetCount)));

		return method;
	}

	protected virtual MethodBuilder DefineHasChangesMethod(TypeBuilder builder)
	{
		var method = builder.DefineMethod(typeof(Zongsoft.Data.IModel).FullName + "." + nameof(IModel.HasChanges),
			MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.NewSlot,
			typeof(bool),
			[typeof(string[])]);

		//定义方法参数
		method.DefineParameter(1, ParameterAttributes.None, "names");

		//添加方法的实现标记
		builder.DefineMethodOverride(method, typeof(Zongsoft.Data.IModel).GetMethod(nameof(IModel.HasChanges)));

		return method;
	}

	protected virtual MethodBuilder DefineGetChangesMethod(TypeBuilder builder)
	{
		var method = builder.DefineMethod(typeof(Zongsoft.Data.IModel).FullName + "." + nameof(IModel.GetChanges),
			MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.NewSlot,
			typeof(IDictionary<string, object>),
			Type.EmptyTypes);

		//添加方法的实现标记
		builder.DefineMethodOverride(method, typeof(Zongsoft.Data.IModel).GetMethod(nameof(IModel.GetChanges)));

		return method;
	}

	protected virtual MethodBuilder DefineTryGetValueMethod(TypeBuilder builder)
	{
		var method = builder.DefineMethod(typeof(Zongsoft.Data.IModel).FullName + "." + nameof(IModel.TryGetValue),
			MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.NewSlot,
			typeof(bool),
			[typeof(string), typeof(object).MakeByRefType()]);

		//添加方法的实现标记
		builder.DefineMethodOverride(method, typeof(Zongsoft.Data.IModel).GetMethod(nameof(IModel.TryGetValue)));

		//定义方法参数
		method.DefineParameter(1, ParameterAttributes.None, "name");
		method.DefineParameter(2, ParameterAttributes.Out, "value");

		return method;
	}

	protected virtual MethodBuilder DefineTrySetValueMethod(TypeBuilder builder)
	{
		var method = builder.DefineMethod(typeof(Zongsoft.Data.IModel).FullName + "." + nameof(IModel.TrySetValue),
			MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.NewSlot,
			typeof(bool),
			[typeof(string), typeof(object)]);

		//添加方法的实现标记
		builder.DefineMethodOverride(method, typeof(Zongsoft.Data.IModel).GetMethod(nameof(IModel.TrySetValue)));

		//定义方法参数
		method.DefineParameter(1, ParameterAttributes.None, "name");
		method.DefineParameter(2, ParameterAttributes.None, "value");

		return method;
	}
	#endregion

	#region 生成方法
	private void GenerateProperties(TypeBuilder builder, FieldBuilder mask, IList<PropertyMetadata> properties, MemberInfo propertyChanged, out MethodToken[] methods)
	{
		//生成嵌套匿名委托静态类
		var nested = builder.DefineNestedType("!Methods!", TypeAttributes.NestedPrivate | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit);

		//创建返回参数（即方法标记）
		methods = new MethodToken[properties.Count];

		//定义只读属性的递增数量
		var timesReadOnly = 0;
		//定义可写属性的总数量
		var countWritable = properties.Count(p => p.CanWrite);

		//生成属性定义
		for(int i = 0; i < properties.Count; i++)
		{
			var metadata = properties[i].Metadata;
			var fieldType = properties[i].PropertyType;
			FieldBuilder field = null;

			//如果指定了实体属性标签，则进行必要的验证
			if(metadata != null)
			{
				switch(metadata.Mode)
				{
					case Model.PropertyImplementationMode.Default:
						if(metadata.Type != null)
						{
							if(!properties[i].PropertyType.IsAssignableFrom(metadata.Type))
								throw new InvalidOperationException($"The '{metadata.Type}' type of the '{properties[i].Name}' PropertyAttribute does not implement '{properties[i].PropertyType}' interface or class.");

							fieldType = metadata.Type;
						}

						break;
					case Model.PropertyImplementationMode.Singleton:
						if(metadata.Type != null && !properties[i].SingletonFactoryEnabled)
						{
							if(metadata.Type.IsValueType)
								throw new InvalidOperationException($"The {metadata.Type} type of singleton cannot be a value type.");

							if(!properties[i].PropertyType.IsAssignableFrom(metadata.Type))
								throw new InvalidOperationException($"The '{metadata.Type}' type of the '{properties[i].Name}' PropertyAttribute does not implement '{properties[i].PropertyType}' interface or class.");

							fieldType = metadata.Type;
						}

						break;
					case Model.PropertyImplementationMode.Extension:
						if(metadata.Type == null)
							throw new InvalidOperationException($"Missing type of the '{properties[i].Name}' property attribute.");

						break;
				}
			}

			if(properties[i].CanWrite || properties[i].HasDefaultValue ||
			  (metadata != null && metadata.Mode == Model.PropertyImplementationMode.Singleton))
				field = properties[i].Field = builder.DefineField(properties[i].GetFieldName(), fieldType, FieldAttributes.Private);

			var property = this.DefineProperty(builder, properties[i]);
			var getter = (MethodBuilder)property.GetMethod;
			var generator = getter.GetILGenerator();

			if(metadata == null || metadata.Mode == Model.PropertyImplementationMode.Default)
			{
				if(field == null)
				{
					//抛出“NotSupportedException”异常
					generator.Emit(OpCodes.Newobj, typeof(NotSupportedException).GetConstructor(Type.EmptyTypes));
					generator.Emit(OpCodes.Throw);
				}
				else
				{
					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, field);
					generator.Emit(OpCodes.Ret);
				}
			}
			else if(metadata.Mode == Model.PropertyImplementationMode.Extension)
			{
				var method = metadata.Type.GetMethod("Get" + properties[i].Name, BindingFlags.Public | BindingFlags.Static, null, new Type[] { properties[i].DeclaringType, properties[i].PropertyType }, null) ??
							 metadata.Type.GetMethod("Get" + properties[i].Name, BindingFlags.Public | BindingFlags.Static, null, new Type[] { properties[i].DeclaringType }, null);

				if(method == null)
					throw new InvalidOperationException($"Not found the extension method of the {properties[i].Name} property in the {metadata.Type.FullName} extension type.");

				if(method.ReturnType == null || method.ReturnType == typeof(void) || !properties[i].PropertyType.IsAssignableFrom(method.ReturnType))
					throw new InvalidOperationException($"The return type of the '{method}' extension method is missing or invalid.");

				generator.Emit(OpCodes.Ldarg_0);

				if(method.GetParameters().Length == 2)
				{
					if(field == null)
						LoadDefaultValue(generator, properties[i].PropertyType);
					else
					{
						generator.Emit(OpCodes.Ldarg_0);
						generator.Emit(OpCodes.Ldfld, field);
					}
				}

				generator.Emit(OpCodes.Call, method);
				generator.Emit(OpCodes.Ret);
			}
			else if(metadata.Mode == Model.PropertyImplementationMode.Singleton)
			{
				if(properties[i].HasDefaultValue)
				{
					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, field);
					generator.Emit(OpCodes.Ret);
				}
				else
				{
					properties[i].Synchrolock = builder.DefineField(properties[i].GetFieldName("@LOCK"), typeof(object), FieldAttributes.Private | FieldAttributes.InitOnly);
					ConstructorInfo ctor = null;

					if(!properties[i].SingletonFactoryEnabled)
					{
						var implementationType = GetCollectionImplementationType(metadata.Type ?? field.FieldType);
						ctor = implementationType.GetConstructor(Type.EmptyTypes);

						if(ctor == null)
							throw new InvalidOperationException($"The '{implementationType}' type of the '{properties[i].Name}' property is missing the default constructor.");
					}

					generator.DeclareLocal(typeof(object)); // for synchrolock variable
					generator.DeclareLocal(typeof(bool));

					var SETTER_EXIT_LABEL = generator.DefineLabel();
					var SETTER_LEAVE_LABEL = generator.DefineLabel();
					var SETTER_FINALLY_LABEL = generator.DefineLabel();

					// if($Field == null)
					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, field);
					generator.Emit(OpCodes.Brtrue_S, SETTER_EXIT_LABEL);

					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, properties[i].Synchrolock);
					generator.Emit(OpCodes.Stloc_0);
					generator.Emit(OpCodes.Ldc_I4_0);
					generator.Emit(OpCodes.Stloc_1);

					// try begin
					generator.BeginExceptionBlock();

					// lock(Synchrolock)
					generator.Emit(OpCodes.Ldloc_0);
					generator.Emit(OpCodes.Ldloca_S, 1);
					generator.Emit(OpCodes.Call, typeof(Monitor).GetMethod("Enter", new Type[] { typeof(object), typeof(bool).MakeByRefType() }));

					// if($Field != null)
					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, field);
					generator.Emit(OpCodes.Brtrue_S, SETTER_LEAVE_LABEL);

					if(ctor != null)
					{
						// $Field = new XXXX();
						generator.Emit(OpCodes.Ldarg_0);
						generator.Emit(OpCodes.Newobj, ctor);
						generator.Emit(OpCodes.Stfld, field);
					}
					else // $Field = FactoryClass.GetProperty(...);
					{
						var factory = properties[i].GetSingletonFactory();

						generator.Emit(OpCodes.Ldarg_0);
						if(factory.GetParameters().Length > 0)
							generator.Emit(OpCodes.Ldarg_0);
						generator.Emit(OpCodes.Call, factory);
						generator.Emit(OpCodes.Stfld, field);
					}

					// try end
					generator.MarkLabel(SETTER_LEAVE_LABEL);

					// finally begin
					generator.BeginFinallyBlock();

					generator.Emit(OpCodes.Ldloc_1);
					generator.Emit(OpCodes.Brfalse_S, SETTER_FINALLY_LABEL);

					generator.Emit(OpCodes.Ldloc_0);
					generator.Emit(OpCodes.Call, typeof(Monitor).GetMethod("Exit", new Type[] { typeof(object) }));

					// finally end
					generator.MarkLabel(SETTER_FINALLY_LABEL);
					generator.EndExceptionBlock();

					generator.MarkLabel(SETTER_EXIT_LABEL);

					// return this.$Field;
					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, field);
					generator.Emit(OpCodes.Ret);
				}
			}

			//生成获取属性字段的方法
			var getMethod = nested.DefineMethod("Get" + properties[i].Name,
				MethodAttributes.Assembly | MethodAttributes.Static, CallingConventions.Standard,
				typeof(object),
				new Type[] { property.DeclaringType });

			getMethod.DefineParameter(1, ParameterAttributes.None, "target");

			generator = getMethod.GetILGenerator();
			generator.Emit(OpCodes.Ldarg_0);
			//generator.Emit(OpCodes.Castclass, field.DeclaringType);
			if(field == null)
				generator.Emit(OpCodes.Callvirt, getter);
			else
				generator.Emit(OpCodes.Ldfld, field);
			if(properties[i].PropertyType.IsValueType)
				generator.Emit(OpCodes.Box, properties[i].PropertyType);
			generator.Emit(OpCodes.Ret);

			MethodBuilder setMethod = null;

			if(properties[i].CanWrite)
			{
				var setter = (MethodBuilder)property.SetMethod;
				generator = setter.GetILGenerator();
				var exit = generator.DefineLabel();
				MethodInfo extensionMethod = null;

				if(metadata == null || metadata.Mode == Model.PropertyImplementationMode.Default)
				{
					if(propertyChanged != null)
					{
						//生成属性值是否发生改变的判断检测
						GeneratePropertyValueChangeChecker(generator, property, field, exit);
					}
				}
				else if(metadata.Mode == Model.PropertyImplementationMode.Extension)
				{
					extensionMethod = metadata.Type.GetMethod("Set" + properties[i].Name, BindingFlags.Public | BindingFlags.Static, null, new Type[] { properties[i].DeclaringType, properties[i].PropertyType, properties[i].PropertyType, properties[i].PropertyType.MakeByRefType() }, null) ??
									  metadata.Type.GetMethod("Set" + properties[i].Name, BindingFlags.Public | BindingFlags.Static, null, new Type[] { properties[i].DeclaringType, properties[i].PropertyType, properties[i].PropertyType.MakeByRefType() }, null);

					if(extensionMethod == null)
					{
						if(propertyChanged != null)
						{
							//生成属性值是否发生改变的判断检测
							GeneratePropertyValueChangeChecker(generator, property, field, exit);
						}
					}
					else
					{
						if(extensionMethod.ReturnType != typeof(bool))
							throw new InvalidOperationException($"Invalid '{extensionMethod}' extension method, it's return type must be boolean type.");

						//定义扩展方法的输出参数类型(即当前属性类型)的本地变量
						generator.DeclareLocal(properties[i].PropertyType);

						//加载扩展方法的第一个参数值(this)
						generator.Emit(OpCodes.Ldarg_0);

						//加载扩展方法的第二个参数值(_memberField)
						if(extensionMethod.GetParameters().Length == 4)
						{
							if(field == null)
								LoadDefaultValue(generator, properties[i].PropertyType);
							else
							{
								generator.Emit(OpCodes.Ldarg_0);
								generator.Emit(OpCodes.Ldfld, field);
							}
						}

						//加载扩展方法的第二或第三个参数值(value)
						generator.Emit(OpCodes.Ldarg_1);

						//加载扩展方法的最后一个输出参数(out result)
						generator.Emit(OpCodes.Ldloca_S, 0);

						//调用扩展方法
						generator.Emit(OpCodes.Call, extensionMethod);
						generator.Emit(OpCodes.Brfalse_S, exit);
					}
				}
				else if(metadata.Mode == Model.PropertyImplementationMode.Singleton)
				{
					//抛出“NotSupportedException”异常
					generator.Emit(OpCodes.Newobj, typeof(NotSupportedException).GetConstructor(Type.EmptyTypes));
					generator.Emit(OpCodes.Throw);
				}

				generator.Emit(OpCodes.Ldarg_0);

				if(extensionMethod != null)
					generator.Emit(OpCodes.Ldloc_0);
				else
					generator.Emit(OpCodes.Ldarg_1);

				generator.Emit(OpCodes.Stfld, field);

				if(countWritable <= 64)
					generator.Emit(OpCodes.Ldarg_0);

				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldfld, mask);

				if(countWritable <= 8)
				{
					generator.Emit(OpCodes.Ldc_I4, (int)Math.Pow(2, i - timesReadOnly));
					generator.Emit(OpCodes.Or);
					generator.Emit(OpCodes.Conv_U1);
					generator.Emit(OpCodes.Stfld, mask);
				}
				else if(countWritable <= 16)
				{
					generator.Emit(OpCodes.Ldc_I4, (int)Math.Pow(2, i - timesReadOnly));
					generator.Emit(OpCodes.Or);
					generator.Emit(OpCodes.Conv_U2);
					generator.Emit(OpCodes.Stfld, mask);
				}
				else if(countWritable <= 32)
				{
					generator.Emit(OpCodes.Ldc_I4, (uint)Math.Pow(2, i - timesReadOnly));
					generator.Emit(OpCodes.Or);
					generator.Emit(OpCodes.Conv_U4);
					generator.Emit(OpCodes.Stfld, mask);
				}
				else if(countWritable <= 64)
				{
					generator.Emit(OpCodes.Ldc_I8, (long)Math.Pow(2, i - timesReadOnly));
					generator.Emit(OpCodes.Or);
					generator.Emit(OpCodes.Conv_U8);
					generator.Emit(OpCodes.Stfld, mask);
				}
				else
				{
					generator.Emit(OpCodes.Ldc_I4, (i - timesReadOnly) / 8);
					generator.Emit(OpCodes.Ldelema, typeof(byte));
					generator.Emit(OpCodes.Dup);
					generator.Emit(OpCodes.Ldind_U1);

					switch((i - timesReadOnly) % 8)
					{
						case 0:
							generator.Emit(OpCodes.Ldc_I4_1);
							break;
						case 1:
							generator.Emit(OpCodes.Ldc_I4_2);
							break;
						case 2:
							generator.Emit(OpCodes.Ldc_I4_4);
							break;
						case 3:
							generator.Emit(OpCodes.Ldc_I4_S, 8);
							break;
						case 4:
							generator.Emit(OpCodes.Ldc_I4_S, 16);
							break;
						case 5:
							generator.Emit(OpCodes.Ldc_I4_S, 32);
							break;
						case 6:
							generator.Emit(OpCodes.Ldc_I4_S, 64);
							break;
						case 7:
							generator.Emit(OpCodes.Ldc_I4_S, 128);
							break;
					}

					generator.Emit(OpCodes.Conv_U1);
					generator.Emit(OpCodes.Or);
					generator.Emit(OpCodes.Conv_U1);
					generator.Emit(OpCodes.Stind_I1);
				}

				//处理“PropertyChanged”事件
				if(propertyChanged != null)
				{
					if(propertyChanged is FieldInfo eventField)
					{
						var RAISE_LABEL = generator.DefineLabel();

						// this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("xxx"));
						generator.Emit(OpCodes.Ldarg_0);
						generator.Emit(OpCodes.Ldfld, eventField);
						generator.Emit(OpCodes.Dup);
						generator.Emit(OpCodes.Brtrue_S, RAISE_LABEL);

						generator.Emit(OpCodes.Pop);
						generator.Emit(OpCodes.Ret);

						generator.MarkLabel(RAISE_LABEL);

						generator.Emit(OpCodes.Ldarg_0);
						generator.Emit(OpCodes.Ldstr, properties[i].Name);
						generator.Emit(OpCodes.Newobj, typeof(PropertyChangedEventArgs).GetConstructor(new Type[] { typeof(string) }));
						generator.Emit(OpCodes.Call, eventField.FieldType.GetMethod("Invoke"));
					}
					else if(propertyChanged is MethodInfo eventMethod)
					{
						var parameters = eventMethod.GetParameters();

						if(parameters.Length == 1 && parameters[0].ParameterType == typeof(string))
						{
							// this.OnPropertyChanged("xxx");
							generator.Emit(OpCodes.Ldarg_0);
							generator.Emit(OpCodes.Ldstr, properties[i].Name);
							generator.Emit(OpCodes.Call, eventMethod);
						}
						else if(parameters.Length == 1 && parameters[0].ParameterType == typeof(PropertyChangedEventArgs))
						{
							// this.OnPropertyChanged(new PropertyChangedEventArgs("xxx"));
							generator.Emit(OpCodes.Ldarg_0);
							generator.Emit(OpCodes.Ldstr, properties[i].Name);
							generator.Emit(OpCodes.Newobj, typeof(PropertyChangedEventArgs).GetConstructor(new Type[] { typeof(string) }));
							generator.Emit(OpCodes.Call, eventMethod);
						}
					}
				}

				generator.MarkLabel(exit);
				generator.Emit(OpCodes.Ret);

				//生成设置属性的方法
				setMethod = nested.DefineMethod("Set" + properties[i].Name,
					MethodAttributes.Assembly | MethodAttributes.Static, CallingConventions.Standard,
					null,
					new Type[] { field.DeclaringType, typeof(object) });

				setMethod.DefineParameter(1, ParameterAttributes.None, "target");
				setMethod.DefineParameter(2, ParameterAttributes.None, "value");

				generator = setMethod.GetILGenerator();
				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldarg_1);
				if(properties[i].PropertyType.IsPrimitive)
				{
					generator.Emit(OpCodes.Ldtoken, properties[i].PropertyType);
					generator.Emit(OpCodes.Call, typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle), BindingFlags.Static | BindingFlags.Public));
					generator.Emit(OpCodes.Call, typeof(Convert).GetMethod(nameof(Convert.ChangeType), new[] { typeof(object), typeof(Type) }));
					generator.Emit(OpCodes.Unbox_Any, properties[i].PropertyType);
				}
				else if(properties[i].PropertyType.IsValueType)
				{
					generator.Emit(OpCodes.Unbox_Any, properties[i].PropertyType);
				}
				else
					generator.Emit(OpCodes.Castclass, properties[i].PropertyType);
				generator.Emit(OpCodes.Call, setter);
				generator.Emit(OpCodes.Ret);
			}
			else
			{
				timesReadOnly++;
			}

			//将委托方法保存到方法标记数组元素中
			methods[i] = new MethodToken(getMethod, setMethod);
		}

		//构建嵌套匿名静态类
		nested.CreateType();
	}

	private static void GeneratePropertyValueChangeChecker(ILGenerator generator, PropertyBuilder property, FieldBuilder field, Label exit)
	{
		if(property.PropertyType.IsPrimitive)
		{
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, field);
			generator.Emit(OpCodes.Ldarg_1);
			generator.Emit(OpCodes.Beq_S, exit);
		}
		else
		{
			var equality = property.PropertyType.GetMethod("op_Equality", BindingFlags.Public | BindingFlags.Static) ??
						   typeof(object).GetMethod("Equals", BindingFlags.Public | BindingFlags.Static);

			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, field);

			if(property.PropertyType.IsValueType && equality.Name == "Equals")
				generator.Emit(OpCodes.Box, property.PropertyType);

			generator.Emit(OpCodes.Ldarg_1);

			if(property.PropertyType.IsValueType && equality.Name == "Equals")
				generator.Emit(OpCodes.Box, property.PropertyType);

			generator.Emit(OpCodes.Call, equality);
			generator.Emit(OpCodes.Brtrue_S, exit);
		}
	}

	private void GenerateTypeInitializer(TypeBuilder builder, IList<PropertyMetadata> properties, MethodToken[] methods, out FieldBuilder names, out FieldBuilder tokens)
	{
		names = builder.DefineField(PROPERTY_NAMES_VARIABLE, typeof(string[]), FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.InitOnly);
		tokens = builder.DefineField(PROPERTY_TOKENS_VARIABLE, typeof(Dictionary<,>).MakeGenericType(typeof(string), PROPERTY_TOKEN_TYPE), FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.InitOnly);
		var entityType = _module.GetType(builder.UnderlyingSystemType.FullName);

		//定义只读属性的递增数量
		var timesReadOnly = 0;
		//定义可写属性的总数量
		var countWritable = properties.Count(p => p.CanWrite);

		var generator = builder.DefineTypeInitializer().GetILGenerator();

		generator.Emit(OpCodes.Ldc_I4, countWritable);
		generator.Emit(OpCodes.Newarr, typeof(string));

		for(int i = 0; i < properties.Count; i++)
		{
			//忽略只读属性
			if(!properties[i].CanWrite)
			{
				timesReadOnly++;
				continue;
			}

			generator.Emit(OpCodes.Dup);
			generator.Emit(OpCodes.Ldc_I4, i - timesReadOnly);
			generator.Emit(OpCodes.Ldstr, properties[i].Name);
			generator.Emit(OpCodes.Stelem_Ref);
		}

		generator.Emit(OpCodes.Stsfld, names);

		//重置只读属性的递增量
		timesReadOnly = 0;

		var DictionaryAddMethod = tokens.FieldType.GetMethod("Add");
		generator.Emit(OpCodes.Ldc_I4, properties.Count);
		generator.Emit(OpCodes.Newobj, tokens.FieldType.GetConstructor(new Type[] { typeof(int) }));

		for(int i = 0; i < properties.Count; i++)
		{
			generator.Emit(OpCodes.Dup);
			generator.Emit(OpCodes.Ldstr, properties[i].Name);
			generator.Emit(OpCodes.Ldc_I4, methods[i].SetMethod == null ? -1 : i - timesReadOnly);

			generator.Emit(OpCodes.Ldnull);
			if(methods[i].GetMethod != null)
			{
				generator.Emit(OpCodes.Ldftn, methods[i].GetMethod);
				generator.Emit(OpCodes.Newobj, typeof(Func<,>).MakeGenericType(typeof(object), typeof(object)).GetConstructor(new Type[] { typeof(object), typeof(IntPtr) }));
			}

			generator.Emit(OpCodes.Ldnull);
			if(methods[i].SetMethod != null)
			{
				generator.Emit(OpCodes.Ldftn, methods[i].SetMethod);
				generator.Emit(OpCodes.Newobj, typeof(Action<,>).MakeGenericType(typeof(object), typeof(object)).GetConstructor(new Type[] { typeof(object), typeof(IntPtr) }));
			}
			else
			{
				timesReadOnly++;
			}

			generator.Emit(OpCodes.Newobj, PROPERTY_TOKEN_CONSTRUCTOR);
			//generator.Emit(OpCodes.Newobj, tokenType.GetConstructor(new Type[] { typeof(int), typeof(Func<object, object>), typeof(Action<object, object>) }));
			generator.Emit(OpCodes.Call, DictionaryAddMethod);
		}

		generator.Emit(OpCodes.Stsfld, tokens);

		generator.Emit(OpCodes.Ret);
	}

	private static ConstructorBuilder GenerateConstructor(TypeBuilder builder, int count, FieldBuilder mask, IEnumerable<PropertyMetadata> properties)
	{
		var constructor = builder.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, null);
		var generator = constructor.GetILGenerator();

		generator.Emit(OpCodes.Ldarg_0);

		if(builder.BaseType == null || builder.BaseType == typeof(object))
			generator.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes));
		else
		{
			var ctor = builder.BaseType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null) ??
				throw new InvalidOperationException($"The {builder.BaseType} data model base class is missing the default constructor.");

			generator.Emit(OpCodes.Call, ctor);
		}

		if(count > 64)
		{
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldc_I4, (int)Math.Ceiling(count / 8.0));
			generator.Emit(OpCodes.Newarr, typeof(byte));
			generator.Emit(OpCodes.Stfld, mask);
		}

		//初始化必要的属性
		GenerateInitializer(generator, properties);

		generator.Emit(OpCodes.Ret);

		return constructor;
	}

	private static void GenerateInitializer(ILGenerator generator, IEnumerable<PropertyMetadata> properties)
	{
		object value;
		Type valueType;

		foreach(var property in properties)
		{
			if(property.Synchrolock != null)
			{
				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Newobj, typeof(object).GetConstructor(Type.EmptyTypes));
				generator.Emit(OpCodes.Stfld, property.Synchrolock);
			}

			if(property.HasDefaultValue)
			{
				valueType = property.DefaultValueAttribute.Value as Type;

				if(property.CanWrite)
				{
					if(valueType != null && property.PropertyType != typeof(Type))
					{
						if(!valueType.IsClass || valueType.IsAbstract)
							throw new InvalidOperationException($"The specified '{valueType.FullName}' type must be a non-abstract class when generate a default value via DefaultValueAttribute of the '{property.Name}' property.");

						if(!property.Field.FieldType.IsAssignableFrom(valueType))
							throw new InvalidOperationException($"The specified '{valueType}' default value type cannot be converted to the '{property.Field.FieldType}' type of property.");

						generator.Emit(OpCodes.Ldarg_0);
						generator.Emit(OpCodes.Newobj, valueType.GetConstructor(Type.EmptyTypes));
						generator.Emit(OpCodes.Call, property.Builder.SetMethod);
					}
					else
					{
						if(!Common.Convert.TryConvertValue(property.DefaultValueAttribute.Value, property.PropertyType, out value))
							throw new InvalidOperationException($"The '{property.DefaultValueAttribute.Value}' default value cannot be converted to the '{property.PropertyType}' type of property.");

						generator.Emit(OpCodes.Ldarg_0);
						LoadDefaultValue(generator, property.PropertyType, value);
						generator.Emit(OpCodes.Call, property.Builder.SetMethod);
					}
				}
				else if(property.Field != null)
				{
					if(valueType != null && property.Field.FieldType != typeof(Type))
					{
						if(property.SingletonFactoryEnabled)
						{
							var factory = property.GetSingletonFactory();

							//$Field=FactoryClass.GetProperty(...);
							generator.Emit(OpCodes.Ldarg_0);
							if(factory.GetParameters().Length > 0)
								generator.Emit(OpCodes.Ldnull);
							generator.Emit(OpCodes.Call, factory);
							generator.Emit(OpCodes.Stfld, property.Field);
						}
						else
						{
							if(!valueType.IsClass || valueType.IsAbstract)
								throw new InvalidOperationException($"The specified '{valueType.FullName}' type must be a non-abstract class when generate a default value via DefaultValueAttribute of the '{property.Name}' property.");

							if(!property.Field.FieldType.IsAssignableFrom(valueType))
								throw new InvalidOperationException($"The specified '{valueType}' default value type cannot be converted to the '{property.Field.FieldType}' type of field.");

							generator.Emit(OpCodes.Ldarg_0);
							generator.Emit(OpCodes.Newobj, valueType.GetConstructor(Type.EmptyTypes));
							generator.Emit(OpCodes.Stfld, property.Field);
						}
					}
					else
					{
						if(!Common.Convert.TryConvertValue(property.DefaultValueAttribute.Value, property.Field.FieldType, out value))
							throw new InvalidOperationException($"The '{property.DefaultValueAttribute.Value}' default value cannot be converted to the '{property.Field.FieldType}' type of field.");

						generator.Emit(OpCodes.Ldarg_0);
						LoadDefaultValue(generator, property.Field.FieldType, value);
						generator.Emit(OpCodes.Stfld, property.Field);
					}
				}
			}
		}
	}

	private void GeneratePropertyTokenClass()
	{
		var builder = _module.DefineType("<PropertyToken>", TypeAttributes.NotPublic | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit | TypeAttributes.SequentialLayout, typeof(ValueType));

		PROPERTY_TOKEN_ORDINAL_FIELD = builder.DefineField("Ordinal", typeof(int), FieldAttributes.Public | FieldAttributes.InitOnly);
		//PROPERTY_TOKEN_GETTER_FIELD = builder.DefineField("Getter", typeof(Func<,>).MakeGenericType(type, typeof(object)), FieldAttributes.Public | FieldAttributes.InitOnly);
		//PROPERTY_TOKEN_SETTER_FIELD = builder.DefineField("Setter", typeof(Action<,>).MakeGenericType(type, typeof(object)), FieldAttributes.Public | FieldAttributes.InitOnly);
		PROPERTY_TOKEN_GETTER_FIELD = builder.DefineField("Getter", typeof(Func<object, object>), FieldAttributes.Public | FieldAttributes.InitOnly);
		PROPERTY_TOKEN_SETTER_FIELD = builder.DefineField("Setter", typeof(Action<object, object>), FieldAttributes.Public | FieldAttributes.InitOnly);

		PROPERTY_TOKEN_CONSTRUCTOR = builder.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, new Type[] { typeof(int), PROPERTY_TOKEN_GETTER_FIELD.FieldType, PROPERTY_TOKEN_SETTER_FIELD.FieldType });
		PROPERTY_TOKEN_CONSTRUCTOR.DefineParameter(1, ParameterAttributes.None, "ordinal");
		PROPERTY_TOKEN_CONSTRUCTOR.DefineParameter(2, ParameterAttributes.None, "getter");
		PROPERTY_TOKEN_CONSTRUCTOR.DefineParameter(3, ParameterAttributes.None, "setter");

		var generator = PROPERTY_TOKEN_CONSTRUCTOR.GetILGenerator();
		generator.Emit(OpCodes.Ldarg_0);
		generator.Emit(OpCodes.Ldarg_1);
		generator.Emit(OpCodes.Stfld, PROPERTY_TOKEN_ORDINAL_FIELD);
		generator.Emit(OpCodes.Ldarg_0);
		generator.Emit(OpCodes.Ldarg_2);
		generator.Emit(OpCodes.Stfld, PROPERTY_TOKEN_GETTER_FIELD);
		generator.Emit(OpCodes.Ldarg_0);
		generator.Emit(OpCodes.Ldarg_3);
		generator.Emit(OpCodes.Stfld, PROPERTY_TOKEN_SETTER_FIELD);
		generator.Emit(OpCodes.Ret);

		PROPERTY_TOKEN_TYPE = builder.CreateType();
	}

	private void GenerateGetCountMethod(TypeBuilder builder, FieldBuilder mask, int count)
	{
		var method = this.DefineGetCountMethod(builder);

		if(method == null)
			return;

		//获取代码生成器
		var generator = method.GetILGenerator();

		//定义标签
		var EXIT_LABEL = generator.DefineLabel();
		var BODY_LABEL = generator.DefineLabel();
		var LOOP_BODY_LABEL = generator.DefineLabel();
		var LOOP_TEST_LABEL = generator.DefineLabel();
		var LOOP_INCREASE_LABEL = generator.DefineLabel();

		//定义本地变量(count)
		generator.DeclareLocal(typeof(int));

		//count = 0;
		generator.Emit(OpCodes.Ldc_I4_0);
		generator.Emit(OpCodes.Stloc_0);

		if(mask.FieldType.IsArray)
		{
			//定义本地变量(mask)
			generator.DeclareLocal(typeof(byte));
			//定义本地变量(i)，即for循环
			generator.DeclareLocal(typeof(int));

			//for(i=0; ...)
			generator.Emit(OpCodes.Ldc_I4_0);
			generator.Emit(OpCodes.Stloc_2);
			generator.Emit(OpCodes.Br_S, LOOP_TEST_LABEL);

			//循环内容区开始
			generator.MarkLabel(LOOP_BODY_LABEL);

			//mark=$MASK$[i];
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, mask);
			generator.Emit(OpCodes.Ldloc_2);
			generator.Emit(OpCodes.Ldelem_U1);
			generator.Emit(OpCodes.Stloc_1);

			//if(mark==0) continue;
			generator.Emit(OpCodes.Ldloc_1);
			generator.Emit(OpCodes.Brfalse_S, LOOP_INCREASE_LABEL);
		}
		else
		{
			//定义本地变量(mask)
			generator.DeclareLocal(mask.FieldType);

			//mark=$MASK$;
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, mask);
			generator.Emit(OpCodes.Stloc_1);

			//if($MASK$ == 0) return count;
			generator.Emit(OpCodes.Ldloc_1);
			generator.Emit(OpCodes.Brtrue_S, BODY_LABEL);

			generator.Emit(OpCodes.Ldc_I4_0);
			generator.Emit(OpCodes.Ret);

			generator.MarkLabel(BODY_LABEL);
		}

		var length = mask.FieldType.IsArray ? 8 : count;
		var labels = new Label[length - 1];

		for(int i = 0; i < length; i++)
		{
			var number = Math.Pow(2, i);

			if(i < length - 1)
				labels[i] = generator.DefineLabel();

			if(i > 0)
				generator.MarkLabel(labels[i - 1]);

			//if((mask & X) == X)
			generator.Emit(OpCodes.Ldloc_1);
			EmitLoadInteger(generator, length, number);
			generator.Emit(OpCodes.And);
			EmitLoadInteger(generator, length, number);
			generator.Emit(OpCodes.Bne_Un_S, i < length - 1 ? labels[i] : (mask.FieldType.IsArray ? LOOP_INCREASE_LABEL : EXIT_LABEL));

			//count++;
			generator.Emit(OpCodes.Ldloc_0);
			generator.Emit(OpCodes.Ldc_I4_1);
			generator.Emit(OpCodes.Add);
			generator.Emit(OpCodes.Stloc_0);
		}

		if(mask.FieldType.IsArray)
		{
			generator.MarkLabel(LOOP_INCREASE_LABEL);

			//for(...; ...; i++)
			generator.Emit(OpCodes.Ldloc_2);
			generator.Emit(OpCodes.Ldc_I4_1);
			generator.Emit(OpCodes.Add);
			generator.Emit(OpCodes.Stloc_2);

			generator.MarkLabel(LOOP_TEST_LABEL);

			//for(...; i<_MASK_.Length; ...)
			generator.Emit(OpCodes.Ldloc_2);
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, mask);
			generator.Emit(OpCodes.Ldlen);
			generator.Emit(OpCodes.Conv_I4);
			generator.Emit(OpCodes.Blt_S, LOOP_BODY_LABEL);
		}
		else
			generator.MarkLabel(EXIT_LABEL);

		//return count;
		generator.Emit(OpCodes.Ldloc_0);
		generator.Emit(OpCodes.Ret);
	}

	private void GenerateResetMethod(TypeBuilder builder, FieldBuilder mask, FieldBuilder tokens)
	{
		var method = this.DefineResetMethod(builder);

		if(method == null)
			return;

		//获取代码生成器
		var generator = method.GetILGenerator();

		//定义标签
		var BODY_LABEL = generator.DefineLabel();
		var EXIT1_LABEL = generator.DefineLabel();
		var EXIT2_LABEL = generator.DefineLabel();

		//声明本地变量
		generator.DeclareLocal(PROPERTY_TOKEN_TYPE);

		//value=null;
		generator.Emit(OpCodes.Ldarg_2);
		generator.Emit(OpCodes.Ldnull);
		generator.Emit(OpCodes.Stind_Ref);

		//if(name==null || name.Length == 0)
		generator.Emit(OpCodes.Ldarg_1);
		generator.Emit(OpCodes.Brfalse_S, EXIT1_LABEL);

		generator.Emit(OpCodes.Ldarg_1);
		generator.Emit(OpCodes.Call, typeof(string).GetProperty(nameof(string.Length)).GetMethod);
		generator.Emit(OpCodes.Brtrue_S, BODY_LABEL);

		generator.MarkLabel(EXIT1_LABEL);

		//return false
		generator.Emit(OpCodes.Ldc_I4_0);
		generator.Emit(OpCodes.Ret);

		generator.MarkLabel(BODY_LABEL);

		//if(_TOKENS_.TryGetValue(name, out var token) && ...)
		generator.Emit(OpCodes.Ldsfld, tokens);
		generator.Emit(OpCodes.Ldarg_1);
		generator.Emit(OpCodes.Ldloca_S, 0);
		generator.Emit(OpCodes.Call, tokens.FieldType.GetMethod("TryGetValue", BindingFlags.Public | BindingFlags.Instance));
		generator.Emit(OpCodes.Brfalse_S, EXIT2_LABEL);

		if(mask.FieldType.IsArray)
		{
			//if(... && (($MASK$[token.Ordinal / 8] >> (token.Ordinal % 8)) & 1) == 1)
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, mask);
			generator.Emit(OpCodes.Ldloc_0);
			generator.Emit(OpCodes.Ldfld, PROPERTY_TOKEN_ORDINAL_FIELD);
			generator.Emit(OpCodes.Ldc_I4_8);
			generator.Emit(OpCodes.Div);
			generator.Emit(OpCodes.Ldelem_U1);
			generator.Emit(OpCodes.Ldloc_0);
			generator.Emit(OpCodes.Ldfld, PROPERTY_TOKEN_ORDINAL_FIELD);
			generator.Emit(OpCodes.Ldc_I4_8);
			generator.Emit(OpCodes.Rem);
			generator.Emit(OpCodes.Shr);
			generator.Emit(OpCodes.Ldc_I4_1);
			if(mask.FieldType == typeof(ulong))
				generator.Emit(OpCodes.Conv_I8);
			generator.Emit(OpCodes.And);
			generator.Emit(OpCodes.Ldc_I4_1);
			if(mask.FieldType == typeof(ulong))
				generator.Emit(OpCodes.Conv_I8);
			generator.Emit(OpCodes.Bne_Un_S, EXIT2_LABEL);

			//value=token.Getter.Invoke(this)
			generator.Emit(OpCodes.Ldarg_2);
			generator.Emit(OpCodes.Ldloc_0);
			generator.Emit(OpCodes.Ldfld, PROPERTY_TOKEN_GETTER_FIELD);
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Call, PROPERTY_TOKEN_GETTER_FIELD.FieldType.GetMethod("Invoke"));
			generator.Emit(OpCodes.Stind_Ref);

			//$MASK$[token.Ordianal / 8] = $MASK$[token.Ordianal / 8] & (byte)~(1 << (token.Ordinal % 8));
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, mask);
			generator.Emit(OpCodes.Ldloc_0);
			generator.Emit(OpCodes.Ldfld, PROPERTY_TOKEN_ORDINAL_FIELD);
			generator.Emit(OpCodes.Ldc_I4_8);
			generator.Emit(OpCodes.Div);

			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, mask);
			generator.Emit(OpCodes.Ldloc_0);
			generator.Emit(OpCodes.Ldfld, PROPERTY_TOKEN_ORDINAL_FIELD);
			generator.Emit(OpCodes.Ldc_I4_8);
			generator.Emit(OpCodes.Div);
			generator.Emit(OpCodes.Ldelem_U1);

			generator.Emit(OpCodes.Ldc_I4_1);
			generator.Emit(OpCodes.Ldloc_0);
			generator.Emit(OpCodes.Ldfld, PROPERTY_TOKEN_ORDINAL_FIELD);
			generator.Emit(OpCodes.Ldc_I4_8);
			generator.Emit(OpCodes.Rem);
			generator.Emit(OpCodes.Shl);
			generator.Emit(OpCodes.Not);
			generator.Emit(OpCodes.And);
			generator.Emit(OpCodes.Conv_U1);
			generator.Emit(OpCodes.Stelem_I1);
		}
		else
		{
			//if(... && ($MASK$ >> token.Ordinal & 1) == 1)
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, mask);
			generator.Emit(OpCodes.Ldloc_0);
			generator.Emit(OpCodes.Ldfld, PROPERTY_TOKEN_ORDINAL_FIELD);
			generator.Emit(OpCodes.Shr);
			generator.Emit(OpCodes.Ldc_I4_1);
			if(mask.FieldType == typeof(ulong))
				generator.Emit(OpCodes.Conv_I8);
			generator.Emit(OpCodes.And);
			generator.Emit(OpCodes.Ldc_I4_1);
			if(mask.FieldType == typeof(ulong))
				generator.Emit(OpCodes.Conv_I8);
			generator.Emit(OpCodes.Bne_Un_S, EXIT2_LABEL);

			//value=token.Getter.Invoke(this)
			generator.Emit(OpCodes.Ldarg_2);
			generator.Emit(OpCodes.Ldloc_0);
			generator.Emit(OpCodes.Ldfld, PROPERTY_TOKEN_GETTER_FIELD);
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Call, PROPERTY_TOKEN_GETTER_FIELD.FieldType.GetMethod("Invoke"));
			generator.Emit(OpCodes.Stind_Ref);

			//$MASK$ = $MASK$ & (type)~(1 << token.Ordinal);
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, mask);
			generator.Emit(OpCodes.Ldc_I4_1);
			generator.Emit(OpCodes.Ldloc_0);
			generator.Emit(OpCodes.Ldfld, PROPERTY_TOKEN_ORDINAL_FIELD);
			generator.Emit(OpCodes.Shl);
			generator.Emit(OpCodes.Not);
			if(mask.FieldType == typeof(ulong))
				generator.Emit(OpCodes.Conv_I8);
			generator.Emit(OpCodes.And);
			if(mask.FieldType == typeof(ulong))
				generator.Emit(OpCodes.Conv_I8);
			generator.Emit(OpCodes.Stfld, mask);
		}

		//return true
		generator.Emit(OpCodes.Ldc_I4_1);
		generator.Emit(OpCodes.Ret);

		generator.MarkLabel(EXIT2_LABEL);

		//return false
		generator.Emit(OpCodes.Ldc_I4_0);
		generator.Emit(OpCodes.Ret);
	}

	private void GenerateResetManyMethod(TypeBuilder builder, FieldBuilder mask, FieldBuilder tokens)
	{
		var method = this.DefineResetManyMethod(builder);

		if(method == null)
			return;

		//获取代码生成器
		var generator = method.GetILGenerator();

		//定义标签
		var EXIT_LABEL = generator.DefineLabel();
		var BODY_LABEL = generator.DefineLabel();
		var LOOP_INCREASE_LABEL = generator.DefineLabel();
		var LOOP_BODY_LABEL = generator.DefineLabel();
		var LOOP_TEST_LABEL = generator.DefineLabel();

		//声明本地变量
		generator.DeclareLocal(PROPERTY_TOKEN_TYPE);
		generator.DeclareLocal(typeof(int));

		generator.Emit(OpCodes.Ldarg_1);
		generator.Emit(OpCodes.Brfalse_S, EXIT_LABEL);

		generator.Emit(OpCodes.Ldarg_1);
		generator.Emit(OpCodes.Ldlen);
		generator.Emit(OpCodes.Brtrue_S, BODY_LABEL);

		generator.MarkLabel(EXIT_LABEL);

		if(mask.FieldType.IsArray)
		{
			//定义标签
			var INNER_LOOP_INCREASE_LABEL = generator.DefineLabel();
			var INNER_LOOP_BODY_LABEL = generator.DefineLabel();
			var INNER_LOOP_TEST_LABEL = generator.DefineLabel();

			//for(i=0;...;...)
			generator.Emit(OpCodes.Ldc_I4_0);
			generator.Emit(OpCodes.Stloc_1);

			//$MASK[i]=0;
			generator.MarkLabel(INNER_LOOP_BODY_LABEL);
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, mask);
			generator.Emit(OpCodes.Ldloc_1);
			generator.Emit(OpCodes.Ldc_I4_0);
			generator.Emit(OpCodes.Stelem_I1);

			//for(...;...;i++)
			generator.MarkLabel(INNER_LOOP_INCREASE_LABEL);
			generator.Emit(OpCodes.Ldloc_1);
			generator.Emit(OpCodes.Ldc_I4_1);
			generator.Emit(OpCodes.Add);
			generator.Emit(OpCodes.Stloc_1);

			//for(...;i<$MASK.Length$;...)
			generator.MarkLabel(INNER_LOOP_TEST_LABEL);
			generator.Emit(OpCodes.Ldloc_1);
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, mask);
			generator.Emit(OpCodes.Ldlen);
			generator.Emit(OpCodes.Conv_I4);
			generator.Emit(OpCodes.Blt_S, INNER_LOOP_BODY_LABEL);

			//return
			generator.Emit(OpCodes.Ret);
		}
		else
		{
			//$MASK$=0
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldc_I4_0);
			generator.Emit(OpCodes.Stfld, mask);
			generator.Emit(OpCodes.Ret);
		}

		generator.MarkLabel(BODY_LABEL);

		//for(i=0;...;...;)
		generator.Emit(OpCodes.Ldc_I4_0);
		generator.Emit(OpCodes.Stloc_1);
		generator.Emit(OpCodes.Br_S, LOOP_TEST_LABEL);

		generator.MarkLabel(LOOP_BODY_LABEL);

		//if(_TOKENS_.TryGetValue(names[i], out var token) && ...)
		generator.Emit(OpCodes.Ldsfld, tokens);
		generator.Emit(OpCodes.Ldarg_1);
		generator.Emit(OpCodes.Ldloc_1);
		generator.Emit(OpCodes.Ldelem_Ref);
		generator.Emit(OpCodes.Ldloca_S, 0);
		generator.Emit(OpCodes.Call, tokens.FieldType.GetMethod("TryGetValue", BindingFlags.Public | BindingFlags.Instance));
		generator.Emit(OpCodes.Brfalse_S, LOOP_INCREASE_LABEL);

		if(mask.FieldType.IsArray)
		{
			//if(... && (($MASK$[token.Ordinal / 8] >> (token.Ordinal % 8)) & 1) == 1)
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, mask);
			generator.Emit(OpCodes.Ldloc_0);
			generator.Emit(OpCodes.Ldfld, PROPERTY_TOKEN_ORDINAL_FIELD);
			generator.Emit(OpCodes.Ldc_I4_8);
			generator.Emit(OpCodes.Div);
			generator.Emit(OpCodes.Ldelem_U1);
			generator.Emit(OpCodes.Ldloc_0);
			generator.Emit(OpCodes.Ldfld, PROPERTY_TOKEN_ORDINAL_FIELD);
			generator.Emit(OpCodes.Ldc_I4_8);
			generator.Emit(OpCodes.Rem);
			generator.Emit(OpCodes.Shr);
			generator.Emit(OpCodes.Ldc_I4_1);
			if(mask.FieldType == typeof(ulong))
				generator.Emit(OpCodes.Conv_I8);
			generator.Emit(OpCodes.And);
			generator.Emit(OpCodes.Ldc_I4_1);
			if(mask.FieldType == typeof(ulong))
				generator.Emit(OpCodes.Conv_I8);
			generator.Emit(OpCodes.Bne_Un_S, LOOP_INCREASE_LABEL);

			//$MASK$[token.Ordianal / 8] = $MASK$[token.Ordianal / 8] & (byte)~(1 << (token.Ordinal % 8));
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, mask);
			generator.Emit(OpCodes.Ldloc_0);
			generator.Emit(OpCodes.Ldfld, PROPERTY_TOKEN_ORDINAL_FIELD);
			generator.Emit(OpCodes.Ldc_I4_8);
			generator.Emit(OpCodes.Div);

			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, mask);
			generator.Emit(OpCodes.Ldloc_0);
			generator.Emit(OpCodes.Ldfld, PROPERTY_TOKEN_ORDINAL_FIELD);
			generator.Emit(OpCodes.Ldc_I4_8);
			generator.Emit(OpCodes.Div);
			generator.Emit(OpCodes.Ldelem_U1);

			generator.Emit(OpCodes.Ldc_I4_1);
			generator.Emit(OpCodes.Ldloc_0);
			generator.Emit(OpCodes.Ldfld, PROPERTY_TOKEN_ORDINAL_FIELD);
			generator.Emit(OpCodes.Ldc_I4_8);
			generator.Emit(OpCodes.Rem);
			generator.Emit(OpCodes.Shl);
			generator.Emit(OpCodes.Not);
			generator.Emit(OpCodes.And);
			generator.Emit(OpCodes.Conv_U1);
			generator.Emit(OpCodes.Stelem_I1);
		}
		else
		{
			//if(... && ($MASK$ >> token.Ordinal & 1) == 1)
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, mask);
			generator.Emit(OpCodes.Ldloc_0);
			generator.Emit(OpCodes.Ldfld, PROPERTY_TOKEN_ORDINAL_FIELD);
			generator.Emit(OpCodes.Shr);
			generator.Emit(OpCodes.Ldc_I4_1);
			if(mask.FieldType == typeof(ulong))
				generator.Emit(OpCodes.Conv_I8);
			generator.Emit(OpCodes.And);
			generator.Emit(OpCodes.Ldc_I4_1);
			if(mask.FieldType == typeof(ulong))
				generator.Emit(OpCodes.Conv_I8);
			generator.Emit(OpCodes.Bne_Un_S, LOOP_INCREASE_LABEL);

			//$MASK$ = $MASK$ & (type)~(1 << token.Ordinal);
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, mask);
			generator.Emit(OpCodes.Ldc_I4_1);
			generator.Emit(OpCodes.Ldloc_0);
			generator.Emit(OpCodes.Ldfld, PROPERTY_TOKEN_ORDINAL_FIELD);
			generator.Emit(OpCodes.Shl);
			generator.Emit(OpCodes.Not);
			if(mask.FieldType == typeof(ulong))
				generator.Emit(OpCodes.Conv_I8);
			generator.Emit(OpCodes.And);
			if(mask.FieldType == typeof(ulong))
				generator.Emit(OpCodes.Conv_I8);
			generator.Emit(OpCodes.Stfld, mask);
		}

		//for(...;...;i++)
		generator.MarkLabel(LOOP_INCREASE_LABEL);
		generator.Emit(OpCodes.Ldloc_1);
		generator.Emit(OpCodes.Ldc_I4_1);
		generator.Emit(OpCodes.Add);
		generator.Emit(OpCodes.Stloc_1);

		//for(...;i<names.Length;...)
		generator.MarkLabel(LOOP_TEST_LABEL);
		generator.Emit(OpCodes.Ldloc_1);
		generator.Emit(OpCodes.Ldarg_1);
		generator.Emit(OpCodes.Ldlen);
		generator.Emit(OpCodes.Conv_I4);
		generator.Emit(OpCodes.Blt_S, LOOP_BODY_LABEL);

		generator.Emit(OpCodes.Ret);
	}

	private void GenerateHasChangesMethod(TypeBuilder builder, FieldBuilder mask, FieldBuilder names, FieldBuilder tokens)
	{
		var method = this.DefineHasChangesMethod(builder);

		if(method == null)
			return;

		//获取代码生成器
		var generator = method.GetILGenerator();

		generator.DeclareLocal(PROPERTY_TOKEN_TYPE);
		generator.DeclareLocal(typeof(int));

		var EXIT_LABEL = generator.DefineLabel();
		var MASKING_LABEL = generator.DefineLabel();
		var LOOP_INITIATE_LABEL = generator.DefineLabel();
		var LOOP_INCREASE_LABEL = generator.DefineLabel();
		var LOOP_BODY_LABEL = generator.DefineLabel();
		var LOOP_TEST_LABEL = generator.DefineLabel();

		// if(names==null || ...)
		generator.Emit(OpCodes.Ldarg_1);
		generator.Emit(OpCodes.Brfalse_S, MASKING_LABEL);

		// if(... || names.Length== 0)
		generator.Emit(OpCodes.Ldarg_1);
		generator.Emit(OpCodes.Ldlen);
		generator.Emit(OpCodes.Ldc_I4_0);
		generator.Emit(OpCodes.Ceq);
		generator.Emit(OpCodes.Brfalse_S, LOOP_INITIATE_LABEL);

		generator.MarkLabel(MASKING_LABEL);

		if(mask.FieldType.IsArray)
		{
			var INNER_LOOP_INCREASE_LABEL = generator.DefineLabel();
			var INNER_LOOP_BODY_LABEL = generator.DefineLabel();
			var INNER_LOOP_TEST_LABEL = generator.DefineLabel();

			// for(int i=0; ...; ...)
			generator.Emit(OpCodes.Ldc_I4_0);
			generator.Emit(OpCodes.Stloc_1, 0);
			generator.Emit(OpCodes.Br_S, INNER_LOOP_TEST_LABEL);

			generator.MarkLabel(INNER_LOOP_BODY_LABEL);

			// if(this.$MASK$[i] != 0)
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, mask);
			generator.Emit(OpCodes.Ldloc_1);
			generator.Emit(OpCodes.Ldelem_U1);
			generator.Emit(OpCodes.Brfalse_S, INNER_LOOP_INCREASE_LABEL);

			// return true;
			generator.Emit(OpCodes.Ldc_I4_1);
			generator.Emit(OpCodes.Ret);

			generator.MarkLabel(INNER_LOOP_INCREASE_LABEL);

			// for(...; ...; i++)
			generator.Emit(OpCodes.Ldloc_1);
			generator.Emit(OpCodes.Ldc_I4_1);
			generator.Emit(OpCodes.Add);
			generator.Emit(OpCodes.Stloc_1);

			generator.MarkLabel(INNER_LOOP_TEST_LABEL);

			// for(...; i<this.$MASK$.Length; ...)
			generator.Emit(OpCodes.Ldloc_1);
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, mask);
			generator.Emit(OpCodes.Ldlen);
			generator.Emit(OpCodes.Conv_I4);
			generator.Emit(OpCodes.Blt_S, INNER_LOOP_BODY_LABEL);

			// return false;
			generator.Emit(OpCodes.Ldc_I4_0);
			generator.Emit(OpCodes.Ret);
		}
		else
		{
			// return $MASK != 0;
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, mask);
			generator.Emit(OpCodes.Ldc_I4_0);
			if(mask.FieldType == typeof(ulong))
				generator.Emit(OpCodes.Conv_I8);
			generator.Emit(OpCodes.Cgt_Un);
			generator.Emit(OpCodes.Ret);
		}

		generator.MarkLabel(LOOP_INITIATE_LABEL);

		// for(int i=0; ...; ...)
		generator.Emit(OpCodes.Ldc_I4_0);
		generator.Emit(OpCodes.Stloc_1, 0);
		generator.Emit(OpCodes.Br_S, LOOP_TEST_LABEL);

		generator.MarkLabel(LOOP_BODY_LABEL);

		// if($$PROPERTIES$$.TryGetValue(names[i], out property) && ...)
		generator.Emit(OpCodes.Ldsfld, tokens);
		generator.Emit(OpCodes.Ldarg_1);
		generator.Emit(OpCodes.Ldloc_1);
		generator.Emit(OpCodes.Ldelem_Ref);
		generator.Emit(OpCodes.Ldloca_S, 0);
		generator.Emit(OpCodes.Call, tokens.FieldType.GetMethod("TryGetValue", BindingFlags.Public | BindingFlags.Instance));
		generator.Emit(OpCodes.Brfalse_S, LOOP_INCREASE_LABEL);

		// if(... && property.Setter != null && ...)
		generator.Emit(OpCodes.Ldloc_0);
		generator.Emit(OpCodes.Ldfld, PROPERTY_TOKEN_SETTER_FIELD);
		generator.Emit(OpCodes.Brfalse_S, LOOP_INCREASE_LABEL);

		if(mask.FieldType.IsArray)
		{
			// if(... && (this.$MASK$[property.Ordinal / 8] >> (property.Ordinal % 8) & 1) == 1)
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, mask);
			generator.Emit(OpCodes.Ldloc_0);
			generator.Emit(OpCodes.Ldfld, PROPERTY_TOKEN_ORDINAL_FIELD);
			generator.Emit(OpCodes.Ldc_I4_8);
			generator.Emit(OpCodes.Div);
			generator.Emit(OpCodes.Ldelem_U1);
			generator.Emit(OpCodes.Ldloc_0);
			generator.Emit(OpCodes.Ldfld, PROPERTY_TOKEN_ORDINAL_FIELD);
			generator.Emit(OpCodes.Ldc_I4_8);
			generator.Emit(OpCodes.Rem);
			generator.Emit(OpCodes.Shr);
			generator.Emit(OpCodes.Ldc_I4_1);
			generator.Emit(OpCodes.And);
			generator.Emit(OpCodes.Ldc_I4_1);
			generator.Emit(OpCodes.Bne_Un_S, LOOP_INCREASE_LABEL);
		}
		else
		{
			// if(... && (this.$MASK$ >> property.Ordinal & 1) == 1)
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, mask);
			generator.Emit(OpCodes.Ldloc_0);
			generator.Emit(OpCodes.Ldfld, PROPERTY_TOKEN_ORDINAL_FIELD);
			generator.Emit(OpCodes.Shr_Un);
			generator.Emit(OpCodes.Ldc_I4_1);
			if(mask.FieldType == typeof(ulong))
				generator.Emit(OpCodes.Conv_I8);
			generator.Emit(OpCodes.And);
			generator.Emit(OpCodes.Ldc_I4_1);
			if(mask.FieldType == typeof(ulong))
				generator.Emit(OpCodes.Conv_I8);
			generator.Emit(OpCodes.Ceq);
			generator.Emit(OpCodes.Brfalse_S, LOOP_INCREASE_LABEL);
		}

		// return true;
		generator.Emit(OpCodes.Ldc_I4_1);
		generator.Emit(OpCodes.Ret);

		generator.MarkLabel(LOOP_INCREASE_LABEL);

		// for(...; ...; i++)
		generator.Emit(OpCodes.Ldloc_1);
		generator.Emit(OpCodes.Ldc_I4_1);
		generator.Emit(OpCodes.Add);
		generator.Emit(OpCodes.Stloc_1);

		generator.MarkLabel(LOOP_TEST_LABEL);

		// for(...; i<names.Length; ...)
		generator.Emit(OpCodes.Ldloc_1);
		generator.Emit(OpCodes.Ldarg_1);
		generator.Emit(OpCodes.Ldlen);
		generator.Emit(OpCodes.Conv_I4);
		generator.Emit(OpCodes.Blt_S, LOOP_BODY_LABEL);

		generator.MarkLabel(EXIT_LABEL);

		// return false;
		generator.Emit(OpCodes.Ldc_I4_0);
		generator.Emit(OpCodes.Ret);
	}

	private void GenerateGetChangesMethod(TypeBuilder builder, FieldBuilder mask, FieldBuilder names, FieldBuilder tokens)
	{
		var method = this.DefineGetChangesMethod(builder);

		if(method == null)
			return;

		//获取代码生成器
		var generator = method.GetILGenerator();

		generator.DeclareLocal(typeof(Dictionary<string, object>));
		generator.DeclareLocal(typeof(int));

		var EXIT_LABEL = generator.DefineLabel();
		var BEGIN_LABEL = generator.DefineLabel();
		var LOOP_INITIATE_LABEL = generator.DefineLabel();
		var LOOP_INCREASE_LABEL = generator.DefineLabel();
		var LOOP_BODY_LABEL = generator.DefineLabel();
		var LOOP_TEST_LABEL = generator.DefineLabel();

		if(!mask.FieldType.IsArray)
		{
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, mask);
			generator.Emit(OpCodes.Brtrue_S, BEGIN_LABEL);

			generator.Emit(OpCodes.Ldnull);
			generator.Emit(OpCodes.Ret);
		}

		generator.MarkLabel(BEGIN_LABEL);

		// var dictioanry = new Dictionary<string, object>($$NAMES$$.Length);
		generator.Emit(OpCodes.Ldsfld, names);
		generator.Emit(OpCodes.Ldlen);
		generator.Emit(OpCodes.Conv_I4);
		generator.Emit(OpCodes.Newobj, typeof(Dictionary<string, object>).GetConstructor(new Type[] { typeof(int) }));
		generator.Emit(OpCodes.Stloc_0);

		generator.MarkLabel(LOOP_INITIATE_LABEL);

		// for(int i=0; ...; ...)
		generator.Emit(OpCodes.Ldc_I4_0);
		generator.Emit(OpCodes.Stloc_1);
		generator.Emit(OpCodes.Br_S, LOOP_TEST_LABEL);

		generator.MarkLabel(LOOP_BODY_LABEL);

		if(mask.FieldType.IsArray)
		{
			// if((this.$MASK$[i / 8] >> (i % 8) & 1) == 1)
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, mask);
			generator.Emit(OpCodes.Ldloc_1);
			generator.Emit(OpCodes.Ldc_I4_8);
			generator.Emit(OpCodes.Div);
			generator.Emit(OpCodes.Ldelem_U1);
			generator.Emit(OpCodes.Ldloc_1);
			generator.Emit(OpCodes.Ldc_I4_8);
			generator.Emit(OpCodes.Rem);
			generator.Emit(OpCodes.Shr);
			generator.Emit(OpCodes.Ldc_I4_1);
			generator.Emit(OpCodes.And);
			generator.Emit(OpCodes.Ldc_I4_1);
			generator.Emit(OpCodes.Bne_Un_S, LOOP_INCREASE_LABEL);
		}
		else
		{
			// if((this.$MASK$ >> i) & i == 1)
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, mask);
			generator.Emit(OpCodes.Ldloc_1);
			generator.Emit(OpCodes.Shr_Un);
			generator.Emit(OpCodes.Ldc_I4_1);
			if(mask.FieldType == typeof(ulong))
				generator.Emit(OpCodes.Conv_I8);
			generator.Emit(OpCodes.And);
			generator.Emit(OpCodes.Ldc_I4_1);
			if(mask.FieldType == typeof(ulong))
				generator.Emit(OpCodes.Conv_I8);
			generator.Emit(OpCodes.Bne_Un_S, LOOP_INCREASE_LABEL);
		}

		// dictioanry[$$NAMES[i]]
		generator.Emit(OpCodes.Ldloc_0);
		generator.Emit(OpCodes.Ldsfld, names);
		generator.Emit(OpCodes.Ldloc_1);
		generator.Emit(OpCodes.Ldelem_Ref);

		// $$PROPERTIES$$[$$NAMES$$[i]]
		generator.Emit(OpCodes.Ldsfld, tokens);
		generator.Emit(OpCodes.Ldsfld, names);
		generator.Emit(OpCodes.Ldloc_1);
		generator.Emit(OpCodes.Ldelem_Ref);
		generator.Emit(OpCodes.Call, tokens.FieldType.GetProperty("Item", new Type[] { typeof(string) }).GetMethod);

		generator.Emit(OpCodes.Ldfld, PROPERTY_TOKEN_GETTER_FIELD);
		generator.Emit(OpCodes.Ldarg_0);
		generator.Emit(OpCodes.Callvirt, PROPERTY_TOKEN_GETTER_FIELD.FieldType.GetMethod("Invoke"));
		generator.Emit(OpCodes.Callvirt, typeof(Dictionary<string, object>).GetProperty("Item", new Type[] { typeof(string) }).SetMethod);

		generator.MarkLabel(LOOP_INCREASE_LABEL);

		// for(...; ...; i++)
		generator.Emit(OpCodes.Ldloc_1);
		generator.Emit(OpCodes.Ldc_I4_1);
		generator.Emit(OpCodes.Add);
		generator.Emit(OpCodes.Stloc_1);

		generator.MarkLabel(LOOP_TEST_LABEL);

		// for(...; i<$NAMES$.Length; ...)
		generator.Emit(OpCodes.Ldloc_1);
		generator.Emit(OpCodes.Ldsfld, names);
		generator.Emit(OpCodes.Ldlen);
		generator.Emit(OpCodes.Conv_I4);
		generator.Emit(OpCodes.Blt_S, LOOP_BODY_LABEL);

		generator.MarkLabel(EXIT_LABEL);

		if(mask.FieldType.IsArray)
		{
			var RETURN_NULL_LABEL = generator.DefineLabel();

			generator.Emit(OpCodes.Ldloc_0);
			generator.Emit(OpCodes.Callvirt, typeof(Dictionary<string, object>).GetProperty("Count").GetMethod);
			generator.Emit(OpCodes.Brfalse_S, RETURN_NULL_LABEL);
			generator.Emit(OpCodes.Ldloc_0);
			generator.Emit(OpCodes.Ret);

			generator.MarkLabel(RETURN_NULL_LABEL);

			generator.Emit(OpCodes.Ldnull);
			generator.Emit(OpCodes.Ret);
		}
		else
		{
			generator.Emit(OpCodes.Ldloc_0);
			generator.Emit(OpCodes.Ret);
		}
	}

	private void GenerateTryGetValueMethod(TypeBuilder builder, FieldBuilder mask, FieldBuilder tokens)
	{
		var method = this.DefineTryGetValueMethod(builder);

		if(method == null)
			return;

		//获取代码生成器
		var generator = method.GetILGenerator();

		//声明本地变量
		generator.DeclareLocal(PROPERTY_TOKEN_TYPE);

		//定义代码标签
		var EXIT_LABEL = generator.DefineLabel();
		var GETBODY_LABEL = generator.DefineLabel();

		// value=null;
		generator.Emit(OpCodes.Ldarg_2);
		generator.Emit(OpCodes.Ldnull);
		generator.Emit(OpCodes.Stind_Ref);

		// if($$PROPERTIES.TryGet(name, out var property) && ...)
		generator.Emit(OpCodes.Ldsfld, tokens);
		generator.Emit(OpCodes.Ldarg_1);
		generator.Emit(OpCodes.Ldloca_S, 0);
		generator.Emit(OpCodes.Call, tokens.FieldType.GetMethod("TryGetValue", BindingFlags.Public | BindingFlags.Instance));
		generator.Emit(OpCodes.Brfalse_S, EXIT_LABEL);

		// if(... && (property.Ordinal<0 || ...))
		generator.Emit(OpCodes.Ldloc_0);
		generator.Emit(OpCodes.Ldfld, PROPERTY_TOKEN_ORDINAL_FIELD);
		generator.Emit(OpCodes.Ldc_I4_0);
		generator.Emit(OpCodes.Blt_S, GETBODY_LABEL);

		if(mask.FieldType.IsArray)
		{
			// if(... && (... || (this.$MASK$[property.Ordinal / 8] >> (property.Ordinal % 8) & 1) == 1))
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, mask);
			generator.Emit(OpCodes.Ldloc_0);
			generator.Emit(OpCodes.Ldfld, PROPERTY_TOKEN_ORDINAL_FIELD);
			generator.Emit(OpCodes.Ldc_I4_8);
			generator.Emit(OpCodes.Div);
			generator.Emit(OpCodes.Ldelem_U1);
			generator.Emit(OpCodes.Ldloc_0);
			generator.Emit(OpCodes.Ldfld, PROPERTY_TOKEN_ORDINAL_FIELD);
			generator.Emit(OpCodes.Ldc_I4_8);
			generator.Emit(OpCodes.Rem);
			generator.Emit(OpCodes.Shr);
			generator.Emit(OpCodes.Ldc_I4_1);
			generator.Emit(OpCodes.And);
			generator.Emit(OpCodes.Ldc_I4_1);
			generator.Emit(OpCodes.Bne_Un_S, EXIT_LABEL);
		}
		else
		{
			// if(... && (... || ((this.$MASK$ >> property.Ordinal) & 1) == 1))
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, mask);
			generator.Emit(OpCodes.Ldloc_0);
			generator.Emit(OpCodes.Ldfld, PROPERTY_TOKEN_ORDINAL_FIELD);
			generator.Emit(OpCodes.Shr_Un);
			generator.Emit(OpCodes.Ldc_I4_1);
			if(mask.FieldType == typeof(ulong))
				generator.Emit(OpCodes.Conv_I8);
			generator.Emit(OpCodes.And);
			generator.Emit(OpCodes.Ldc_I4_1);
			if(mask.FieldType == typeof(ulong))
				generator.Emit(OpCodes.Conv_I8);
			generator.Emit(OpCodes.Bne_Un_S, EXIT_LABEL);
		}

		generator.MarkLabel(GETBODY_LABEL);

		// value = property.Getter(this);
		generator.Emit(OpCodes.Ldarg_2);
		generator.Emit(OpCodes.Ldloc_0);
		generator.Emit(OpCodes.Ldfld, PROPERTY_TOKEN_GETTER_FIELD);
		generator.Emit(OpCodes.Ldarg_0);
		generator.Emit(OpCodes.Call, PROPERTY_TOKEN_GETTER_FIELD.FieldType.GetMethod("Invoke"));
		generator.Emit(OpCodes.Stind_Ref);

		// return true;
		generator.Emit(OpCodes.Ldc_I4_1);
		generator.Emit(OpCodes.Ret);

		generator.MarkLabel(EXIT_LABEL);

		//return false;
		generator.Emit(OpCodes.Ldc_I4_0);
		generator.Emit(OpCodes.Ret);
	}

	private void GenerateTrySetValueMethod(TypeBuilder builder, FieldBuilder tokens)
	{
		var method = this.DefineTrySetValueMethod(builder);

		if(method == null)
			return;

		//获取代码生成器
		var generator = method.GetILGenerator();

		//声明本地变量
		generator.DeclareLocal(PROPERTY_TOKEN_TYPE);

		//定义代码标签
		var EXIT_LABEL = generator.DefineLabel();

		// if($$PROPERTIES$$.TryGetValue(name, out var property))
		generator.Emit(OpCodes.Ldsfld, tokens);
		generator.Emit(OpCodes.Ldarg_1);
		generator.Emit(OpCodes.Ldloca_S, 0);
		generator.Emit(OpCodes.Call, tokens.FieldType.GetMethod("TryGetValue", BindingFlags.Public | BindingFlags.Instance));
		generator.Emit(OpCodes.Brfalse_S, EXIT_LABEL);

		// if(... && property.Setter != null)
		generator.Emit(OpCodes.Ldloc_0);
		generator.Emit(OpCodes.Ldfld, PROPERTY_TOKEN_SETTER_FIELD);
		generator.Emit(OpCodes.Brfalse_S, EXIT_LABEL);

		// property.Setter(this, value);
		generator.Emit(OpCodes.Ldloc_0);
		generator.Emit(OpCodes.Ldfld, PROPERTY_TOKEN_SETTER_FIELD);
		generator.Emit(OpCodes.Ldarg_0);
		generator.Emit(OpCodes.Ldarg_2);
		generator.Emit(OpCodes.Call, PROPERTY_TOKEN_SETTER_FIELD.FieldType.GetMethod("Invoke"));

		// return true;
		generator.Emit(OpCodes.Ldc_I4_1);
		generator.Emit(OpCodes.Ret);

		generator.MarkLabel(EXIT_LABEL);

		//return false;
		generator.Emit(OpCodes.Ldc_I4_0);
		generator.Emit(OpCodes.Ret);
	}

	protected static CustomAttributeBuilder GetAnnotation(CustomAttributeData attribute)
	{
		var arguments = new object[attribute.ConstructorArguments.Count];

		if(arguments.Length > 0)
		{
			for(int i = 0; i < attribute.ConstructorArguments.Count; i++)
			{
				if(attribute.ConstructorArguments[i].Value == null)
					arguments[i] = null;
				else
				{
					if(Zongsoft.Common.TypeExtension.IsEnumerable(attribute.ConstructorArguments[i].Value.GetType()) &&
					   Zongsoft.Common.TypeExtension.GetElementType(attribute.ConstructorArguments[i].Value.GetType()) == typeof(CustomAttributeTypedArgument))
					{
						var args = new List<object>();

						foreach(CustomAttributeTypedArgument arg in (IEnumerable)attribute.ConstructorArguments[i].Value)
						{
							args.Add(arg.Value);
						}

						arguments[i] = args.ToArray();
					}
					else
						arguments[i] = attribute.ConstructorArguments[i].Value;
				}
			}
		}

		if(attribute.NamedArguments.Count == 0)
			return new CustomAttributeBuilder(attribute.Constructor, arguments);

		var properties = attribute.NamedArguments.Where(p => !p.IsField).ToArray();
		var fields = attribute.NamedArguments.Where(p => p.IsField).ToArray();

		return new CustomAttributeBuilder(attribute.Constructor, arguments,
										  properties.Select(p => (PropertyInfo)p.MemberInfo).ToArray(),
										  properties.Select(p => p.TypedValue.Value).ToArray(),
										  fields.Select(p => (FieldInfo)p.MemberInfo).ToArray(),
										  fields.Select(p => p.TypedValue.Value).ToArray());
	}
	#endregion

	#region 私有方法
	private static void GenerateModelTypeAnnotation(TypeBuilder builder, Type type)
	{
		var constructor = typeof(Model.ModelTypeAttribute).GetConstructor([typeof(Type)]);
		var annotation = new CustomAttributeBuilder(constructor, [type]);
		builder.SetCustomAttribute(annotation);
	}

	private static void LoadDefaultValue(ILGenerator generator, Type type, object value = null)
	{
		if(type.IsValueType && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
		{
			if(value == null)
				generator.Emit(OpCodes.Ldnull);
			else
				LoadDefaultValue(generator, Nullable.GetUnderlyingType(type), value);

			return;
		}

		if(type.IsEnum)
		{
			LoadDefaultValue(generator, Enum.GetUnderlyingType(type), Convert.ChangeType(value, Enum.GetUnderlyingType(type)));
			return;
		}

		switch(Type.GetTypeCode(type))
		{
			case TypeCode.Boolean:
				if(value != null && (bool)Convert.ChangeType(value, typeof(bool)))
					generator.Emit(OpCodes.Ldc_I4_1);
				else
					generator.Emit(OpCodes.Ldc_I4_0);
				return;
			case TypeCode.Byte:
				if(value == null || (byte)Convert.ChangeType(value, TypeCode.Byte) == 0)
					generator.Emit(OpCodes.Ldc_I4_0);
				else
					generator.Emit(OpCodes.Ldc_I4, (byte)Convert.ChangeType(value, TypeCode.Byte));

				generator.Emit(OpCodes.Conv_U1);
				return;
			case TypeCode.SByte:
				if(value == null || (sbyte)Convert.ChangeType(value, TypeCode.SByte) == 0)
					generator.Emit(OpCodes.Ldc_I4_0);
				else
					generator.Emit(OpCodes.Ldc_I4, (sbyte)Convert.ChangeType(value, TypeCode.SByte));

				generator.Emit(OpCodes.Conv_I1);
				return;
			case TypeCode.Single:
				generator.Emit(OpCodes.Ldc_R4, value == null ? 0 : (float)Convert.ChangeType(value, TypeCode.Single));
				return;
			case TypeCode.Double:
				generator.Emit(OpCodes.Ldc_R8, value == null ? 0 : (double)Convert.ChangeType(value, TypeCode.Double));
				return;
			case TypeCode.Int16:
				if(value == null || (short)Convert.ChangeType(value, TypeCode.Int16) == 0)
					generator.Emit(OpCodes.Ldc_I4_0);
				else
					generator.Emit(OpCodes.Ldc_I4, (short)Convert.ChangeType(value, TypeCode.Int16));

				generator.Emit(OpCodes.Conv_I2);
				return;
			case TypeCode.Int32:
				if(value == null || (int)Convert.ChangeType(value, TypeCode.Int32) == 0)
					generator.Emit(OpCodes.Ldc_I4_0);
				else
					generator.Emit(OpCodes.Ldc_I4, (int)Convert.ChangeType(value, TypeCode.Int32));
				return;
			case TypeCode.Int64:
				if(value == null || (long)Convert.ChangeType(value, TypeCode.Int64) == 0)
				{
					generator.Emit(OpCodes.Ldc_I4_0);
					generator.Emit(OpCodes.Conv_I8);
				}
				else
					generator.Emit(OpCodes.Ldc_I8, (long)Convert.ChangeType(value, TypeCode.Int64));

				return;
			case TypeCode.UInt16:
				if(value == null || (ushort)Convert.ChangeType(value, TypeCode.UInt16) == 0)
					generator.Emit(OpCodes.Ldc_I4_0);
				else
					generator.Emit(OpCodes.Ldc_I4, (ushort)Convert.ChangeType(value, TypeCode.UInt16));

				generator.Emit(OpCodes.Conv_U2);
				return;
			case TypeCode.UInt32:
				if(value == null || (uint)Convert.ChangeType(value, TypeCode.UInt32) == 0)
					generator.Emit(OpCodes.Ldc_I4_0);
				else
					generator.Emit(OpCodes.Ldc_I4, (uint)Convert.ChangeType(value, TypeCode.UInt32));

				generator.Emit(OpCodes.Conv_U4);
				return;
			case TypeCode.UInt64:
				if(value == null || (ulong)Convert.ChangeType(value, TypeCode.UInt64) == 0)
					generator.Emit(OpCodes.Ldc_I4_0);
				else
					generator.Emit(OpCodes.Ldc_I8, (ulong)Convert.ChangeType(value, TypeCode.UInt64));

				generator.Emit(OpCodes.Conv_U8);
				return;
			case TypeCode.String:
				if(value == null)
					generator.Emit(OpCodes.Ldnull);
				else
					generator.Emit(OpCodes.Ldstr, value.ToString());

				return;
			case TypeCode.Char:
				if(value == null || (char)Convert.ChangeType(value, TypeCode.Char) == '\0')
					generator.Emit(OpCodes.Ldsfld, typeof(Char).GetField("MinValue", BindingFlags.Public | BindingFlags.Static));
				else
					generator.Emit(OpCodes.Ldc_I4_S, (char)Convert.ChangeType(value, TypeCode.Char));
				return;
			case TypeCode.DateTime:
				if(value == null || (DateTime)Convert.ChangeType(value, TypeCode.DateTime) == DateTime.MinValue)
					generator.Emit(OpCodes.Ldsfld, typeof(DateTime).GetField("MinValue", BindingFlags.Public | BindingFlags.Static));
				else
				{
					generator.Emit(OpCodes.Ldc_I8, ((DateTime)Convert.ChangeType(value, TypeCode.DateTime)).Ticks);
					generator.Emit(OpCodes.Newobj, typeof(DateTime).GetConstructor(new Type[] { typeof(long) }));
				}

				return;
			case TypeCode.Decimal:
				if(value == null)
					generator.Emit(OpCodes.Ldsfld, typeof(Decimal).GetField("Zero", BindingFlags.Public | BindingFlags.Static));
				else
				{
					switch(Type.GetTypeCode(value.GetType()))
					{
						case TypeCode.Single:
							generator.Emit(OpCodes.Ldc_R4, (float)Convert.ChangeType(value, typeof(float)));
							generator.Emit(OpCodes.Newobj, typeof(Decimal).GetConstructor(new Type[] { typeof(float) }));
							break;
						case TypeCode.Double:
							generator.Emit(OpCodes.Ldc_R8, (double)Convert.ChangeType(value, typeof(double)));
							generator.Emit(OpCodes.Newobj, typeof(Decimal).GetConstructor(new Type[] { typeof(double) }));
							break;
						case TypeCode.Decimal:
							var bits = Decimal.GetBits((decimal)Convert.ChangeType(value, TypeCode.Decimal));
							generator.Emit(OpCodes.Ldc_I4_S, bits.Length);
							generator.Emit(OpCodes.Newarr, typeof(int));

							for(int i = 0; i < bits.Length; i++)
							{
								generator.Emit(OpCodes.Dup);
								generator.Emit(OpCodes.Ldc_I4_S, i);
								generator.Emit(OpCodes.Ldc_I4, bits[i]);
								generator.Emit(OpCodes.Stelem_I4);
							}

							generator.Emit(OpCodes.Newobj, typeof(Decimal).GetConstructor(new Type[] { typeof(int[]) }));

							break;
						case TypeCode.SByte:
						case TypeCode.Int16:
						case TypeCode.Int32:
							generator.Emit(OpCodes.Ldc_I4, (int)Convert.ChangeType(value, typeof(int)));
							generator.Emit(OpCodes.Newobj, typeof(Decimal).GetConstructor(new Type[] { typeof(int) }));
							break;
						case TypeCode.Int64:
							generator.Emit(OpCodes.Ldc_I8, (long)Convert.ChangeType(value, typeof(long)));
							generator.Emit(OpCodes.Newobj, typeof(Decimal).GetConstructor(new Type[] { typeof(long) }));
							break;
						case TypeCode.Byte:
						case TypeCode.Char:
						case TypeCode.UInt16:
						case TypeCode.UInt32:
							generator.Emit(OpCodes.Ldc_I4, (int)Convert.ChangeType(value, typeof(int)));
							generator.Emit(OpCodes.Conv_U4);
							generator.Emit(OpCodes.Newobj, typeof(Decimal).GetConstructor(new Type[] { typeof(uint) }));
							break;
						case TypeCode.UInt64:
							generator.Emit(OpCodes.Ldc_I8, (long)Convert.ChangeType(value, typeof(long)));
							generator.Emit(OpCodes.Conv_U8);
							generator.Emit(OpCodes.Newobj, typeof(Decimal).GetConstructor(new Type[] { typeof(ulong) }));
							break;
						default:
							throw new InvalidOperationException($"Unable to convert '{value.GetType()}' type to decimal type.");
					}
				}

				return;
			case TypeCode.DBNull:
				generator.Emit(OpCodes.Ldsfld, typeof(DBNull).GetField("Value", BindingFlags.Public | BindingFlags.Static));
				return;
		}

		if(type.IsValueType)
			generator.Emit(OpCodes.Initobj, type);
		else
			generator.Emit(OpCodes.Ldnull);
	}

	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	private static void EmitLoadInteger(ILGenerator generator, int bits, double number)
	{
		switch(bits)
		{
			case 64:
				if((ulong)number > 0x7F_FF_FF_FF_FF_FF_FF_FF)
				{
					generator.Emit(OpCodes.Ldc_R8, number);
					generator.Emit(OpCodes.Conv_U8);
				}
				else
					generator.Emit(OpCodes.Ldc_I8, (long)number);
				break;
			case 16:
				generator.Emit(OpCodes.Ldc_I4, (ushort)number);
				break;
			case 8:
				if(number > 0x7F)
				{
					generator.Emit(OpCodes.Ldc_I4_S, (byte)number);
					generator.Emit(OpCodes.Conv_U1);
				}
				else
					generator.Emit(OpCodes.Ldc_I4_S, (byte)number);
				break;
			default:
				if(number > 0x7F_FF_FF_FF)
				{
					generator.Emit(OpCodes.Ldc_I8, (long)number);
					generator.Emit(OpCodes.Conv_U4);
				}
				else
					generator.Emit(OpCodes.Ldc_I4, (uint)number);
				break;
		}
	}

	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	private static bool IsCollectionType(Type type)
	{
		if(type.IsPrimitive || type.IsValueType || type == typeof(string))
			return false;

		return Zongsoft.Common.TypeExtension.IsEnumerable(type);
	}

	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	private static Type GetCollectionImplementationType(Type type)
	{
		if(type.IsClass && type.IsAbstract)
			throw new NotSupportedException($"The '{type}' type cannot be an abstract class and must be a deterministic class or interface.");

		if(type.IsValueType || type.IsClass)
			return type;

		if(type.IsGenericType)
		{
			var prototype = type.GetGenericTypeDefinition();

			if(prototype == typeof(ISet<>))
				return typeof(HashSet<>).MakeGenericType(type.GetGenericArguments());
			if(prototype == typeof(System.Collections.Specialized.INotifyCollectionChanged))
				return typeof(System.Collections.ObjectModel.ObservableCollection<>).MakeGenericType(type.GetGenericArguments());
			else if(prototype == typeof(IDictionary<,>))
				return typeof(Dictionary<,>).MakeGenericType(type.GetGenericArguments());
			else if(prototype == typeof(IReadOnlyDictionary<,>))
				return typeof(Dictionary<,>).MakeGenericType(type.GetGenericArguments());
			else if(prototype == typeof(IList<>) || prototype == typeof(ICollection<>))
				return typeof(List<>).MakeGenericType(type.GetGenericArguments());
			else if(prototype == typeof(IReadOnlyList<>) || prototype == typeof(IReadOnlyCollection<>))
				return typeof(List<>).MakeGenericType(type.GetGenericArguments());
			else if(prototype == typeof(IEnumerable<>))
				return typeof(List<>).MakeGenericType(type.GetGenericArguments());
		}
		else
		{
			if(type == typeof(IDictionary))
				return typeof(Dictionary<object, object>);
			else if(type == typeof(IList) || type == typeof(ICollection))
				return typeof(List<object>);
			else if(type == typeof(IEnumerable))
				return typeof(List<object>);
		}

		throw new InvalidOperationException($"The '{type}' type is not a specific or supported collection type.");
	}

	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	private static string GetClassName(Type type)
	{
		var @prefix = string.IsNullOrEmpty(type.Namespace) ? string.Empty : type.Namespace + ".";

		return type.IsInterface ?
			prefix + type.Name + "!Contract" :
			prefix + type.Name + "!Abstract";
	}
	#endregion

	#region 嵌套结构
	protected class PropertyMetadata
	{
		#region 私有变量
		private readonly PropertyInfo _property;
		private readonly Model.PropertyAttribute _metadata;
		private readonly DefaultValueAttribute _defaultAttribute;
		#endregion

		#region 公共字段
		public PropertyBuilder Builder;
		public FieldBuilder Field;
		public FieldBuilder Synchrolock;
		public bool IsExplicitImplementation;
		#endregion

		#region 构造函数
		public PropertyMetadata(PropertyInfo property)
		{
			_property = property ?? throw new ArgumentNullException(nameof(property));
			_metadata = property.GetCustomAttribute<Model.PropertyAttribute>(true);
			_defaultAttribute = _property.GetCustomAttribute<DefaultValueAttribute>(true);

			if(_metadata != null)
				this.IsExplicitImplementation = _metadata.IsExplicitImplementation;
		}
		#endregion

		#region 公共属性
		public bool IsReadOnly => _property.CanRead && !_property.CanWrite;
		public bool CanRead => _property.CanRead;
		public bool CanWrite => _property.CanWrite;
		public string Name => _property.Name;
		public Type DeclaringType => _property.DeclaringType;
		public Type PropertyType => _property.PropertyType;
		public MethodInfo GetMethod => _property.GetMethod;
		public MethodInfo SetMethod => _property.SetMethod;
		public Model.PropertyAttribute Metadata => _metadata;
		public bool HasDefaultValue => _defaultAttribute != null;
		public DefaultValueAttribute DefaultValueAttribute => _defaultAttribute;

		public bool SingletonFactoryEnabled
		{
			get
			{
				//当条件满足以下条件之一则返回真，否则返回假：
				//1. 属性有 DefaultValueAttribute 自定义标签，且它的 Value 是 Type，且指定的 Type 是一个静态类；
				//2. 属性有 PropertyAttribute 元数据标签，且实现方式是单例模式，且它的 Type 指定的是一个静态类。
				return (_defaultAttribute != null &&
						_defaultAttribute.Value is Type type &&
						type.IsAbstract && type.IsSealed) ||
					   (_metadata != null && _metadata.Mode == Model.PropertyImplementationMode.Singleton &&
						_metadata.Type != null && _metadata.Type.IsAbstract && _metadata.Type.IsSealed);
			}
		}
		#endregion

		#region 公共方法
		public string GetName() => this.IsExplicitImplementation ?
		   _property.DeclaringType.FullName + '.' + _property.Name :
		   _property.Name;

		public string GetFieldName(string suffix = null) => '$' +
			(this.IsExplicitImplementation ?
			_property.DeclaringType.FullName + '.' + _property.Name :
			_property.Name) + suffix;

		public string GetGetterName() => this.IsExplicitImplementation ?
			_property.DeclaringType.FullName + '.' + _property.GetMethod.Name :
			_property.GetMethod.Name;

		public string GetSetterName() => this.IsExplicitImplementation ?
			_property.DeclaringType.FullName + '.' + _property.SetMethod.Name :
			_property.SetMethod.Name;

		public IList<CustomAttributeData> GetCustomAttributesData() => _property.GetCustomAttributesData();

		public MethodInfo GetSingletonFactory()
		{
			Type factoryType = null;

			if(_defaultAttribute != null && _defaultAttribute.Value is Type type && type.IsAbstract && type.IsSealed)
				factoryType = type;
			else if(_metadata != null && _metadata.Mode == Model.PropertyImplementationMode.Singleton && _metadata.Type != null && _metadata.Type.IsAbstract && _metadata.Type.IsSealed)
				factoryType = _metadata.Type;

			if(factoryType == null)
				return null;

			var method = factoryType.GetMethod("Get" + _property.Name, BindingFlags.Public | BindingFlags.Static, null, new Type[] { _property.DeclaringType }, null) ??
						 factoryType.GetMethod("Get" + _property.Name, BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);

			if(method == null)
				throw new InvalidOperationException($"Not found the 'Get{_property.Name}(...)' factory method in the '{factoryType}' extension class.");

			if(method.ReturnType == null || method.ReturnType == typeof(void) || !_property.PropertyType.IsAssignableFrom(method.ReturnType))
				throw new InvalidOperationException($"The return type of the 'Get{_property.Name}(...)' factory method in the '{factoryType}' extension class is missing or invliad.");

			return method;
		}
		#endregion
	}

	private readonly struct MethodToken(MethodBuilder getMethod, MethodBuilder setMethod)
	{
		public readonly MethodBuilder GetMethod = getMethod;
		public readonly MethodBuilder SetMethod = setMethod;
	}
	#endregion
}

internal sealed class ModelContractEmitter(ModuleBuilder module) : ModelEmitterBase(module)
{
	#region 重写方法
	protected override TypeBuilder Build(Type type, string name)
	{
		var builder = this.Module.DefineType(name,
			TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed);

		//添加类型的实现接口声明
		builder.AddInterfaceImplementation(type);
		builder.AddInterfaceImplementation(typeof(IModel));

		//生成所有接口定义的注解(自定义特性)
		GenerateAnnotations(builder, new HashSet<Type>());

		return builder;
	}

	protected override IList<PropertyMetadata> GetProperties(Type type, TypeBuilder builder, out MemberInfo propertyChanged)
	{
		var properties = MakeInterfaces(type, builder, out var eventField);
		propertyChanged = eventField;
		return properties;
	}

	protected override PropertyBuilder DefineProperty(TypeBuilder builder, PropertyMetadata property)
	{
		var propertyBuilder = property.Builder = builder.DefineProperty(
			property.GetName(),
			PropertyAttributes.None,
			property.PropertyType,
			null);

		//获取当前属性的所有自定义标签
		var customAttributes = property.GetCustomAttributesData();

		//设置属性的自定义标签
		if(customAttributes != null && customAttributes.Count > 0)
		{
			foreach(var customAttribute in customAttributes)
			{
				var annotation = GetAnnotation(customAttribute);

				if(annotation != null)
					propertyBuilder.SetCustomAttribute(annotation);
			}
		}

		//定义属性的获取器方法
		var getter = builder.DefineMethod(property.GetGetterName(),
			(property.IsExplicitImplementation ? MethodAttributes.Private : MethodAttributes.Public) | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.NewSlot,
			property.PropertyType,
			Type.EmptyTypes);

		//如果当前属性需要显式实现，则定义其方法覆盖元数据
		if(property.IsExplicitImplementation)
			builder.DefineMethodOverride(getter, property.GetMethod);

		propertyBuilder.SetGetMethod(getter);

		if(property.CanWrite)
		{
			//定义属性的设置器方法
			var setter = builder.DefineMethod(property.GetSetterName(),
				(property.IsExplicitImplementation ? MethodAttributes.Private : MethodAttributes.Public) | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.NewSlot,
				null,
				[property.PropertyType]);

			//定义设置器参数名
			setter.DefineParameter(1, ParameterAttributes.None, "value");

			//如果当前属性需要显式实现，则定义其方法覆盖元数据
			if(property.IsExplicitImplementation)
				builder.DefineMethodOverride(setter, property.SetMethod);

			propertyBuilder.SetSetMethod(setter);
		}

		return propertyBuilder;
	}
	#endregion

	#region 私有方法
	private static IList<PropertyMetadata> MakeInterfaces(Type type, TypeBuilder builder, out FieldBuilder propertyChanged)
	{
		propertyChanged = null;
		var queue = new Queue<Type>(type.GetInterfaces());
		var result = new List<PropertyMetadata>();

		//首先将当前类型的属性信息加入到结果集中
		result.AddRange(type.GetProperties().Select(p => new PropertyMetadata(p)));

		while(queue.Count > 0)
		{
			var interfaceType = queue.Dequeue();

			//如果该接口已经被声明，则跳过它
			if(builder.GetTypeInfo().ImplementedInterfaces.Contains(interfaceType))
				continue;

			//将指定类型继承的接口加入到实现接口声明中
			builder.AddInterfaceImplementation(interfaceType);

			if(interfaceType == typeof(INotifyPropertyChanged))
			{
				if(propertyChanged == null)
					propertyChanged = GeneratePropertyChangedEvent(builder); //生成“INotifyPropertyChanged”接口实现
			}
			else
			{
				result.AddRange(interfaceType.GetProperties().Select(p => new PropertyMetadata(p)));

				//获取当前接口的父接口
				var baseInterfaces = interfaceType.GetInterfaces();

				if(baseInterfaces != null && baseInterfaces.Length > 0)
				{
					foreach(var baseInterface in baseInterfaces)
					{
						queue.Enqueue(baseInterface);
					}
				}
			}
		}

		return result;
	}

	private static FieldBuilder GeneratePropertyChangedEvent(TypeBuilder builder)
	{
		var exchangeMethod = typeof(Interlocked)
			.GetMethods(BindingFlags.Public | BindingFlags.Static)
			.First(p => p.Name == nameof(Interlocked.CompareExchange) && p.IsGenericMethod)
			.MakeGenericMethod(typeof(PropertyChangedEventHandler));

		//添加类型的实现接口声明
		if(!builder.GetTypeInfo().ImplementedInterfaces.Contains(typeof(INotifyPropertyChanged)))
			builder.AddInterfaceImplementation(typeof(INotifyPropertyChanged));

		//定义“PropertyChanged”事件的委托链字段
		var field = builder.DefineField("PropertyChanged", typeof(PropertyChangedEventHandler), FieldAttributes.Private);

		//定义“PropertyChanged”事件
		var e = builder.DefineEvent("PropertyChanged", EventAttributes.None, typeof(PropertyChangedEventHandler));

		//定义事件的Add方法
		var add = builder.DefineMethod("add_PropertyChanged", MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.NewSlot, null, new Type[] { typeof(PropertyChangedEventHandler) });
		//定义事件方法的参数
		add.DefineParameter(1, ParameterAttributes.None, "value");

		var generator = add.GetILGenerator();
		generator.DeclareLocal(typeof(PropertyChangedEventHandler)); //original
		generator.DeclareLocal(typeof(PropertyChangedEventHandler)); //current
		generator.DeclareLocal(typeof(PropertyChangedEventHandler)); //latest

		var ADD_LOOP_LABEL = generator.DefineLabel();

		// var original = this.PropertyChanged;
		generator.Emit(OpCodes.Ldarg_0);
		generator.Emit(OpCodes.Ldfld, field);
		generator.Emit(OpCodes.Stloc_0);

		// do{}
		generator.MarkLabel(ADD_LOOP_LABEL);

		// current=original
		generator.Emit(OpCodes.Ldloc_0);
		generator.Emit(OpCodes.Stloc_1);

		// var latest=Delegate.Combine(current, value);
		generator.Emit(OpCodes.Ldloc_1);
		generator.Emit(OpCodes.Ldarg_1);
		generator.Emit(OpCodes.Call, typeof(Delegate).GetMethod("Combine", new Type[] { typeof(Delegate), typeof(Delegate) }));
		generator.Emit(OpCodes.Castclass, typeof(PropertyChangedEventHandler));
		generator.Emit(OpCodes.Stloc_2);

		// original = Interlocked.CompareExchange<PropertyChangedEventHandler>(ref this.PropertyChanged, latest, current);
		generator.Emit(OpCodes.Ldarg_0);
		generator.Emit(OpCodes.Ldflda, field);
		generator.Emit(OpCodes.Ldloc_2);
		generator.Emit(OpCodes.Ldloc_1);
		generator.Emit(OpCodes.Call, exchangeMethod);
		generator.Emit(OpCodes.Stloc_0);

		// while(original != current);
		generator.Emit(OpCodes.Ldloc_0);
		generator.Emit(OpCodes.Ldloc_1);
		generator.Emit(OpCodes.Bne_Un_S, ADD_LOOP_LABEL);

		generator.Emit(OpCodes.Ret);

		//设置事件的Add方法
		e.SetAddOnMethod(add);

		//定义事件的Remove方法
		var remove = builder.DefineMethod("remove_PropertyChanged", MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.NewSlot, null, new Type[] { typeof(PropertyChangedEventHandler) });
		//定义事件方法的参数
		remove.DefineParameter(1, ParameterAttributes.None, "value");

		generator = remove.GetILGenerator();
		generator.DeclareLocal(typeof(PropertyChangedEventHandler)); //original
		generator.DeclareLocal(typeof(PropertyChangedEventHandler)); //current
		generator.DeclareLocal(typeof(PropertyChangedEventHandler)); //latest

		var REMOVE_LOOP_LABEL = generator.DefineLabel();

		// var original = this.PropertyChanged;
		generator.Emit(OpCodes.Ldarg_0);
		generator.Emit(OpCodes.Ldfld, field);
		generator.Emit(OpCodes.Stloc_0);

		// do{}
		generator.MarkLabel(REMOVE_LOOP_LABEL);

		// current=original
		generator.Emit(OpCodes.Ldloc_0);
		generator.Emit(OpCodes.Stloc_1);

		// var latest=Delegate.Remove(current, value);
		generator.Emit(OpCodes.Ldloc_1);
		generator.Emit(OpCodes.Ldarg_1);
		generator.Emit(OpCodes.Call, typeof(Delegate).GetMethod("Remove", new Type[] { typeof(Delegate), typeof(Delegate) }));
		generator.Emit(OpCodes.Castclass, typeof(PropertyChangedEventHandler));
		generator.Emit(OpCodes.Stloc_2);

		// original = Interlocked.CompareExchange<PropertyChangedEventHandler>(ref this.PropertyChanged, latest, current);
		generator.Emit(OpCodes.Ldarg_0);
		generator.Emit(OpCodes.Ldflda, field);
		generator.Emit(OpCodes.Ldloc_2);
		generator.Emit(OpCodes.Ldloc_1);
		generator.Emit(OpCodes.Call, exchangeMethod);
		generator.Emit(OpCodes.Stloc_0);

		// while(original != current);
		generator.Emit(OpCodes.Ldloc_0);
		generator.Emit(OpCodes.Ldloc_1);
		generator.Emit(OpCodes.Bne_Un_S, REMOVE_LOOP_LABEL);

		generator.Emit(OpCodes.Ret);

		//设置事件的Remove方法
		e.SetRemoveOnMethod(remove);

		return field;
	}

	private static void GenerateAnnotations(TypeBuilder builder, ISet<Type> types)
	{
		foreach(var type in builder.GetTypeInfo().ImplementedInterfaces)
		{
			var attributes = type.GetCustomAttributesData();

			//设置接口的自定义标签
			if(attributes != null && attributes.Count > 0)
			{
				foreach(var attribute in attributes)
				{
					var usage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(attribute.AttributeType, typeof(AttributeUsageAttribute));

					if(usage != null && !usage.AllowMultiple && types.Contains(attribute.AttributeType))
						continue;

					var annotation = GetAnnotation(attribute);

					if(annotation != null)
					{
						builder.SetCustomAttribute(annotation);
						types.Add(attribute.AttributeType);
					}
				}
			}
		}
	}
	#endregion
}

internal sealed class ModelAbstractEmitter(ModuleBuilder module) : ModelEmitterBase(module)
{
	#region 重写方法
	protected override TypeBuilder Build(Type type, string name)
	{
		var builder = this.Module.DefineType(name,
			TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed, type);

		//添加类型的实现接口声明
		if(type.GetInterface(nameof(IModel)) == null)
			builder.AddInterfaceImplementation(typeof(IModel));

		return builder;
	}

	protected override IList<PropertyMetadata> GetProperties(Type type, TypeBuilder builder, out MemberInfo propertyChanged)
	{
		propertyChanged = null;

		if(type.GetInterface(nameof(INotifyPropertyChanged)) == typeof(INotifyPropertyChanged))
		{
			var methods = type.FindMembers(MemberTypes.Method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, (m, _) => ((MethodInfo)m).Name == "OnPropertyChanged", null);

			if(methods == null || methods.Length == 0)
			{
				var field = (FieldInfo)type.FindMembers(MemberTypes.Field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, (m, _) => ((FieldInfo)m).FieldType == typeof(PropertyChangedEventHandler), null).FirstOrDefault();
				propertyChanged = field;
			}
			else
			{
				if(methods.Length == 1)
					propertyChanged = methods[0];
				else
				{
					MethodInfo matched = null;

					for(int i = 0; i < methods.Length; i++)
					{
						var method = (MethodInfo)methods[i];
						var parameters = method.GetParameters();

						if(parameters.Length == 1)
						{
							if(parameters[0].ParameterType == typeof(string))
							{
								matched = method;
								break;
							}
							else if(parameters[0].ParameterType == typeof(PropertyChangedEventArgs))
							{
								matched = method;
							}
						}
					}

					propertyChanged = matched;
				}
			}
		}

		return type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead && p.GetMethod.IsAbstract).Select(p => new PropertyMetadata(p)).ToArray();
	}

	protected override PropertyBuilder DefineProperty(TypeBuilder builder, PropertyMetadata property)
	{
		var propertyBuilder = property.Builder = builder.DefineProperty(
			property.GetName(),
			PropertyAttributes.None,
			property.PropertyType,
			null);

		//获取当前属性的所有自定义标签
		var customAttributes = property.GetCustomAttributesData();

		//设置属性的自定义标签
		if(customAttributes != null && customAttributes.Count > 0)
		{
			foreach(var customAttribute in customAttributes)
			{
				var annotation = GetAnnotation(customAttribute);

				if(annotation != null)
					propertyBuilder.SetCustomAttribute(annotation);
			}
		}

		//定义属性的获取器方法
		var getter = builder.DefineMethod(property.GetGetterName(),
			GetMethodModifier(property.GetMethod.Attributes) | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Virtual,
			property.PropertyType,
			Type.EmptyTypes);

		//重写抽象属性的获取方法，必须定义其方法覆盖元数据
		builder.DefineMethodOverride(getter, property.GetMethod);

		propertyBuilder.SetGetMethod(getter);

		if(property.CanWrite)
		{
			//定义属性的设置器方法
			var setter = builder.DefineMethod(property.GetSetterName(),
				GetMethodModifier(property.SetMethod.Attributes) | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Virtual,
				null,
				[property.PropertyType]);

			//定义设置器参数名
			setter.DefineParameter(1, ParameterAttributes.None, "value");

			//重写抽象属性的设置方法，必须定义其方法覆盖元数据
			builder.DefineMethodOverride(setter, property.SetMethod);

			propertyBuilder.SetSetMethod(setter);
		}

		return propertyBuilder;
	}

	protected override MethodBuilder DefineResetMethod(TypeBuilder builder) => IsImplement<IModel>(builder.BaseType) ? 
		DefineOverrideMethod(builder, nameof(IModel.Reset), new Type[] { typeof(string), typeof(object).MakeByRefType() }) :
		base.DefineResetMethod(builder);

	protected override MethodBuilder DefineResetManyMethod(TypeBuilder builder) => IsImplement<IModel>(builder.BaseType) ? 
		DefineOverrideMethod(builder, nameof(IModel.Reset), new Type[] { typeof(string[]) }) :
		base.DefineResetManyMethod(builder);

	protected override MethodBuilder DefineGetCountMethod(TypeBuilder builder) => IsImplement<IModel>(builder.BaseType) ?
		DefineOverrideMethod(builder, nameof(IModel.GetCount)) :
		base.DefineGetCountMethod(builder);

	protected override MethodBuilder DefineHasChangesMethod(TypeBuilder builder) => IsImplement<IModel>(builder.BaseType) ? 
		DefineOverrideMethod(builder, nameof(IModel.HasChanges), new Type[] { typeof(string[]) }) :
		base.DefineHasChangesMethod(builder);

	protected override MethodBuilder DefineGetChangesMethod(TypeBuilder builder) => IsImplement<IModel>(builder.BaseType) ? 
		DefineOverrideMethod(builder, nameof(IModel.GetChanges)) :
		base.DefineGetChangesMethod(builder);

	protected override MethodBuilder DefineTryGetValueMethod(TypeBuilder builder) => IsImplement<IModel>(builder.BaseType) ? 
		DefineOverrideMethod(builder, nameof(IModel.TryGetValue), new Type[] { typeof(string), typeof(object).MakeByRefType() }) :
		base.DefineTryGetValueMethod(builder);

	protected override MethodBuilder DefineTrySetValueMethod(TypeBuilder builder) => IsImplement<IModel>(builder.BaseType) ? 
		DefineOverrideMethod(builder, nameof(IModel.TrySetValue), new Type[] { typeof(string), typeof(object) }) :
		base.DefineTrySetValueMethod(builder);
	#endregion

	#region 私有方法
	private static MethodBuilder DefineOverrideMethod(TypeBuilder builder, string name, Type[] parameterTypes = null)
	{
		var method = Zongsoft.Common.TypeExtension.GetMethod(builder.BaseType, name, parameterTypes, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

		if(method == null)
			return null;

		if(!method.IsVirtual && !method.IsAbstract)
			throw new InvalidOperationException($"The {name} method of the {method.DeclaringType.FullName} data model is not a virtual method or an abstract method.");

		var methodBuilder = builder.DefineMethod(
			method.Name,
			GetMethodModifier(method.Attributes) | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Virtual,
			method.ReturnType,
			method.GetParameters().Select(p => p.ParameterType).ToArray());

		//添加方法的实现标记
		builder.DefineMethodOverride(methodBuilder, method);

		var parameters = method.GetParameters();

		if(parameters != null && parameters.Length > 0)
		{
			for(int i = 0; i < parameters.Length; i++)
			{
				methodBuilder.DefineParameter(i + 1, parameters[i].IsOut ? ParameterAttributes.Out : ParameterAttributes.None, parameters[i].Name);
			}
		}

		return methodBuilder;
	}

	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	private static bool IsImplement<TContract>(Type type) where TContract : class => type != null && type.GetInterface(typeof(TContract).FullName) == typeof(TContract);

	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	private static MethodAttributes GetMethodModifier(MethodAttributes attributes) => attributes & (MethodAttributes)7;
	#endregion
}
