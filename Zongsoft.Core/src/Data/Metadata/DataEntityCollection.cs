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
 * Copyright (C) 2010-2022 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Zongsoft.Data.Metadata;

public class DataEntityCollection() : KeyedCollection<string, IDataEntity>(StringComparer.OrdinalIgnoreCase)
{
	#region 成员字段
	private readonly Dictionary<string, IDataEntity[]> _aliases = new();
	#endregion

	#region 公共属性
	public IDataEntity this[string name, string @namespace = null] => base[GetKey(name, @namespace)];
	#endregion

	#region 公共方法
	public bool TryAdd(IDataEntity entity)
	{
		if(entity == null)
			throw new ArgumentNullException(nameof(entity));

		if(this.Contains(entity))
			return false;

		this.Add(entity);
		return true;
	}

	public bool Contains(string name, string @namespace = null) => base.Contains(GetKey(name, @namespace));
	public bool Remove(string name, string @namespace = null) => base.Remove(GetKey(name, @namespace));
	public bool TryGetValue(string name, string @namespace, out IDataEntity value) => base.TryGetValue(GetKey(name, @namespace), out value);

	/// <summary>根据别名查找对应的数据实体。</summary>
	/// <param name="alias">指定要查找的数据实体别名。</param>
	/// <returns>返回找到的数据实体数组，如果为空则表示查找失败。</returns>
	public IDataEntity[] Find(string alias)
	{
		if(string.IsNullOrEmpty(alias))
			return null;

		return _aliases.TryGetValue(alias, out var entities) ? entities : base.TryGetValue(alias, out var entity) ? [entity] : null;
	}
	#endregion

	#region 重写方法
	protected override string GetKeyForItem(IDataEntity entity) => entity.QualifiedName;

	protected override void ClearItems()
	{
		base.ClearItems();
		_aliases.Clear();
	}

	protected override void InsertItem(int index, IDataEntity entity)
	{
		if(entity == null)
			return;

		base.InsertItem(index, entity);

		if(!string.IsNullOrEmpty(entity.Alias))
		{
			if(!_aliases.TryAdd(entity.Alias, [entity]))
			{
				if(_aliases.TryGetValue(entity.Alias, out var entities))
					_aliases[entity.Alias] = [.. entities, entity];
				else
					_aliases[entity.Alias] = [entity];
			}
		}
	}

	protected override void RemoveItem(int index)
	{
		var item = this.Items[index];

		base.RemoveItem(index);

		if(item != null && !string.IsNullOrEmpty(item.Alias))
			_aliases.Remove(item.Alias);
	}

	protected override void SetItem(int index, IDataEntity entity)
	{
		if(entity == null)
			return;

		if(index >= 0 && index < this.Items.Count)
		{
			var item = this.Items[index];

			if(item != null && !string.IsNullOrEmpty(item.Alias))
				_aliases.Remove(item.Alias);
		}

		base.SetItem(index, entity);

		if(!string.IsNullOrEmpty(entity.Alias))
		{
			if(!_aliases.TryAdd(entity.Alias, [entity]))
			{
				if(_aliases.TryGetValue(entity.Alias, out var entities))
					_aliases[entity.Alias] = [.. entities, entity];
				else
					_aliases[entity.Alias] = [entity];
			}
		}
	}
	#endregion

	#region 私有方法
	private static string GetKey(string name, string @namespace) => string.IsNullOrEmpty(@namespace) ? name : $"{@namespace}.{name}";
	#endregion
}
