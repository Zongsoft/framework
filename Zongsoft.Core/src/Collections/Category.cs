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

namespace Zongsoft.Collections
{
	[Serializable]
	[System.Reflection.DefaultMember(nameof(Children))]
	public class Category : HierarchicalNode<Category>
	{
		#region 成员字段
		private bool _visible;
		private CategoryCollection _children;
		#endregion

		#region 构造函数
		public Category()
		{
			_visible = true;
		}

		public Category(string name) : this(name, name, string.Empty, true)
		{
		}

		public Category(string name, string title, string description) : this(name, title, description, true)
		{
		}

		public Category(string name, string title, string description, bool visible) : base(name, title, description)
		{
			_visible = visible;
		}
		#endregion

		#region 公共属性
		public bool Visible
		{
			get
			{
				return _visible;
			}
			set
			{
				_visible = value;
			}
		}

		public CategoryCollection Children
		{
			get
			{
				if(_children == null)
					System.Threading.Interlocked.CompareExchange(ref _children, new CategoryCollection(this), null);

				return _children;
			}
		}
		#endregion

		#region 重写方法
		protected override HierarchicalNode GetChild(string name)
		{
			var children = _children;

			if(children != null && children.TryGet(name, out var child))
				return child;

			return null;
		}
		#endregion
	}
}
