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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Data.Common.Expressions;

public sealed class ExpressionCollection : ExpressionCollection<IExpression>
{
	public ExpressionCollection() { }
	public ExpressionCollection(IEnumerable<IExpression> items) : base(items) { }
}

public class ExpressionCollection<TExpression> : Expression, ICollection<TExpression> where TExpression : IExpression
{
	#region 成员字段
	private readonly List<TExpression> _items;
	#endregion

	#region 构造函数
	public ExpressionCollection() => _items = [];
	public ExpressionCollection(IEnumerable<TExpression> items) => _items = [.. items];
	#endregion

	#region 公共属性
	public int Count => _items.Count;
	bool ICollection<TExpression>.IsReadOnly => false;
	public IExpression this[int index] => _items[index];
	#endregion

	#region 公共方法
	public void Add(TExpression item)
	{
		if(item != null)
			_items.Add(item);
	}

	public void Insert(int index, TExpression item) => _items.Insert(index, item);
	public void Clear() => _items.Clear();
	public bool Remove(TExpression item) => _items.Remove(item);
	public void RemoveAt(int index) => _items.RemoveAt(index);
	#endregion

	#region 显式实现
	bool ICollection<TExpression>.Contains(TExpression item) => _items.Contains(item);
	void ICollection<TExpression>.CopyTo(TExpression[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
	#endregion

	#region 枚举遍历
	public IEnumerator<TExpression> GetEnumerator() => _items.GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
	#endregion
}
