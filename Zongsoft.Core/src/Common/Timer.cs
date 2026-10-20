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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
 * associated documentation files (the "Software"), to deal in the Software without restriction,
 * including without limitation the rights to use, copy, modify, merge, publish, distribute,
 * sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all copies or
 * substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
 * NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Zongsoft.Common;

/// <summary>串行执行周期性异步回调，并管理每次运行的停止、故障和资源释放。</summary>
/// <remarks>
///		<para>每次运行由内部取消源管理；停止和释放均请求取消在途回调，回调必须自行响应取消或正常返回。</para>
///		<para><see cref="Stop"/> 和 <see cref="Dispose"/> 不等待本次运行结束；<see cref="StopAsync"/> 和 <see cref="DisposeAsync"/> 等待回调退出、资源清理及事件通知完成。</para>
///		<para>调用释放方法后，调用方不得再启动计时器或访问 <see cref="Period"/>；仍可读取 <see cref="IsRunning"/> 和 <see cref="IsDisposed"/>。重复释放不重复清理，先调用 <see cref="Dispose"/> 后仍可等待 <see cref="DisposeAsync"/> 确认清理完成。</para>
///		<para>回调、取消通知、事件通知及其依赖工作可以调用 <see cref="Stop"/> 或 <see cref="Dispose"/>，但不得等待本计时器停止或释放；调用方负责避免自等待，本类不检测调用依赖。</para>
///		<para>调用方负责在本次运行结束后才释放回调使用的外部资源；计时器不拥有传入的时钟来源、状态和回调所属对象。</para>
/// </remarks>
public class Timer : IDisposable, IAsyncDisposable
{
	#region 事件声明
	/// <summary>运行发生故障且计时资源已经释放后发生；运行和通知异常由异步停止或异步释放操作传播。</summary>
	/// <remarks>通知使用 <see cref="Diagnostics.FailureEventArgs"/>；修改其异常或处理标志不改变运行结果。</remarks>
	public event EventHandler<Diagnostics.FailureEventArgs> Failed;

	/// <summary>本次运行的回调和资源清理结束后发生；发生故障时先通知 <see cref="Failed"/>。</summary>
	/// <remarks>主动停止、故障和释放导致的运行结束均通知一次。通知内可调用 <see cref="Stop"/> 或 <see cref="Dispose"/>，不得等待本次停止或释放；通知异常由异步停止或异步释放操作传播。</remarks>
	public event EventHandler Stopped;
	#endregion

	#region 成员字段
	private readonly object _sync = new();
	private readonly TimeProvider _clock;
	private readonly Func<object, CancellationToken, ValueTask> _tick;
	private TimeSpan _period;
	private Runner _runner;
	private Task _disposal;
	#endregion

	#region 构造函数
	/// <summary>初始化计时器；构造时不创建底层时钟或启动回调。</summary>
	/// <param name="period">回调周期，毫秒值限制在 <c>1</c> 与 <c>4,294,967,294</c> 之间，超出范围时取最近的边界值。</param>
	/// <param name="tick">串行执行的异步回调；取消令牌由计时器提供，停止或释放时请求取消。</param>
	/// <param name="clock">时钟来源；为空时使用系统时钟，自定义时钟须能成功创建底层计时器。</param>
	public Timer(TimeSpan period, Func<object, CancellationToken, ValueTask> tick, TimeProvider clock = null)
	{
		ArgumentNullException.ThrowIfNull(tick);

		_tick = tick;
		_clock = clock ?? TimeProvider.System;
		_period = TimeSpan.FromMilliseconds(Math.Clamp(period.TotalMilliseconds, 1, uint.MaxValue - 1));
	}
	#endregion

	#region 公共属性
	/// <summary>获取一个值，指示本次运行尚未完成，包括停止、资源清理及事件通知阶段。</summary>
	public bool IsRunning
	{
		get
		{
			lock(_sync)
				return _runner != null && !_runner.Completion.IsCompleted;
		}
	}

	/// <summary>获取一个值，指示计时器已经永久关闭；回调和资源清理可能仍在结束中。</summary>
	public bool IsDisposed
	{
		get
		{
			lock(_sync)
				return _disposal != null;
		}
	}

	/// <summary>获取或设置当前计时器的定时周期。</summary>
	/// <remarks>毫秒值限制在 <c>1</c> 与 <c>4,294,967,294</c> 之间，超出范围时取最近的边界值。</remarks>
	public TimeSpan Period
	{
		get
		{
			lock(_sync)
			{
				ObjectDisposedException.ThrowIf(_disposal != null, this);
				return _period;
			}
		}
		set
		{
			lock(_sync)
			{
				ObjectDisposedException.ThrowIf(_disposal != null, this);
				value = TimeSpan.FromMilliseconds(Math.Clamp(value.TotalMilliseconds, 1, uint.MaxValue - 1));
				if(_runner?.Timer != null)
					_runner.Timer.Period = value;

				_period = value;
			}
		}
	}
	#endregion

	#region 公共方法
	/// <summary>启动一次运行；正在运行时重复调用不改变本次运行。</summary>
	/// <remarks>停止后再次启动须等待上次运行完全结束，释放后不得再启动；每次运行使用新的内部取消源。</remarks>
	public void Start() => this.Start(null);
	/// <summary>启动一次运行；停止尚未完成时不能启动，完成后可以再次启动。</summary>
	/// <param name="state">传递给回调和故障通知的状态。</param>
	/// <remarks>调用方不得启动已经释放的计时器；本次运行的取消由停止或释放方法控制。</remarks>
	public void Start(object state)
	{
		Runner runner;

		lock(_sync)
		{
			ObjectDisposedException.ThrowIf(_disposal != null, this);

			if(_runner != null && !_runner.Completion.IsCompleted)
			{
				if(_runner.IsStopping)
					throw new InvalidOperationException(Properties.Resources.Timer_Stopping_Message);

				return;
			}

			_runner = runner = new Runner(_sync);
		}

		//执行器的完成任务在全部回调及资源清理结束后发布结果。
		_ = this.RunAsync(runner, state);
	}

	/// <summary>同步请求当前运行停止；未运行或已经释放时不执行任何操作。</summary>
	/// <remarks>
	///		<para>本方法仅发出取消请求，不等待正在执行的回调、取消通知、资源清理或事件通知完成；返回时 <see cref="IsRunning"/> 仍可能为 <c>true</c>。</para>
	///		<para>可在本次运行的回调、取消通知和事件通知中调用。回调外的调用方可等待 <see cref="StopAsync"/> 确认停止完成并观察运行故障；停止完成前不得释放回调依赖的资源或重新启动。</para>
	/// </remarks>
	public void Stop()
	{
		lock(_sync)
			_runner?.Stop();
	}

	/// <summary>请求停止并等待本次运行结束。</summary>
	/// <returns>等待当前运行及其资源清理、事件通知结束的任务。</returns>
	/// <remarks>
	///		<para>等待的是回调实际退出及清理完成；回调未响应取消且未返回时，本方法持续等待。</para>
	///		<para>调用方不得在本次运行的回调、取消通知、事件通知及其依赖工作中等待本方法，以免等待自身；此约束由调用方保证。</para>
	/// </remarks>
	public Task StopAsync()
	{
		lock(_sync)
		{
			_runner?.Stop();
			return _runner?.Completion ?? Task.CompletedTask;
		}
	}
	#endregion

	#region 释放方法
	/// <summary>永久关闭计时器并请求取消本次运行，不等待回调退出或资源清理完成。</summary>
	/// <remarks>
	///		<para>重复调用不重复清理；本方法返回时在途回调可能仍在执行，资源在本次运行退出时清理。需要确认清理完成时，在回调外等待 <see cref="DisposeAsync"/>。</para>
	///		<para>可在本次运行的回调、取消通知和事件通知中调用。调用后不得再启动计时器或访问 <see cref="Period"/>；调用方须保持回调依赖的外部资源有效，直至本次运行结束。</para>
	/// </remarks>
	public void Dispose()
	{
		lock(_sync)
		{
			_disposal ??= _runner?.Completion ?? Task.CompletedTask;
			_runner?.Stop();
		}
	}

	/// <summary>永久关闭计时器，并等待本次运行结束；重复调用共享相同结果。</summary>
	/// <returns>表示停止、资源清理及事件通知已经完成的异步操作。</returns>
	/// <remarks>
	///		<para>调用后不得再启动计时器或访问 <see cref="Period"/>。先调用 <see cref="Dispose"/> 后，本方法仍等待同一次清理；调用方应在等待完成后释放回调依赖的外部资源。</para>
	///		<para>回调未响应取消且未返回时，本方法持续等待。调用方不得在本次运行的回调、取消通知、事件通知及其依赖工作中等待本方法，以免等待自身；此约束由调用方保证。</para>
	/// </remarks>
	public ValueTask DisposeAsync()
	{
		this.Dispose();
		return new(_disposal);
	}
	#endregion

	#region 事件触发
	/// <summary>通知本次运行的故障，并收集通知异常。</summary>
	/// <param name="exceptions">本次运行的异常集合。</param>
	/// <param name="state">启动时传入的状态。</param>
	private void OnFailed(List<Exception> exceptions, object state)
	{
		var args = new Diagnostics.FailureEventArgs(exceptions.Count == 1 ? exceptions[0] : new AggregateException(exceptions), state);

		try { this.OnFailed(args); }
		catch(AggregateException exception) when(exception.InnerExceptions.Count > 0) { exceptions.AddRange(exception.InnerExceptions); }
		catch(Exception exception) { exceptions.Add(exception); }
	}

	/// <summary>触发 <see cref="Failed"/> 事件，逐个通知订阅者。</summary>
	/// <param name="args">故障异常及启动时传入的状态。</param>
	/// <remarks>重写时调用基类方法以通知事件订阅者；通知异常由异步停止或异步释放操作传播。</remarks>
	protected virtual void OnFailed(Diagnostics.FailureEventArgs args)
	{
		var handlers = this.Failed;
		if(handlers == null)
			return;

		List<Exception> exceptions = null;

		foreach(EventHandler<Diagnostics.FailureEventArgs> handler in handlers.GetInvocationList())
		{
			try { handler(this, args); }
			catch(Exception exception) { (exceptions ??= []).Add(exception); }
		}

		if(exceptions != null)
			throw new AggregateException(exceptions);
	}

	/// <summary>通知本次运行已停止，并保留通知异常。</summary>
	/// <param name="exceptions">本次运行的异常集合。</param>
	private void OnStopped(List<Exception> exceptions)
	{
		try { this.OnStopped(EventArgs.Empty); }
		catch(Exception exception) { exceptions.Add(exception); }
	}

	/// <summary>触发 <see cref="Stopped"/> 事件。</summary>
	/// <param name="args">停止通知的事件参数。</param>
	/// <remarks>重写时调用基类方法以通知事件订阅者；订阅者抛错会中断后续通知，异常由异步停止或异步释放操作传播。</remarks>
	protected virtual void OnStopped(EventArgs args) => this.Stopped?.Invoke(this, args);
	#endregion

	#region 私有方法
	/// <summary>执行本次周期回调，在清理和通知结束后完成运行。</summary>
	/// <param name="runner">本次运行的执行器。</param>
	/// <param name="state">传递给回调和故障通知的状态。</param>
	/// <returns>表示本次运行及其收尾过程的任务。</returns>
	private async Task RunAsync(Runner runner, object state)
	{
		var exceptions = new List<Exception>();
		var token = runner.CancellationSource.Token;

		try
		{
			token.ThrowIfCancellationRequested();
			TimeSpan period;

			lock(_sync)
				period = _period;

			var timer = new PeriodicTimer(period, _clock);

			lock(_sync)
			{
				runner.Timer = timer;
				if(period != _period)
					timer.Period = _period;
			}

			while(await timer.WaitForNextTickAsync(token).ConfigureAwait(false))
			{
				token.ThrowIfCancellationRequested();
				await _tick(state, token).ConfigureAwait(false);
			}
		}
		catch(OperationCanceledException exception) when(token.IsCancellationRequested && exception.CancellationToken == token) { }
		catch(Exception exception) { exceptions.Add(exception); }

		var finishing = runner.FinishAsync();
		PeriodicTimer current;

		lock(_sync)
		{
			current = runner.Timer;
			runner.Timer = null;
		}

		try { current?.Dispose(); }
		catch(Exception exception) { exceptions.Add(exception); }

		try { await finishing.ConfigureAwait(false); }
		catch(Exception exception) { exceptions.Add(exception); }

		if(exceptions.Count > 0)
			this.OnFailed(exceptions, state);

		this.OnStopped(exceptions);

		if(exceptions.Count == 0)
			runner.CompletionSource.TrySetResult();
		else
		{
			runner.CompletionSource.TrySetException(exceptions.Count == 1 ? exceptions[0] : new AggregateException(exceptions));

			//观察后台运行的故障；停止和释放仍可取得完整异常，无需调用者另建观察任务。
			_ = runner.Completion.Exception;
		}
	}
	#endregion

	#region 嵌套类型
	private sealed class Runner(object sync)
	{
		#region 成员字段
		private readonly object _sync = sync;
		private Task _cancellationTask;
		private bool _finishing;
		#endregion

		#region 公共字段
		public readonly CancellationTokenSource CancellationSource = new();
		public readonly TaskCompletionSource CompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
		#endregion

		#region 公共属性
		public PeriodicTimer Timer { get; set; }
		public Task Completion => this.CompletionSource.Task;
		public bool IsStopping => _finishing || _cancellationTask != null;
		#endregion

		#region 公共方法
		public void Stop()
		{
			lock(_sync)
			{
				if(_finishing)
					return;

				_cancellationTask ??= this.CancellationSource.CancelAsync();
			}
		}

		public async Task FinishAsync()
		{
			Task cancellationTask;

			lock(_sync)
			{
				_finishing = true;
				cancellationTask = _cancellationTask;
			}

			try
			{
				if(cancellationTask != null)
					await cancellationTask.ConfigureAwait(false);
			}
			finally
			{
				this.CancellationSource.Dispose();
			}
		}
		#endregion
	}
	#endregion
}
