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

namespace Zongsoft.Data;

/// <summary>
/// 表示聚合元素的结构。
/// </summary>
public readonly struct DataAggregate
{
	#region 构造函数
	public DataAggregate(DataAggregateFunction function, string name, string alias = null) : this(function, name, false, alias) { }
	public DataAggregate(DataAggregateFunction function, string name, bool distinct, string alias = null)
	{
		this.Function = function;
		this.Name = name?.Trim();
		this.Alias = alias;
		this.Distinct = distinct && !string.IsNullOrEmpty(this.Name) && this.Name != "*";
	}
	#endregion

	#region 公共属性
	/// <summary>获取聚合元素的成员名(字段名或通配符)。</summary>
	public string Name { get; }

	/// <summary>获取聚合元素的别称。</summary>
	public string Alias { get; }

	/// <summary>获取一个值，指示是否开启去重。</summary>
	public bool Distinct { get; }

	/// <summary>获取聚合元素的聚合函数。</summary>
	public DataAggregateFunction Function { get; }
	#endregion

	#region 重写方法
	public override string ToString() => string.IsNullOrEmpty(this.Alias) ?
		$"{this.Function}({(string.IsNullOrEmpty(this.Name) ? "*" : this.Name)})" :
		$"{this.Function}({(string.IsNullOrEmpty(this.Name) ? "*" : this.Name)})@{this.Alias}";
	#endregion

	#region 静态方法
	public static DataAggregate Count(string name, string alias = null) => new(DataAggregateFunction.Count, name, false, alias);
	public static DataAggregate Count(string name, bool distinct, string alias = null) => new(DataAggregateFunction.Count, name, distinct, alias);
	public static DataAggregate Sum(string name, string alias = null) => new(DataAggregateFunction.Sum, name, false, alias);
	public static DataAggregate Sum(string name, bool distinct, string alias = null) => new(DataAggregateFunction.Sum, name, distinct, alias);
	public static DataAggregate Average(string name, string alias = null) => new(DataAggregateFunction.Average, name, false, alias);
	public static DataAggregate Average(string name, bool distinct, string alias = null) => new(DataAggregateFunction.Average, name, distinct, alias);
	public static DataAggregate Median(string name, string alias = null) => new(DataAggregateFunction.Median, name, false, alias);
	public static DataAggregate Median(string name, bool distinct, string alias = null) => new(DataAggregateFunction.Median, name, distinct, alias);
	public static DataAggregate Maximum(string name, string alias = null) => new(DataAggregateFunction.Maximum, name, false, alias);
	public static DataAggregate Maximum(string name, bool distinct, string alias = null) => new(DataAggregateFunction.Maximum, name, distinct, alias);
	public static DataAggregate Minimum(string name, string alias = null) => new(DataAggregateFunction.Minimum, name, false, alias);
	public static DataAggregate Minimum(string name, bool distinct, string alias = null) => new(DataAggregateFunction.Minimum, name, distinct, alias);
	public static DataAggregate Deviation(string name, string alias = null) => new(DataAggregateFunction.Deviation, name, false, alias);
	public static DataAggregate Deviation(string name, bool distinct, string alias = null) => new(DataAggregateFunction.Deviation, name, distinct, alias);
	public static DataAggregate DeviationPopulation(string name, string alias = null) => new(DataAggregateFunction.DeviationPopulation, name, false, alias);
	public static DataAggregate DeviationPopulation(string name, bool distinct, string alias = null) => new(DataAggregateFunction.DeviationPopulation, name, distinct, alias);
	public static DataAggregate Variance(string name, string alias = null) => new(DataAggregateFunction.Variance, name, false, alias);
	public static DataAggregate Variance(string name, bool distinct, string alias = null) => new(DataAggregateFunction.Variance, name, distinct, alias);
	public static DataAggregate VariancePopulation(string name, string alias = null) => new(DataAggregateFunction.VariancePopulation, name, false, alias);
	public static DataAggregate VariancePopulation(string name, bool distinct, string alias = null) => new(DataAggregateFunction.VariancePopulation, name, distinct, alias);
	#endregion
}
