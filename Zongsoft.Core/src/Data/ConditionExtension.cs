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
	public static class ConditionExtension
	{
		public static ICondition Replace(this ICondition criteria, string name, Func<Condition, int, ICondition> matched)
		{
			return Replace(criteria, name, 0, matched, out _);
		}

		public static ICondition Replace(this ICondition criteria, string name, Func<Condition, int, ICondition> matched, out int count)
		{
			return Replace(criteria, name, 0, matched, out count);
		}

		private static ICondition Replace(this ICondition criteria, string name, int depth, Func<Condition, int, ICondition> matched, out int count)
		{
			count = 0;

			if(criteria == null || string.IsNullOrEmpty(name))
				return criteria;

			if(criteria is Condition condition)
			{
				if(string.Equals(name, condition.Name, StringComparison.OrdinalIgnoreCase))
				{
					var replacement = matched(condition, depth);
					count = object.Equals(condition, replacement) ? 0 : 1;
					return replacement;
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
						conditions[i] is ConditionCollection cs && cs.Combination != conditions.Combination ? depth + 1 : depth,
						matched,
						out var temp);

					count += temp;
				}
			}

			return criteria;
		}
	}
}
