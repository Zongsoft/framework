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
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace Zongsoft.Data
{
	partial class Model
	{
		private static readonly Dictionary<Type, ModelDescriptor> _descriptors = new();

		public static ModelDescriptor GetDescriptor<TModel>(this IDataService<TModel> service) => GetDescriptor(service?.DataAccess, service?.Name, typeof(TModel));
		public static ModelDescriptor GetDescriptor(this IDataService service, Type modelType) => GetDescriptor(service?.DataAccess, null, modelType);
		public static ModelDescriptor GetDescriptor<TModel>(this IDataAccess accessor, string name = null) => GetDescriptor(accessor, name, typeof(TModel));
		public static ModelDescriptor GetDescriptor(this IDataAccess accessor, Type modelType) => GetDescriptor(accessor, null, modelType);
		public static ModelDescriptor GetDescriptor(this IDataAccess accessor, string name, Type modelType)
		{
			if(accessor == null)
				throw new ArgumentNullException(nameof(accessor));

			//如果未指定模型名称则根据模型类型获取其名称
			if(string.IsNullOrEmpty(name))
				name = accessor.Naming.Get(modelType);

			return GetDescriptor(accessor.Metadata.Entities[name], modelType);
		}

		public static ModelDescriptor GetDescriptor<TModel>(this Metadata.IDataEntity entity) => GetDescriptor(entity, typeof(TModel));
		public static ModelDescriptor GetDescriptor(this Metadata.IDataEntity entity, Type modelType)
		{
			if(modelType == null)
				throw new ArgumentNullException(nameof(modelType));

			//对动态模型类进行特殊处理
			if(modelType.IsClass && modelType.Assembly.IsDynamic && modelType.BaseType.IsAbstract)
				modelType = modelType.BaseType;

			//如果已经缓存则直接从缓存中获取
			if(_descriptors.TryGetValue(modelType, out var descriptor))
				return descriptor;

			//创建模型描述器
			var model = new ModelDescriptor(modelType, entity);

			//添加模型的属性定义
			model.Properties.AddRange(
				modelType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
					.Select(property => new ModelPropertyDescriptor(property))
			);

			//添加模型的字段定义
			model.Properties.AddRange(
				modelType.GetFields(BindingFlags.Instance | BindingFlags.Public)
					.Select(field => new ModelPropertyDescriptor(field))
			);

			return _descriptors.TryAdd(modelType, model) ? model : _descriptors[modelType];
		}
	}
}
