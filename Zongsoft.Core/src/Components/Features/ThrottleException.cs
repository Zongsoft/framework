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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
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
