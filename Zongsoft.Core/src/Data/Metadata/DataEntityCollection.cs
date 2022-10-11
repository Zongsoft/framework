/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
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
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Data.Metadata
{
	public class DataEntityCollection : IDataEntityCollection
	{
		#region 成员字段
		private readonly Dictionary<string, IDataEntity> _dictionary;
		#endregion

		#region 构造函数
		public DataEntityCollection() => _dictionary = new Dictionary<string, IDataEntity>(StringComparer.OrdinalIgnoreCase);
		#endregion

		#region 公共属性
		public int Count => _dictionary.Count;
		bool ICollection<IDataEntity>.IsReadOnly => false;
		public IDataEntity this[string name, string @namespace = null]
		{
			get
			{
				var key = string.IsNullOrEmpty(@namespace) ? name : $"{@namespace}.{name}";

				if(_dictionary.TryGetValue(key, out var entity))
					return entity;

				throw new DataException($"The specified '{key}' entity mapping does not exist.");
			}
		}
		#endregion

		#region 公共方法
		public void Add(IDataEntity entity) => _dictionary.Add(GetKey(entity), entity);
		public void Clear() => _dictionary.Clear();
		public bool Contains(IDataEntity entity) => entity != null && _dictionary.ContainsKey(GetKey(entity));
		public bool Remove(IDataEntity entity) => entity != null && _dictionary.Remove(GetKey(entity));

		public bool Contains(string name, string @namespace = null)
		{
			if(string.IsNullOrEmpty(name))
				return false;

			return string.IsNullOrEmpty(@namespace) ? _dictionary.ContainsKey(name) : _dictionary.ContainsKey($"{@namespace}.{name}");
		}

		public bool Remove(string name, string @namespace = null)
		{
			if(string.IsNullOrEmpty(name))
				return false;

			return string.IsNullOrEmpty(@namespace) ? _dictionary.Remove(name) : _dictionary.Remove($"{@namespace}.{name}");
		}

		public bool TryAdd(IDataEntity entity) => _dictionary.TryAdd(GetKey(entity), entity);
		public bool TryGetValue(string name, out IDataEntity value) => _dictionary.TryGetValue(name, out value);
		public bool TryGetValue(string name, string @namespace, out IDataEntity value) => _dictionary.TryGetValue(string.IsNullOrEmpty(@namespace) ? name : $"{@namespace}.{name}", out value);

		public void CopyTo(IDataEntity[] array, int arrayIndex)
		{
			if(array == null)
				throw new ArgumentNullException(nameof(array));
			if(arrayIndex < 0 || arrayIndex >= array.Length)
				throw new ArgumentOutOfRangeException(nameof(arrayIndex));

			_dictionary.Values.CopyTo(array, arrayIndex);
		}
		#endregion

		#region 枚举遍历
		public IEnumerator<IDataEntity> GetEnumerator() => _dictionary.Values.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		#endregion

		#region 私有方法
		private static string GetKey(IDataEntity entity)
		{
			if(entity is null)
				throw new ArgumentNullException(nameof(entity));

			return entity.QualifiedName;
		}
		#endregion
	}
}
