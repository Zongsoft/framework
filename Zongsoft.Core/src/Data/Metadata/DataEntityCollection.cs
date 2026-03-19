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
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Data.Metadata;

public class DataEntityCollection : ICollection<IDataEntity>
{
	#region 成员字段
	private readonly Collections.SynchronizedDictionary<string, IDataEntity> _dictionary = new(StringComparer.OrdinalIgnoreCase);
	#endregion

	#region 公共属性
	public int Count => _dictionary.Count;
	public IDataEntity this[string qualifiedName] => _dictionary[qualifiedName];
	public IDataEntity this[string name, string @namespace] => _dictionary[DataUtility.Qualify(name, @namespace)];
	#endregion

	#region 公共方法
	public void Add(IDataEntity entity)
	{
		ArgumentNullException.ThrowIfNull(entity);
		if(!_dictionary.TryAdd(entity.QualifiedName, entity))
			throw new InvalidOperationException($"The specified '{entity.QualifiedName}' data entity already exists in the collection.");
	}

	public bool TryAdd(IDataEntity entity) => entity != null && _dictionary.TryAdd(entity.QualifiedName, entity);
	public IDataEntity GetOrAdd(IDataEntity entity) => entity == null ? null : _dictionary.GetOrAdd(entity.QualifiedName, entity);

	public bool Contains(string qualifiedName) => qualifiedName != null && _dictionary.ContainsKey(qualifiedName);
	public bool Contains(string name, string @namespace) => name != null && _dictionary.ContainsKey(DataUtility.Qualify(name, @namespace));

	public void Clear() => _dictionary.Clear();
	public bool Remove(string qualifiedName) => qualifiedName != null && _dictionary.Remove(qualifiedName);
	public bool Remove(string name, string @namespace) => name != null && _dictionary.Remove(DataUtility.Qualify(name, @namespace));
	public bool Remove(string qualifiedName, out IDataEntity entity) => _dictionary.Remove(qualifiedName, out entity);
	public bool Remove(string name, string @namespace, out IDataEntity entity) => _dictionary.Remove(DataUtility.Qualify(name, @namespace), out entity);
	public bool TryGetValue(string qualifiedName, out IDataEntity entity) => _dictionary.TryGetValue(qualifiedName, out entity);
	public bool TryGetValue(string name, string @namespace, out IDataEntity entity) => _dictionary.TryGetValue(DataUtility.Qualify(name, @namespace), out entity);
	#endregion

	#region 显式实现
	bool ICollection<IDataEntity>.IsReadOnly => false;
	bool ICollection<IDataEntity>.Contains(IDataEntity entity) => entity != null && _dictionary.ContainsKey(entity.QualifiedName);
	bool ICollection<IDataEntity>.Remove(IDataEntity entity) => entity != null && _dictionary.Remove(entity.QualifiedName);
	void ICollection<IDataEntity>.CopyTo(IDataEntity[] array, int arrayIndex)
	{
		ArgumentNullException.ThrowIfNull(array);
		ArgumentOutOfRangeException.ThrowIfLessThan(arrayIndex, 0);
		ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(arrayIndex, array.Length);

		using var enumerator = _dictionary.GetEnumerator();

		for(int i = arrayIndex; i < array.Length; i++)
		{
			if(enumerator.MoveNext())
				array[i] = enumerator.Current.Value;
			else
				break;
		}
	}
	#endregion

	#region 枚举遍历
	IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
	public IEnumerator<IDataEntity> GetEnumerator() => _dictionary.Values.GetEnumerator();
	#endregion
}
