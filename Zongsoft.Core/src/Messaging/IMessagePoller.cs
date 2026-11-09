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