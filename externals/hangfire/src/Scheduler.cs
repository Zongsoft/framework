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
using System.Collections.Generic;

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
						RecurringJob.AddOrUpdate(() => HandlerFactory.HandleAsync(name, CancellationToken.None), cron.Expression);
					else
						RecurringJob.AddOrUpdate(options.Identifier, () => HandlerFactory.HandleAsync(name, CancellationToken.None), cron.Expression);
					return options.Identifier;
				case TriggerOptions.Latency latency:
					return BackgroundJob.Schedule(() => HandlerFactory.HandleAsync(name, CancellationToken.None), latency.Duration);
				default:
					return BackgroundJob.Enqueue(() => HandlerFactory.HandleAsync(name, CancellationToken.None));
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
						RecurringJob.AddOrUpdate(() => HandlerFactory.HandleAsync(name, parameter, CancellationToken.None), cron.Expression);
					else
						RecurringJob.AddOrUpdate(options.Identifier, () => HandlerFactory.HandleAsync(name, parameter, CancellationToken.None), cron.Expression);
					return options.Identifier;
				case TriggerOptions.Latency latency:
					return BackgroundJob.Schedule(() => HandlerFactory.HandleAsync(name, parameter, CancellationToken.None), latency.Duration);
				default:
					return BackgroundJob.Enqueue(() => HandlerFactory.HandleAsync(name, parameter, CancellationToken.None));
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
			public static async Task HandleAsync(string name, CancellationToken cancellation)
			{
				var handlers = GetHandlers(name);

				if(handlers == null || !handlers.Any())
					throw new InvalidOperationException($"No matching handlers found for job named '{name}'.");

				foreach(var handler in handlers)
				{
					var result = await handler.HandleAsync(null, null, cancellation);

					if(result.Failed)
						throw new InvalidOperationException(result.Failure.ToString());
				}
			}

			public static async Task HandleAsync<TParameter>(string name, TParameter parameter, CancellationToken cancellation)
			{
				var handlers = GetHandlers<TParameter>(name);

				if(handlers == null || !handlers.Any())
					throw new InvalidOperationException($"No matching handlers found for job named '{name}'.");

				foreach(var handler in handlers)
				{
					var result = await handler.HandleAsync(null, parameter, cancellation);

					if(result.Failed)
						throw new InvalidOperationException(result.Failure.ToString());
				}
			}

			private static IEnumerable<IHandler> GetHandlers(string name)
			{
				var handlers = ApplicationContext.Current.Services.ResolveAll<IHandler>(name);

				if(!string.IsNullOrEmpty(name) && (handlers == null || !handlers.Any()))
				{
					handlers = ApplicationContext.Current.Services.ResolveAll<IHandler>();

					if(handlers == null)
						return null;

					return handlers.Where(handler => handler != null &&
						(
							handler.GetType().Name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
							handler.GetType().FullName.Equals(name, StringComparison.OrdinalIgnoreCase)
						));
				}

				return handlers;
			}

			private static IEnumerable<IHandler<T>> GetHandlers<T>(string name)
			{
				var handlers = ApplicationContext.Current.Services.ResolveAll<IHandler<T>>(name);

				if(!string.IsNullOrEmpty(name) && (handlers == null || !handlers.Any()))
				{
					handlers = ApplicationContext.Current.Services.ResolveAll<IHandler<T>>();

					if(handlers == null)
						return null;

					return handlers.Where(handler => handler != null &&
						(
							handler.GetType().Name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
							handler.GetType().FullName.Equals(name, StringComparison.OrdinalIgnoreCase)
						));
				}

				return handlers;
			}
		}
		#endregion
	}
}
