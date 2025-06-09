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
public class FallbackFeature<TResult> : IFeature
{
	public FallbackFeature(Func<Argument<TResult>, CancellationToken, ValueTask<TResult>> fallback, bool enabled = true) : this(fallback, null, enabled) { }
	public FallbackFeature(Func<Argument<TResult>, CancellationToken, ValueTask<TResult>> fallback, Common.IPredication<Argument<TResult>> predicator, bool enabled = true)
	{
		this.Enabled = enabled;
		this.Fallback = fallback ?? throw new ArgumentNullException(nameof(fallback));
		this.Predicator = predicator;
	}

	public bool Enabled { get; set; }
	/// <summary>获取或设置回退断言器。</summary>
	public Common.IPredication<Argument<TResult>> Predicator { get; set; }
	/// <summary>获取或设置回退处理函数。</summary>
	public Func<Argument<TResult>, CancellationToken, ValueTask<TResult>> Fallback { get; set; }
}
