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

namespace Zongsoft.Distributing;

/// <summary>
/// 表示分布式锁的接口。
/// </summary>
/// <example>
/// <code>
/// class MyService
/// {
/// 	[ServiceDependency("~")]
/// 	public IDistributedLockManager Locker { get; set; }
/// 
/// 	async Task FooAsync(MyModel model, CancellationToken cancellation)
/// 	{
/// 		using var locker = await this.Locker.AcquireAsync($"LOCKER:{nameof(MyService)}:{model.Key}", TimeSpan.FromSeconds(60), cancellation);
/// 		await locker.EnterAsync(cancellation);
/// 
/// 		//TODO: Some business code implementation.
/// 
/// 
/// 		//Optional: Manually release the distributed lock.
/// 		await locker.DisposeAsync();
/// 	}
/// }
/// </code>
/// </example>
public interface IDistributedLock : IDisposable, IAsyncDisposable
{
	/// <summary>获取分布式锁的标识。</summary>
	string Key { get; }
	/// <summary>获取分布式锁的令牌。</summary>
	byte[] Token { get; }

	/// <summary>获取一个值，指示锁是否已过期。</summary>
	bool IsExpired { get; }
	/// <summary>获取一个值，指示当前是否处于持有状态。</summary>
	bool IsHeld { get; }
	/// <summary>获取一个值，指示当前是否处于未持有状态。</summary>
	bool IsUnheld { get; }
	/// <summary>获取一个值，指示当前是否处于锁定（持有且未过期）状态。</summary>
	bool IsLocked { get; }
	/// <summary>获取一个值，指示当前是否处于未锁定（未持有或已过期）状态。</summary>
	bool IsUnlocked { get; }

	/// <summary>进入分布式锁的临界区。</summary>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回异步操作任务。</returns>
	ValueTask EnterAsync(CancellationToken cancellation = default);
}
