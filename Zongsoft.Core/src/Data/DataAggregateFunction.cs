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

/// <summary>表示聚合函数的枚举。</summary>
public enum DataAggregateFunction
{
	/// <summary>数量</summary>
	Count,

	/// <summary>总和</summary>
	Sum,

	/// <summary>平均值</summary>
	Average,

	/// <summary>中间值</summary>
	Median,

	/// <summary>最大值</summary>
	Maximum,

	/// <summary>最小值</summary>
	Minimum,

	/// <summary>标准偏差</summary>
	Deviation,

	/// <summary>总体标准偏差</summary>
	DeviationPopulation,

	/// <summary>方差</summary>
	Variance,

	/// <summary>总体方差</summary>
	VariancePopulation,
}
