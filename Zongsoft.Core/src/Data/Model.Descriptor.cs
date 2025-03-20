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
 * Copyright (C) 2010-2023 Zongsoft Studio <http://www.zongsoft.com>
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
	public static ModelDescriptor GetDescriptor<TModel>(this IDataService<TModel> service) => GetDescriptor(typeof(TModel));
	public static ModelDescriptor GetDescriptor(this IDataService service)
	{
		if(service == null)
			throw new ArgumentNullException(nameof(service));

		var contracts = service.GetType().GetInterfaces();

		for(int i = 0; i < contracts.Length; i++)
		{
			if(contracts[i].IsGenericType && contracts[i].GetGenericTypeDefinition() == typeof(IDataService<>))
				return GetDescriptor(contracts[i].GetGenericArguments()[0]);
		}

		return null;
	}

	public static ModelDescriptor GetDescriptor(Type modelType) => GetDescriptor(null, modelType);
	public static ModelDescriptor GetDescriptor<TModel>(this Metadata.IDataEntity entity) => GetDescriptor(entity, typeof(TModel));
	public static ModelDescriptor GetDescriptor(this Metadata.IDataEntity entity, Type modelType)
	{
		if(modelType == null)
			throw new ArgumentNullException(nameof(modelType));

		//对动态模型类进行特殊处理
		if(modelType.IsClass && modelType.Assembly.IsDynamic && modelType.BaseType.IsAbstract)
			modelType = modelType.BaseType;

		return _descriptors.GetOrAdd(modelType, modelType => new ModelDescriptor(modelType, entity));
	}
	#endregion
}
