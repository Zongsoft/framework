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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
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
