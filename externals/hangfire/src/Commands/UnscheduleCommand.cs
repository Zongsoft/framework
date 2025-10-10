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

using Zongsoft.Components;
using Zongsoft.Scheduling;

namespace Zongsoft.Externals.Hangfire.Commands
{
	public class UnscheduleCommand : CommandBase<CommandContext>
	{
		public UnscheduleCommand() : base("Unschedule") { }

		protected override async ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
		{
			if(context.Arguments == null || context.Arguments.IsEmpty)
				throw new CommandException($"Missing the required arguments.");

			var scheduler = context.Find<SchedulerCommand>(true)?.Scheduler ?? throw new CommandException($"Missing the required scheduler.");

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
}
