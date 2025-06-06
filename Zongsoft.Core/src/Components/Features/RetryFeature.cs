﻿/*
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
/// 提供重试功能的特性类。
/// </summary>
public class RetryFeature : IFeature
{
	#region 构造函数
	public RetryFeature(RetryLatency latency, int attempts = 0) : this(BackoffMode.None, latency, attempts) { }
	public RetryFeature(BackoffMode mode, RetryLatency latency, int attempts = 0)
	{
		this.Enabled = true;
		this.Jitterable = true;
		this.Mode = mode;
		this.Latency = latency;
		this.Attempts = attempts;
	}
	#endregion

	#region 公共属性
	public bool Enabled { get; set; }
	/// <summary>获取或设置一个值，指示是否叠加随机延迟量。</summary>
	public bool Jitterable { get; set; }
	/// <summary>获取或设置最大重试次数。</summary>
	public int Attempts { get; set; }
	/// <summary>获取或设置延迟补偿方式。</summary>
	public BackoffMode Mode { get; set; }
	/// <summary>获取或设置重试延迟时长。</summary>
	public RetryLatency Latency { get; set; }
	#endregion

	#region 嵌套枚举
	/// <summary>表示延迟补偿(回退)方式的枚举。</summary>
	public enum BackoffMode
	{
		/// <summary>默认，即指定的固定延迟。</summary>
		None,
		/// <summary>线性递增。</summary>
		Linear,
		/// <summary>指数增长。</summary>
		Exponential,
	}
	#endregion
}

/// <summary>
/// 表示重试延迟的结构。
/// </summary>
public struct RetryLatency
{
	#region 构造函数
	public RetryLatency(TimeSpan value, TimeSpan limit, Func<RetryFeature, int, TimeSpan> generator = null)
	{
		this.Value = value;
		this.Limit = limit;
		this.Generator = generator;
	}
	#endregion

	#region 公共属性
	/// <summary>获取一个值，指示该结构是否有值。</summary>
	public readonly bool HasValue => this.Value > TimeSpan.Zero || this.Generator != null;
	/// <summary>获取一个值，指示该结构是否为空。</summary>
	public readonly bool IsEmpty => this.Value <= TimeSpan.Zero && this.Generator == null;
	#endregion

	#region 公共字段
	/// <summary>获取或设置延迟时长。</summary>
	public TimeSpan Value { get; set; }

	/// <summary>获取或设置延迟时长限制，即最大延迟时长。</summary>
	public TimeSpan Limit { get; set; }

	/// <summary>获取或设置重试延迟时长的生成器。</summary>
	public Func<RetryFeature, int, TimeSpan> Generator { get; set; }
	#endregion

	#region 符号重写
	public static implicit operator RetryLatency(TimeSpan value) => new(value, TimeSpan.Zero);
	#endregion
}
