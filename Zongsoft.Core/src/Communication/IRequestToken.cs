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
using System.Collections.Generic;

namespace Zongsoft.Communication;

/// <summary>表示请求令牌的接口。</summary>
public interface IRequestToken : IDisposable
{
	/// <summary>获取关联的请求对象。</summary>
	IRequest Request { get; }

	/// <summary>获取当前请求对应的响应集。</summary>
	/// <param name="cancellation">指定的响应操作取消标记。</param>
	/// <returns>返回的响应集，如果当前请求尚未响应则返回的响应集为空集。</returns>
	IAsyncEnumerable<IResponse> GetResponsesAsync(CancellationToken cancellation = default);

	/// <summary>获取当前请求对应的响应集，遍历时异步等待应答，最多等待<paramref name="timeout"/>指定的时长。</summary>
	/// <param name="timeout">指定的响应等待时长。</param>
	/// <param name="cancellation">指定的响应操作取消标记。</param>
	/// <returns>返回的响应集，如果当前请求在<paramref name="timeout"/>指定的时长内尚未响应则返回的响应集为空集。</returns>
	IAsyncEnumerable<IResponse> GetResponsesAsync(TimeSpan timeout, CancellationToken cancellation = default);
}
