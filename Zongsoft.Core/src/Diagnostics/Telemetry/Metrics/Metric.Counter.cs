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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Diagnostics.Telemetry.Metrics;

partial class Metric 
{
	public sealed class Counter : Metric
	{
		#region 构造函数
		public Counter(string name, string unit, bool isMonotonic, params Point[] points) : this(name, unit, isMonotonic, null, points) { }
		public Counter(string name, string unit, bool isMonotonic, string description, params Point[] points) : base(name, unit, description)
		{
			this.IsMonotonic = isMonotonic;
			this.Points = points;
		}
		#endregion

		#region 公共属性
		public bool IsMonotonic { get; }
		public Point[] Points { get; set; }
		#endregion

		#region 嵌套结构
		public readonly struct Point
		{
			public Point(object value, DateTimeOffset creation, DateTimeOffset timestamp, params IEnumerable<KeyValuePair<string, object>> tags)
			{
				this.Value = value;
				this.Creation = creation;
				this.Timestamp = timestamp;
				this.Tags = tags;
			}

			public readonly object Value;
			public readonly DateTimeOffset Creation;
			public readonly DateTimeOffset Timestamp;
			public readonly IEnumerable<KeyValuePair<string, object>> Tags;

			public override string ToString() => $"{this.Value}({this.Creation}~{this.Timestamp})";
		}
		#endregion
	}
}
