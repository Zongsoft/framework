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
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Data;

public class DataAggregateCollection : IEnumerable<DataAggregate>
{
	#region 私有变量
	private readonly ICollection<DataAggregate> _members;
	#endregion

	#region 私有构造
	internal DataAggregateCollection() => _members = new List<DataAggregate>();
	#endregion

	#region 公共属性
	public bool IsEmpty => _members.Count == 0;
	#endregion

	#region 公共方法
	public DataAggregateCollection Count(bool distinct = false, string alias = null) => this.Aggregate(DataAggregateFunction.Count, null, distinct, alias);
	public DataAggregateCollection Count(string member, string alias = null) => this.Aggregate(DataAggregateFunction.Count, member, false, alias);
	public DataAggregateCollection Count(string member, bool distinct, string alias = null) => this.Aggregate(DataAggregateFunction.Count, member, distinct, alias);

	public DataAggregateCollection Sum(string member, string alias = null) => this.Aggregate(DataAggregateFunction.Sum, member, false, alias);
	public DataAggregateCollection Sum(string member, bool distinct, string alias = null) => this.Aggregate(DataAggregateFunction.Sum, member, distinct, alias);

	public DataAggregateCollection Average(string member, string alias = null) => this.Aggregate(DataAggregateFunction.Average, member, false, alias);
	public DataAggregateCollection Average(string member, bool distinct, string alias = null) => this.Aggregate(DataAggregateFunction.Average, member, distinct, alias);

	public DataAggregateCollection Median(string member, string alias = null) => this.Aggregate(DataAggregateFunction.Median, member, false, alias);
	public DataAggregateCollection Median(string member, bool distinct, string alias = null) => this.Aggregate(DataAggregateFunction.Median, member, distinct, alias);

	public DataAggregateCollection Maximum(string member, string alias = null) => this.Aggregate(DataAggregateFunction.Maximum, member, false, alias);
	public DataAggregateCollection Maximum(string member, bool distinct, string alias = null) => this.Aggregate(DataAggregateFunction.Maximum, member, distinct, alias);

	public DataAggregateCollection Minimum(string member, string alias = null) => this.Aggregate(DataAggregateFunction.Minimum, member, false, alias);
	public DataAggregateCollection Minimum(string member, bool distinct, string alias = null) => this.Aggregate(DataAggregateFunction.Minimum, member, distinct, alias);

	public DataAggregateCollection Deviation(string member, string alias = null) => this.Aggregate(DataAggregateFunction.Deviation, member, false, alias);
	public DataAggregateCollection Deviation(string member, bool distinct, string alias = null) => this.Aggregate(DataAggregateFunction.Deviation, member, distinct, alias);

	public DataAggregateCollection DeviationPopulation(string member, string alias = null) => this.Aggregate(DataAggregateFunction.DeviationPopulation, member, false, alias);
	public DataAggregateCollection DeviationPopulation(string member, bool distinct, string alias = null) => this.Aggregate(DataAggregateFunction.DeviationPopulation, member, distinct, alias);

	public DataAggregateCollection Variance(string member, string alias = null) => this.Aggregate(DataAggregateFunction.Variance, member, false, alias);
	public DataAggregateCollection Variance(string member, bool distinct, string alias = null) => this.Aggregate(DataAggregateFunction.Variance, member, distinct, alias);

	public DataAggregateCollection VariancePopulation(string member, string alias = null) => this.Aggregate(DataAggregateFunction.VariancePopulation, member, false, alias);
	public DataAggregateCollection VariancePopulation(string member, bool distinct, string alias = null) => this.Aggregate(DataAggregateFunction.VariancePopulation, member, distinct, alias);
	#endregion

	#region 私有方法
	private DataAggregateCollection Aggregate(DataAggregateFunction function, string name, bool distinct = false, string alias = null)
	{
		if(string.IsNullOrEmpty(name) && function != DataAggregateFunction.Count)
			throw new ArgumentNullException(nameof(name));

		_members.Add(new DataAggregate(function, name, distinct, alias));
		return this;
	}
	#endregion

	#region 遍历实现
	public IEnumerator<DataAggregate> GetEnumerator() => _members.GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
	#endregion
}
