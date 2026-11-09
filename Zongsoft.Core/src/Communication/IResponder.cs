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

/// <summary>表示请求响应器的接口。</summary>
public interface IResponder
{
	/// <summary>接收请求的方法。</summary>
	/// <param name="request">接收到的请求对象。</param>
	/// <param name="cancellation">请求的异步操作取消标记。</param>
	/// <returns>返回的接收异步任务。</returns>
	ValueTask OnRequested(IRequest request, CancellationToken cancellation);

	/// <summary>响应方法。</summary>
	/// <param name="response">指定的响应对象。</param>
	/// <param name="cancellation">响应的异步操作取消标记。</param>
	/// <returns>返回的响应异步任务。</returns>
	ValueTask RespondAsync(IResponse response, CancellationToken cancellation = default);
}
