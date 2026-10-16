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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Components;

/// <summary>以异步操作串行协调启动、停止、暂停及释放的工作器基类。</summary>
/// <remarks>同步入口阻塞等待异步结果。生命周期回调不得等待包含自身的操作；通知异常不回滚已经完成的状态转换。</remarks>
public abstract class WorkerBase : IWorker, IDisposable, IAsyncDisposable
{
	#region 事件声明
	/// <summary>表示生命周期转换已经确定结果；通知异常不会回滚状态。</summary>
	public event EventHandler<WorkerStateChangedEventArgs> StateChanged;
	#endregion

	#region 成员字段
	private string _name;
	private bool _enabled = true;
	private bool _canPauseAndContinue;
	private int _state;
	private int _disposed;
	private Operation _operation;
	private TaskCompletionSource _disposing;
	#endregion

	#region 构造函数
	protected WorkerBase() : this(null) { }
	protected WorkerBase(string name) => _name = string.IsNullOrWhiteSpace(name) ? this.GetType().Name : name.Trim();
	#endregion

	#region 公共属性
	/// <summary>获取或设置工作器名称。</summary>
	public string Name
	{
		get => _name;
		protected set => _name = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <summary>获取或设置工作器是否启用；禁用会通过同步入口停止工作器。</summary>
	public bool Enabled
	{
		get => Volatile.Read(ref _enabled);
		set
		{
			Volatile.Write(ref _enabled, value);
			if(!value)
				this.Stop();
		}
	}

	/// <summary>获取或设置工作器是否支持暂停和恢复；仅允许在已停止时设置。</summary>
	public bool CanPauseAndContinue
	{
		get => _canPauseAndContinue;
		protected set
		{
			if(this.State != WorkerState.Stopped)
				throw new InvalidOperationException();

			_canPauseAndContinue = value;
		}
	}

	/// <summary>获取当前生命周期状态。停止失败时保持停止中，不允许再次启动。</summary>
	public WorkerState State => (WorkerState)Volatile.Read(ref _state);
	/// <summary>获取一个值，指示释放流程已经结束。</summary>
	public bool IsDisposed => Volatile.Read(ref _disposed) != 0;
	#endregion

	#region 公共方法
	/// <summary>同步等待 <see cref="StartAsync(string[], CancellationToken)" />。</summary>
	/// <param name="args">生命周期操作参数。</param>
	public void Start(params string[] args) => this.StartAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
	/// <summary>启动工作器；并发启动共用同一结果，首个启动令牌传给扩展点。</summary>
	/// <param name="cancellation">取消令牌；仅取消等待的入口不影响已经开始的资源清理。</param>
	/// <returns>启动结果；初始化失败对调用者可见。</returns>
	public Task StartAsync(CancellationToken cancellation = default) => this.ChangeStateAsync(WorkerState.Starting, null, cancellation);
	/// <summary>启动工作器；并发启动共用同一结果，首个启动令牌传给扩展点。</summary>
	/// <param name="args">生命周期操作参数。</param>
	/// <param name="cancellation">取消令牌；仅取消等待的入口不影响已经开始的资源清理。</param>
	/// <returns>启动结果；初始化失败对调用者可见。</returns>
	public Task StartAsync(string[] args, CancellationToken cancellation = default) => this.ChangeStateAsync(WorkerState.Starting, args, cancellation);

	/// <summary>同步等待 <see cref="StopAsync(string[], CancellationToken)" />。</summary>
	/// <param name="args">生命周期操作参数。</param>
	public void Stop(params string[] args) => this.StopAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
	/// <summary>停止工作器；开始后取消只影响本调用方等待。</summary>
	/// <param name="cancellation">取消令牌；仅取消等待的入口不影响已经开始的资源清理。</param>
	/// <returns>停止结果；失败保持 <see cref="WorkerState.Stopping" />，可再次请求停止。</returns>
	public Task StopAsync(CancellationToken cancellation = default) => this.ChangeStateAsync(WorkerState.Stopping, null, cancellation);
	/// <summary>停止工作器；开始后取消只影响本调用方等待。</summary>
	/// <param name="args">生命周期操作参数。</param>
	/// <param name="cancellation">取消令牌；仅取消等待的入口不影响已经开始的资源清理。</param>
	/// <returns>停止结果；失败保持 <see cref="WorkerState.Stopping" />，可再次请求停止。</returns>
	public Task StopAsync(string[] args, CancellationToken cancellation = default) => this.ChangeStateAsync(WorkerState.Stopping, args, cancellation);

	/// <summary>同步等待 <see cref="PauseAsync" />。</summary>
	public void Pause() => this.PauseAsync().ConfigureAwait(false).GetAwaiter().GetResult();
	/// <summary>在支持暂停时协调一次暂停转换。</summary>
	/// <param name="cancellation">取消令牌；仅取消等待的入口不影响已经开始的资源清理。</param>
	/// <returns>暂停结果。</returns>
	public Task PauseAsync(CancellationToken cancellation = default) => this.ChangeStateAsync(WorkerState.Pausing, null, cancellation);

	/// <summary>同步等待 <see cref="ResumeAsync" />。</summary>
	public void Resume() => this.ResumeAsync().ConfigureAwait(false).GetAwaiter().GetResult();
	/// <summary>在支持恢复时协调一次恢复转换。</summary>
	/// <param name="cancellation">取消令牌；仅取消等待的入口不影响已经开始的资源清理。</param>
	/// <returns>恢复结果。</returns>
	public Task ResumeAsync(CancellationToken cancellation = default) => this.ChangeStateAsync(WorkerState.Resuming, null, cancellation);
	#endregion

	#region 状态转换
	/// <summary>合并相同操作，异步等待不同操作，并原子取得下一次转换权。</summary>
	/// <param name="state">目标转换状态。</param>
	/// <param name="args">生命周期操作参数。</param>
	/// <param name="cancellation">取消令牌；仅取消等待的入口不影响已经开始的资源清理。</param>
	/// <returns>本调用方的等待任务。</returns>
	private async Task ChangeStateAsync(WorkerState state, string[] args, CancellationToken cancellation)
	{
		while(true)
		{
			cancellation.ThrowIfCancellationRequested();

			if(state != WorkerState.Stopping)
				ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposing) != null, this);

			var current = Volatile.Read(ref _operation);

			if(current != null)
			{
				if(current.State == state)
				{
					await current.Completion.Task.WaitAsync(cancellation).ConfigureAwait(false);
					return;
				}

				try { await current.Completion.Task.WaitAsync(cancellation).ConfigureAwait(false); }
				catch when(current.Completion.Task.IsCompleted && !cancellation.IsCancellationRequested) { }

				continue;
			}

			var operation = new Operation(state);
			if(Interlocked.CompareExchange(ref _operation, operation, null) != null)
				continue;

			//操作对象发布后才读取并改变状态，保证等待者不会错过正在进行的转换
			_ = this.ExecuteAsync(operation, args, cancellation);
			await operation.Completion.Task.WaitAsync(cancellation).ConfigureAwait(false);

			return;
		}
	}

	/// <summary>执行派生扩展点并发布稳定状态和共享结果。</summary>
	/// <param name="operation">持有状态转换权的操作。</param>
	/// <param name="args">生命周期操作参数。</param>
	/// <param name="cancellation">取消令牌；仅取消等待的入口不影响已经开始的资源清理。</param>
	/// <returns>唯一状态转换执行任务。</returns>
	private async Task ExecuteAsync(Operation operation, string[] args, CancellationToken cancellation)
	{
		Exception error = null;
		var original = this.State;
		var transition = operation.State;
		var action = transition switch
		{
			WorkerState.Starting => nameof(Start),
			WorkerState.Stopping => nameof(Stop),
			WorkerState.Pausing => nameof(Pause),
			_ => nameof(Resume),
		};

		try
		{
			cancellation.ThrowIfCancellationRequested();

			if(transition != WorkerState.Stopping)
				ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposing) != null, this);

			if(transition is WorkerState.Pausing or WorkerState.Resuming && !_canPauseAndContinue)
				throw new NotSupportedException(string.Format(Properties.Resources.Worker_PauseResumeUnsupported_Message, _name));

			if(transition == WorkerState.Starting && original == WorkerState.Stopping)
				throw new InvalidOperationException();

			var execute = transition switch
			{
				WorkerState.Starting => this.Enabled && original == WorkerState.Stopped,
				WorkerState.Stopping => original != WorkerState.Stopped,
				WorkerState.Pausing => this.Enabled && original == WorkerState.Running,
				_ => this.Enabled && original == WorkerState.Paused,
			};

			if(execute)
			{
				Volatile.Write(ref _state, (int)transition);

				try
				{
					switch(transition)
					{
						case WorkerState.Starting:
							await this.OnStartAsync(args, cancellation).ConfigureAwait(false);
							break;
						case WorkerState.Stopping:
							await this.OnStopAsync(args, CancellationToken.None).ConfigureAwait(false);
							break;
						case WorkerState.Pausing:
							await this.OnPauseAsync(cancellation).ConfigureAwait(false);
							break;
						default:
							await this.OnResumeAsync(cancellation).ConfigureAwait(false);
							break;
					}

					Volatile.Write(ref _state, (int)(transition switch
					{
						WorkerState.Stopping => WorkerState.Stopped,
						WorkerState.Pausing => WorkerState.Paused,
						_ => WorkerState.Running,
					}));
				}
				catch(Exception exception)
				{
					error = exception;

					if(transition == WorkerState.Starting)
					{
						Volatile.Write(ref _state, (int)WorkerState.Stopping);

						try
						{
							await this.OnStopAsync(args, CancellationToken.None).ConfigureAwait(false);
							Volatile.Write(ref _state, (int)WorkerState.Stopped);
						}
						catch(Exception cleanup) { error = new AggregateException(exception, cleanup); }
					}
					else if(transition != WorkerState.Stopping)
						Volatile.Write(ref _state, (int)original);
				}

				try { this.OnStateChanged(action, this.State, error); }
				catch(Exception notification) { error = error == null ? notification : new AggregateException(error, notification); }
			}
		}
		catch(Exception exception) { error = exception; }

		Interlocked.CompareExchange(ref _operation, null, operation);

		if(error == null)
			operation.Completion.TrySetResult();
		else if(error is OperationCanceledException canceled)
			operation.Completion.TrySetCanceled(canceled.CancellationToken);
		else
			operation.Completion.TrySetException(error);
	}
	#endregion

	#region 扩展方法
	/// <summary>启动工作器；失败后的部分资源由停止扩展点清理。</summary>
	/// <param name="args">启动参数。</param>
	/// <param name="cancellation">启动取消令牌。</param>
	/// <returns>启动操作。</returns>
	protected abstract Task OnStartAsync(string[] args, CancellationToken cancellation);

	/// <summary>停止并清理工作器，必须允许启动仅完成部分初始化的情况。</summary>
	/// <param name="args">停止参数。</param>
	/// <param name="cancellation">收尾令牌，不随调用者取消等待。</param>
	/// <returns>停止操作；成功意味着所拥有的运行资源已经退场。</returns>
	protected abstract Task OnStopAsync(string[] args, CancellationToken cancellation);

	/// <summary>暂停工作器。</summary>
	/// <param name="cancellation">取消令牌。</param>
	/// <returns>暂停操作。</returns>
	protected virtual Task OnPauseAsync(CancellationToken cancellation) => Task.CompletedTask;

	/// <summary>恢复工作器。</summary>
	/// <param name="cancellation">取消令牌。</param>
	/// <returns>恢复操作。</returns>
	protected virtual Task OnResumeAsync(CancellationToken cancellation) => Task.CompletedTask;

	/// <summary>在状态确定后发出通知，通知失败不会回滚状态。</summary>
	/// <param name="actionName">操作名称。</param>
	/// <param name="state">操作后的状态。</param>
	/// <param name="exception">操作故障，没有故障时为空。</param>
	protected virtual void OnStateChanged(string actionName, WorkerState state, Exception exception = null) => this.StateChanged?.Invoke(this, new(actionName, state, exception));
	#endregion

	#region 重写方法
	/// <summary>格式化当前状态、启用状态和名称。</summary>
	/// <returns>工作器诊断文本。</returns>
	public override string ToString() => this.Enabled ? $"[{this.State}] {_name}" : $"[{this.State}](Disabled) {_name}";
	#endregion

	#region 释放资源
	/// <summary>停止完成后释放派生资源。</summary>
	/// <param name="disposing">是否释放托管资源。</param>
	protected virtual void Dispose(bool disposing) { }

	/// <summary>停止完成后异步释放派生资源。默认调用同步释放扩展点。</summary>
	/// <param name="disposing">是否释放托管资源。</param>
	/// <returns>返回派生资源释放操作。</returns>
	protected virtual ValueTask DisposeAsync(bool disposing)
	{
		this.Dispose(disposing);
		return ValueTask.CompletedTask;
	}

	/// <summary>停止并释放工作器，并发调用共享完成结果。</summary>
	/// <returns>返回资源释放操作。</returns>
	public ValueTask DisposeAsync()
	{
		var disposing = Volatile.Read(ref _disposing);

		if(disposing == null)
		{
			var candidate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			disposing = Interlocked.CompareExchange(ref _disposing, candidate, null) ?? candidate;

			if(ReferenceEquals(disposing, candidate))
				_ = this.DisposeCoreAsync(candidate);
		}

		return new(disposing.Task);
	}

	/// <summary>同步等待 <see cref="DisposeAsync()" />。</summary>
	void IDisposable.Dispose() => this.DisposeAsync().AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
	/// <summary>停止后继续尝试派生资源释放，并保留两个阶段的故障。</summary>
	/// <param name="completion">已发布的共享完成信号。</param>
	/// <returns>唯一释放执行任务。</returns>
	private async Task DisposeCoreAsync(TaskCompletionSource completion)
	{
		Exception error = null;

		try { await this.StopAsync().ConfigureAwait(false); }
		catch(Exception exception) { error = exception; }

		try { await this.DisposeAsync(true).ConfigureAwait(false); }
		catch(Exception exception) { error = error == null ? exception : new AggregateException(error, exception); }

		Volatile.Write(ref _disposed, 1);
		GC.SuppressFinalize(this);

		if(error == null)
			completion.TrySetResult();
		else
			completion.TrySetException(error);
	}
	#endregion

	#region 嵌套类型
	private sealed class Operation(WorkerState state)
	{
		public readonly WorkerState State = state;
		public readonly TaskCompletionSource Completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
	}
	#endregion
}
