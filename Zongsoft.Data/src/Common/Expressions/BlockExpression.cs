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

/// <summary>表示由多个表达式组成的块级表达式类。</summary>
public class BlockExpression : Expression, ICollection<IExpression>
{
	#region 成员字段
	private readonly List<IExpression> _items;
	#endregion

	#region 构造函数
	public BlockExpression(BlockExpressionDelimiter delimiter = BlockExpressionDelimiter.None)
	{
		this.Delimiter = delimiter;
		_items = new List<IExpression>();
	}

	public BlockExpression(BlockExpressionDelimiter delimiter, IEnumerable<IExpression> items)
	{
		this.Delimiter = delimiter;

		if(items == null)
			_items = new List<IExpression>();
		else
			_items = new List<IExpression>(items);
	}
	#endregion

	#region 公共属性
	/// <summary>获取块级表达式的元素分割符。</summary>
	public BlockExpressionDelimiter Delimiter { get; }
	public int Count => _items.Count;
	bool ICollection<IExpression>.IsReadOnly => false;
	public IExpression this[int index] => _items[index];
	#endregion

	#region 公共方法
	public void Add(IExpression item)
	{
		if(item != null)
			_items.Add(item);
	}

	public void Insert(int index, IExpression item) => _items.Insert(index, item);
	public void Clear() => _items.Clear();
	public bool Remove(IExpression item) => _items.Remove(item);
	public void RemoveAt(int index) => _items.RemoveAt(index);
	#endregion

	#region 显式实现
	bool ICollection<IExpression>.Contains(IExpression item) => _items.Contains(item);
	void ICollection<IExpression>.CopyTo(IExpression[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
	#endregion

	#region 迭代遍历
	public IEnumerator<IExpression> GetEnumerator() => _items.GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
	#endregion
}

/// <summary>表示块级表达式的分割符枚举。</summary>
public enum BlockExpressionDelimiter
{
	/// <summary>无分隔。</summary>
	None,

	/// <summary>空格符。</summary>
	Space,

	/// <summary>换行符。</summary>
	Break,

	/// <summary>语句终结符，通常为分号。</summary>
	Terminator,
}
