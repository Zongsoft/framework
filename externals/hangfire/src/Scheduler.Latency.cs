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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Hangfire;
using Hangfire.Common;
using Hangfire.States;

using Zongsoft.Services;
using Zongsoft.Components;
using Zongsoft.Scheduling;

namespace Zongsoft.Externals.Hangfire;

partial class Scheduler
{
	private sealed class LatencyScheduler : IScheduler<TriggerOptions.Latency>, IMatchable, IMatchable<string>
	{
		#region 常量定义
		private const string NAME = "Latency";
		#endregion

		#region 成员字段
		private BackgroundJobClient _scheduler;
		#endregion

		#region 公共属性
		internal BackgroundJobClient Client
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
		public ValueTask<string> ScheduleAsync(string name, TriggerOptions.Latency options, CancellationToken cancellation = default)
		{
			if(options == null)
				throw new ArgumentNullException(nameof(options));
			if(string.IsNullOrEmpty(options.Identifier))
				options.Identifier = $"X{Common.Randomizer.GenerateString()}";

			var job = HandlerFactory.GetJob(name, cancellation);
			options.Identifier = this.Client.Create(job, new ScheduledState(options.Duration));

			return ValueTask.FromResult(options.Identifier);
		}

		public ValueTask<string> ScheduleAsync<TArgument>(string name, TArgument argument, TriggerOptions.Latency options, CancellationToken cancellation = default)
		{
			if(options == null)
				throw new ArgumentNullException(nameof(options));
			if(string.IsNullOrEmpty(options.Identifier))
				options.Identifier = $"X{Common.Randomizer.GenerateString()}";

			var job = HandlerFactory.GetJob(name, argument, cancellation);
			options.Identifier = this.Client.Create(job, new ScheduledState(options.Duration));
			return ValueTask.FromResult(options.Identifier);
		}

		public ValueTask<bool> RescheduleAsync(string identifier, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(identifier))
				return ValueTask.FromResult(false);

			try
			{
				return ValueTask.FromResult(this.Client.Requeue(identifier));
			}
			catch(Exception ex)
			{
				Zongsoft.Diagnostics.Logging.GetLogging(typeof(Scheduler)).Error(ex);
				return ValueTask.FromResult(false);
			}
		}

		public ValueTask<bool> UnscheduleAsync(string identifier, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(identifier))
				return ValueTask.FromResult(false);

			return ValueTask.FromResult(this.Client.Delete(identifier));
		}
		#endregion

		#region 显式实现
		ValueTask<string> IScheduler.ScheduleAsync(string name, ITriggerOptions options, CancellationToken cancellation) => this.ScheduleAsync(name, options as TriggerOptions.Latency, cancellation);
		ValueTask<string> IScheduler.ScheduleAsync<TArgument>(string name, TArgument argument, ITriggerOptions options, CancellationToken cancellation) => this.ScheduleAsync(name, argument, options as TriggerOptions.Latency, cancellation);
		#endregion

		#region 服务匹配
		bool IMatchable.Match(object argument) => argument is string name && string.Equals(name, NAME, StringComparison.OrdinalIgnoreCase);
		bool IMatchable<string>.Match(string argument) => string.Equals(argument, NAME, StringComparison.OrdinalIgnoreCase);
		#endregion
	}
}
