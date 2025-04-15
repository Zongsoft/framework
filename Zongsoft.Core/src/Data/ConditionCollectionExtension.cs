﻿/*
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
using System.Collections.Generic;

namespace Zongsoft.Data;

/// <summary>
/// 提供条件集合扩展方法的静态扩展类。
/// </summary>
public static class ConditionCollectionExtension
{
	#region 集合构建
	/// <summary>构建条件「与」集。</summary>
	/// <param name="criteria">指定构建条件「与」集的条件项。</param>
	/// <param name="conditions">指定构建条件「与」集的其他项数组。</param>
	/// <returns>返回构建的条件「与」集。</returns>
	public static ConditionCollection And(this ICondition criteria, params ICondition[] conditions)
	{
		return ConditionCollection.And(Enumerate(criteria, conditions));
	}

	/// <summary>构建条件「与」集。</summary>
	/// <param name="criteria">指定构建条件「与」集的条件项。</param>
	/// <param name="conditions">指定构建条件「与」集的其他项集合。</param>
	/// <returns>返回构建的条件「与」集。</returns>
	public static ConditionCollection And(this ICondition criteria, IEnumerable<ICondition> conditions)
	{
		return ConditionCollection.And(Enumerate(criteria, conditions));
	}

	/// <summary>构建条件「与」集。</summary>
	/// <param name="criteria">指定构建条件「与」集的条件项。</param>
	/// <param name="conditions">指定构建条件「与」集的其他项集合。</param>
	/// <returns>返回构建的条件「与」集。</returns>
	public static ConditionCollection And(this ICondition criteria, ConditionCollection conditions)
	{
		if(criteria == null)
			return conditions;

		return ConditionCollection.And(Enumerate(criteria, conditions));
	}

	/// <summary>构建条件「或」集。</summary>
	/// <param name="criteria">指定构建条件「或」集的条件项。</param>
	/// <param name="conditions">指定构建条件「或」集的其他项数组。</param>
	/// <returns>返回构建的条件「或」集。</returns>
	public static ConditionCollection Or(this ICondition criteria, params ICondition[] conditions)
	{
		return ConditionCollection.Or(Enumerate(criteria, conditions));
	}

	/// <summary>构建条件「或」集。</summary>
	/// <param name="criteria">指定构建条件「或」集的条件项。</param>
	/// <param name="conditions">指定构建条件「或」集的其他项集合。</param>
	/// <returns>返回构建的条件「或」集。</returns>
	public static ConditionCollection Or(this ICondition criteria, IEnumerable<ICondition> conditions)
	{
		return ConditionCollection.Or(Enumerate(criteria, conditions));
	}

	/// <summary>构建条件「或」集。</summary>
	/// <param name="criteria">指定构建条件「或」集的条件项。</param>
	/// <param name="conditions">指定构建条件「或」集的其他项集合。</param>
	/// <returns>返回构建的条件「或」集。</returns>
	public static ConditionCollection Or(this ICondition criteria, ConditionCollection conditions)
	{
		if(criteria == null)
			return conditions;

		return ConditionCollection.Or(Enumerate(criteria, conditions));
	}

	private static IEnumerable<ICondition> Enumerate(ICondition first, IEnumerable<ICondition> conditions)
	{
		if(first != null)
			yield return first;

		if(conditions != null)
		{
			if(conditions is ConditionCollection cs)
				yield return cs;
			else
			{
				foreach(var condition in conditions)
					yield return condition;
			}
		}
	}
	#endregion

	#region 展平处理
	/// <summary>将指定的条件展平。</summary>
	/// <param name="criteria">指定要展平的条件，如果该参数不是 <see cref="ConditionCollection"/> 条件集合类型则忽略本次调用。</param>
	/// <returns>如果指定的 <paramref name="criteria"/> 是 <see cref="ConditionCollection"/> 条件集合类型则对其进行展平并返回其自身；否则返回 <paramref name="criteria"/> 参数本身。</returns>
	public static ICondition Flatten(this ICondition criteria)
	{
		return Flatten(criteria as ConditionCollection) ?? criteria;
	}

	/// <summary>将指定的条件集合展平。</summary>
	/// <param name="conditions">指定要展平的条件集合。</param>
	/// <returns>返回展品后的集合本身，即指定的 <paramref name="conditions"/> 参数本身。</returns>
	public static ConditionCollection Flatten(this ConditionCollection conditions)
	{
		if(conditions == null || conditions.Count == 0)
			return conditions;

		List<ConditionCollection> removables = null;

		foreach(var condition in conditions)
		{
			//只有集合项才需要展平处理
			if(condition is ConditionCollection cs)
			{
				//对当前集合项进行递归展平
				if(cs.Count > 0)
					Flatten(cs);

				/* 以下即为对展平后的当前集合项进行再处理 */

				//① 集合元素数量为1或者0的，无论条件组合方式是什么都可以向上合并；
				//② 集合组合方式等于当前集合组合方式的，可以向上合并
				if(cs.Count <= 1 || cs.Combination == conditions.Combination)
				{
					if(removables == null)
						removables = new List<ConditionCollection>();

					removables.Add(cs);
				}
			}
		}

		if(removables != null && removables.Count > 0)
		{
			foreach(var removable in removables)
			{
				var index = conditions.IndexOf(removable);

				foreach(var child in removable)
					conditions.Insert(index++, child);

				conditions.Remove(removable);
			}
		}

		return conditions;
	}
	#endregion
}
