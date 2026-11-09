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
using System.Collections.Generic;

namespace Zongsoft.Diagnostics.Telemetry.Metrics;

partial class Metric
{
	public sealed class Summary : Metric
	{
		#region 构造函数
		public Summary(string name, string unit, params Point[] points) : this(name, unit, null, points) { }
		public Summary(string name, string unit, string description, params Point[] points) : base(name, unit, description)
		{
			this.Points = points;
		}
		#endregion

		#region 公共属性
		public Point[] Points { get; set; }
		#endregion

		#region 嵌套结构
		public readonly struct Point
		{
			public Point(double value, ulong count, DateTimeOffset startup, DateTimeOffset timestamp, QuantileValue[] quantiles, params IEnumerable<KeyValuePair<string, object>> tags) : this(value, count, 0, startup, timestamp, quantiles, tags) { }
			public Point(double value, ulong count, uint flags, DateTimeOffset startup, DateTimeOffset timestamp, QuantileValue[] quantiles, params IEnumerable<KeyValuePair<string, object>> tags)
			{
				this.Value = value;
				this.Count = count;
				this.Flags = flags;
				this.Startup = startup;
				this.Timestamp = timestamp;
				this.Quantiles = quantiles;
				this.Tags = tags;
			}

			public readonly ulong Count;
			public readonly double Value;
			public readonly uint Flags;
			public readonly DateTimeOffset Startup;
			public readonly DateTimeOffset Timestamp;
			public readonly QuantileValue[] Quantiles;
			public readonly IEnumerable<KeyValuePair<string, object>> Tags;

			public override string ToString() => $"{this.Value}({this.Startup}~{this.Timestamp})";

			public readonly struct QuantileValue(double value, double quantile)
			{
				public readonly double Value = value;
				public readonly double Quantile = quantile;
				public override string ToString() => $"{this.Value}:{this.Quantile}";
			}
		}
		#endregion
	}
}
