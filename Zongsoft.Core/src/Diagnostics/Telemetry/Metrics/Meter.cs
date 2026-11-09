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

public class Meter
{
	public Meter(string name, string version, params IEnumerable<Metric> metrics)
	{
		this.Name = name;
		this.Version = version;
		this.Metrics = [.. metrics];
	}

	public string Name { get; }
	public string Version { get; }
	public ICollection<Metric> Metrics { get; }
	public IEnumerable<KeyValuePair<string, object>> Tags { get; set; }

	public override string ToString() => string.IsNullOrEmpty(this.Version) ? this.Name : $"{this.Name} ({this.Version})";
}
