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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Plugins library.
 *
 * The Zongsoft.Plugins is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Plugins is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Plugins library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Plugins
{
	public abstract class PluginElementCollection<TElement> : ICollection<TElement> where TElement : PluginElement
	{
		#region 成员变量
		private bool _isReadOnly;
		private List<TElement> _innerList;
		#endregion

		#region 构造函数
		protected PluginElementCollection()
		{
			_isReadOnly = true;
			_innerList = new List<TElement>();
		}
		#endregion

		#region 公共属性
		public int Count
		{
			get
			{
				return _innerList.Count;
			}
		}

		public virtual bool IsReadOnly
		{
			get
			{
				return _isReadOnly;
			}
			protected set
			{
				_isReadOnly = value;
			}
		}

		public TElement[] Values
		{
			get
			{
				return _innerList.ToArray();
			}
		}
		#endregion

		#region 公共方法
		public int IndexOf(string name)
		{
			if(string.IsNullOrEmpty(name) || _innerList.Count < 1)
				return -1;

			for(int i = 0; i < _innerList.Count; i++)
			{
				TElement element = _innerList[i];

				if(string.Equals(name, element.Name, StringComparison.OrdinalIgnoreCase))
					return i;
			}

			return -1;
		}

		public bool Contains(TElement item)
		{
			if(item == null)
				return false;

			return this.IndexOf(item.Name) >= 0;
		}
		#endregion

		#region 虚拟方法
		protected virtual bool ValidateElement(TElement element)
		{
			return true;
		}

		protected virtual void OnClear()
		{
		}

		protected virtual void OnClearComplete()
		{
		}

		protected virtual void OnRemoveComplete(TElement value)
		{
		}

		protected virtual void OnInsertComplete(TElement value, int index)
		{
		}

		protected virtual void OnSetComplete(TElement oldValue, TElement newValue)
		{
		}
		#endregion

		#region 保护方法
		internal protected TElement Get(int index)
		{
			return _innerList[index];
		}

		internal protected TElement Get(string name)
		{
			if(string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException("name");

			int index = this.IndexOf(name.Trim());

			if(index < 0)
				return null;

			return _innerList[index];
		}

		internal void Insert(TElement value, string name)
		{
			if(string.IsNullOrWhiteSpace(name))
			{
				this.Insert(value, -1);
				return;
			}

			name = name.Trim();

			if(name == "^")
			{
				this.Insert(value, 0);
				return;
			}

			int index = -1;

			if(name.StartsWith(":") || name.StartsWith("-"))
			{
				index = this.IndexOf(name.Substring(1));

				if(index >= 0)
					index++;
			}
			else
			{
				index = this.IndexOf(name);
			}

			this.Insert(value, index);
		}

		protected void Insert(TElement value, int index)
		{
			if(value == null)
				throw new ArgumentNullException("value");

			//首先验证要插入的元素是否合法，如果非法则抛出异常
			if(!this.ValidateElement(value))
				throw new PluginException(string.Format("This '{0}' plugin element was validate failed.", value));

			//获取要插入的元素对象是否存在，如果存在则返回值大于或等于零
			int existedIndex = this.IndexOf(value.Name);

			//如果要插入的元素对象存在，则首先将其所有者置空
			//if(existedIndex >= 0)
			//	this.SetElementOwner(_innerList[existedIndex], null);

			if(index < 0 || index > _innerList.Count)
			{
				if(existedIndex < 0)
				{
					_innerList.Add(value);

					//通知插入操作完成
					this.OnInsertComplete(value, index);
				}
				else
				{
					//保存当前位置的原有值
					TElement oldValue = _innerList[existedIndex];

					//更新当前位置为新值
					_innerList[existedIndex] = value;

					//通知设置操作完成
					this.OnSetComplete(oldValue, value);
				}
			}
			else
			{
				if(existedIndex < 0)
				{
					_innerList.Insert(index, value);

					//通知插入操作完成
					this.OnInsertComplete(value, index);
				}
				else
				{
					//保存当前位置的原有值
					TElement oldValue = _innerList[existedIndex];

					if(index > existedIndex)
						index = index - 1;

					_innerList.RemoveAt(existedIndex);
					_innerList.Insert(index, value);

					//通知设置操作完成
					this.OnSetComplete(oldValue, value);
				}
			}

			//设置新插入元素的所有者对象
			//this.SetElementOwner(value, this.Owner);
		}

		protected void BaseClear()
		{
			//通知准备清空操作
			this.OnClear();

			//循环将列表中的元素对象的所有者置空
			//foreach(TElement element in _innerList)
			//	this.SetElementOwner(element, null);

			//将内部列表清空
			_innerList.Clear();

			//通知清空操作完成
			this.OnClearComplete();
		}

		protected bool BaseRemoveKey(string key)
		{
			if(string.IsNullOrEmpty(key))
				return false;

			foreach(TElement item in _innerList)
			{
				if(string.Equals(key, item.Name, StringComparison.OrdinalIgnoreCase))
				{
					this.BaseRemove(item);
					return true;
				}
			}

			return false;
		}

		protected bool BaseRemove(TElement item)
		{
			if(item == null)
				return false;

			//将元素对象的所有者置空
			//this.SetElementOwner(item, null);

			//将元素对象从列表中删除
			bool result = _innerList.Remove(item);

			//如果删除成功则通知删除操作完成
			if(result)
				this.OnRemoveComplete(item);

			return result;
		}
		#endregion

		#region 显式实现
		void ICollection<TElement>.Add(TElement item)
		{
			if(this.IsReadOnly)
				throw new InvalidOperationException();

			this.Insert(item, -1);
		}

		void ICollection<TElement>.Clear()
		{
			if(this.IsReadOnly)
				throw new InvalidOperationException();

			this.BaseClear();
		}

		bool ICollection<TElement>.Remove(TElement item)
		{
			if(this.IsReadOnly)
				throw new InvalidOperationException();

			return this.BaseRemove(item);
		}

		void ICollection<TElement>.CopyTo(TElement[] array, int arrayIndex)
		{
			throw new NotSupportedException();
		}
		#endregion

		#region IEnumerable<T> 成员
		public IEnumerator<TElement> GetEnumerator()
		{
			foreach(TElement value in _innerList)
			{
				yield return value;
			}
		}
		#endregion

		#region IEnumerable 成员
		IEnumerator IEnumerable.GetEnumerator()
		{
			foreach(TElement value in _innerList)
			{
				yield return value;
			}
		}
		#endregion
	}
}
