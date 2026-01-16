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
 * This file is part of Zongsoft.Data library.
 *
 * The Zongsoft.Data is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Data.Common.Expressions;

public class ConditionExpression : Expression, ICollection<IExpression>
{
	#region 成员字段
	private readonly List<IExpression> _items;
	#endregion

	#region 构造函数
	public ConditionExpression(ConditionCombination combination, IEnumerable<IExpression> items = null)
	{
		this.Combination = combination;
		_items = new List<IExpression>();

		if(items != null)
		{
			foreach(var item in items)
				this.Add(item);
		}
	}
	#endregion

	#region 公共属性
	public ConditionCombination Combination { get; }
	public int Count => _items.Count;
	bool ICollection<IExpression>.IsReadOnly => false;
	#endregion

	#region 公共方法
	public void Add(IExpression item)
	{
		if(item == null)
			return;

		if(item is ConditionExpression conditions && conditions.Combination == this.Combination)
		{
			foreach(var condition in conditions)
				_items.Add(condition);
		}
		else if(item is BinaryExpression binary && Same(this.Combination, binary.Operator))
		{
			_items.Add(binary.Left);
			_items.Add(binary.Right);
		}
		else
			_items.Add(item);

		static bool Same(ConditionCombination combination, Operator @operator) => combination switch
		{
			ConditionCombination.And => @operator == Operator.AndAlso,
			ConditionCombination.Or => @operator == Operator.OrElse,
			_ => false,
		};
	}

	public void Clear() => _items.Clear();
	public bool Contains(IExpression item) => _items.Contains(item);
	public void CopyTo(IExpression[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
	public bool Remove(IExpression item) => _items.Remove(item);
	public IEnumerator<IExpression> GetEnumerator() => _items.GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
	#endregion

	#region 静态方法
	public static ConditionExpression And(IEnumerable<IExpression> items) => new(ConditionCombination.And, items);
	public static ConditionExpression And(params IExpression[] items) => new(ConditionCombination.And, items);
	public static ConditionExpression Or(IEnumerable<IExpression> items) => new(ConditionCombination.Or, items);
	public static ConditionExpression Or(params IExpression[] items) => new(ConditionCombination.Or, items);
	#endregion
}
