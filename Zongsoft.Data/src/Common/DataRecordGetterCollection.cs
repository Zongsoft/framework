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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Data.Common;

public class DataRecordGetterCollection : IDataRecordGetterProvider, ICollection<object>
{
	#region 私有变量
	private readonly Dictionary<Type, object> _getters = [];
	#endregion

	#region 公共属性
	public int Count => _getters.Count;
	bool ICollection<object>.IsReadOnly => false;
	#endregion

	#region 公共方法
	public void Add(object item)
	{
		if(item == null)
			throw new ArgumentNullException(nameof(item));

		var type = GetGetterType(item) ??
			throw new ArgumentException($"The specified ‘{item.GetType().FullName}’ type does not implement the ‘{typeof(IDataRecordGetter<>)}’ interface.");

		if(_getters.TryAdd(type, item))
			return;

		throw new ArgumentException($"The data record getter for the '{type.FullName}' type has already existed.");
	}

	public IDataRecordGetter<T> Get<T>() => _getters.TryGetValue(typeof(T), out var value) ? (IDataRecordGetter<T>)value : null;
	public bool TryGet<T>(out IDataRecordGetter<T> result)
	{
		if(_getters.TryGetValue(typeof(T), out var value))
		{
			result = (IDataRecordGetter<T>)value;
			return true;
		}

		result = null;
		return false;
	}

	public void Clear() => _getters.Clear();
	public bool Remove<T>() => _getters.Remove(typeof(T));
	public bool Remove(Type type) => type != null && _getters.Remove(type);
	public bool Contains<T>() => _getters.ContainsKey(typeof(T));
	public bool Contains(Type type) => type != null && _getters.ContainsKey(type);
	#endregion

	#region 显式实现
	bool ICollection<object>.Remove(object item)
	{
		var type = GetGetterType(item);
		return type != null && _getters.Remove(type);
	}

	bool ICollection<object>.Contains(object item)
	{
		var type = GetGetterType(item);
		return type != null && _getters.ContainsKey(type);
	}

	void ICollection<object>.CopyTo(object[] array, int arrayIndex) => _getters.Values.CopyTo(array, arrayIndex);
	IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
	public IEnumerator<object> GetEnumerator() => _getters.Values.GetEnumerator();
	#endregion

	#region 私有方法
	private static Type GetGetterType(object getter)
	{
		if(getter == null)
			return null;

		var contracts = getter.GetType().GetInterfaces();

		for(int i = 0; i < contracts.Length; i++)
		{
			if(contracts[i].IsGenericType && contracts[i].GetGenericTypeDefinition() == typeof(IDataRecordGetter<>))
				return contracts[i].GetGenericArguments()[0];
		}

		return null;
	}
	#endregion
}
