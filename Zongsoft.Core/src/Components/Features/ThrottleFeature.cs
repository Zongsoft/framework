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
using System.Collections.Generic;

namespace Zongsoft.Components.Features;

/// <summary>
/// 提供限流(限速)功能的特性类。
/// </summary>
public class ThrottleFeature : IFeature
{
	#region 构造函数
	public ThrottleFeature(int permitLimit, int queueLimit, ThrottleLimiter limiter = null, IHandler<ThrottleArgument> rejected = null) : this(permitLimit, queueLimit, ThrottleQueueOrder.Oldest, limiter, rejected) { }
	public ThrottleFeature(int permitLimit, int queueLimit, ThrottleQueueOrder queueOrder, ThrottleLimiter limiter = null, IHandler<ThrottleArgument> rejected = null)
	{
		this.Enabled = true;
		this.PermitLimit = permitLimit > 0 ? permitLimit : 1000;
		this.QueueLimit = Math.Max(queueLimit, 0);
		this.QueueOrder = queueOrder;
		this.Limiter = limiter;
		this.Rejected = rejected;
	}
	#endregion

	#region 公共属性
	public bool Enabled { get; set; }
	public int PermitLimit { get; set; }
	public int QueueLimit { get; set; }
	public ThrottleQueueOrder QueueOrder { get; set; }
	public ThrottleLimiter Limiter { get; set; }
	public IHandler<ThrottleArgument> Rejected { get; set; }
	#endregion
}

public enum ThrottleQueueOrder
{
	Oldest,
	Newest,
}

public class ThrottleArgument : Argument<ThrottleLease>
{
	#region 构造函数
	public ThrottleArgument(string name, ThrottleLease lease) : base(lease) => this.Name = name;
	#endregion

	#region 公共属性
	public string Name { get; }
	#endregion

	#region 重写方法
	public override string ToString() => this.Name;
	#endregion
}

public class ThrottleLease : IDisposable
{
	protected ThrottleLease() { }

	public virtual bool IsLeased { get; }
	public virtual IMetadataCollection Metadata { get; }

	public void Dispose()
	{
		this.Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing) { }

	public interface IMetadataCollection : IReadOnlyCollection<KeyValuePair<string, object>>
	{
		bool TryGetValue<T>(string key, out T value);
		bool TryGetValue(string key, out object value);
	}
}

public class ThrottleLimiter
{
	public static TokenBucket Token(int value, TimeSpan period) => new(value, period);
	public static FixedWindown Fixed(TimeSpan window) => new(window);
	public static FixedWindown Fixed(int window) => new(TimeSpan.FromMilliseconds(window));
	public static SlidingWindown Sliding(TimeSpan window, int windowSegments = 0) => new(window, windowSegments);
	public static SlidingWindown Sliding(int window, int windowSegments = 0) => new(TimeSpan.FromMilliseconds(window), windowSegments);

	/// <summary>表示令牌桶限制器。</summary>
	public class TokenBucket : ThrottleLimiter
	{
		public TokenBucket(int value, TimeSpan period)
		{
			this.Value = value;
			this.Period = period;
		}

		/// <summary>获取或设置每个周期补充的令牌数。</summary>
		public int Value { get; set; }
		/// <summary>获取或设置令牌补充周期。</summary>
		public TimeSpan Period { get; set; }

		public override string ToString() => $"{nameof(TokenBucket)}({this.Value}@{this.Period})";
	}

	/// <summary>表示固定窗口限制器。</summary>
	public class FixedWindown : ThrottleLimiter
	{
		public FixedWindown(TimeSpan window) => this.Window = window;

		/// <summary>获取或设置窗口时长。</summary>
		public TimeSpan Window { get; set; }

		public override string ToString() => $"{nameof(FixedWindown)}({this.Window})";
	}

	/// <summary>表示滑动窗口限制器。</summary>
	public class SlidingWindown : ThrottleLimiter
	{
		public SlidingWindown(TimeSpan window, int windowSegments = 0)
		{
			this.Window = window;
			this.WindowSegments = windowSegments;
		}

		/// <summary>获取或设置窗口分段数。</summary>
		public int WindowSegments { get; set; }
		/// <summary>获取或设置窗口时长。</summary>
		public TimeSpan Window { get; set; }

		public override string ToString() => $"{nameof(SlidingWindown)}({this.WindowSegments}@{this.Window})";
	}
}