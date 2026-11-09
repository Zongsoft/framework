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

using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Components;

/// <summary>根据调用环境决定事件何时交给执行器。</summary>
/// <remarks>拥有后台工作的实现应同时实现 <see cref="IWorker"/>，停止完成时必须已经结束其所有工作。</remarks>
public interface IEventDispatcher
{
	/// <summary>立即执行、放弃或接收延期事件；延期接收完成不表示远端投递完成。</summary>
	/// <param name="argument">事件上下文。</param>
	/// <param name="executor">实际投递执行器。</param>
	/// <param name="cancellation">取消本次事件等待及投递。</param>
	/// <returns>立即执行的结果或延期接收结果。</returns>
	ValueTask DispatchAsync(EventContext argument, IExecutor<EventContext> executor, CancellationToken cancellation = default);
}
