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
using System.Reflection;

using Hangfire;
using Hangfire.Common;
using Hangfire.States;

using Zongsoft.Components;
using Zongsoft.Scheduling;

namespace Zongsoft.Externals.Hangfire
{
	public class Scheduler : IScheduler
	{
		private static readonly Lazy<BackgroundJobClient> _client = new Lazy<BackgroundJobClient>(true);
		private static readonly MethodInfo _handleMethod = typeof(IHandler).GetMethod(nameof(IHandler.HandleAsync), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

		public void Schedule(IHandler handler, ITriggerOptions options = null)
		{
			if(handler is null)
				throw new ArgumentNullException(nameof(handler));

			switch(options)
			{
				case TriggerOptions.Cron cron:
					RecurringJob.AddOrUpdate(() => handler.HandleAsync(null, null, CancellationToken.None).AsTask(), cron.Expression);
					break;
				case TriggerOptions.Latency latency:
					BackgroundJob.Schedule(() => handler.HandleAsync(null, null, CancellationToken.None).AsTask(), latency.Duration);
					break;
				default:
					BackgroundJob.Enqueue(() => handler.HandleAsync(null, null, CancellationToken.None).AsTask());
					break;
			}
		}

		public void Schedule(Type handlerType, ITriggerOptions options = null)
		{
			if(handlerType is null)
				throw new ArgumentNullException(nameof(handlerType));
			if(typeof(IHandler).IsAssignableFrom(handlerType))
				throw new ArgumentException($"The specified '{handlerType.FullName}' type does not implement the '{typeof(IHandler).FullName}' interface.", nameof(handlerType));

			_client.Value.Create(new Job(handlerType, _handleMethod, new object[] { null, null, CancellationToken.None }), new EnqueuedState());
		}

		public void Schedule<THandler>(ITriggerOptions options = null) where THandler : IHandler
		{
			switch(options)
			{
				case TriggerOptions.Cron cron:
					RecurringJob.AddOrUpdate<THandler>(handler => handler.HandleAsync(null, null, CancellationToken.None).AsTask(), cron.Expression);
					break;
				case TriggerOptions.Latency latency:
					BackgroundJob.Schedule<THandler>(handler => handler.HandleAsync(null, null, CancellationToken.None).AsTask(), latency.Duration);
					break;
				default:
					BackgroundJob.Enqueue<THandler>(handler => handler.HandleAsync(null, null, CancellationToken.None).AsTask());
					break;
			}
		}

		public void Schedule<TParameter>(IHandler<TParameter> handler, TParameter parameter, ITriggerOptions options = null)
		{
			if(handler is null)
				throw new ArgumentNullException(nameof(handler));

			switch(options)
			{
				case TriggerOptions.Cron cron:
					RecurringJob.AddOrUpdate(() => handler.HandleAsync(null, parameter, CancellationToken.None).AsTask(), cron.Expression);
					break;
				case TriggerOptions.Latency latency:
					BackgroundJob.Schedule(() => handler.HandleAsync(null, parameter, CancellationToken.None).AsTask(), latency.Duration);
					break;
				default:
					BackgroundJob.Enqueue(() => handler.HandleAsync(null, parameter, CancellationToken.None).AsTask());
					break;
			}
		}

		public void Schedule<TParameter>(Type handlerType, TParameter parameter, ITriggerOptions options = null)
		{
			if(handlerType is null)
				throw new ArgumentNullException(nameof(handlerType));
			if(typeof(IHandler).IsAssignableFrom(handlerType))
				throw new ArgumentException($"The specified '{handlerType.FullName}' type does not implement the '{typeof(IHandler).FullName}' interface.", nameof(handlerType));

			_client.Value.Create(new Job(handlerType, _handleMethod, new object[] { null, parameter, CancellationToken.None }), new EnqueuedState());
		}

		public void Schedule<THandler, TParameter>(TParameter parameter, ITriggerOptions options = null) where THandler : IHandler
		{
			switch(options)
			{
				case TriggerOptions.Cron cron:
					RecurringJob.AddOrUpdate<THandler>(handler => handler.HandleAsync(null, parameter, CancellationToken.None).AsTask(), cron.Expression);
					break;
				case TriggerOptions.Latency latency:
					BackgroundJob.Schedule<THandler>(handler => handler.HandleAsync(null, parameter, CancellationToken.None).AsTask(), latency.Duration);
					break;
				default:
					BackgroundJob.Enqueue<THandler>(handler => handler.HandleAsync(null, parameter, CancellationToken.None).AsTask());
					break;
			}
		}

		public bool Reschedule(string identifier)
		{
			if(string.IsNullOrWhiteSpace(identifier))
				return false;

			return BackgroundJob.Requeue(identifier);
		}

		public bool Unschedule(string identifier)
		{
			if(string.IsNullOrWhiteSpace(identifier))
				return false;

			RecurringJob.RemoveIfExists(identifier);
			return BackgroundJob.Delete(identifier);
		}
	}
}
