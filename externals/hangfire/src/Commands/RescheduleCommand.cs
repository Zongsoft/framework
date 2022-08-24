/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Services;
using Zongsoft.Scheduling;

namespace Zongsoft.Externals.Hangfire.Commands
{
	public class RescheduleCommand : CommandBase<CommandContext>
	{
		public RescheduleCommand() : base("Reschedule") { }

		protected override object OnExecute(CommandContext context)
		{
			if(context.Expression.Arguments == null || context.Expression.Arguments.Length == 0)
				throw new CommandException($"Missing the required arguments.");

			var scheduler = SchedulerCommand.GetScheduler(context.CommandNode);

			for(int i = 0; i < context.Expression.Arguments.Length; i++)
			{
				var rescheduled = scheduler.Reschedule(context.Expression.Arguments[i]);

				context.Output.Write($"[{i + 1}] ");
				context.Output.Write(CommandOutletColor.DarkYellow, context.Expression.Arguments[i]);
				context.Output.Write(":");

				if(rescheduled)
					context.Output.WriteLine(CommandOutletColor.Green, "Succeed");
				else
					context.Output.WriteLine(CommandOutletColor.DarkRed, "Failed");
			}

			return null;
		}
	}
}
