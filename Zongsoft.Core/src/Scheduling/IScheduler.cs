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
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Scheduling;

/// <summary>表示作业任务调度器的接口。</summary>
public interface IScheduler
{
	/// <summary>调度作业任务。</summary>
	/// <param name="name">任务名称或处理程序标识。</param>
	/// <param name="options">任务触发选项。</param>
	/// <param name="cancellation">异步操作取消标记。</param>
	/// <returns>返回调度成功的作业标识。</returns>
	ValueTask<string> ScheduleAsync(string name, ITriggerOptions options, CancellationToken cancellation = default);

	/// <summary>调度作业任务。</summary>
	/// <typeparam name="TArgument">任务参数类型。</typeparam>
	/// <param name="name">任务名称即处理程序标识。</param>
	/// <param name="argument">任务处理程序的回调参数。</param>
	/// <param name="options">任务触发选项。</param>
	/// <param name="cancellation">异步操作取消标记。</param>
	/// <returns>返回调度成功的作业标识。</returns>
	ValueTask<string> ScheduleAsync<TArgument>(string name, TArgument argument, ITriggerOptions options, CancellationToken cancellation = default);

	/// <summary>触发作业调度。</summary>
	/// <param name="identifier">要触发的作业标识。</param>
	/// <param name="cancellation">异步操作取消标记。</param>
	/// <returns>如果触发成功则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	ValueTask<bool> RescheduleAsync(string identifier, CancellationToken cancellation = default);

	/// <summary>取消作业调度。</summary>
	/// <param name="identifier">要取消的作业标识。</param>
	/// <param name="cancellation">异步操作取消标记。</param>
	/// <returns>如果取消作业成功则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	ValueTask<bool> UnscheduleAsync(string identifier, CancellationToken cancellation = default);
}

/// <summary>表示作业任务调度器的接口。</summary>
public interface IScheduler<in TOptions> : IScheduler where TOptions : class, ITriggerOptions
{
	/// <summary>调度作业任务。</summary>
	/// <param name="name">任务名称或处理程序标识。</param>
	/// <param name="options">任务触发选项。</param>
	/// <param name="cancellation">异步操作取消标记。</param>
	/// <returns>返回调度成功的作业标识。</returns>
	ValueTask<string> ScheduleAsync(string name, TOptions options, CancellationToken cancellation = default);

	/// <summary>调度作业任务。</summary>
	/// <typeparam name="TArgument">任务参数类型。</typeparam>
	/// <param name="name">任务名称即处理程序标识。</param>
	/// <param name="argument">任务处理程序的回调参数。</param>
	/// <param name="options">任务触发选项。</param>
	/// <param name="cancellation">异步操作取消标记。</param>
	/// <returns>返回调度成功的作业标识。</returns>
	ValueTask<string> ScheduleAsync<TArgument>(string name, TArgument argument, TOptions options, CancellationToken cancellation = default);
}
