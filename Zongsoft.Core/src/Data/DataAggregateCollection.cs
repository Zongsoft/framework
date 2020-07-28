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
		public DataAggregateCollection Count(string name, string alias = null)
		{
			return this.Aggregate(DataAggregateMethod.Count, name, alias);
		}

		public DataAggregateCollection Sum(string name, string alias = null)
		{
			return this.Aggregate(DataAggregateMethod.Sum, name, alias);
		}

		public DataAggregateCollection Average(string name, string alias = null)
		{
			return this.Aggregate(DataAggregateMethod.Average, name, alias);
		}

		public DataAggregateCollection Median(string name, string alias = null)
		{
			return this.Aggregate(DataAggregateMethod.Median, name, alias);
		}

		public DataAggregateCollection Maximum(string name, string alias = null)
		{
			return this.Aggregate(DataAggregateMethod.Maximum, name, alias);
		}

		public DataAggregateCollection Minimum(string name, string alias = null)
		{
			return this.Aggregate(DataAggregateMethod.Minimum, name, alias);
		}

		public DataAggregateCollection Deviation(string name, string alias = null)
		{
			return this.Aggregate(DataAggregateMethod.Deviation, name, alias);
		}

		public DataAggregateCollection DeviationPopulation(string name, string alias = null)
		{
			return this.Aggregate(DataAggregateMethod.DeviationPopulation, name, alias);
		}

		public DataAggregateCollection Variance(string name, string alias = null)
		{
			return this.Aggregate(DataAggregateMethod.Variance, name, alias);
		}

		public DataAggregateCollection VariancePopulation(string name, string alias = null)
		{
			return this.Aggregate(DataAggregateMethod.VariancePopulation, name, alias);
		}
		#endregion

		#region 私有方法
		private DataAggregateCollection Aggregate(DataAggregateMethod method, string name, string alias = null)
		{
			if(string.IsNullOrEmpty(name) && method != DataAggregateMethod.Count)
				throw new ArgumentNullException(nameof(name));

			_members.Add(new DataAggregate(method, name, alias));

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
