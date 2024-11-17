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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Hangfire;
using Hangfire.Common;

using Zongsoft.Services;
using Zongsoft.Components;
using Zongsoft.Scheduling;

namespace Zongsoft.Externals.Hangfire
{
	[Service<IScheduler>(NAME)]
	public class Scheduler : IScheduler
	{
		#region 常量定义
		internal const string NAME = "Hangfire";
		#endregion

		#region 公共方法
		public ValueTask<string> ScheduleAsync(string name, CancellationToken cancellation = default) => this.ScheduleAsync(name, null, cancellation);
		public ValueTask<string> ScheduleAsync(string name, ITriggerOptions options, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			switch(options)
			{
				case TriggerOptions.Cron cron:
					if(string.IsNullOrEmpty(options.Identifier))
						options.Identifier = $"X{Common.Randomizer.GenerateString()}";

					RecurringJob.AddOrUpdate(options.Identifier, () => HandlerFactory.HandleAsync(name, cancellation), cron.Expression);
					return ValueTask.FromResult(options.Identifier);
				case TriggerOptions.Latency latency:
					return ValueTask.FromResult(BackgroundJob.Schedule(() => HandlerFactory.HandleAsync(name, cancellation), latency.Duration));
				default:
					return ValueTask.FromResult(BackgroundJob.Enqueue(() => HandlerFactory.HandleAsync(name, cancellation)));
			}
		}

		public ValueTask<string> ScheduleAsync<TArgument>(string name, TArgument argument, CancellationToken cancellation = default) => this.ScheduleAsync(name, argument, null, cancellation);
		public ValueTask<string> ScheduleAsync<TArgument>(string name, TArgument argument, ITriggerOptions options, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			switch(options)
			{
				case TriggerOptions.Cron cron:
					if(string.IsNullOrEmpty(options.Identifier))
						options.Identifier = $"X{Common.Randomizer.GenerateString()}";

					RecurringJob.AddOrUpdate(options.Identifier, () => HandlerFactory.HandleAsync(name, argument, cancellation), cron.Expression);
					return ValueTask.FromResult(options.Identifier);
				case TriggerOptions.Latency latency:
					return ValueTask.FromResult(BackgroundJob.Schedule(() => HandlerFactory.HandleAsync(name, argument, cancellation), latency.Duration));
				default:
					return ValueTask.FromResult(BackgroundJob.Enqueue(() => HandlerFactory.HandleAsync(name, argument, cancellation)));
			}
		}

		public ValueTask<bool> RescheduleAsync(string identifier, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(identifier))
				return ValueTask.FromResult(false);

			return ValueTask.FromResult(BackgroundJob.Requeue(identifier));
		}

		public ValueTask<bool> UnscheduleAsync(string identifier, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(identifier))
				return ValueTask.FromResult(false);

			if(!BackgroundJob.Delete(identifier))
				RecurringJob.RemoveIfExists(identifier);

			return ValueTask.FromResult(true);
		}
		#endregion

		#region 嵌套子类
		private static class HandlerFactory
		{
			public static Job GetJob(string name, CancellationToken cancellation) => Job.FromExpression(() => HandleAsync(name, cancellation));
			public static Job GetJob<TArgument>(string name, TArgument argument, CancellationToken cancellation) => Job.FromExpression(() => HandleAsync(name, argument, cancellation));

			public static async Task HandleAsync(string name, CancellationToken cancellation)
			{
				var count = 0L;

				foreach(var server in ApplicationContext.Current.Workers.OfType<Server>())
				{
					if(server.Handlers.TryGetValue(name, out var handler) && handler != null)
					{
						count++;
						await handler.HandleAsync(null, cancellation);
					}
				}

				if(count < 1)
					Zongsoft.Diagnostics.Logger.GetLogger(typeof(HandlerFactory)).Warn($"No matching handlers found for job named '{name}'.");
			}

			public static async Task HandleAsync<TArgument>(string name, TArgument argument, CancellationToken cancellation)
			{
				var count = 0L;

				foreach(var server in ApplicationContext.Current.Workers.OfType<Server>())
				{
					if(server.Handlers.TryGetValue(name, out var handler) && handler != null)
					{
						count++;

						if(handler is IHandler<TArgument> strong)
							await strong.HandleAsync(argument, cancellation);
						else
							await handler.HandleAsync(argument, cancellation);
					}
				}

				if(count < 1)
					Zongsoft.Diagnostics.Logger.GetLogger(typeof(HandlerFactory)).Warn($"No matching handlers found for job named '{name}'.");
			}
		}
		#endregion
	}
}
