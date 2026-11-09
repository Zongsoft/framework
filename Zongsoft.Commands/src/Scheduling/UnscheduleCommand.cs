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

using Zongsoft.Components;
using Zongsoft.Scheduling;

namespace Zongsoft.Scheduling.Commands;

public class UnscheduleCommand : CommandBase<CommandContext>
{
	public UnscheduleCommand() : base("Unschedule") { }

	protected override async ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		if(context.Arguments == null || context.Arguments.IsEmpty)
			throw new CommandException(Properties.Resources.Command_ArgumentsRequired_Message);

		var scheduler = context.Find<SchedulerCommand>(true)?.Scheduler ?? throw new CommandException(Properties.Resources.Scheduler_Required_Message);

		for(int i = 0; i < context.Arguments.Count; i++)
		{
			var unscheduled = await scheduler.UnscheduleAsync(context.Arguments[i], cancellation);

			context.Output.Write($"[{i + 1}] ");
			context.Output.Write(CommandOutletColor.DarkYellow, context.Arguments[i]);
			context.Output.Write(":");

			if(unscheduled)
				context.Output.WriteLine(CommandOutletColor.Green, "Succeed");
			else
				context.Output.WriteLine(CommandOutletColor.DarkRed, "Failed");
		}

		return null;
	}
}
