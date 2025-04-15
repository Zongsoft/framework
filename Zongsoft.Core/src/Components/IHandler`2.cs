﻿/*
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

namespace Zongsoft.Components;

/// <summary>
/// 表示处理程序的接口。
/// </summary>
/// <typeparam name="TArgument">处理程序的请求参数类型。</typeparam>
/// <typeparam name="TResult">处理程序的结果类型。</typeparam>
public interface IHandler<in TArgument, TResult> : IHandler
{
	/// <summary>异步处理执行请求。</summary>
	/// <param name="argument">当前处理的请求对象。</param>
	/// <param name="cancellation">指定的异步取消标记。</param>
	/// <returns>返回的异步任务。</returns>
	ValueTask<TResult> HandleAsync(TArgument argument, CancellationToken cancellation = default);

	/// <summary>异步处理执行请求。</summary>
	/// <param name="argument">当前处理的请求对象。</param>
	/// <param name="parameters">当前处理的参数集。</param>
	/// <param name="cancellation">指定的异步取消标记。</param>
	/// <returns>返回的异步任务。</returns>
	ValueTask<TResult> HandleAsync(TArgument argument, Collections.Parameters parameters, CancellationToken cancellation = default);
}
