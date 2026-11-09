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

namespace Zongsoft.Components;

/// <summary>关于工作器的接口。</summary>
/// <remarks>实现者应协调并发生命周期调用，避免状态转换和资源操作相互重叠。调用方应避免在回调中反向等待包含自身的生命周期操作。</remarks>
public interface IWorker
{
	#region 事件定义
	/// <summary>表示状态发生了改变。</summary>
	event EventHandler<WorkerStateChangedEventArgs> StateChanged;
	#endregion

	#region 属性定义
	/// <summary>获取当前工作器的名称。</summary>
	string Name { get; }

	/// <summary>获取当前工作器的状态。</summary>
	WorkerState State { get; }

	/// <summary>获取或设置工作器是否允许启动；启动后修改仅影响下一次启动，不影响当前运行及其它生命周期操作。</summary>
	bool Enabled { get; set; }

	/// <summary>获取工作器是否允许暂停和继续。</summary>
	bool CanPauseAndContinue { get; }
	#endregion

	#region 方法定义
	/// <summary>启动工作器。</summary>
	/// <param name="args">启动的参数。</param>
	/// <param name="cancellation">异步操作取消标记。</param>
	/// <returns>表示工作器启动操作的任务。</returns>
	Task StartAsync(string[] args, CancellationToken cancellation = default);

	/// <summary>停止运行并释放全部自有资源；停止成功后可重新启动。</summary>
	/// <param name="args">停止的参数。</param>
	/// <param name="cancellation">异步操作取消标记。</param>
	/// <returns>表示工作器停止操作的任务。</returns>
	Task StopAsync(string[] args, CancellationToken cancellation = default);

	/// <summary>暂停执行并保留资源与内部状态，仅适用于支持暂停和恢复的工作器。</summary>
	/// <param name="cancellation">异步操作取消标记。</param>
	/// <returns>表示工作器暂停操作的任务。</returns>
	Task PauseAsync(CancellationToken cancellation = default);

	/// <summary>使用暂停时保留的资源和内部状态继续运行。</summary>
	/// <param name="cancellation">异步操作取消标记。</param>
	/// <returns>表示工作器恢复运行操作的任务。</returns>
	Task ResumeAsync(CancellationToken cancellation = default);
	#endregion
}
