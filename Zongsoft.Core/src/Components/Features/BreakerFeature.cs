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

/// <summary>
/// 提供熔断功能的特性类。
/// </summary>
public class BreakerFeature : IFeature
{
	#region 构造函数
	public BreakerFeature(bool enabled = true) : this(TimeSpan.Zero, 0, TimeSpan.Zero) => this.Enabled = enabled;
	public BreakerFeature(TimeSpan duration, int threshold = 100) : this(duration, 0.1, TimeSpan.Zero, threshold) { }
	public BreakerFeature(TimeSpan duration, double ratio, int threshold = 100) : this(duration, ratio, TimeSpan.Zero, threshold) { }
	public BreakerFeature(TimeSpan duration, double ratio, TimeSpan period, int threshold = 100)
	{
		this.Enabled = true;
		this.Duration = duration > TimeSpan.Zero ? duration : TimeSpan.FromSeconds(5);
		this.FailureRatio = ratio <= 0 ? 0.1 : Math.Clamp(ratio, 0, 1);
		this.FailurePeriod = period > TimeSpan.Zero ? period : TimeSpan.FromSeconds(30);
		this.Threshold = Math.Max(threshold, 1);
	}
	#endregion

	#region 公共属性
	public bool Enabled { get; set; }
	/// <summary>获取或设置熔断的时长，默认值为 <c>5</c> 秒。</summary>
	public TimeSpan Duration { get; set; }
	/// <summary>获取或设置熔断时长的生成器。</summary>
	public Func<BreakerFeature, int, double, TimeSpan> DurationFactory { get; set; }
	/// <summary>获取或设置熔断的失败率，范围介于 <c>0</c> 至 <c>1</c> 之间，默认值为 <c>0.1</c>（即<c>10%</c>）。</summary>
	public double FailureRatio { get; set; }
	/// <summary>获取或设置评定失败率的采样时长。</summary>
	public TimeSpan FailurePeriod { get; set; }
	/// <summary>获取或设置熔断器的阈值(最小流量)，必须大于 <c>1</c> 才有效，默认值为 <c>100</c>。</summary>
	public int Threshold { get; set; }
	#endregion
}
