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
/// 提供回退(后备)功能的特性类。
/// </summary>
public class FallbackFeature : IFeature
{
	#region 构造函数
	public FallbackFeature(Func<Argument, CancellationToken, ValueTask> fallback) : this(fallback, null) { }
	public FallbackFeature(Func<Argument, CancellationToken, ValueTask> fallback, Common.IPredication<Argument> predicator)
	{
		this.Fallback = fallback ?? throw new ArgumentNullException(nameof(fallback));
		this.Predicator = predicator;
	}
	#endregion

	#region 公共属性
	/// <summary>获取或设置回退断言器。</summary>
	public Common.IPredication<Argument> Predicator { get; set; }
	/// <summary>获取或设置回退处理函数。</summary>
	public Func<Argument, CancellationToken, ValueTask> Fallback { get; set; }
	#endregion
}

/// <summary>
/// 提供回退(后备)功能的特性类。
/// </summary>
public class FallbackFeature<T> : IFeature
{
	#region 构造函数
	public FallbackFeature(Func<Argument<T>, CancellationToken, ValueTask> fallback) : this(fallback, null) { }
	public FallbackFeature(Func<Argument<T>, CancellationToken, ValueTask> fallback, Common.IPredication<Argument<T>> predicator)
	{
		this.Fallback = fallback ?? throw new ArgumentNullException(nameof(fallback));
		this.Predicator = predicator;
	}
	#endregion

	#region 公共属性
	/// <summary>获取或设置回退断言器。</summary>
	public Common.IPredication<Argument<T>> Predicator { get; set; }
	/// <summary>获取或设置回退处理函数。</summary>
	public Func<Argument<T>, CancellationToken, ValueTask> Fallback { get; set; }
	#endregion
}

/// <summary>
/// 提供回退(后备)功能的特性类。
/// </summary>
public class FallbackFeature<T, TResult> : IFeature
{
	#region 构造函数
	public FallbackFeature(Func<Argument<T, TResult>, CancellationToken, ValueTask<TResult>> fallback) : this(fallback, null) { }
	public FallbackFeature(Func<Argument<T, TResult>, CancellationToken, ValueTask<TResult>> fallback, Common.IPredication<Argument<T, TResult>> predicator)
	{
		this.Fallback = fallback ?? throw new ArgumentNullException(nameof(fallback));
		this.Predicator = predicator;
	}
	#endregion

	#region 公共属性
	/// <summary>获取或设置回退断言器。</summary>
	public Common.IPredication<Argument<T, TResult>> Predicator { get; set; }
	/// <summary>获取或设置回退处理函数。</summary>
	public Func<Argument<T, TResult>, CancellationToken, ValueTask<TResult>> Fallback { get; set; }
	#endregion
}
