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

namespace Zongsoft.Reflection.Expressions
{
	/// <summary>
	/// 提供表达式元素的扩展方法类。
	/// </summary>
	public static class MemberExpressionExtension
	{
		/// <summary>
		/// 查找指定表达式节点位于表达式中的首个节点元素。
		/// </summary>
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

		/// <summary>
		/// 查找指定表达式节点位于表达式中的最末节点元素。
		/// </summary>
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
}
