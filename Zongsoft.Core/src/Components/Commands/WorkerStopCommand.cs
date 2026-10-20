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

[CommandOption(KEY_TIMEOUT_OPTION, 't', typeof(TimeSpan), DefaultValue = "5s", Description = "Command.Options.Timeout")]
public class WorkerStopCommand : CommandBase<CommandContext>
{
	#region 单例字段
	public static readonly WorkerStopCommand Default = new();
	#endregion

	#region 常量定义
	private const string KEY_TIMEOUT_OPTION = "timeout";
	#endregion

	#region 构造函数
	public WorkerStopCommand() : base("Stop") { }
	public WorkerStopCommand(string name) : base(name) { }
	#endregion

	#region 执行方法
	/// <summary>异步执行工作器停止命令。</summary>
	/// <param name="context">命令执行上下文。</param>
	/// <param name="cancellation">传递给工作器的命令取消标记。</param>
	/// <returns>返回关联的工作器。</returns>
	/// <exception cref="TimeoutException">命令等待超时；工作器操作继续执行。</exception>
	protected override async ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		cancellation.ThrowIfCancellationRequested();

		//向上查找工作者命令对象，如果找到则获取其对应的工作者对象
		var worker = context.Find<WorkerCommandBase>(true)?.Worker ?? throw new CommandException(Properties.Resources.WorkerCommand_MissingWorker_Message);

		//停止工作者
		var timeout = context.Options.GetValue<TimeSpan>(KEY_TIMEOUT_OPTION);
		await Utility.WaitAsync(worker.StopAsync(context.Arguments, cancellation), timeout, cancellation).ConfigureAwait(false);

		//调用停止完成方法
		this.OnStopped(context, worker, timeout);

		//返回执行成功的工作者
		return worker;
	}
	#endregion

	#region 虚拟方法
	/// <summary>处理工作器停止结果。</summary>
	/// <param name="context">命令执行上下文。</param>
	/// <param name="worker">关联的工作器。</param>
	/// <param name="timeout">本次命令的等待期限；结果处理不再等待工作器状态。</param>
	protected virtual void OnStopped(CommandContext context, IWorker worker, TimeSpan timeout)
	{
		if(worker.State == WorkerState.Stopped)
			OnSucceed(context.Output, worker);
		else
			OnFailed(context.Output, worker);
	}
	#endregion

	#region 私有方法
	/// <summary>输出工作器停止失败信息。</summary>
	/// <param name="output">命令输出目标。</param>
	/// <param name="worker">关联的工作器。</param>
	private static void OnFailed(ICommandOutlet output, IWorker worker)
	{
		output.WriteLine(Utility.GetWorkerActionContent(worker, string.Format(Properties.Resources.Command_ExecutionFailed_Message, Properties.Resources.WorkerStopCommand_Name), CommandOutletColor.DarkRed));
	}

	/// <summary>输出工作器停止成功信息。</summary>
	/// <param name="output">命令输出目标。</param>
	/// <param name="worker">关联的工作器。</param>
	private static void OnSucceed(ICommandOutlet output, IWorker worker)
	{
		output.WriteLine(Utility.GetWorkerActionContent(worker, string.Format(Properties.Resources.Command_ExecutionSucceed_Message, Properties.Resources.WorkerStopCommand_Name), CommandOutletColor.DarkGreen));
	}
	#endregion
}
