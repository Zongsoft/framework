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
using System.Collections.Generic;

namespace Zongsoft.Communication;

/// <summary>
/// 表示请求令牌的接口。
/// </summary>
public interface IRequestToken : IDisposable
{
	/// <summary>获取关联的请求对象。</summary>
	IRequest Request { get; }

	/// <summary>获取当前请求对应的响应集。</summary>
	/// <param name="cancellation">指定的响应操作取消标记。</param>
	/// <returns>返回的响应集，如果当前请求尚未响应则返回的响应集为空集。</returns>
	IEnumerable<IResponse> GetResponses(CancellationToken cancellation = default);

	/// <summary>获取当前请求对应的响应集，注意：遍历返回的响应集会导致调用线程被堵塞<paramref name="timeout"/>参数指定的时长。</summary>
	/// <param name="timeout">指定的响应等待时长。</param>
	/// <param name="cancellation">指定的响应操作取消标记。</param>
	/// <returns>返回的响应集，如果当前请求在<paramref name="timeout"/>指定的时长内尚未响应则返回的响应集为空集。</returns>
	IEnumerable<IResponse> GetResponses(TimeSpan timeout, CancellationToken cancellation = default);
}
