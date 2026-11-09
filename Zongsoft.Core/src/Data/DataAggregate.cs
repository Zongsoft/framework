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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;

namespace Zongsoft.Data;

/// <summary>表示聚合元素的结构。</summary>
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
