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

public partial class Metric
{
	public Metric(string name, string unit, string description = null)
	{
		this.Name = name;
		this.Unit = unit;
		this.Description = description;
	}

	public string Name { get; }
	public string Unit { get; }
	public string Description { get; set; }
	public IEnumerable<KeyValuePair<string, object>> Tags { get; set; }

	public override string ToString() => string.IsNullOrEmpty(this.Unit) ? this.Name : $"{this.Name}/{this.Unit}";
}
