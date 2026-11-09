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

namespace Zongsoft.Reflection.Expressions;

/// <summary>提供表达式元素的扩展方法类。</summary>
public static class MemberExpressionExtension
{
	/// <summary>查找指定表达式节点位于表达式中的首个节点元素。</summary>
	/// <param name="expression">指定的要查找的表达式节点。</param>
	/// <returns>返回的首个表达式元素。</returns>
	public static IMemberExpression First(this IMemberExpression expression)
	{
		if(expression == null)
			return null;

		while(expression.Previous != null)
		{
			expression = expression.Previous;
		}

		return expression;
	}

	/// <summary>查找指定表达式节点位于表达式中的最末节点元素。</summary>
	/// <param name="expression">指定的要查找的表达式节点。</param>
	/// <returns>返回的最末表达式元素。</returns>
	public static IMemberExpression Last(this IMemberExpression expression)
	{
		if(expression == null)
			return null;

		while(expression.Next != null)
		{
			expression = expression.Next;
		}

		return expression;
	}
}
