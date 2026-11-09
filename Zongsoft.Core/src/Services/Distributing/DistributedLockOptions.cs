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

namespace Zongsoft.Services.Distributing;

/// <summary>表示分布式锁的获取和续期选项。</summary>
public sealed class DistributedLockOptions
{
	public DistributedLockOptions(TimeSpan expiry)
	{
		if(expiry <= TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(expiry));

		this.Expiry = expiry;
	}

	/// <summary>获取锁的有效时长。</summary>
	public TimeSpan Expiry { get; }
	/// <summary>获取或设置自动续期的间隔；空值或零表示不自动续期。</summary>
	public TimeSpan? RenewalInterval { get; set; }
	/// <summary>获取是否启用了自动续期。</summary>
	public bool AutoRenewal => this.RenewalInterval.HasValue && this.RenewalInterval.Value > TimeSpan.Zero;
}
