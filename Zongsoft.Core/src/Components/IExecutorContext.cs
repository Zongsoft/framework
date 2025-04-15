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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Components;

/// <summary>
/// 表示执行器上下文的接口。
/// </summary>
public interface IExecutorContext
{
	/// <summary>获取处理本次执行请求的执行器。</summary>
	IExecutor Executor { get; }

	/// <summary>获取执行请求对象。</summary>
	object Argument { get; }

	/// <summary>获取可用于在本次执行过程中在各处理模块之间组织和共享数据的键/值集合。</summary>
	Collections.Parameters Parameters { get; }

	/// <summary>设置一个异常。</summary>
	/// <param name="exception">发生的异常对象。</param>
	void Error(Exception exception);

	/// <summary>获取一个值，指示本次执行中是否发生了异常。</summary>
	/// <param name="exception">输出参数，不为空则表示发生的异常。</param>
	/// <returns>返回真(True)表示执行过程有错误，否则返回假(False)。</returns>
	bool HasError(out Exception exception);
}
