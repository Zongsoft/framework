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

namespace Zongsoft.Caching;

public class CacheEvictedEventArgs
{
	public CacheEvictedEventArgs(object key, object value, CacheEvictedReason reason, object state = null)
	{
		this.Key = key;
		this.Value = value;
		this.Reason = reason;
		this.State = state;
	}

	public object Key { get; }
	public object Value { get; }
	public CacheEvictedReason Reason { get; }
	public object State { get; set; }

	public override string ToString() => $"[{this.Reason}] {this.Key}={this.Value}";
}
