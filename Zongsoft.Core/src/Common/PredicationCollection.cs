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
using System.Collections.ObjectModel;

namespace Zongsoft.Common
{
	public class PredicationCollection : Collection<IPredication>, IPredication
	{
		#region 成员字段
		private readonly PredicationCombination _combination;
		#endregion

		#region 构造函数
		public PredicationCollection() : this(PredicationCombination.And) { }
		public PredicationCollection(PredicationCombination combination) => _combination = combination;
		#endregion

		#region 公共属性
		/// <summary>获取或设置断言集合内各断言的逻辑组合方式。</summary>
		public PredicationCombination Combination => _combination;
		#endregion

		#region 断言方法
		/// <summary>对断言集合内的所有断言进行遍历断言调用，并根据<see cref="Combination"/>属性值进行组合判断。</summary>
		/// <param name="parameter">对断言集合内所有断言调用时的传入参数。</param>
		/// <returns>集合内所有断言的组合结果，如果集合为空则始终返回真(true)。</returns>
		/// <remarks>
		///		<para>在调用过程中如果是“或”组合则会发生“真”短路；如果是“与”组合则会发生“假”短路。</para>
		/// </remarks>
		public bool Predicate(object parameter)
		{
			var predications = base.Items;

			if(predications == null || predications.Count < 1)
				return true;

			foreach(var predication in predications)
			{
				if(predication == null)
					continue;

				if(predication.Predicate(parameter))
				{
					if(_combination == PredicationCombination.Or)
						return true;
				}
				else
				{
					if(_combination == PredicationCombination.And)
						return false;
				}
			}

			return _combination == PredicationCombination.Or ? false : true;
		}
		#endregion
	}
}
