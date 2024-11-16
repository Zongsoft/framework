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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Collections
{
	public class CategoryCollection : HierarchicalNodeCollection<Category>
	{
		#region 构造函数
		public CategoryCollection() : base(null) { }
		internal CategoryCollection(Category owner) : base(owner) { }
		#endregion

		#region 公共方法
		public Category Add(string name)
		{
			var category = new Category(name);
			this.Add(category);
			return category;
		}

		public void AddRange(params Category[] categories) => this.AddRange((IEnumerable<Category>)categories);
		public void AddRange(IEnumerable<Category> categories)
		{
			if(categories == null)
				return;

			foreach(var category in categories)
				this.Add(category);
		}
		#endregion

		#region 重写方法
		protected override void SetOwner(Category owner, Category node) => node?.SetParent(owner);
		#endregion
	}
}
