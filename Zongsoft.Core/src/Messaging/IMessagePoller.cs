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
 * Copyright (C) 2010-2023 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Messaging;

/// <summary>表示消息队列轮询器的接口。</summary>
public interface IMessagePoller : IAsyncDisposable
{
	/// <summary>获取一个值，指示是否处于轮询中。</summary>
	bool IsPolling { get; }

	/// <summary>启动轮询。</summary>
	/// <param name="cancellation">取消令牌。</param>
	/// <returns>启动完成任务。</returns>
	ValueTask StartAsync(CancellationToken cancellation = default);

	/// <summary>停止轮询。</summary>
	/// <param name="cancellation">等待生命周期锁时使用的取消令牌；已开始的停止会等待处理完成。</param>
	/// <returns>轮询和在途处理全部结束的任务。</returns>
	ValueTask StopAsync(CancellationToken cancellation = default);
}