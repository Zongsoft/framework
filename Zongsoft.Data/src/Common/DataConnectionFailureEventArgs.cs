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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Data library.
 *
 * The Zongsoft.Data is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

namespace Zongsoft.Data.Common;

/// <summary>表示数据连接失败的事件参数。</summary>
public sealed class DataConnectionFailureEventArgs : Zongsoft.Diagnostics.FailureEventArgs
{
	#region 构造函数
	internal DataConnectionFailureEventArgs(
		IDataSource source,
		Exception exception,
		int failureCount,
		DateTimeOffset? retryAt,
		TimeSpan retryAfter) : base(exception)
	{
		this.Source = source ?? throw new ArgumentNullException(nameof(source));
		this.FailureCount = Math.Max(failureCount, 1);
		this.RetryAt = retryAt;
		this.RetryAfter = retryAfter > TimeSpan.Zero ? retryAfter : TimeSpan.Zero;
	}
	#endregion

	#region 公共属性
	/// <summary>获取连接失败的数据源。</summary>
	public IDataSource Source { get; }

	/// <summary>获取连续物理连接失败的次数。</summary>
	public int FailureCount { get; }

	/// <summary>获取连接器恢复物理连接尝试的时间；为空表示尚未熔断。</summary>
	public DateTimeOffset? RetryAt { get; }

	/// <summary>获取距离恢复物理连接尝试的剩余时长。</summary>
	public TimeSpan RetryAfter { get; }

	/// <summary>获取连接器是否已经暂停新的物理连接尝试。</summary>
	public bool IsSuspended => this.RetryAt.HasValue;
	#endregion
}
