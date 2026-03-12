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
using System.Collections.Concurrent;

namespace Zongsoft.Data.Metadata;

public class DataEntityPropertyCollection(IDataEntity entity) : ICollection<IDataEntityProperty>
{
	#region 成员字段
	private readonly IDataEntity _entity = entity ?? throw new ArgumentNullException(nameof(entity));
	private readonly ConcurrentDictionary<string, IDataEntityProperty> _dictionary = new(StringComparer.OrdinalIgnoreCase);
	#endregion

	#region 公共属性
	public int Count => _dictionary.Count;
	public IDataEntityProperty this[string name] => _dictionary[name];
	#endregion

	#region 集合方法
	public void Add(IDataEntityProperty property)
	{
		ArgumentNullException.ThrowIfNull(property);

		if(_dictionary.TryAdd(property.Name, property))
			SetEntity(_entity, property, (property, dictionary) => dictionary.TryRemove(property.Name, out _), _dictionary);
		else
			throw new InvalidOperationException($"The specified '{property.Name}' entity property already exists in the properties.");
	}

	public bool TryAdd(IDataEntityProperty property)
	{
		if(property != null && _dictionary.TryAdd(property.Name, property))
		{
			SetEntity(_entity, property, (property, dictionary) => dictionary.TryRemove(property.Name, out _), _dictionary);
			return true;
		}

		return false;
	}

	public IDataEntityProperty GetOrAdd(IDataEntityProperty property)
	{
		if(property == null)
			return null;

		return _dictionary.GetOrAdd(property.Name, (key, state) =>
		{
			SetEntity(state.Entity, state.Property);
			return state.Property;
		}, (Entity : _entity, Property : property));
	}

	public void Clear() => _dictionary.Clear();
	public bool Contains(string name) => name != null && _dictionary.ContainsKey(name);
	public bool Remove(string name) => name != null && _dictionary.TryRemove(name, out _);
	public bool Remove(string name, out IDataEntityProperty property) => _dictionary.TryRemove(name, out property);
	public bool TryGetValue(string name, out IDataEntityProperty property) => _dictionary.TryGetValue(name, out property);
	#endregion

	#region 公共方法
	public void Replace(IDataEntityProperty property)
	{
		if(property == null)
			return;

		if(property.IsSimplex(out var simplex))
			this.Replace(simplex);
		else if(property.IsComplex(out var complex))
			this.Replace(complex);
	}

	public void Replace(IDataEntitySimplexProperty property)
	{
		if(property == null || this.TryAdd(property))
			return;

		if(this[property.Name].IsSimplex)
		{
			var simplex = (IDataEntitySimplexProperty)this[property.Name];

			simplex.Type = property.Type;
			simplex.Hint = property.Hint;
			simplex.Alias = property.Alias;
			simplex.Length = property.Length;
			simplex.Precision = property.Precision;
			simplex.Scale = property.Scale;
			simplex.Nullable = property.Nullable;
			simplex.Sortable = property.Sortable;
			simplex.Immutable = property.Immutable;
			simplex.Sequence = property.Sequence;
			simplex.DefaultValue = property.DefaultValue;
		}
	}

	public void Replace(IDataEntityComplexProperty property)
	{
		if(property == null || this.TryAdd(property))
			return;

		if(this[property.Name].IsComplex)
		{
			var complex = (IDataEntityComplexProperty)this[property.Name];

			complex.Hint = property.Hint;
			complex.Immutable = property.Immutable;
			complex.Behaviors = property.Behaviors;
			complex.Multiplicity = property.Multiplicity;
		}
	}

	public DataEntitySimplexProperty Simplex(string name, DataType type, bool nullable, bool immutable = false)
	{
		var property = new DataEntitySimplexProperty(name, type, nullable, immutable);
		this.Add(property);
		return property;
	}

	public DataEntitySimplexProperty Simplex(string name, DataType type, int length, bool nullable, bool immutable = false)
	{
		var property = new DataEntitySimplexProperty(name, type, length, nullable, immutable);
		this.Add(property);
		return property;
	}

	public DataEntitySimplexProperty Simplex(string name, DataType type, byte precision, byte scale, bool nullable, bool immutable = false)
	{
		var property = new DataEntitySimplexProperty(name, type, precision, scale, nullable, immutable);
		this.Add(property);
		return property;
	}

	public DataEntityComplexProperty Complex(string name, string port, DataEntityComplexPropertyBehaviors behaviors = DataEntityComplexPropertyBehaviors.None) => this.Complex(name, port, true, behaviors);
	public DataEntityComplexProperty Complex(string name, string port, bool immutable, DataEntityComplexPropertyBehaviors behaviors = DataEntityComplexPropertyBehaviors.None)
	{
		var property = new DataEntityComplexProperty(name, port, immutable, behaviors);
		this.Add(property);
		return property;
	}

	public DataEntityComplexProperty Complex(string name, string port, bool immutable, DataAssociationMultiplicity multiplicity, params DataAssociationLink[] links) =>
		this.Complex(name, port, immutable, DataEntityComplexPropertyBehaviors.None, multiplicity, links);
	public DataEntityComplexProperty Complex(string name, string port, DataEntityComplexPropertyBehaviors behaviors, DataAssociationMultiplicity multiplicity, params DataAssociationLink[] links) =>
		this.Complex(name, port, true, behaviors, multiplicity, links);
	public DataEntityComplexProperty Complex(string name, string port, bool immutable, DataEntityComplexPropertyBehaviors behaviors, DataAssociationMultiplicity multiplicity, params DataAssociationLink[] links)
	{
		var property = new DataEntityComplexProperty(name, port, immutable, behaviors, multiplicity, links);
		this.Add(property);
		return property;
	}
	#endregion

	#region 私有方法
	private static void SetEntity(IDataEntity entity, IDataEntityProperty property) => SetEntity<object>(entity, property, null, null);
	private static void SetEntity<T>(IDataEntity entity, IDataEntityProperty property, Action<IDataEntityProperty, T> failure = null, T state = default)
	{
		if(entity is DataEntityBase entityBase && property is DataEntityPropertyBase propertyBase)
			propertyBase.Entity = entityBase;
		else if(!ReferenceEquals(entity, property.Entity))
		{
			failure?.Invoke(property, state);
			throw new InvalidOperationException($"The entity property cannot be added or updated to the properties because the entity to which this property belongs cannot be set.");
		}
	}
	#endregion

	#region 显式实现
	bool ICollection<IDataEntityProperty>.IsReadOnly => false;
	bool ICollection<IDataEntityProperty>.Contains(IDataEntityProperty property) => property != null && _dictionary.ContainsKey(property.Name);
	bool ICollection<IDataEntityProperty>.Remove(IDataEntityProperty property) => property != null && _dictionary.TryRemove(property.Name, out _);
	void ICollection<IDataEntityProperty>.CopyTo(IDataEntityProperty[] array, int arrayIndex)
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
	public IEnumerator<IDataEntityProperty> GetEnumerator() => _dictionary.Values.GetEnumerator();
	#endregion
}
