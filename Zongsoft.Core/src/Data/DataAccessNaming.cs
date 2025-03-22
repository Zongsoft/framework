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
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Data
{
	/// <summary>
	/// 提供数据访问名映射功能的类。
	/// </summary>
	public class DataAccessNaming : IDataAccessNaming, ICollection<KeyValuePair<Type, string>>
	{
		#region 公共属性
		public int Count => Model.Naming.Count;
		#endregion

		#region 公共方法
		public void Map<TModel>(string name = null) => Model.Naming.Map(typeof(TModel), name);
		public void Map(Type modelType, string name = null) => Model.Naming.Map(modelType, name);

		public string Get<TModel>() => Model.Naming.Get(typeof(TModel));
		public string Get(Type modelType) => Model.Naming.Get(modelType);
		#endregion

		#region 集合成员
		bool ICollection<KeyValuePair<Type, string>>.IsReadOnly => false;
		void ICollection<KeyValuePair<Type, string>>.Add(KeyValuePair<Type, string> item) => this.Map(item.Key, item.Value);
		void ICollection<KeyValuePair<Type, string>>.Clear() => throw new NotSupportedException();
		bool ICollection<KeyValuePair<Type, string>>.Remove(KeyValuePair<Type, string> item) => Model.Naming.Unmap(item.Key);
		bool ICollection<KeyValuePair<Type, string>>.Contains(KeyValuePair<Type, string> item) => Model.Naming.Contains(item.Key);

		void ICollection<KeyValuePair<Type, string>>.CopyTo(KeyValuePair<Type, string>[] array, int arrayIndex)
		{
			int offset = 0;

			foreach(var entry in Model.Naming.Enumerable())
			{
				if(arrayIndex + offset >= array.Length)
					break;

				array[arrayIndex + offset++] = entry;
			}
		}

		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		public IEnumerator<KeyValuePair<Type, string>> GetEnumerator() => Model.Naming.Enumerable().GetEnumerator();
		#endregion
	}
}
