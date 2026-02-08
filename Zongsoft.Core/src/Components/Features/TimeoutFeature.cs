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
/// 提供超时功能的特性类。
/// </summary>
public abstract class TimeoutFeatureBase : IFeature
{
	#region 构造函数
	protected TimeoutFeatureBase() : this(TimeSpan.Zero) { }
	protected TimeoutFeatureBase(TimeSpan timeout) => this.Timeout = timeout > TimeSpan.Zero ? timeout : TimeSpan.FromSeconds(30);
	#endregion

	#region 公共属性
	/// <summary>获取或设置超时的时长（必须大于零才有效）。</summary>
	public TimeSpan Timeout { get; set; }
	#endregion
}

/// <summary>
/// 提供超时功能的特性类。
/// </summary>
public class TimeoutFeature : TimeoutFeatureBase
{
	#region 构造函数
	public TimeoutFeature(TimeSpan timeout) : base(timeout) { }
	public TimeoutFeature(Func<TimeoutArgument, CancellationToken, ValueTask<TimeSpan>> timeout)
	{
		this.TimeoutGenerator = timeout;
	}
	#endregion

	#region 公共属性
	/// <summary>获取或设置超时时长的生成方法。</summary>
	public Func<TimeoutArgument, CancellationToken, ValueTask<TimeSpan>> TimeoutGenerator { get; set; }
	/// <summary>获取或设置超时被触发的回调方法。</summary>
	public Func<TimeoutArgument, CancellationToken, ValueTask> OnTimeout { get; set; }
	#endregion
}

/// <summary>
/// 提供超时功能的特性类。
/// </summary>
public class TimeoutFeature<T> : TimeoutFeatureBase
{
	#region 构造函数
	public TimeoutFeature(TimeSpan timeout) : base(timeout) { }
	public TimeoutFeature(Func<TimeoutArgument<T>, CancellationToken, ValueTask<TimeSpan>> timeout)
	{
		this.TimeoutGenerator = timeout;
	}
	#endregion

	#region 公共属性
	/// <summary>获取或设置超时时长的生成方法。</summary>
	public Func<TimeoutArgument<T>, CancellationToken, ValueTask<TimeSpan>> TimeoutGenerator { get; set; }
	/// <summary>获取或设置超时被触发的回调方法。</summary>
	public Func<TimeoutArgument<T>, CancellationToken, ValueTask> OnTimeout { get; set; }
	#endregion
}

/// <summary>
/// 提供超时功能的特性类。
/// </summary>
public class TimeoutFeature<T, TResult> : TimeoutFeatureBase
{
	#region 构造函数
	public TimeoutFeature(TimeSpan timeout) : base(timeout) { }
	public TimeoutFeature(Func<TimeoutArgument<T, TResult>, CancellationToken, ValueTask<TimeSpan>> timeout)
	{
		this.TimeoutGenerator = timeout;
	}
	#endregion

	#region 公共属性
	/// <summary>获取或设置超时时长的生成方法。</summary>
	public Func<TimeoutArgument<T, TResult>, CancellationToken, ValueTask<TimeSpan>> TimeoutGenerator { get; set; }
	/// <summary>获取或设置超时被触发的回调方法。</summary>
	public Func<TimeoutArgument<T, TResult>, CancellationToken, ValueTask> OnTimeout { get; set; }
	#endregion
}

public class TimeoutArgument : Argument
{
	#region 构造函数
	public TimeoutArgument(TimeSpan timeout, Exception exception = null) : base(exception)
	{
		this.Timeout = timeout;
	}
	#endregion

	#region 公共属性
	public TimeSpan Timeout { get; }
	#endregion
}

public class TimeoutArgument<T> : Argument<T>
{
	#region 构造函数
	public TimeoutArgument(TimeSpan timeout, Exception exception = null) : base(exception)
	{
		this.Timeout = timeout;
	}
	public TimeoutArgument(TimeSpan timeout, T value, Exception exception = null) : base(value, exception)
	{
		this.Timeout = timeout;
	}
	#endregion

	#region 公共属性
	public TimeSpan Timeout { get; }
	#endregion
}

public class TimeoutArgument<T, TResult> : Argument<T, TResult>
{
	#region 构造函数
	public TimeoutArgument(TimeSpan timeout, Exception exception = null) : base(exception)
	{
		this.Timeout = timeout;
	}
	public TimeoutArgument(TimeSpan timeout, T value, Exception exception = null) : base(value, exception)
	{
		this.Timeout = timeout;
	}
	public TimeoutArgument(TimeSpan timeout, T value, TResult result, Exception exception = null) : base(value, result, exception)
	{
		this.Timeout = timeout;
	}
	#endregion

	#region 公共属性
	public TimeSpan Timeout { get; }
	#endregion
}
