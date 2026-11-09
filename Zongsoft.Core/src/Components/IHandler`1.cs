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

namespace Zongsoft.Components;

/// <summary>表示处理程序的接口。</summary>
/// <typeparam name="TArgument">处理程序的请求参数类型。</typeparam>
public interface IHandler<in TArgument> : IHandler
{
	/// <summary>异步处理执行请求。</summary>
	/// <param name="argument">当前处理的请求对象。</param>
	/// <param name="cancellation">指定的异步取消标记。</param>
	/// <returns>返回的异步任务。</returns>
	ValueTask HandleAsync(TArgument argument, CancellationToken cancellation = default);

	/// <summary>异步处理执行请求。</summary>
	/// <param name="argument">当前处理的请求对象。</param>
	/// <param name="parameters">当前处理的参数集。</param>
	/// <param name="cancellation">指定的异步取消标记。</param>
	/// <returns>返回的异步任务。</returns>
	ValueTask HandleAsync(TArgument argument, Collections.Parameters parameters, CancellationToken cancellation = default);
}
