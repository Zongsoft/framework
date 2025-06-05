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

namespace Zongsoft.Components.Features;

public class RetryFeature : IFeature
{
	public RetryFeature(TimeSpan latency, int attempts = 0) : this(BackoffMode.None, latency, TimeSpan.Zero, attempts) { }
	public RetryFeature(TimeSpan latency, TimeSpan latencyLimit, int attempts = 0) : this(BackoffMode.None, latency, latencyLimit, attempts) { }
	public RetryFeature(BackoffMode mode, TimeSpan latency, int attempts = 0) : this(mode, latency, TimeSpan.Zero, attempts) { }
	public RetryFeature(BackoffMode mode, TimeSpan latency, TimeSpan latencyLimit, int attempts = 0)
	{
		this.Enabled = true;
		this.Jitterable = true;

		this.Mode = mode;
		this.Latency = latency;
		this.LatencyLimit = latencyLimit;
		this.Attempts = attempts;
	}

	public bool Enabled { get; set; }
	public bool Jitterable { get; set; }
	public int Attempts { get; set; }
	public BackoffMode Mode { get; set; }
	public TimeSpan Latency { get; set; }
	public TimeSpan LatencyLimit { get; set; }
	public Func<RetryFeature, int, TimeSpan, TimeSpan> Delay { get; set; }

	public enum BackoffMode
	{
		None,
		Linear,
		Exponential,
	}
}
