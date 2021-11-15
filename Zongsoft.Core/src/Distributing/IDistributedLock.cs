/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Distributing
{
	/// <summary>
	/// 表示分布式锁的接口。
	/// </summary>
	public interface IDistributedLock : IDisposable, IAsyncDisposable
	{
		/// <summary>获取分布式锁的标识。</summary>
		string Key { get; }

		/// <summary>获取分布式锁的令牌。</summary>
		byte[] Token { get; }

		/// <summary>获取分布式锁的过期时间。</summary>
		DateTime Expiry { get; }

		/// <summary>获取一个值，指示锁是否已过期。</summary>
		bool IsExpired { get; }
		/// <summary>获取一个值，指示当前是否处于锁定状态。</summary>
		bool IsLocked { get; }
		/// <summary>获取一个值，指示当前是否处于未锁定状态。</summary>
		bool IsUnlocked { get; }
	}
}
