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

[CommandOption(KEY_FORCE_OPTION, 'f')]
[CommandOption(KEY_TIMEOUT_OPTION, 't', typeof(TimeSpan), DefaultValue = "5s", Description = "Command.Options.Timeout")]
public class WorkerStartCommand : CommandBase<CommandContext>
{
	#region 单例字段
	public static readonly WorkerStartCommand Default = new WorkerStartCommand();
	#endregion

	#region 常量定义
	private const string KEY_FORCE_OPTION = "force";
	private const string KEY_TIMEOUT_OPTION = "timeout";
	#endregion

	#region 构造函数
	public WorkerStartCommand() : base("Start") { }
	public WorkerStartCommand(string name) : base(name) { }
	#endregion

	#region 执行方法
	protected override ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		//向上查找工作者命令对象，如果找到则获取其对应的工作者对象
		var worker = context.Find<WorkerCommandBase>(true)?.Worker ?? throw new CommandException("Missing required worker of depends on.");

		//获取是否开启了强制启动选项
		var force = context.Options.GetValue<bool>(KEY_FORCE_OPTION);

		//如果没有开启强制启动选项并且当前工作器不可用，则抛出异常
		if(!force && !worker.Enabled)
			throw new CommandException($"The '{worker.Name}' worker are disabled.");

		//启动工作者
		worker.Start(context.Arguments);

		//调用启动完成方法
		this.OnStarted(context, worker, context.Options.GetValue<TimeSpan>(KEY_TIMEOUT_OPTION));

		//返回执行成功的工作者
		return ValueTask.FromResult<object>(worker);
	}
	#endregion

	#region 虚拟方法
	protected virtual void OnStarted(CommandContext context, IWorker worker, TimeSpan timeout)
	{
		if(timeout <= TimeSpan.Zero)
			timeout = TimeSpan.FromSeconds(5);
		else if(timeout.TotalSeconds > 30)
			timeout = TimeSpan.FromSeconds(30);

		switch(worker.State)
		{
			case WorkerState.Running:
				this.OnSucceed(context.Output, worker);
				break;
			case WorkerState.Stopped:
			case WorkerState.Stopping:
				this.OnFailed(context.Output, worker);
				break;
			case WorkerState.Starting:
				SpinWait.SpinUntil(() => worker.State == WorkerState.Running, timeout);

				if(worker.State == WorkerState.Running)
					this.OnSucceed(context.Output, worker);
				else
					this.OnFailed(context.Output, worker);

				break;
		}
	}
	#endregion

	#region 私有方法
	private void OnFailed(ICommandOutlet output, IWorker worker)
	{
		output.WriteLine(Utility.GetWorkerActionContent(worker, string.Format(Properties.Resources.Command_ExecutionFailed_Message, Properties.Resources.WorkerStartCommand_Name), CommandOutletColor.DarkRed));
	}

	private void OnSucceed(ICommandOutlet output, IWorker worker)
	{
		output.WriteLine(Utility.GetWorkerActionContent(worker, string.Format(Properties.Resources.Command_ExecutionSucceed_Message, Properties.Resources.WorkerStartCommand_Name), CommandOutletColor.DarkGreen));
	}
	#endregion
}
