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

namespace Zongsoft.Communication;

/// <summary>提供关于通讯侦听的功能的接口。</summary>
public interface IListener<T> : Components.IWorker, IAsyncDisposable
{
	/// <summary>获取当前是否处于侦听状态。</summary>
	bool IsListening { get; }

	/// <summary>处理请求。</summary>
	/// <param name="package">处理的请求包。</param>
	/// <param name="cancellation">指定的异步取消标记。</param>
	/// <returns>表示请求包处理操作的异步任务。</returns>
	ValueTask HandleAsync(T package, CancellationToken cancellation = default);
}
