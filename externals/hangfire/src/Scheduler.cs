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
	[Service<IScheduler, IScheduler<TriggerOptions.Cron>>(Members = $"{nameof(Cron)}")]
	[Service<IScheduler, IScheduler<TriggerOptions.Latency>>(Members = $"{nameof(Latency)}")]
	public partial class Scheduler
	{
		private static readonly Lazy<CronScheduler> _cron = new(() => new CronScheduler(), LazyThreadSafetyMode.PublicationOnly);
		private static readonly Lazy<LatencyScheduler> _latency = new(() => new LatencyScheduler(), LazyThreadSafetyMode.PublicationOnly);
		private static readonly Lazy<JobStorage> _storageFactory = new(() => ApplicationContext.Current.Services.Resolve<JobStorage>() ?? JobStorage.Current, LazyThreadSafetyMode.PublicationOnly);

		private static JobStorage _storage;
		public static JobStorage Storage
		{
			get => _storage ??= _storageFactory.Value;
			set => _storage = value ?? throw new ArgumentNullException(nameof(value));
		}

		public static IScheduler<TriggerOptions.Cron> Cron => _cron.Value;
		public static IScheduler<TriggerOptions.Latency> Latency => _latency.Value;

		#region 嵌套子类
		internal static class HandlerFactory
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
