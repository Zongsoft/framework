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
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Components.Commands;

internal static class Utility
{
	/// <summary>获取工作器状态对应的输出颜色。</summary>
	/// <param name="state">工作器状态。</param>
	/// <returns>状态对应的颜色。</returns>
	public static CommandOutletColor GetStateColor(WorkerState state) => state switch
	{
		WorkerState.Pausing or WorkerState.Paused => CommandOutletColor.Yellow,
		WorkerState.Resuming or WorkerState.Starting => CommandOutletColor.DarkCyan,
		WorkerState.Stopped or WorkerState.Stopping => CommandOutletColor.DarkGray,
		_ => CommandOutletColor.Green,
	};

	/// <summary>创建包含工作器名称、状态和操作消息的输出内容。</summary>
	/// <param name="worker">关联的工作器。</param>
	/// <param name="message">操作消息。</param>
	/// <param name="color">操作消息的颜色。</param>
	/// <returns>组合后的输出内容。</returns>
	public static CommandOutletContent GetWorkerActionContent(IWorker worker, string message, CommandOutletColor? color = null)
	{
		var content = CommandOutletContent.Create(CommandOutletColor.DarkCyan, worker.Name)
			.Append(CommandOutletColor.DarkGray, "(")
			.Append(GetStateColor(worker.State), worker.State.ToString())
			.Append(CommandOutletColor.DarkGray, ") ");

		if(color == null)
			return content.Last.Append(message);
		else
			return content.Last.Append(color.Value, message);
	}

	/// <summary>在命令期限内等待工作器操作；超时只结束等待，并观察操作随后发生的故障。</summary>
	/// <param name="operation">由工作器拥有的生命周期操作。</param>
	/// <param name="timeout">等待期限；非正数使用五秒，最大三十秒。</param>
	/// <param name="cancellation">命令等待取消标记。</param>
	/// <returns>命令等待任务；超时以 <see cref="TimeoutException" /> 表示，不调用命令的完成回调。</returns>
	public static async Task WaitAsync(Task operation, TimeSpan timeout, CancellationToken cancellation)
	{
		if(timeout <= TimeSpan.Zero)
			timeout = TimeSpan.FromSeconds(5);
		else if(timeout > TimeSpan.FromSeconds(30))
			timeout = TimeSpan.FromSeconds(30);

		try { await operation.WaitAsync(timeout, cancellation).ConfigureAwait(false); }
		catch
		{
			//命令已经结束等待，后续结果由工作器负责通知，不再访问命令上下文。
			_ = operation.ContinueWith(static completed => { _ = completed.Exception; }, CancellationToken.None,
				TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
			throw;
		}
	}
}
