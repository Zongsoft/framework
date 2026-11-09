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
	public sealed class Histogram : Metric
	{
		#region 构造函数
		public Histogram(string name, string unit, params Point[] points) : this(name, unit, null, points) { }
		public Histogram(string name, string unit, string description, params Point[] points) : base(name, unit, description)
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
			public Point(ulong count, double value, double minimum, double maximum, DateTimeOffset startup, DateTimeOffset timestamp, params IEnumerable<KeyValuePair<string, object>> tags)
			{
				this.Count = count;
				this.Value = value;
				this.Minimum = minimum;
				this.Maximum = maximum;
				this.Startup = startup;
				this.Timestamp = timestamp;
				this.Tags = tags;
			}

			public readonly ulong Count;
			public readonly double Value;
			public readonly double Minimum;
			public readonly double Maximum;
			public readonly DateTimeOffset Startup;
			public readonly DateTimeOffset Timestamp;
			public readonly IEnumerable<KeyValuePair<string, object>> Tags;

			public override string ToString() => $"{this.Value}({this.Startup}~{this.Timestamp})";
		}
		#endregion
	}
}
