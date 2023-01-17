/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2023 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Components
{
	/// <summary>
	/// 表示处理程序的接口。
	/// </summary>
	/// <typeparam name="TRequest">处理程序的请求参数类型。</typeparam>
	public interface IHandler<in TRequest> : IHandler
	{
		/// <summary>确认当前处理程序能否处理本次执行请求。</summary>
		/// <param name="request">当前处理的请求对象。</param>
		/// <param name="parameters">当前处理的参数集。</param>
		/// <returns>如果能处理本次执行请求则返回真(true)，否则返回假(false)。</returns>
		bool CanHandle(TRequest request, IEnumerable<KeyValuePair<string, object>> parameters = null);

		/// <summary>异步处理执行请求。</summary>
		/// <param name="caller">处理程序的调用者。</param>
		/// <param name="request">当前处理的请求对象。</param>
		/// <param name="cancellation">指定的异步取消标记。</param>
		/// <returns>返回的异步任务。</returns>
		ValueTask HandleAsync(object caller, TRequest request, CancellationToken cancellation = default);

		/// <summary>异步处理执行请求。</summary>
		/// <param name="caller">处理程序的调用者。</param>
		/// <param name="request">当前处理的请求对象。</param>
		/// <param name="parameters">当前处理的参数集。</param>
		/// <param name="cancellation">指定的异步取消标记。</param>
		/// <returns>返回的异步任务。</returns>
		ValueTask HandleAsync(object caller, TRequest request, IEnumerable<KeyValuePair<string, object>> parameters, CancellationToken cancellation = default);
	}
}
