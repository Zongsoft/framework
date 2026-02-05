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
 * This file is part of Zongsoft.Externals.Polly library.
 *
 * The Zongsoft.Externals.Polly is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Polly is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Polly library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading.Tasks;
using System.Threading.RateLimiting;

using Polly;
using Polly.RateLimiting;

namespace Zongsoft.Externals.Polly.Strategies;

internal sealed class ThrottleStrategyOptions : ResilienceStrategyOptions
{
	#region 常量定义
	internal const int QUEUE_LIMIT = 0;
	internal const int PERMIT_LIMIT = 1000;
	#endregion

	#region 构造函数
	public ThrottleStrategyOptions()
	{
		this.Name = "RateLimiter";
		this.DefaultRateLimiterOptions = new()
		{
			QueueLimit = QUEUE_LIMIT,
			PermitLimit = PERMIT_LIMIT,
		};
	}
	#endregion

	#region 公共属性
	public ConcurrencyLimiterOptions DefaultRateLimiterOptions { get; set; }
	public Func<RateLimiterArguments, ValueTask<RateLimitLease>> RateLimiter { get; set; }
	public Func<OnRateLimiterRejectedArguments, ValueTask<bool>> OnRejected { get; set; }
	#endregion
}
