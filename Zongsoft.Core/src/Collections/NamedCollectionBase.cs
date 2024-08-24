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

namespace Zongsoft.Collections
{
	public abstract class NamedCollectionBase<T> : ReadOnlyNamedCollectionBase<T>, ICollection<T>, INamedCollection<T>
	{
		#region 构造函数
		protected NamedCollectionBase() { }
		protected NamedCollectionBase(StringComparer comparer) : base(comparer) { }
		#endregion

		#region 公共属性
		public new T this[string name]
		{
			get => this.GetItem(name);
			set => this.SetItem(name, value);
		}
		#endregion

		#region 公共方法
		public void Clear() => this.ClearItems();
		public void Add(T item) => this.AddItem(item);
		public bool Remove(string name) => this.RemoveItem(name);
		#endregion

		#region 虚拟方法
		protected virtual void AddItem(T item) => this.InnerDictionary.Add(this.GetKeyForItem(item), item);
		protected virtual bool RemoveItem(string name) => this.InnerDictionary.Remove(name);
		protected virtual void ClearItems() => this.InnerDictionary.Clear();
		protected virtual void SetItem(string name, T value)
		{
			var key = this.GetKeyForItem(value);
			var comparer = (StringComparer)this.InnerDictionary.Comparer;

			if(comparer.Compare(key, name) != 0)
				throw new InvalidOperationException("Specified name not equal to computed key of the item.");

			this.InnerDictionary[name] = value;
		}
		#endregion

		#region 显式实现
		bool ICollection<T>.IsReadOnly => false;
		bool ICollection<T>.Contains(T item) => this.InnerDictionary.ContainsKey(this.GetKeyForItem(item));
		bool ICollection<T>.Remove(T item) => this.InnerDictionary.Remove(this.GetKeyForItem(item));
		#endregion
	}
}
