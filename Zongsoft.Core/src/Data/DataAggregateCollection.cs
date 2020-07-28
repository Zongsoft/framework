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
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Data
{
	public class DataAggregateCollection : IEnumerable<DataAggregate>
	{
		#region 私有变量
		private readonly ICollection<DataAggregate> _members;
		#endregion

		#region 私有构造
		internal DataAggregateCollection()
		{
			_members = new List<DataAggregate>();
		}
		#endregion

		#region 公共方法
		public DataAggregateCollection Count(string member, string alias = null)
		{
			return this.Aggregate(DataAggregateFunction.Count, member, alias);
		}

		public DataAggregateCollection Sum(string member, string alias = null)
		{
			return this.Aggregate(DataAggregateFunction.Sum, member, alias);
		}

		public DataAggregateCollection Average(string member, string alias = null)
		{
			return this.Aggregate(DataAggregateFunction.Average, member, alias);
		}

		public DataAggregateCollection Median(string member, string alias = null)
		{
			return this.Aggregate(DataAggregateFunction.Median, member, alias);
		}

		public DataAggregateCollection Maximum(string member, string alias = null)
		{
			return this.Aggregate(DataAggregateFunction.Maximum, member, alias);
		}

		public DataAggregateCollection Minimum(string member, string alias = null)
		{
			return this.Aggregate(DataAggregateFunction.Minimum, member, alias);
		}

		public DataAggregateCollection Deviation(string member, string alias = null)
		{
			return this.Aggregate(DataAggregateFunction.Deviation, member, alias);
		}

		public DataAggregateCollection DeviationPopulation(string member, string alias = null)
		{
			return this.Aggregate(DataAggregateFunction.DeviationPopulation, member, alias);
		}

		public DataAggregateCollection Variance(string member, string alias = null)
		{
			return this.Aggregate(DataAggregateFunction.Variance, member, alias);
		}

		public DataAggregateCollection VariancePopulation(string member, string alias = null)
		{
			return this.Aggregate(DataAggregateFunction.VariancePopulation, member, alias);
		}
		#endregion

		#region 私有方法
		private DataAggregateCollection Aggregate(DataAggregateFunction function, string name, string alias = null)
		{
			if(string.IsNullOrEmpty(name) && function != DataAggregateFunction.Count)
				throw new ArgumentNullException(nameof(name));

			_members.Add(new DataAggregate(function, name, alias));

			return this;
		}
		#endregion

		#region 遍历实现
		public IEnumerator<DataAggregate> GetEnumerator()
		{
			return _members.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}
		#endregion
	}
}
