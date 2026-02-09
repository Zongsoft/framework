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
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Components.Features;

/// <summary>
/// 提供熔断功能的特性类。
/// </summary>
public abstract class BreakerFeatureBase : IFeature
{
	#region 构造函数
	protected BreakerFeatureBase(TimeSpan duration, double ratio, TimeSpan period, int threshold = 0)
	{
		this.Duration = duration > TimeSpan.Zero ? duration : TimeSpan.FromSeconds(5);
		this.FailureRatio = ratio <= 0 ? 0.1 : Math.Clamp(ratio, 0, 1);
		this.FailurePeriod = period > TimeSpan.Zero ? period : TimeSpan.FromSeconds(30);
		this.Threshold = threshold <= 0 ? 100 : Math.Max(threshold, 2);
	}
	#endregion

	#region 公共属性
	/// <summary>获取或设置熔断的时长，默认值为 <c>5</c> 秒。</summary>
	public TimeSpan Duration { get; set; }
	/// <summary>获取或设置熔断的失败率，范围介于 <c>0</c> 至 <c>1</c> 之间，默认值为 <c>0.1</c>（即<c>10%</c>）。</summary>
	public double FailureRatio { get; set; }
	/// <summary>获取或设置评定失败率的采样时长，默认值为 <c>30</c> 秒。</summary>
	public TimeSpan FailurePeriod { get; set; }
	/// <summary>获取或设置熔断器的阈值(最小流量)，必须大于 <c>1</c> 才有效，默认值为 <c>100</c>。</summary>
	public int Threshold { get; set; }
	#endregion
}

/// <summary>
/// 提供熔断功能的特性类。
/// </summary>
public class BreakerFeature : BreakerFeatureBase
{
	#region 构造函数
	public BreakerFeature(TimeSpan duration, double ratio, TimeSpan period, int threshold = 0, Common.IPredication<Argument> predicator = null) : base(duration, ratio, period, threshold)
	{
		this.Predicator = predicator;
	}
	public BreakerFeature(Func<BreakerFeature, int, double, TimeSpan> durationFactory, double ratio, TimeSpan period, int threshold = 0, Common.IPredication<Argument> predicator = null) : base(TimeSpan.Zero, ratio, period, threshold)
	{
		this.DurationFactory = durationFactory;
		this.Predicator = predicator;
	}
	#endregion

	#region 公共属性
	/// <summary>获取或设置熔断时长的生成器。</summary>
	public Func<BreakerFeature, int, double, TimeSpan> DurationFactory { get; set; }
	/// <summary>获取或设置熔断断言器。</summary>
	public Common.IPredication<Argument> Predicator { get; set; }
	/// <summary>获取或设置熔断器被关闭时的回调函数。</summary>
	public Func<BreakerClosedArgument, CancellationToken, ValueTask> Closed { get; set; }
	/// <summary>获取或设置熔断器被打开时的回调函数。</summary>
	public Func<BreakerOpenedArgument, CancellationToken, ValueTask> Opened { get; set; }
	#endregion
}

/// <summary>
/// 提供熔断功能的特性类。
/// </summary>
public class BreakerFeature<T> : BreakerFeatureBase
{
	#region 构造函数
	public BreakerFeature(TimeSpan duration, double ratio, TimeSpan period, int threshold = 0, Common.IPredication<Argument<T>> predicator = null) : base(duration, ratio, period, threshold)
	{
		this.Predicator = predicator;
	}
	public BreakerFeature(Func<BreakerFeature<T>, int, double, TimeSpan> durationFactory, double ratio, TimeSpan period, int threshold = 0, Common.IPredication<Argument<T>> predicator = null) : base(TimeSpan.Zero, ratio, period, threshold)
	{
		this.DurationFactory = durationFactory;
		this.Predicator = predicator;
	}
	#endregion

	#region 公共属性
	/// <summary>获取或设置熔断时长的生成器。</summary>
	public Func<BreakerFeature<T>, int, double, TimeSpan> DurationFactory { get; set; }
	/// <summary>获取或设置熔断断言器。</summary>
	public Common.IPredication<Argument<T>> Predicator { get; set; }
	/// <summary>获取或设置熔断器被关闭时的回调函数。</summary>
	public Func<BreakerClosedArgument<T>, CancellationToken, ValueTask> Closed { get; set; }
	/// <summary>获取或设置熔断器被打开时的回调函数。</summary>
	public Func<BreakerOpenedArgument<T>, CancellationToken, ValueTask> Opened { get; set; }
	#endregion
}

/// <summary>
/// 提供熔断功能的特性类。
/// </summary>
public class BreakerFeature<T, TResult> : BreakerFeatureBase
{
	#region 构造函数
	public BreakerFeature(TimeSpan duration, double ratio, TimeSpan period, int threshold = 0, Common.IPredication<Argument<T, TResult>> predicator = null) : base(duration, ratio, period, threshold)
	{
		this.Predicator = predicator;
	}
	public BreakerFeature(Func<BreakerFeature<T, TResult>, int, double, TimeSpan> durationFactory, double ratio, TimeSpan period, int threshold = 0, Common.IPredication<Argument<T, TResult>> predicator = null) : base(TimeSpan.Zero, ratio, period, threshold)
	{
		this.DurationFactory = durationFactory;
		this.Predicator = predicator;
	}
	#endregion

	#region 公共属性
	/// <summary>获取或设置熔断时长的生成器。</summary>
	public Func<BreakerFeature<T, TResult>, int, double, TimeSpan> DurationFactory { get; set; }
	/// <summary>获取或设置熔断断言器。</summary>
	public Common.IPredication<Argument<T, TResult>> Predicator { get; set; }
	/// <summary>获取或设置熔断器被关闭时的回调函数。</summary>
	public Func<BreakerClosedArgument<T, TResult>, CancellationToken, ValueTask> Closed { get; set; }
	/// <summary>获取或设置熔断器被打开时的回调函数。</summary>
	public Func<BreakerOpenedArgument<T, TResult>, CancellationToken, ValueTask> Opened { get; set; }
	#endregion
}

public class BreakerClosedArgument(Exception exception = null) : Argument(exception)
{
}

public class BreakerClosedArgument<T> : Argument<T>
{
	#region 构造函数
	public BreakerClosedArgument(Exception exception = null) : base(exception) { }
	public BreakerClosedArgument(T value, Exception exception = null) : base(value, exception) { }
	#endregion
}

public class BreakerClosedArgument<T, TResult> : Argument<T, TResult>
{
	#region 构造函数
	public BreakerClosedArgument(Exception exception = null) : base(exception) { }
	public BreakerClosedArgument(T value, Exception exception = null) : base(value, exception) { }
	public BreakerClosedArgument(T value, TResult result, Exception exception = null) : base(value, result, exception) { }
	#endregion
}

public class BreakerOpenedArgument(bool isHalf, Exception exception = null) : Argument(exception)
{
	#region 公共属性
	public bool IsHalf { get; } = isHalf;
	#endregion
}

public class BreakerOpenedArgument<T> : Argument<T>
{
	#region 构造函数
	public BreakerOpenedArgument(bool isHalf, Exception exception = null) : base(exception) => this.IsHalf = isHalf;
	public BreakerOpenedArgument(bool isHalf, T value, Exception exception = null) : base(value, exception) => this.IsHalf = isHalf;
	#endregion

	#region 公共属性
	public bool IsHalf { get; }
	#endregion
}

public class BreakerOpenedArgument<T, TResult> : Argument<T, TResult>
{
	#region 构造函数
	public BreakerOpenedArgument(bool isHalf, Exception exception = null) : base(exception) => this.IsHalf = isHalf;
	public BreakerOpenedArgument(bool isHalf, T value, Exception exception = null) : base(value, exception) => this.IsHalf = isHalf;
	public BreakerOpenedArgument(bool isHalf, T value, TResult result, Exception exception = null) : base(value, result, exception) => this.IsHalf = isHalf;
	#endregion

	#region 公共属性
	public bool IsHalf { get; }
	#endregion
}
