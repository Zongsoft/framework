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
using System.Threading;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Data;

/// <summary>
/// 提供 <see cref="IModel"/> 数据实体或其他模型的动态编译及构建的静态类。
/// </summary>
public static partial class Model
{
	#region 常量定义
	private const string ASSEMBLY_NAME = "Zongsoft.Dynamics.Models";
	#endregion

	#region 成员字段
	private static readonly ReaderWriterLockSlim _locker = new();
	private static readonly Dictionary<Type, Func<object>> _cache = new();

	private static readonly AssemblyBuilder _assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(ASSEMBLY_NAME), AssemblyBuilderAccess.RunAndCollect);
	private static readonly ModuleBuilder _module = _assembly.DefineDynamicModule(ASSEMBLY_NAME);

	private static readonly ModelAbstractEmitter _abstractEmitter = new(_module);
	private static readonly ModelContractEmitter _contractEmitter = new(_module);
	#endregion

	#region 公共方法
	public static Type GetModelType(object model) => GetModelType(model?.GetType());
	public static Type GetModelType(Type modelType)
	{
		if(modelType == null)
			return null;

		if(modelType.Assembly.IsDynamic)
		{
			//注意：由于 ModelTypeAttribute 是 internal 而非 public 可见性，
			//导致 modelType.GetCustomAttribute(...) 和 Attribute.GetCustomAttribute(...) 方法获取不到注解，因而只能采用下面的方式获取。
			foreach(var annotation in modelType.CustomAttributes)
			{
				if(annotation.AttributeType == typeof(ModelTypeAttribute) && annotation.ConstructorArguments.Count > 0)
				{
					var argument = annotation.ConstructorArguments[0];

					if(argument.Value != null && argument.Value is Type type)
						return type;
				}
			}
		}

		return modelType;
	}

	public static TModel Build<TModel>() => (TModel)GetCreator(typeof(TModel))();
	public static TModel Build<TModel>(Action<TModel> map)
	{
		var entity = (TModel)GetCreator(typeof(TModel))();
		map?.Invoke(entity);
		return entity;
	}

	public static TModel Build<TModel, TState>(Action<TModel, TState> map, in TState state)
	{
		var entity = (TModel)GetCreator(typeof(TModel))();
		map?.Invoke(entity, state);
		return entity;
	}

	public static IEnumerable<TModel> Build<TModel>(int count, Action<TModel, int> map = null)
	{
		if(count < 1)
			throw new ArgumentOutOfRangeException(nameof(count));

		var creator = GetCreator(typeof(TModel));

		if(map == null)
		{
			for(int i = 0; i < count; i++)
			{
				yield return (TModel)creator();
			}
		}
		else
		{
			for(int i = 0; i < count; i++)
			{
				var entity = (TModel)creator();
				map(entity, i);
				yield return entity;
			}
		}
	}

	public static IEnumerable<TModel> Build<TModel, TState>(int count, Action<TModel, TState, int> map, TState state)
	{
		if(count < 1)
			throw new ArgumentOutOfRangeException(nameof(count));

		var creator = GetCreator(typeof(TModel));

		if(map == null)
		{
			for(int i = 0; i < count; i++)
			{
				yield return (TModel)creator();
			}
		}
		else
		{
			for(int i = 0; i < count; i++)
			{
				var entity = (TModel)creator();
				map(entity, state, i);
				yield return entity;
			}
		}
	}

	public static object Build(Type type) => GetCreator(type)();
	public static object Build(Type type, Action<object> map)
	{
		var entity = GetCreator(type)();
		map?.Invoke(entity);
		return entity;
	}

	public static object Build<TState>(Type type, Action<object, TState> map, in TState state)
	{
		var entity = GetCreator(type)();
		map?.Invoke(entity, state);
		return entity;
	}

	public static IEnumerable Build(Type type, int count, Action<object, int> map = null)
	{
		if(count < 1)
			throw new ArgumentOutOfRangeException(nameof(count));

		var creator = GetCreator(type);

		if(map == null)
		{
			for(int i = 0; i < count; i++)
			{
				yield return creator();
			}
		}
		else
		{
			for(int i = 0; i < count; i++)
			{
				var entity = creator();
				map(entity, i);
				yield return entity;
			}
		}
	}

	public static IEnumerable Build<TState>(Type type, int count, Action<object, TState, int> map, TState state)
	{
		if(count < 1)
			throw new ArgumentOutOfRangeException(nameof(count));

		var creator = GetCreator(type);

		if(map == null)
		{
			for(int i = 0; i < count; i++)
			{
				yield return creator();
			}
		}
		else
		{
			for(int i = 0; i < count; i++)
			{
				var entity = creator();
				map(entity, state, i);
				yield return entity;
			}
		}
	}

	public static Func<object> GetCreator(Type type)
	{
		_locker.EnterReadLock();
		var existed = _cache.TryGetValue(type, out var creator);
		_locker.ExitReadLock();

		if(existed)
			return creator;

		if(type.IsInterface)
		{
			if(type.GetEvents().Length > 0)
				throw new ArgumentException($"The '{type.FullName}' model interface cannot define any events.");

			if(type.GetMethods().Length > type.GetProperties().Length * 2)
				throw new ArgumentException($"The '{type.FullName}' model interface cannot define any methods.");
		}
		else if(!type.IsAbstract)
		{
			throw new ArgumentException($"The '{type.FullName}' model type must be an interface or abstract class.");
		}

		try
		{
			_locker.EnterWriteLock();

			if(!_cache.TryGetValue(type, out creator))
				creator = _cache[type] = type.IsInterface ? _contractEmitter.Compile(type) : _abstractEmitter.Compile(type);

			return creator;
		}
		finally
		{
			_locker.ExitWriteLock();
		}
	}
	#endregion

	#region 嵌套子类
	/// <summary>
	/// 表示实体属性实现代码的生成方式。
	/// </summary>
	public enum PropertyImplementationMode
	{
		/// <summary>默认实现方式。</summary>
		Default,

		/// <summary>以类似扩展方法的方式实现属性。</summary>
		Extension,

		/// <summary>以单例模式的方式实现属性。</summary>
		Singleton,
	}

	/// <summary>
	/// 提供实体属性动态编译的自定义特性。
	/// </summary>
	[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Property | AttributeTargets.Method)]
	public class PropertyAttribute : Attribute
	{
		#region 构造函数
		public PropertyAttribute() : this(PropertyImplementationMode.Default, null) { }
		public PropertyAttribute(PropertyImplementationMode mode, Type type = null)
		{
			this.Mode = mode;
			this.Type = type;
		}
		#endregion

		#region 公共属性
		/// <summary>获取扩展方法的静态类的类型或属性的具体类型，具体含义由<see cref="Mode"/>属性值确定。</summary>
		public Type Type { get; }

		/// <summary>获取或设置实体属性代码的实现方式。</summary>
		public PropertyImplementationMode Mode { get; set; }

		/// <summary>获取或设置属性是否以显式实现方式生成。</summary>
		public bool IsExplicitImplementation { get; set; }
		#endregion
	}

	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	internal class ModelTypeAttribute(Type modelType) : Attribute
	{
		public readonly Type ModelType = modelType;
	}
	#endregion
}
