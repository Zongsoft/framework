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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Collections.Concurrent;

namespace Zongsoft.Data;

partial class Model
{
	#region 私有变量
	private static readonly ConcurrentDictionary<Type, ModelDescriptor> _descriptors = new();
	#endregion

	#region 公共方法
	/// <summary>获取指定数据服务对应的模型描述器。</summary>
	/// <typeparam name="TModel">指定的数据模型类型。</typeparam>
	/// <param name="service">指定的数据服务。</param>
	/// <returns>返回结合数据服务映射元数据的模型描述器。</returns>
	/// <remarks>服务上下文描述器不会进入全局类型缓存，以免相同模型类型的不同服务映射互相污染。</remarks>
	public static ModelDescriptor GetDescriptor<TModel>(this IDataService<TModel> service)
	{
		ArgumentNullException.ThrowIfNull(service);
		return GetDescriptor(service, typeof(TModel));
	}

	/// <summary>获取指定数据服务对应的模型描述器。</summary>
	/// <param name="service">指定的数据服务。</param>
	/// <returns>返回结合数据服务映射元数据的模型描述器，如果无法确定服务的模型类型则返回空(<c>null</c>)。</returns>
	public static ModelDescriptor GetDescriptor(this IDataService service)
	{
		ArgumentNullException.ThrowIfNull(service);

		var contracts = service.GetType().GetInterfaces();
		Type modelType = null;

		for(int i = 0; i < contracts.Length; i++)
		{
			if(contracts[i].IsGenericType && contracts[i].GetGenericTypeDefinition() == typeof(IDataService<>))
			{
				var type = contracts[i].GetGenericArguments()[0];

				if(modelType == null)
					modelType = type;
				else if(modelType != type)
					throw new InvalidOperationException($"The specified '{service.GetType().FullName}' data service implements multiple model contracts.");
			}
		}

		return modelType == null ? null : GetDescriptor(service, modelType);
	}

	/// <summary>获取指定类型的模型描述器。</summary>
	/// <typeparam name="TModel">指定的数据模型类型。</typeparam>
	/// <returns>返回指定类型的模型描述器。</returns>
	public static ModelDescriptor GetDescriptor<TModel>() => GetDescriptor(typeof(TModel));

	/// <summary>获取指定类型的模型描述器。</summary>
	/// <param name="modelType">指定的数据模型类型。</param>
	/// <returns>返回指定类型的模型描述器。</returns>
	/// <remarks>类型描述器仅包含模型声明元数据，并按规范化后的模型类型全局缓存。</remarks>
	public static ModelDescriptor GetDescriptor(Type modelType)
	{
		modelType = Normalize(modelType);
		return modelType == null ? null : _descriptors.GetOrAdd(modelType, modelType => new ModelDescriptor(modelType));
	}
	#endregion

	#region 私有方法
	private static ModelDescriptor GetDescriptor(IDataService service, Type modelType)
	{
		modelType = Normalize(modelType);
		if(modelType == null)
			return null;

		//服务上下文中的映射可能因服务名或数据访问器而不同，必须使用独立的描述器实例
		var model = new ModelDescriptor(modelType);
		var schema = service.DataAccess?.Schema?.Parse(service.Name, "*", modelType);

		if(schema == null)
			return model;

		foreach(var property in model.Properties)
		{
			if(property is not ModelPropertyDescriptor.SimplexPropertyDescriptor descriptor ||
			   schema.Find(property.Name)?.Property is not Metadata.IDataEntitySimplexProperty metadata)
				continue;

			descriptor.Hint = metadata.Hint;
			descriptor.Alias = metadata.Alias;
			descriptor.DataType = metadata.Type;
			descriptor.Immutable = metadata.Immutable;
			descriptor.IsPrimaryKey = metadata.IsPrimaryKey;
			descriptor.Length = metadata.Length;
			descriptor.Precision = metadata.Precision;
			descriptor.Scale = metadata.Scale;
			descriptor.DefaultValue = metadata.DefaultValue;
			descriptor.Nullable = metadata.Nullable;
			descriptor.Sortable = metadata.Sortable;
			descriptor.Sequence = GetSequence(metadata.Sequence);
		}

		return model;

		static DataPropertySequence GetSequence(Metadata.IDataEntityPropertySequence sequence)
		{
			if(sequence == null)
				return default;

			var references = sequence.References;
			var names = references == null || references.Length == 0 ? null : new string[references.Length];

			for(int index = 0; names != null && index < names.Length; index++)
				names[index] = references[index].Name;

			return new(sequence.Name, sequence.Seed, sequence.Interval, names);
		}
	}

	private static Type Normalize(Type modelType)
	{
		ArgumentNullException.ThrowIfNull(modelType);

		//如果模型类型是基元类型、枚举类型、数组类型或者泛型、静态类，则返回空
		if(modelType.IsPrimitive || modelType.IsEnum || modelType.IsArray || modelType.IsGenericType || (modelType.IsAbstract && modelType.IsSealed))
			return null;

		//对动态模型类进行特殊处理
		while(modelType.IsClass && modelType.Assembly.IsDynamic && modelType.BaseType != null)
			modelType = modelType.BaseType;

		return modelType;
	}
	#endregion
}
