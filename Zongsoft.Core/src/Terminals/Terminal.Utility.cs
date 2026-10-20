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
	/// <summary>获取命令执行器所属的终端。</summary>
	/// <param name="executor">命令执行器。</param>
	/// <returns>所属终端；不是终端执行器时返回 <see langword="null"/>。</returns>
	public static ITerminal GetTerminal(this ICommandExecutor executor) => (executor as ITerminalExecutor)?.Terminal;
	/// <summary>获取命令上下文所属的终端。</summary>
	/// <param name="context">命令上下文。</param>
	/// <returns>所属终端；上下文为空或不属于终端时返回 <see langword="null"/>。</returns>
	public static ITerminal GetTerminal(this CommandContextBase context) => (context?.Executor as ITerminalExecutor)?.Terminal;
	/// <summary>尝试获取命令上下文所属的终端。</summary>
	/// <param name="context">命令上下文。</param>
	/// <param name="terminal">接收所属终端；不存在时为 <see langword="null"/>。</param>
	/// <returns>存在所属终端则返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
	public static bool TryGetTerminal(this CommandContextBase context, out ITerminal terminal) => TryGetTerminal(context?.Executor, out terminal);
	/// <summary>尝试获取命令执行器所属的终端。</summary>
	/// <param name="executor">命令执行器。</param>
	/// <param name="terminal">接收所属终端；不存在时为 <see langword="null"/>。</param>
	/// <returns>存在所属终端则返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
	public static bool TryGetTerminal(this ICommandExecutor executor, out ITerminal terminal)
	{
		terminal = (executor as ITerminalExecutor)?.Terminal;
		return terminal != null;
	}

	/// <summary>执行响应命令。</summary>
	/// <param name="context">指定的命令上下文对象。</param>
	/// <param name="enter">进入被动响应模式的处理方法。</param>
	/// <param name="exit">退出被动响应模式的处理方法。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回的响应命令结果。</returns>
	/// <remarks>
	/// 	<para>终端中断结束等待，运行取消向调用方传播。</para>
	/// 	<para>进入失败或运行取消后仍等待 exit 完成，并传入原始异常及不可取消的清理令牌；</para>
	/// 	<para>运行与清理同时失败时抛出 <see cref="System.AggregateException"/>。</para>
	/// </remarks>
	/// <exception cref="NotSupportedException">当前命令执行器不是一个终端命令执行器。</exception>
	public static async ValueTask<object> ReactiveAsync(this CommandContext context,
		Func<CommandContext, CancellationToken, ValueTask> enter,
		Func<CommandContext, Exception, CancellationToken, ValueTask> exit,
		CancellationToken cancellation = default)
	{
		//获取当前命令执行器对应的终端
		var terminal = context.GetTerminal() ??
			throw new NotSupportedException(string.Format(Properties.Resources.Terminal_CommandRequiresTerminal_Message, context.Command?.Name));

		cancellation.ThrowIfCancellationRequested();
		var signal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		terminal.Aborting += Terminal_Aborting;
		Exception failure = null;

		try
		{
			if(enter != null)
				await enter(context, cancellation).ConfigureAwait(false);

			await signal.Task.WaitAsync(cancellation).ConfigureAwait(false);
		}
		catch(Exception exception) { failure = exception; }
		finally { terminal.Aborting -= Terminal_Aborting; }

		try
		{
			//退出负责释放已建立的订阅，不沿用已经取消的运行令牌。
			if(exit != null)
				await exit(context, failure, CancellationToken.None).ConfigureAwait(false);
		}
		catch(Exception exception)
		{
			if(failure != null)
				throw new AggregateException(failure, exception);

			throw;
		}

		if(failure != null)
			System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();

		return context.Result;

		void Terminal_Aborting(object sender, System.ComponentModel.CancelEventArgs args)
		{
			args.Cancel = true;
			signal.TrySetResult();
		}
	}
}
