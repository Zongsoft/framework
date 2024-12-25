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
 * Copyright (C) 2020-2024 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Communication;

/// <summary>
/// 表示请求响应器的接口。
/// </summary>
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
