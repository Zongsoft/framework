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

namespace Zongsoft.Externals.Hangfire;

partial class Scheduler
{
	private sealed class CronScheduler : IScheduler<TriggerOptions.Cron>, IMatchable, IMatchable<string>
	{
		#region 常量定义
		private const string NAME = "Cron";
		#endregion

		#region 成员字段
		private RecurringJobManager _scheduler;
		#endregion

		#region 公共属性
		internal RecurringJobManager Client
		{
			get
			{
				if(_scheduler != null)
					return _scheduler;

				lock(this)
				{
					_scheduler ??= new(Scheduler.Storage);
				}

				return _scheduler;
			}
		}
		#endregion

		#region 公共方法
		public ValueTask<string> ScheduleAsync(string name, TriggerOptions.Cron options, CancellationToken cancellation = default)
		{
			if(options == null)
				throw new ArgumentNullException(nameof(options));
			if(string.IsNullOrEmpty(options.Expression))
				throw new ArgumentException($"Missing required Cron expression parameter value.");

			if(string.IsNullOrEmpty(options.Identifier))
				options.Identifier = $"X{Common.Randomizer.GenerateString()}";

			var job = HandlerFactory.GetJob(name, cancellation);
			this.Client.AddOrUpdate(options.Identifier, job, options.Expression, new RecurringJobOptions() { TimeZone = options?.Timezone });
			return ValueTask.FromResult(options.Identifier);
		}

		public ValueTask<string> ScheduleAsync<TArgument>(string name, TArgument argument, TriggerOptions.Cron options, CancellationToken cancellation = default)
		{
			if(options == null)
				throw new ArgumentNullException(nameof(options));
			if(string.IsNullOrEmpty(options.Expression))
				throw new ArgumentException($"Missing required Cron expression parameter value.");

			if(string.IsNullOrEmpty(options.Identifier))
				options.Identifier = $"X{Common.Randomizer.GenerateString()}";

			var job = HandlerFactory.GetJob(name, argument, cancellation);
			this.Client.AddOrUpdate(options.Identifier, job, options.Expression, new RecurringJobOptions() { TimeZone = options.Timezone });
			return ValueTask.FromResult(options.Identifier);
		}

		public ValueTask<bool> RescheduleAsync(string identifier, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(identifier))
				return ValueTask.FromResult(false);

			try
			{
				this.Client.TriggerJob(identifier);
				return ValueTask.FromResult(true);
			}
			catch(Exception ex)
			{
				Zongsoft.Diagnostics.Logger.GetLogger(typeof(Scheduler)).Error(ex);
				return ValueTask.FromResult(false);
			}
		}

		public ValueTask<bool> UnscheduleAsync(string identifier, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(identifier))
				return ValueTask.FromResult(false);

			this.Client.RemoveIfExists(identifier);
			return ValueTask.FromResult(true);
		}
		#endregion

		#region 显式实现
		ValueTask<string> IScheduler.ScheduleAsync(string name, ITriggerOptions options, CancellationToken cancellation) => this.ScheduleAsync(name, options as TriggerOptions.Cron, cancellation);
		ValueTask<string> IScheduler.ScheduleAsync<TArgument>(string name, TArgument argument, ITriggerOptions options, CancellationToken cancellation) => this.ScheduleAsync(name, argument, options as TriggerOptions.Cron, cancellation);
		#endregion

		#region 服务匹配
		bool IMatchable.Match(object argument) => argument is string name && string.Equals(name, NAME, StringComparison.OrdinalIgnoreCase);
		bool IMatchable<string>.Match(string argument) => string.Equals(argument, NAME, StringComparison.OrdinalIgnoreCase);
		#endregion
	}
}
