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
using System.Linq;

namespace Zongsoft.Configuration.Profiles
{
	internal class ProfileCommentCollection : ICollection<ProfileComment>
	{
		#region 成员字段
		private readonly ProfileItemCollection _items;
		#endregion

		#region 构造函数
		public ProfileCommentCollection(ProfileItemCollection items)
		{
			_items = items ?? throw new ArgumentNullException(nameof(items));
		}
		#endregion

		#region 公共属性
		public int Count
		{
			get
			{
				return _items.Count(item => item.ItemType == ProfileItemType.Comment);
			}
		}

		public bool IsReadOnly
		{
			get
			{
				return false;
			}
		}
		#endregion

		#region 公共方法
		public ProfileComment Add(string comment, int lineNumber = -1)
		{
			if(comment == null)
				return null;

			var item = new ProfileComment(comment, lineNumber);
			_items.Add(item);
			return item;
		}

		public void Add(ProfileComment item)
		{
			_items.Add(item);
		}

		public void Clear()
		{
			foreach(var item in _items)
			{
				if(item.ItemType == ProfileItemType.Comment)
					_items.Remove(item);
			}
		}

		public bool Contains(ProfileComment item)
		{
			return _items.Contains(item);
		}

		public void CopyTo(ProfileComment[] array, int arrayIndex)
		{
			if(array == null)
				return;

			if(arrayIndex >= array.Length)
				throw new ArgumentOutOfRangeException("arrayIndex");

			int index = 0;

			foreach(var item in _items)
			{
				if(arrayIndex + index >= array.Length)
					return;

				if(item.ItemType == ProfileItemType.Comment)
					array[arrayIndex + index++] = (ProfileComment)item;
			}
		}

		public bool Remove(ProfileComment item)
		{
			return _items.Remove(item);
		}
		#endregion

		#region 遍历枚举
		public IEnumerator<ProfileComment> GetEnumerator()
		{
			foreach(var item in _items)
			{
				if(item.ItemType == ProfileItemType.Comment)
					yield return (ProfileComment)item;
			}
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}
		#endregion
	}
}
