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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Services;
using Zongsoft.Components;
using Zongsoft.Scheduling;

namespace Zongsoft.Scheduling.Commands;

public class SchedulerCommand : CommandBase<CommandContext>
{
	#region 公共属性
	public IScheduler Scheduler { get; set; }
	#endregion

	#region 重写方法
	protected override ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		if(context.Arguments.Count > 0)
			this.Scheduler = ApplicationContext.Current.Services.Find<IScheduler>(context.Arguments[0]);
		else if(context.Value is string name)
			this.Scheduler = ApplicationContext.Current.Services.Find<IScheduler>(name);
		else if(context.Value is IScheduler scheduler)
			this.Scheduler = scheduler;

		return ValueTask.FromResult<object>(this.Scheduler);
	}
	#endregion
}
