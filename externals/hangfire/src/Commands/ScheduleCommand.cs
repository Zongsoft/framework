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
 * Copyright (C) 2010-2022 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.Hangfire library.
 *
 * The Zongsoft.Externals.Hangfire is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Hangfire is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Hangfire library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Common;
using Zongsoft.Components;
using Zongsoft.Scheduling;

namespace Zongsoft.Externals.Hangfire.Commands
{
	[CommandOption("id", typeof(string))]
	[CommandOption("cron", typeof(string))]
	[CommandOption("delay", typeof(TimeSpan))]
	public class ScheduleCommand : CommandBase<CommandContext>
	{
		public ScheduleCommand() : base("Schedule") { }

		protected override async ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
		{
			if(context.Expression.Arguments == null || context.Expression.Arguments.Length == 0)
				throw new CommandException($"Missing the required argments.");

			var scheduler = context.CommandNode.Find<SchedulerCommand>(true)?.Scheduler ?? throw new CommandException($"Missing the required scheduler.");

			var options = string.IsNullOrEmpty(context.Expression.Options.GetValue<string>("id")) ?
				Trigger.Options.Identifier(Timestamp.Millennium.Now.ToString()) :
				Trigger.Options.Identifier(context.Expression.Options.GetValue<string>("id"));

			string[] identifiers;

			if(context.Expression.Options.TryGetValue<string>("cron", out var cron) && !string.IsNullOrEmpty(cron))
				identifiers = await ScheduleAsync(scheduler, context.Expression.Arguments, context.Parameter, options.Cron(cron), cancellation);
			else if(context.Expression.Options.TryGetValue<TimeSpan>("delay", out var duration) && duration > TimeSpan.Zero)
				identifiers = await ScheduleAsync(scheduler, context.Expression.Arguments, context.Parameter, options.Delay(duration), cancellation);
			else
				identifiers = await ScheduleAsync(scheduler, context.Expression.Arguments, context.Parameter, options, cancellation);

			context.Output.WriteLine(CommandOutletColor.DarkMagenta, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]");

			for(int i = 0; i < identifiers.Length; i++)
			{
				context.Output.Write(CommandOutletColor.DarkGray, $"{(i + 1)}# ");
				context.Output.WriteLine(CommandOutletColor.DarkYellow, identifiers[i]);
			}

			return identifiers;
		}

		private static async ValueTask<string[]> ScheduleAsync(IScheduler scheduler, string[] names, object parameter, ITriggerOptions options, CancellationToken cancellation)
		{
			var result = new string[names.Length];

			for(int i = 0; i < names.Length; i++)
			{
				result[i] = await scheduler.ScheduleAsync(names[i], parameter, options, cancellation);
			}

			return result;
		}
	}
}
