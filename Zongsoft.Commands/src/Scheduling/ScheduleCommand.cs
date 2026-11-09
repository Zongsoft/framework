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

using Zongsoft.Common;
using Zongsoft.Components;
using Zongsoft.Scheduling;

namespace Zongsoft.Scheduling.Commands;

[CommandOption("id", typeof(string))]
[CommandOption("cron", typeof(string))]
[CommandOption("delay", typeof(TimeSpan))]
public class ScheduleCommand : CommandBase<CommandContext>
{
	public ScheduleCommand() : base("Schedule") { }

	protected override async ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		if(context.Arguments == null || context.Arguments.IsEmpty)
			throw new CommandException(Properties.Resources.Command_ArgumentsRequired_Message);

		var scheduler = context.Find<SchedulerCommand>(true)?.Scheduler ?? throw new CommandException(Properties.Resources.Scheduler_Required_Message);

		var options = string.IsNullOrEmpty(context.Options.GetValue<string>("id")) ?
			Trigger.Options.Identifier(Timestamp.Millennium.Now.ToString()) :
			Trigger.Options.Identifier(context.Options.GetValue<string>("id"));

		string[] identifiers;

		if(context.Options.TryGetValue<string>("cron", out var cron) && !string.IsNullOrEmpty(cron))
			identifiers = await ScheduleAsync(scheduler, context.Arguments, context.Value, options.Cron(cron), cancellation);
		else if(context.Options.TryGetValue<TimeSpan>("delay", out var duration) && duration > TimeSpan.Zero)
			identifiers = await ScheduleAsync(scheduler, context.Arguments, context.Value, options.Delay(duration), cancellation);
		else
			identifiers = await ScheduleAsync(scheduler, context.Arguments, context.Value, options, cancellation);

		context.Output.WriteLine(CommandOutletColor.DarkMagenta, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]");

		for(int i = 0; i < identifiers.Length; i++)
		{
			context.Output.Write(CommandOutletColor.DarkGray, $"{(i + 1)}# ");
			context.Output.WriteLine(CommandOutletColor.DarkYellow, identifiers[i]);
		}

		return identifiers;
	}

	private static async ValueTask<string[]> ScheduleAsync(IScheduler scheduler, string[] names, object argument, ITriggerOptions options, CancellationToken cancellation)
	{
		var result = new string[names.Length];

		for(int i = 0; i < names.Length; i++)
		{
			result[i] = await scheduler.ScheduleAsync(names[i], argument, options, cancellation);
		}

		return result;
	}
}
