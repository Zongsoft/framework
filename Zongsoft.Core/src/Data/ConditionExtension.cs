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
		#region 匹配方法
		/// <summary>
		/// 判断当前条件语句中是否包含指定名称的条件项。
		/// </summary>
		/// <param name="criteria">待判断的条件。</param>
		/// <param name="name">指定的条件项名称。</param>
		/// <param name="maxDepth">最大的搜索深度，如果为零或负数则表示不限深度。</param>
		/// <returns>如果存在则返回真(True)，否则返回假(False)。</returns>
		public static bool Contains(this ICondition criteria, string name, int maxDepth = 0)
		{
			return Matches(criteria, name, 1, 0, maxDepth, 0, null) > 0;
		}

		/// <summary>
		/// 在条件语句中查找指定名称的条件项。
		/// </summary>
		/// <param name="criteria">待查找的条件。</param>
		/// <param name="name">指定要查找的条件项名称。</param>
		/// <param name="maxDepth">最大的查找深度，如果为零或负数则表示不限深度。</param>
		/// <returns>如果查找成功则返回找到的条件项，否则返回空(null)。</returns>
		public static Condition Find(this ICondition criteria, string name, int maxDepth = 0)
		{
			Condition found = null;
			Matches(criteria, name, 1, 0, maxDepth, 0, (condition, depth) => found = condition);
			return found;
		}

		/// <summary>
		/// 在条件语句中查找指定名称的所有条件项。
		/// </summary>
		/// <param name="criteria">待查找的条件。</param>
		/// <param name="name">指定要查找的条件项名称。</param>
		/// <param name="maxDepth">最大的查找深度，如果为零或负数则表示不限深度。</param>
		/// <returns>返回匹配成功的所有条件项数组，否则返回空(null)或空数组。</returns>
		public static ICollection<Condition> FindAll(this ICondition criteria, string name, int maxDepth = 0)
		{
			var conditions = new List<Condition>();
			Matches(criteria, name, 1, 0, maxDepth, 0, (condition, depth) => conditions.Add(condition));
			return conditions;
		}

		/// <summary>
		/// 在条件语句中查找指定名称的所有条件项，如果匹配到则回调指定的匹配函数。
		/// </summary>
		/// <param name="criteria">待查找的条件。</param>
		/// <param name="name">指定要匹配的条件项名称。</param>
		/// <param name="maxCount">最大的搜索数量，如果为零或负数则表示不限数量。</param>
		/// <param name="maxDepth">最大的搜索深度，如果为零或负数则表示不限深度。</param>
		/// <param name="matched">指定的匹配成功的回调函数。</param>
		/// <returns>返回匹配成功的条件项数量，如果为零则表示没有匹配到任何条件项。</returns>
		public static int Matches(this ICondition criteria, string name, int maxCount, int maxDepth, Action<Condition, int> matched)
		{
			return Matches(criteria, name, maxCount, 0, maxDepth, 0, matched);
		}

		private static int Matches(ICondition criteria, string name, int maxCount, int count, int maxDepth, int depth, Action<Condition, int> match)
		{
			if(maxCount > 0 && count >= maxCount)
				return 0;

			if(maxDepth > 0 && depth >= maxDepth)
				return 0;

			if(criteria == null || string.IsNullOrEmpty(name))
				return 0;

			if(criteria is Condition condition)
			{
				if(string.Equals(name, condition.Name, StringComparison.OrdinalIgnoreCase))
				{
					match?.Invoke(condition, depth);
					return 1;
				}

				return 0;
			}

			if(criteria is ConditionCollection conditions)
			{
				if(conditions.Count == 0)
					return 0;

				for(int i = 0; i < conditions.Count; i++)
				{
					count += Matches(
						conditions[i],
						name,
						maxCount,
						count,
						maxDepth,
						conditions[i] is ConditionCollection cs && cs.Combination != conditions.Combination ? depth + 1 : depth,
						match);

					if(maxCount > 0 && count >= maxCount)
						break;
				}

				return count;
			}

			return 0;
		}
		#endregion

		#region 替换方法
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
		#endregion
	}
}
