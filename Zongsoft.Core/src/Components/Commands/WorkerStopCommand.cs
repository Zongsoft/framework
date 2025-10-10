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

[CommandOption(KEY_TIMEOUT_OPTION, typeof(TimeSpan), DefaultValue = "5s", Description = "${Command.Options.Timeout}")]
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
	protected override ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		//向上查找工作者命令对象，如果找到则获取其对应的工作者对象
		var worker = context.Find<WorkerCommandBase>(true)?.Worker ?? throw new CommandException("Missing required worker of depends on.");

		//停止工作者
		worker.Stop(context.Arguments);

		//调用停止完成方法
		this.OnStopped(context, worker, context.GetOptions().GetValue<TimeSpan>(KEY_TIMEOUT_OPTION));

		//返回执行成功的工作者
		return ValueTask.FromResult<object>(worker);
	}
	#endregion

	#region 虚拟方法
	protected virtual void OnStopped(CommandContext context, IWorker worker, TimeSpan timeout)
	{
		if(timeout <= TimeSpan.Zero)
			timeout = TimeSpan.FromSeconds(5);
		else if(timeout.TotalSeconds > 30)
			timeout = TimeSpan.FromSeconds(30);

		switch(worker.State)
		{
			case WorkerState.Stopped:
				OnSucceed(context.Output, worker);
				break;
			case WorkerState.Running:
				OnFailed(context.Output, worker);
				break;
			case WorkerState.Stopping:
				SpinWait.SpinUntil(() => worker.State == WorkerState.Stopped, timeout);

				if(worker.State == WorkerState.Stopped)
					OnSucceed(context.Output, worker);
				else
					OnFailed(context.Output, worker);

				break;
		}
	}
	#endregion

	#region 私有方法
	private static void OnFailed(ICommandOutlet output, IWorker worker)
	{
		output.WriteLine(Utility.GetWorkerActionContent(worker, string.Format(Properties.Resources.Command_ExecutionFailed_Message, Properties.Resources.WorkerStopCommand_Name), CommandOutletColor.DarkRed));
	}

	private static void OnSucceed(ICommandOutlet output, IWorker worker)
	{
		output.WriteLine(Utility.GetWorkerActionContent(worker, string.Format(Properties.Resources.Command_ExecutionSucceed_Message, Properties.Resources.WorkerStopCommand_Name), CommandOutletColor.DarkGreen));
	}
	#endregion
}
