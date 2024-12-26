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
/// 表示请求器的接口。
/// </summary>
public interface IRequester
{
	/// <summary>发送一个请求。</summary>
	/// <param name="url">发送的请求地址。</param>
	/// <param name="data">发送的请求数据。</param>
	/// <param name="cancellation">请求的异步操作取消标记。</param>
	/// <returns>返回请求令牌，如果返回值为空(<c>null</c>)则表示请求失败。</returns>
	/// <remarks>注意：一个请求可能会对应多次响应，可以通过响应<see cref="IResponse" />对象的关联的<see cref="IResponse.Request" />请求对象的<see cref="IRequest.Identifier" />属性获取其关联性。</remarks>
	ValueTask<IRequestToken> RequestAsync(string url, ReadOnlyMemory<byte> data, CancellationToken cancellation = default);

	/// <summary>请求响应的回调方法。</summary>
	/// <param name="response">请求的响应对象。</param>
	/// <param name="cancellation">回调的异步操作取消标记。</param>
	/// <returns>返回的异步任务。</returns>
	/// <remarks>注意：一个请求可能会对应多个响应，因此某个请求所对应的该回调方法可能会被触发多次。</remarks>
	ValueTask OnRespondedAsync(IResponse response, CancellationToken cancellation);
}
