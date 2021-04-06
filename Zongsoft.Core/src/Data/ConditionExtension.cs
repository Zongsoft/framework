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

namespace Zongsoft.Data
{
	/// <summary>
	/// 提供条件操作的扩展方法。
	/// </summary>
	public static class ConditionExtension
	{
		/// <summary>
		/// 替换条件中指定名称的条件项。
		/// </summary>
		/// <param name="criteria">待替换的条件。</param>
		/// <param name="name">要替换的条件项名称。</param>
		/// <param name="match">替换处理函数，该函数参数说明：
		/// 	<list type="bullet">
		/// 		<item>第1个参数：指定名称的待替换条件项；</item>
		/// 		<item>第2个参数：待替换条件项所处深度(深度值从零开始)。</item>
		/// 	</list>
		/// </param>
		/// <returns>返回替换后的条件。</returns>
		public static ICondition Replace(this ICondition criteria, string name, Func<Condition, int, ICondition> match)
		{
			return Replace(criteria, name, -1, 0, match, out _);
		}

		/// <summary>
		/// 替换条件中指定名称的条件项。
		/// </summary>
		/// <param name="criteria">待替换的条件。</param>
		/// <param name="name">要替换的条件项名称。</param>
		/// <param name="maxDepth">最大的替换深度，如果为零或负数则表示不限深度。</param>
		/// <param name="match">替换处理函数，该函数参数说明：
		/// 	<list type="bullet">
		/// 		<item>第1个参数：指定名称的待替换条件项；</item>
		/// 		<item>第2个参数：待替换条件项所处深度(深度值从零开始)。</item>
		/// 	</list>
		/// </param>
		/// <returns>返回替换后的条件。</returns>
		public static ICondition Replace(this ICondition criteria, string name, int maxDepth, Func<Condition, int, ICondition> match)
		{
			return Replace(criteria, name, maxDepth, 0, match, out _);
		}

		/// <summary>
		/// 替换条件中指定名称的条件项。
		/// </summary>
		/// <param name="criteria">待替换的条件。</param>
		/// <param name="name">要替换的条件项名称。</param>
		/// <param name="match">替换处理函数，该函数参数说明：
		/// 	<list type="bullet">
		/// 		<item>第1个参数：指定名称的待替换条件项；</item>
		/// 		<item>第2个参数：待替换条件项所处深度(深度值从零开始)。</item>
		/// 	</list>
		/// </param>
		/// <param name="count">输出参数，表示匹配项的数量。</param>
		/// <returns>返回替换后的条件。</returns>
		public static ICondition Replace(this ICondition criteria, string name, Func<Condition, int, ICondition> match, out int count)
		{
			return Replace(criteria, name, -1, 0, match, out count);
		}

		/// <summary>
		/// 替换条件中指定名称的条件项。
		/// </summary>
		/// <param name="criteria">待替换的条件。</param>
		/// <param name="name">要替换的条件项名称。</param>
		/// <param name="maxDepth">最大的替换深度，如果为零或负数则表示不限深度。</param>
		/// <param name="match">替换处理函数，该函数参数说明：
		/// 	<list type="bullet">
		/// 		<item>第1个参数：指定名称的待替换条件项；</item>
		/// 		<item>第2个参数：待替换条件项所处深度(深度值从零开始)。</item>
		/// 	</list>
		/// </param>
		/// <param name="count">输出参数，表示匹配项的数量。</param>
		/// <returns>返回替换后的条件。</returns>
		public static ICondition Replace(this ICondition criteria, string name, int maxDepth, Func<Condition, int, ICondition> match, out int count)
		{
			return Replace(criteria, name, maxDepth, 0, match, out count);
		}

		private static ICondition Replace(this ICondition criteria, string name, int maxDepth, int depth, Func<Condition, int, ICondition> match, out int count)
		{
			count = 0;

			if(maxDepth > 0 && depth >= maxDepth)
				return criteria;

			if(criteria == null || string.IsNullOrEmpty(name))
				return criteria;

			if(criteria is Condition condition)
			{
				if(string.Equals(name, condition.Name, StringComparison.OrdinalIgnoreCase))
				{
					count = 1;
					return match(condition, depth);
				}

				return criteria;
			}

			if(criteria is ConditionCollection conditions)
			{
				if(conditions.Count == 0)
					return criteria;

				for(int i = 0; i < conditions.Count; i++)
				{
					conditions[i] = Replace(
						conditions[i],
						name,
						maxDepth,
						conditions[i] is ConditionCollection cs && cs.Combination != conditions.Combination ? depth + 1 : depth,
						match,
						out var temp);

					count += temp;
				}
			}

			return criteria;
		}
	}
}
