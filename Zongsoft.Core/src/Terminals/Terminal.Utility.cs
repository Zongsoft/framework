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

partial class Terminal
{
	public static ITerminal GetTerminal(this ICommandExecutor executor) => (executor as ITerminalExecutor)?.Terminal;
	public static ITerminal GetTerminal(this CommandContextBase context) => (context?.Executor as ITerminalExecutor)?.Terminal;

	/// <summary>执行响应命令。</summary>
	/// <param name="context">指定的命令上下文对象。</param>
	/// <param name="enter">进入被动响应模式的处理方法。</param>
	/// <param name="exit">退出被动响应模式的处理方法。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回的响应命令结果。</returns>
	/// <exception cref="NotSupportedException">当前命令执行器不是一个终端命令执行器。</exception>
	public static async ValueTask<object> ReactiveAsync(this CommandContext context,
		Func<CommandContext, CancellationToken, ValueTask> enter,
		Func<CommandContext, Exception, CancellationToken, ValueTask> exit,
		CancellationToken cancellation = default)
	{
		//获取当前命令执行器对应的终端
		var terminal = context.GetTerminal() ??
			throw new NotSupportedException($"The {context.Command?.Name} command must be run in terminal environment.");

		//创建信号量，默认为堵塞状态
		using var semaphore = new AutoResetEvent(false);

		//挂载当前终端的中断事件
		terminal.Aborting += Terminal_Aborting;

		//定义异常对象
		Exception exception = null;

		try
		{
			//进入被动响应模式
			if(enter != null)
				await enter(context, cancellation);
		}
		catch(Exception ex)
		{
			//设置当前异常对象
			exception = ex;

			//释放信号量
			semaphore.Set();
		}

		//等待信号量
		semaphore.WaitOne();

		//注销当前终端的中断事件
		terminal.Aborting -= Terminal_Aborting;

		//退出被动响应模式
		if(exit != null)
			await exit(context, exception, cancellation);

		//返回结果
		return context.Result;

		void Terminal_Aborting(object sender, System.ComponentModel.CancelEventArgs e)
		{
			//阻止命令执行器被关闭
			e.Cancel = true;

			//释放信号量
			semaphore?.Set();
		}
	}
}
