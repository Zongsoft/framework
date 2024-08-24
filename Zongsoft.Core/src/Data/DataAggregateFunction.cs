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

namespace Zongsoft.Data
{
	/// <summary>
	/// 表示聚合函数的枚举。
	/// </summary>
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
}
