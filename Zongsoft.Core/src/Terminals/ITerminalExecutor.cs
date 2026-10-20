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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
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

using Zongsoft.Components;

namespace Zongsoft.Terminals;

/// <summary>表示终端命令执行器的接口。</summary>
public interface ITerminalExecutor : ICommandExecutor
{
	/// <summary>终端退出事件。</summary>
	event EventHandler<Terminal.ExitEventArgs> Exit;

	/// <summary>获取所属的终端。</summary>
	ITerminal Terminal { get; }

	/// <summary>异步运行终端命令循环。</summary>
	/// <param name="cancellation">取消本次运行的令牌。</param>
	/// <returns>命令循环结束后的退出码。</returns>
	Task<int> RunAsync(CancellationToken cancellation = default);
	/// <summary>使用指定的欢迎文本异步运行终端命令循环。</summary>
	/// <param name="splash">运行开始时显示的欢迎文本。</param>
	/// <param name="cancellation">取消本次运行的令牌。</param>
	/// <returns>命令循环结束后的退出码。</returns>
	Task<int> RunAsync(string splash, CancellationToken cancellation = default);
	/// <summary>使用指定的欢迎内容异步运行终端命令循环。</summary>
	/// <param name="splash">运行开始时显示的欢迎内容。</param>
	/// <param name="cancellation">取消本次运行的令牌。</param>
	/// <returns>命令循环结束后的退出码。</returns>
	Task<int> RunAsync(CommandOutletContent splash, CancellationToken cancellation = default);
}
