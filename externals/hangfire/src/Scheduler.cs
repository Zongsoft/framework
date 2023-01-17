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

using Hangfire;

using Zongsoft.Services;
using Zongsoft.Components;
using Zongsoft.Scheduling;

namespace Zongsoft.Externals.Hangfire
{
	[Service(typeof(IScheduler))]
	public class Scheduler : IScheduler
	{
		#region 公共方法
		public string Schedule(string name, ITriggerOptions options = null)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			switch(options)
			{
				case TriggerOptions.Cron cron:
					if(string.IsNullOrEmpty(options.Identifier))
						RecurringJob.AddOrUpdate(() => HandlerFactory.HandleAsync(this, name, CancellationToken.None), cron.Expression);
					else
						RecurringJob.AddOrUpdate(options.Identifier, () => HandlerFactory.HandleAsync(this, name, CancellationToken.None), cron.Expression);
					return options.Identifier;
				case TriggerOptions.Latency latency:
					return BackgroundJob.Schedule(() => HandlerFactory.HandleAsync(this, name, CancellationToken.None), latency.Duration);
				default:
					return BackgroundJob.Enqueue(() => HandlerFactory.HandleAsync(this, name, CancellationToken.None));
			}
		}

		public string Schedule<TParameter>(string name, TParameter parameter, ITriggerOptions options)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			switch(options)
			{
				case TriggerOptions.Cron cron:
					if(string.IsNullOrEmpty(options.Identifier))
						RecurringJob.AddOrUpdate(() => HandlerFactory.HandleAsync(this, name, parameter, CancellationToken.None), cron.Expression);
					else
						RecurringJob.AddOrUpdate(options.Identifier, () => HandlerFactory.HandleAsync(this, name, parameter, CancellationToken.None), cron.Expression);
					return options.Identifier;
				case TriggerOptions.Latency latency:
					return BackgroundJob.Schedule(() => HandlerFactory.HandleAsync(this, name, parameter, CancellationToken.None), latency.Duration);
				default:
					return BackgroundJob.Enqueue(() => HandlerFactory.HandleAsync(this, name, parameter, CancellationToken.None));
			}
		}

		public bool Reschedule(string identifier)
		{
			if(string.IsNullOrEmpty(identifier))
				return false;

			return BackgroundJob.Requeue(identifier);
		}

		public bool Unschedule(string identifier)
		{
			if(string.IsNullOrEmpty(identifier))
				return false;

			if(BackgroundJob.Delete(identifier))
				return true;

			RecurringJob.RemoveIfExists(identifier);
			return false;
		}
		#endregion

		#region 嵌套子类
		private static class HandlerFactory
		{
			public static async Task HandleAsync(object caller, string name, CancellationToken cancellation)
			{
				var count = 0L;

				foreach(var server in ApplicationContext.Current.Workers.OfType<Server>())
				{
					if(server.Handlers.TryGetValue(name, out var handler) && handler != null)
					{
						count++;
						await handler.HandleAsync(caller, null, cancellation);
					}
				}

				if(count < 1)
					Zongsoft.Diagnostics.Logger.Warn($"No matching handlers found for job named '{name}'.") ;
			}

			public static async Task HandleAsync<TParameter>(object caller, string name, TParameter parameter, CancellationToken cancellation)
			{
				var count = 0L;

				foreach(var server in ApplicationContext.Current.Workers.OfType<Server>())
				{
					if(server.Handlers.TryGetValue(name, out var handler) && handler != null)
					{
						count++;

						if(handler is IHandler<TParameter> strong)
							await strong.HandleAsync(caller, parameter, cancellation);
						else
							await handler.HandleAsync(caller, parameter, cancellation);
					}
				}

				if(count < 1)
					Zongsoft.Diagnostics.Logger.Warn($"No matching handlers found for job named '{name}'.");
			}
		}
		#endregion
	}
}
