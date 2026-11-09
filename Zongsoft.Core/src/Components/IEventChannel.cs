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

using Zongsoft.Communication;

namespace Zongsoft.Components;

/// <summary>可重复打开、关闭的事件传输桥；释放后不能再次打开。</summary>
public interface IEventChannel : IAsyncDisposable, ISender<EventContext>
{
	/// <summary>打开事件订阅；重复打开不创建重复订阅。</summary>
	/// <param name="receiver">接收远端事件的处理器。</param>
	/// <param name="cancellation">取消打开操作的令牌。</param>
	/// <returns>订阅初始化任务。</returns>
	ValueTask OpenAsync(IEventReceiver receiver, CancellationToken cancellation = default);

	/// <summary>关闭当前订阅，之后可以再次调用 <see cref="OpenAsync"/>。</summary>
	/// <param name="cancellation">取消等待进入关闭流程的令牌；清理开始后应完整执行。</param>
	/// <returns>当前订阅清理完成的任务。</returns>
	ValueTask CloseAsync(CancellationToken cancellation = default);
}
