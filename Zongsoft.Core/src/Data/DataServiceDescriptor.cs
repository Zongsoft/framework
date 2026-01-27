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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Data;

public class DataServiceDescriptor<TModel> : Zongsoft.Components.ServiceDescriptor<DataServiceDescriptor<TModel>.OperationCollection>
{
	#region 构造函数
	public DataServiceDescriptor(IDataService<TModel> service, ModelDescriptor model = null)
	{
		ArgumentNullException.ThrowIfNull(service);
		this.Name = service.Name;
		this.Type = service.GetType();
		this.Model = model ?? Data.Model.GetDescriptor(Mapping.Entities.TryGetValue(service.Name, out var entity) ? entity : null, typeof(TModel));
		this.Operations = new OperationCollection(this);
	}
	#endregion

	#region 公共属性
	public ModelDescriptor Model { get; }
	#endregion

	#region 嵌套子类
	public sealed class OperationCollection : IReadOnlyCollection<Operation>
	{
		private readonly Operation[] _operations;

		public OperationCollection(DataServiceDescriptor<TModel> descriptor)
		{
			var methods = descriptor.Type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
			_operations = new Operation[methods.Length];

			for(int i = 0; i < methods.Length; i++)
			{
				_operations[i] = new Operation(descriptor, methods[i]);
			}
		}

		public int Count => _operations.Length;

		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		public IEnumerator<Operation> GetEnumerator()
		{
			for(int i = 0; i < _operations.Length; i++)
				yield return _operations[i];
		}
	}
	#endregion
}
