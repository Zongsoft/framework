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
