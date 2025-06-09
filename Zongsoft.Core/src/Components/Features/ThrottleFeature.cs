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
/// 提供限流(限速)功能的特性类。
/// </summary>
public class ThrottleFeature : IFeature
{
	#region 构造函数
	public ThrottleFeature(int permitLimit = 0, int queueLimit = 0, ThrottleLimiter limiter = null)
	{
		this.Enabled = true;
		this.PermitLimit = permitLimit <= 0 ? 1000 : permitLimit;
		this.QueueLimit = Math.Max(queueLimit, 0);
		this.Limiter = limiter;
	}
	#endregion

	#region 公共属性
	public bool Enabled { get; set; }
	public int PermitLimit { get; set; }
	public int QueueLimit { get; set; }
	public ThrottleLimiter Limiter { get; set; }
	#endregion
}

public class ThrottleLimiter
{
	public static TokenBucket Token(int threshold) => new(threshold);
	public static FixedWindown Fixed(TimeSpan window) => new(window);
	public static FixedWindown Fixed(int window) => new(TimeSpan.FromMilliseconds(window));
	public static SlidingWindown Sliding(TimeSpan window, int windowSize = 0) => new(window, windowSize);
	public static SlidingWindown Sliding(int window, int windowSize = 0) => new(TimeSpan.FromMilliseconds(window), windowSize);

	public class TokenBucket : ThrottleLimiter
	{
		public TokenBucket(int threshold) => this.Threshold = threshold;

		public int Threshold { get; set; }
	}

	public class FixedWindown : ThrottleLimiter
	{
		public FixedWindown(TimeSpan window) => this.Window = window;

		public TimeSpan Window { get; set; }
	}

	public class SlidingWindown : ThrottleLimiter
	{
		public SlidingWindown(TimeSpan window, int windowSize = 0)
		{
			this.Window = window;
			this.WindowSize = windowSize;
		}

		public TimeSpan Window { get; set; }
		public int WindowSize { get; set; }
	}
}