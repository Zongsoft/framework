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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Linq;
using System.Collections.Generic;

namespace Zongsoft.Collections;

public abstract class CategoryCollectionBase<TCategory> : HierarchicalNodeCollection<TCategory> where TCategory : CategoryBase<TCategory>
{
	#region 构造函数
	protected CategoryCollectionBase() : base(null) { }
	protected CategoryCollectionBase(TCategory owner) : base(owner) { }
	#endregion

	#region 公共方法
	public void AddRange(params TCategory[] categories) => this.AddRange((IEnumerable<TCategory>)categories);
	public void AddRange(IEnumerable<TCategory> categories)
	{
		if(categories == null)
			return;

		foreach(var category in categories.OrderBy(category => category.Ordinal))
			this.Add(category);
	}
	#endregion

	#region 重写方法
	protected override void SetOwner(TCategory owner, TCategory category) => category?.SetParent(owner);
	protected override void InsertItem(int index, TCategory category)
	{
		index = Locate(this.Items, category.Ordinal);
		base.InsertItem(index, category);
	}
	#endregion

	#region 私有方法
	private static int Locate(IList<TCategory> categories, int value)
	{
		int left = 0, right = categories.Count - 1;

		while(left <= right)
		{
			int middle = left + (right - left) / 2;

			if(categories[middle].Ordinal == value)
				return middle;

			if(categories[middle].Ordinal < value)
			{
				left = middle + 1;

				if(left >= categories.Count)
					return categories.Count;

				if(value < categories[left].Ordinal)
					return left;
			}
			else
			{
				right = middle - 1;

				if(right < 0)
					return 0;

				if(value > categories[right].Ordinal)
					return middle;
			}
		}

		return 0;
	}
	#endregion
}