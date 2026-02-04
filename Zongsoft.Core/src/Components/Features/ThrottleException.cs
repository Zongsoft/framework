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

[Serializable]
public class ThrottleException : FeatureException
{
	#region 构造函数
	public ThrottleException(TimeSpan retryAfter) => this.RetryAfter = retryAfter;
	public ThrottleException(string message) : this(null, TimeSpan.MinValue, message, null) { }
	public ThrottleException(string message, Exception innerException) : this(null, TimeSpan.MinValue, message, innerException) { }
	public ThrottleException(TimeSpan retryAfter, string message) : this(null, retryAfter, message, null) { }
	public ThrottleException(TimeSpan retryAfter, string message, Exception innerException) : this(null, retryAfter, message, innerException) { }
	public ThrottleException(string identifier, TimeSpan retryAfter) : base(identifier) => this.RetryAfter = retryAfter;
	public ThrottleException(string identifier, TimeSpan retryAfter, string message) : this(identifier, retryAfter, message, null) { }
	public ThrottleException(string identifier, TimeSpan retryAfter, string message, Exception innerException) : base(identifier, message, innerException)
	{
		this.RetryAfter = retryAfter;
	}
	#endregion

	#region 公共属性
	public TimeSpan RetryAfter { get; }
	#endregion
}
