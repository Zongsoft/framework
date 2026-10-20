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
/// <remarks>
///		<para>相同的在途动作共享结果；停止等待当前动作结束，启动、暂停和恢复遇到其它在途动作直接返回，不排队执行。</para>
///		<para>生命周期扩展点和状态通知应完成各自的工作，不得反向等待所属工作器的生命周期或释放操作，以免形成循环等待。后续操作由回调外的所有者协调。通知异常不回滚已经完成的状态转换。</para>
/// </remarks>
public abstract class WorkerBase : IWorker, IAsyncDisposable
{
	#region 事件声明
	/// <summary>表示生命周期转换已经确定结果；通知异常不会回滚状态。</summary>
	public event EventHandler<WorkerStateChangedEventArgs> StateChanged;
	#endregion

	#region 成员字段
	private readonly object _sync = new();
	private string _name;
	private bool _enabled = true;
	private bool _canPauseAndContinue;
	private int _state;
	private int _disposed;
	private Transitioner _transitioner;
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

	/// <summary>获取或设置工作器是否允许启动；启动后修改仅影响下一次启动，不影响当前运行及其它生命周期操作。</summary>
	public bool Enabled
	{
		get => Volatile.Read(ref _enabled);
		set => Volatile.Write(ref _enabled, value);
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
	/// <summary>获取一个值，指示释放所需的资源清理已经成功完成。</summary>
	public bool IsDisposed => Volatile.Read(ref _disposed) != 0;
	#endregion

	#region 公共方法
	/// <summary>启动工作器；并发启动共用同一结果，首个启动令牌传给扩展点。</summary>
	/// <param name="cancellation">取消令牌；仅取消等待的入口不影响已经开始的资源清理。</param>
	/// <returns>启动结果；初始化失败对调用者可见。</returns>
	public Task StartAsync(CancellationToken cancellation = default) => this.ChangeStateAsync(WorkerState.Starting, null, cancellation);
	/// <summary>启动工作器；并发启动共用同一结果，首个启动令牌传给扩展点。</summary>
	/// <param name="args">生命周期操作参数。</param>
	/// <param name="cancellation">取消令牌；仅取消等待的入口不影响已经开始的资源清理。</param>
	/// <returns>启动结果；初始化失败对调用者可见。</returns>
	public Task StartAsync(string[] args, CancellationToken cancellation = default) => this.ChangeStateAsync(WorkerState.Starting, args, cancellation);

	/// <summary>停止工作器；开始后取消只影响本调用方等待。</summary>
	/// <param name="cancellation">取消令牌；仅取消等待的入口不影响已经开始的资源清理。</param>
	/// <returns>停止结果；成功后可重新启动，失败保持 <see cref="WorkerState.Stopping" />，可再次停止。释放开始后拒绝新的停止调用。</returns>
	public Task StopAsync(CancellationToken cancellation = default) => this.ChangeStateAsync(WorkerState.Stopping, null, cancellation);
	/// <summary>停止工作器；开始后取消只影响本调用方等待。</summary>
	/// <param name="args">生命周期操作参数。</param>
	/// <param name="cancellation">取消令牌；仅取消等待的入口不影响已经开始的资源清理。</param>
	/// <returns>停止结果；成功后可重新启动，失败保持 <see cref="WorkerState.Stopping" />，可再次停止。释放开始后拒绝新的停止调用。</returns>
	public Task StopAsync(string[] args, CancellationToken cancellation = default) => this.ChangeStateAsync(WorkerState.Stopping, args, cancellation);

	/// <summary>在支持暂停时协调一次暂停转换。</summary>
	/// <param name="cancellation">取消令牌；仅取消等待的入口不影响已经开始的资源清理。</param>
	/// <returns>暂停结果。</returns>
	public Task PauseAsync(CancellationToken cancellation = default) => this.ChangeStateAsync(WorkerState.Pausing, null, cancellation);

	/// <summary>在支持恢复时协调一次恢复转换。</summary>
	/// <param name="cancellation">取消令牌；仅取消等待的入口不影响已经开始的资源清理。</param>
	/// <returns>恢复结果。</returns>
	public Task ResumeAsync(CancellationToken cancellation = default) => this.ChangeStateAsync(WorkerState.Resuming, null, cancellation);
	#endregion

	#region 状态转换
	/// <summary>合并相同操作；停止等待当前操作，其余动作仅在当前状态允许时执行。</summary>
	/// <param name="state">目标转换状态。</param>
	/// <param name="args">生命周期操作参数。</param>
	/// <param name="cancellation">取消令牌；仅取消等待的入口不影响已经开始的资源清理。</param>
	/// <param name="disposing">是否由释放流程发起停止操作。</param>
	/// <returns>本调用方的等待任务。</returns>
	private async Task ChangeStateAsync(WorkerState state, string[] args, CancellationToken cancellation, bool disposing = false)
	{
		var accepted = disposing;

		while(true)
		{
			cancellation.ThrowIfCancellationRequested();

			Transitioner transitioner = null;
			var execute = false;

			lock(_sync)
			{
				ObjectDisposedException.ThrowIf(!accepted && _disposing != null, this);
				accepted = true;

				if(state is WorkerState.Pausing or WorkerState.Resuming && !this.CanPauseAndContinue)
					throw new NotSupportedException(string.Format(Properties.Resources.Worker_PauseResumeUnsupported_Message, this.Name));

				if(_transitioner != null)
				{
					if(_transitioner.State != state && state != WorkerState.Stopping)
						return;

					transitioner = _transitioner;
				}
				else
				{
					if(!disposing && !this.CanChangeState(state))
						return;

					transitioner = _transitioner = new(this, state);
					execute = true;
				}
			}

			//接收操作与发布释放标记共用短锁，扩展点始终在锁外执行。
			if(execute)
				_ = transitioner.TransitionAsync(args, cancellation);

			if(transitioner.State == state)
			{
				await transitioner.Completion.Task.WaitAsync(cancellation).ConfigureAwait(false);
				return;
			}

			try { await transitioner.Completion.Task.WaitAsync(cancellation).ConfigureAwait(false); }
			catch when(transitioner.Completion.Task.IsCompleted && !cancellation.IsCancellationRequested) { }
		}
	}

	/// <summary>检查当前稳定状态是否允许指定动作；停止失败后须先完成停止。</summary>
	/// <param name="state">目标转换状态。</param>
	/// <returns>是否执行状态转换。</returns>
	private bool CanChangeState(WorkerState state)
	{
		if(state != WorkerState.Stopping && this.State == WorkerState.Stopping)
			throw new InvalidOperationException();

		return state switch
		{
			WorkerState.Starting => this.Enabled && this.State == WorkerState.Stopped,
			WorkerState.Stopping => this.State != WorkerState.Stopped,
			WorkerState.Pausing => this.State == WorkerState.Running,
			_ => this.State == WorkerState.Paused,
		};
	}

	/// <summary>发布故障并由操作所有者观察，保留后续调用方获取相同故障的能力。</summary>
	/// <param name="completion">操作的完成信号。</param>
	/// <param name="exception">操作故障。</param>
	private static void SetException(TaskCompletionSource completion, Exception exception)
	{
		completion.TrySetException(exception);
		_ = completion.Task.Exception;
	}
	#endregion

	#region 扩展方法
	/// <summary>启动工作器；失败后的部分资源由停止扩展点清理。</summary>
	/// <param name="args">启动参数。</param>
	/// <param name="cancellation">启动取消令牌。</param>
	/// <returns>启动操作。</returns>
	protected abstract Task OnStartAsync(string[] args, CancellationToken cancellation);

	/// <summary>停止运行并释放全部自有资源，允许尚未启动、部分初始化及失败后重试。</summary>
	/// <param name="args">停止参数。</param>
	/// <param name="cancellation">收尾令牌，不随调用者取消等待。</param>
	/// <returns>停止操作；成功意味着所拥有的运行资源已经退场。</returns>
	protected abstract Task OnStopAsync(string[] args, CancellationToken cancellation);

	/// <summary>暂停执行并保留资源与内部状态；失败时由实现者恢复运行。</summary>
	/// <param name="cancellation">取消令牌。</param>
	/// <returns>暂停操作。</returns>
	protected virtual Task OnPauseAsync(CancellationToken cancellation) => Task.CompletedTask;

	/// <summary>使用保留的资源和内部状态恢复执行；失败时由实现者保持暂停。</summary>
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
	/// <summary>永久禁用工作器并通过停止扩展点清理资源；失败后可再次调用以重试清理。</summary>
	/// <returns>本次释放操作；并发调用共享当前尝试，成功后重复调用不再清理。</returns>
	public ValueTask DisposeAsync()
	{
		TaskCompletionSource completion;

		lock(_sync)
		{
			if(this.IsDisposed)
				return ValueTask.CompletedTask;

			if(_disposing != null && !_disposing.Task.IsCompleted)
				return new(_disposing.Task);

			completion = _disposing = new(TaskCreationOptions.RunContinuationsAsynchronously);
		}

		_ = this.DisposeCoreAsync(completion);
		return new(completion.Task);
	}

	/// <summary>执行本次停止清理并发布结果，失败时保留重试入口。</summary>
	/// <param name="completion">本次释放尝试的完成信号。</param>
	/// <returns>本次释放的执行任务。</returns>
	private async Task DisposeCoreAsync(TaskCompletionSource completion)
	{
		Exception error = null;

		try
		{
			await this.ChangeStateAsync(WorkerState.Stopping, null, CancellationToken.None, disposing: true).ConfigureAwait(false);
		}
		catch(Exception exception) { error = exception; }

		//停止通知失败不改变资源已经释放的事实。
		if(this.State == WorkerState.Stopped)
		{
			Volatile.Write(ref _disposed, 1);
			GC.SuppressFinalize(this);
		}

		if(error == null)
			completion.TrySetResult();
		else
			SetException(completion, error);
	}
	#endregion

	#region 嵌套类型
	//每个转换器负责一次状态转换及其完成结果，扩展点始终在协调锁之外执行。
	private sealed class Transitioner(WorkerBase owner, WorkerState state)
	{
		#region 成员字段
		private readonly WorkerBase _owner = owner;
		public readonly WorkerState State = state;
		public readonly TaskCompletionSource Completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
		#endregion

		#region 转换方法
		public async Task TransitionAsync(string[] args, CancellationToken cancellation)
		{
			Exception error = null;
			var original = _owner.State;

			try
			{
				cancellation.ThrowIfCancellationRequested();
				error = await this.TransitionAsync(original, args, cancellation).ConfigureAwait(false);
			}
			catch(Exception exception) { error = exception; }

			lock(_owner._sync)
				_owner._transitioner = null;

			if(error == null)
				this.Completion.TrySetResult();
			else if(error is OperationCanceledException canceled)
				this.Completion.TrySetCanceled(canceled.CancellationToken);
			else
				SetException(this.Completion, error);
		}
		#endregion

		#region 状态转换
		private async ValueTask<Exception> TransitionAsync(WorkerState original, string[] args, CancellationToken cancellation)
		{
			Exception error = null;
			Volatile.Write(ref _owner._state, (int)this.State);

			try
			{
				var pending = this.State switch
				{
					WorkerState.Starting => _owner.OnStartAsync(args, cancellation),
					WorkerState.Stopping => _owner.OnStopAsync(args, CancellationToken.None),
					WorkerState.Pausing => _owner.OnPauseAsync(cancellation),
					_ => _owner.OnResumeAsync(cancellation),
				};

				await pending.ConfigureAwait(false);
				Volatile.Write(ref _owner._state, (int)(this.State switch
				{
					WorkerState.Stopping => WorkerState.Stopped,
					WorkerState.Pausing => WorkerState.Paused,
					_ => WorkerState.Running,
				}));
			}
			catch(Exception exception)
			{
				error = await this.RecoverAsync(original, args, exception).ConfigureAwait(false);
			}

			//通知描述已经确定的结果；通知失败不回滚状态。
			var action = this.State switch
			{
				WorkerState.Starting => "Start",
				WorkerState.Stopping => "Stop",
				WorkerState.Pausing => "Pause",
				_ => "Resume",
			};

			try { _owner.OnStateChanged(action, _owner.State, error); }
			catch(Exception notification) { error = error == null ? notification : new AggregateException(error, notification); }

			return error;
		}

		private async ValueTask<Exception> RecoverAsync(WorkerState original, string[] args, Exception error)
		{
			if(this.State == WorkerState.Stopping)
				return error;

			if(this.State != WorkerState.Starting)
			{
				Volatile.Write(ref _owner._state, (int)original);
				return error;
			}

			//启动失败先清理部分初始化资源；清理失败时保留停止中状态。
			Volatile.Write(ref _owner._state, (int)WorkerState.Stopping);

			try
			{
				await _owner.OnStopAsync(args, CancellationToken.None).ConfigureAwait(false);
				Volatile.Write(ref _owner._state, (int)WorkerState.Stopped);
			}
			catch(Exception cleanup) { return new AggregateException(error, cleanup); }

			return error;
		}
		#endregion
	}
	#endregion
}
