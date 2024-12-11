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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Services
{
	/// <summary>
	/// 这是工作器的基类。
	/// </summary>
	/// <remarks>
	///		<para>该实现提供了对<see cref="OnStartAsync(string[], CancellationToken)"/>、<see cref="OnStopAsync(string[], CancellationToken)"/>、<see cref="OnPauseAsync"/>、<see cref="OnResumeAsync"/>这四个方法之间的线程重入的隔离。</para>
	///		<para>对于子类的实现者而言，无需担心这些方法会在多线程中会导致状态的不一致，并确保了它们不会发生线程重入。</para>
	/// </remarks>
	public abstract class WorkerBase : IWorker, IDisposable
	{
		#region 事件声明
		public event EventHandler<WorkerStateChangedEventArgs> StateChanged;
		#endregion

		#region 成员变量
		private string _name;
		private bool _enabled;
		private bool _canPauseAndContinue;
		private int _state;
		private int _disposed;
		private readonly AutoResetEvent _semaphore;
		#endregion

		#region 构造函数
		protected WorkerBase() : this(null) { }
		protected WorkerBase(string name)
		{
			_name = string.IsNullOrWhiteSpace(name) ? this.GetType().Name : name.Trim();
			_enabled = true;
			_canPauseAndContinue = false;
			_state = (int)WorkerState.Stopped;
			_semaphore = new AutoResetEvent(true);
		}
		#endregion

		#region 公共属性
		/// <summary>获取或设置工作器的名称。</summary>
		public string Name
		{
			get => _name;
			protected set => _name = value ?? throw new ArgumentNullException();
		}

		/// <summary>获取或设置是否禁用当前工作器。</summary>
		/// <remarks>如果当前工作器</remarks>
		public bool Enabled
		{
			get => _enabled;
			set
			{
				if(_enabled == value)
					return;

				_enabled = value;

				if(!value)
					this.Stop();
			}
		}

		/// <summary>获取或设置工作器工作器是否可以暂停和继续。</summary>
		public bool CanPauseAndContinue
		{
			get => _canPauseAndContinue;
			protected set
			{
				if(_state != (int)WorkerState.Stopped)
					throw new InvalidOperationException();

				_canPauseAndContinue = value;
			}
		}

		/// <summary>获取工作器的状态。</summary>
		public WorkerState State => (WorkerState)_state;
		public bool IsDisposed => _disposed != 0;
		#endregion

		#region 公共方法
		public void Start(params string[] args) => this.StartAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
		public async Task StartAsync(string[] args, CancellationToken cancellation = default)
		{
			if(this.IsDisposed)
				throw new ObjectDisposedException($"{this.GetType().Name}:{this.Name}");

			//如果不可用或当前状态不是已停止则返回
			if(!_enabled || _state != (int)WorkerState.Stopped)
				return;

			try
			{
				//等待信号量
				_semaphore.WaitOne();

				if(!_enabled || _state != (int)WorkerState.Stopped)
					return;

				//更新当前状态为“正在启动中”
				_state = (int)WorkerState.Starting;

				//调用启动抽象方法，以执行实际的启动操作
				await this.OnStartAsync(args, cancellation);

				//更新当前状态为“运行中”
				_state = (int)WorkerState.Running;

				//激发“StateChanged”事件
				this.OnStateChanged(nameof(Start), WorkerState.Running);
			}
			catch(Exception ex)
			{
				_state = (int)WorkerState.Stopped;

				//激发“StateChanged”事件
				this.OnStateChanged(nameof(Start), WorkerState.Stopped, ex);

				if(System.Diagnostics.Debugger.IsAttached)
					throw;
				else
					Zongsoft.Diagnostics.Logger.GetLogger(this).Error(ex);
			}
			finally
			{
				//释放信号量
				_semaphore.Set();
			}
		}

		public void Stop(params string[] args) => this.StopAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
		public async Task StopAsync(string[] args, CancellationToken cancellation = default)
		{
			if(this.IsDisposed)
				throw new ObjectDisposedException($"{this.GetType().Name}:{this.Name}");

			if(_state == (int)WorkerState.Stopping || _state == (int)WorkerState.Stopped)
				return;

			int originalState = -1;

			try
			{
				//等待信号量
				_semaphore.WaitOne();

				if(_state == (int)WorkerState.Stopping || _state == (int)WorkerState.Stopped)
					return;

				//更新当前状态为“正在停止中”，并保存原来的状态
				originalState = Interlocked.Exchange(ref _state, (int)WorkerState.Stopping);

				//调用停止抽象方法，以执行实际的停止操作
				await this.OnStopAsync(args, cancellation);

				//更新当前状态为已停止
				_state = (int)WorkerState.Stopped;

				//激发“StateChanged”事件
				this.OnStateChanged(nameof(Stop), WorkerState.Stopped);
			}
			catch(Exception ex)
			{
				//还原状态
				_state = originalState;

				//激发“StateChanged”事件
				this.OnStateChanged(nameof(Stop), (WorkerState)originalState, ex);

				if(System.Diagnostics.Debugger.IsAttached)
					throw;
				else
					Zongsoft.Diagnostics.Logger.GetLogger(this).Error(ex);
			}
			finally
			{
				//释放信号量
				_semaphore.Set();
			}
		}

		public void Pause() => this.PauseAsync().ConfigureAwait(false).GetAwaiter().GetResult();
		public async Task PauseAsync(CancellationToken cancellation = default)
		{
			if(this.IsDisposed)
				throw new ObjectDisposedException($"{this.GetType().Name}:{this.Name}");

			//如果不可用则退出
			if(!_enabled)
				return;

			//如果不支持暂停继续则抛出异常
			if(!_canPauseAndContinue)
				throw new NotSupportedException($"The {_name} worker does not support the Pause/Resume operation.");

			if(_state != (int)WorkerState.Running)
				return;

			int originalState = -1;

			try
			{
				//等待信号量
				_semaphore.WaitOne();

				if(_state != (int)WorkerState.Running)
					return;

				//更新当前状态为“正在暂停中”，并保存原来的状态
				originalState = Interlocked.Exchange(ref _state, (int)WorkerState.Pausing);

				//执行暂停操作
				await this.OnPauseAsync(cancellation);

				//更新当前状态为“已经暂停”
				_state = (int)WorkerState.Paused;

				//激发“StateChanged”事件
				this.OnStateChanged(nameof(Pause), WorkerState.Paused);
			}
			catch(Exception ex)
			{
				//还原状态
				_state = originalState;

				//激发“StateChanged”事件
				this.OnStateChanged(nameof(Pause), (WorkerState)originalState, ex);

				if(System.Diagnostics.Debugger.IsAttached)
					throw;
				else
					Zongsoft.Diagnostics.Logger.GetLogger(this).Error(ex);
			}
			finally
			{
				//释放信号量
				_semaphore.Set();
			}
		}

		public void Resume() => this.ResumeAsync().ConfigureAwait(false).GetAwaiter().GetResult();
		public async Task ResumeAsync(CancellationToken cancellation = default)
		{
			if(this.IsDisposed)
				throw new ObjectDisposedException($"{this.GetType().Name}:{this.Name}");

			//如果不可用则退出
			if(!_enabled)
				return;

			//如果不支持暂停继续则抛出异常
			if(!_canPauseAndContinue)
				throw new NotSupportedException($"The {_name} worker does not support the Pause/Resume operation.");

			if(_state != (int)WorkerState.Paused)
				return;

			int originalState = -1;

			try
			{
				//等待信号量
				_semaphore.WaitOne();

				if(_state != (int)WorkerState.Paused)
					return;

				//更新当前状态为“正在恢复中”，并保存原来的状态
				originalState = Interlocked.Exchange(ref _state, (int)WorkerState.Resuming);

				//执行恢复操作
				await this.OnResumeAsync(cancellation);

				_state = (int)WorkerState.Running;

				//激发“StateChanged”事件
				this.OnStateChanged(nameof(Resume), WorkerState.Running);
			}
			catch(Exception ex)
			{
				//还原状态
				_state = originalState;

				//激发“StateChanged”事件
				this.OnStateChanged(nameof(Resume), (WorkerState)originalState, ex);

				if(System.Diagnostics.Debugger.IsAttached)
					throw;
				else
					Zongsoft.Diagnostics.Logger.GetLogger(this).Error(ex);
			}
			finally
			{
				//释放信号量
				_semaphore.Set();
			}
		}
		#endregion

		#region 抽象方法
		protected abstract Task OnStartAsync(string[] args, CancellationToken cancellation);
		protected abstract Task OnStopAsync(string[] args, CancellationToken cancellation);
		protected virtual Task OnPauseAsync(CancellationToken cancellation) => Task.CompletedTask;
		protected virtual Task OnResumeAsync(CancellationToken cancellation) => Task.CompletedTask;
		#endregion

		#region 重写方法
		public override string ToString() => _enabled ? $"[{_state}] {_name}" : $"[{_state}](Disabled) {_name}";
		#endregion

		#region 事件激发
		protected virtual void OnStateChanged(string actionName, WorkerState state, Exception exception = null)
		{
			this.StateChanged?.Invoke(this, new WorkerStateChangedEventArgs(actionName, state, exception));
		}
		#endregion

		#region 释放资源
		protected virtual void Dispose(bool disposing) => this.Stop();
		void IDisposable.Dispose()
		{
			var disposed = Interlocked.CompareExchange(ref _disposed, 1, 0);

			if(disposed == 0)
			{
				this.Dispose(true);
				_semaphore.Dispose();
				GC.SuppressFinalize(this);
			}
		}
		#endregion
	}
}
