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
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Components;
using Zongsoft.Communication;

namespace Zongsoft.Caching;

/// <summary>表示分布式缓存通知的订阅。</summary>
/// <remarks>
/// 	<para><see cref="UnsubscribeAsync"/>、<see cref="IChannel.CloseAsync"/>和<see cref="IAsyncDisposable.DisposeAsync"/>共享同一个幂等退订流程。</para>
/// 	<para>关闭后不再启动新的处理程序调用；已经开始的调用将收到订阅生命周期的取消标记。</para>
/// </remarks>
public interface IDistributedCacheSubscription : IChannel
{
	#region 属性定义
	/// <summary>获取所属的分布式缓存。</summary>
	IDistributedCache Cache { get; }
	/// <summary>获取订阅选项的快照。</summary>
	DistributedCacheSubscriptionOptions Options { get; }
	/// <summary>获取缓存通知处理程序。</summary>
	IHandler<DistributedCacheNotification> Handler { get; }
	#endregion

	#region 订阅方法
	/// <summary>取消当前通知订阅。</summary>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回表示异步操作的任务对象。</returns>
	ValueTask UnsubscribeAsync(CancellationToken cancellation = default);
	#endregion
}
