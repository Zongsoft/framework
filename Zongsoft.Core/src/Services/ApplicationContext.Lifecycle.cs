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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

using Microsoft.Extensions.Hosting;

namespace Zongsoft.Services;

partial class ApplicationContext
{
	#region 成员字段
	private readonly object _lifecycleSync = new();
	private readonly AsyncLocal<Invocation> _invocation = new();
	private CancellationTokenSource _startupCancellation;
	private TaskCompletionSource _starting;
	private TaskCompletionSource _stopping;
	private TaskCompletionSource _disposing;
	private Task _rollback;
	private int _stopped;
	#endregion

	#region 宿主生命周期
	Task IHostedLifecycleService.StartingAsync(CancellationToken cancellation)
	{
		cancellation.ThrowIfCancellationRequested();
		ObjectDisposedException.ThrowIf(_disposing != null, this);

		this.Initialize();
		return Task.CompletedTask;
	}

	Task IHostedService.StartAsync(CancellationToken cancellation) => Task.CompletedTask;
	Task IHostedLifecycleService.StartedAsync(CancellationToken cancellation)
	{
		cancellation.ThrowIfCancellationRequested();

		this.CheckReentrancy();
		TaskCompletionSource starting;
		bool execute;

		lock(_lifecycleSync)
		{
			ObjectDisposedException.ThrowIf(_disposing != null, this);
			if(_stopping != null)
				throw new InvalidOperationException();

			execute = _starting == null;
			starting = _starting ??= new(TaskCreationOptions.RunContinuationsAsynchronously);

			if(execute)
				_startupCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
		}

		if(execute)
			_ = this.StartCoreAsync(starting, _startupCancellation);

		return cancellation.CanBeCanceled ? starting.Task.WaitAsync(cancellation) : starting.Task;
	}

	Task IHostedLifecycleService.StoppingAsync(CancellationToken cancellation) => Task.CompletedTask;
	Task IHostedService.StopAsync(CancellationToken cancellation)
	{
		this.CheckReentrancy();

		//宿主的截止时间只限制等待；已登记的工作器均须收到停止请求。
		var stopping = this.GetStopping();
		return cancellation.CanBeCanceled ? stopping.WaitAsync(cancellation) : stopping;
	}

	async Task IHostedLifecycleService.StoppedAsync(CancellationToken cancellation)
	{
		var stopping = this.GetStopping();

		try { await stopping.WaitAsync(cancellation).ConfigureAwait(false); }
		finally
		{
			if(stopping.IsCompleted)
				this.NotifyStopped();
		}
	}

	private void NotifyStopped()
	{
		if(Interlocked.Exchange(ref _stopped, 1) == 0)
			this.OnStopped();
	}
	#endregion

	#region 扩展方法
	/// <summary>启动已登记且启用的工作器，全部完成后发出应用启动通知。</summary>
	/// <param name="cancellation">启动阶段的取消令牌，不代表工作器运行期间的停止信号。</param>
	/// <returns>所有启动及通知完成的任务。</returns>
	protected virtual async Task OnStartedAsync(CancellationToken cancellation)
	{
		var workers = _workers.Where(worker => worker != null && worker.Enabled)
			.Distinct<Components.IWorker>(ReferenceEqualityComparer.Instance)
			.ToArray();

		using var stopping = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
		var pending = Task.WhenAll(workers.Select(worker => StartAsync(worker, stopping)));

		try { await pending.ConfigureAwait(false); }
		catch
		{
			if(pending.Exception != null)
				throw pending.Exception;

			throw;
		}

		this.OnStarted();

		static async Task StartAsync(Components.IWorker worker, CancellationTokenSource stopping)
		{
			try { await worker.StartAsync([], stopping.Token).ConfigureAwait(false); }
			catch(Exception exception)
			{
				try { await stopping.CancelAsync().ConfigureAwait(false); }
				catch(Exception cleanup) { throw new AggregateException(exception, cleanup); }

				throw;
			}
		}
	}

	/// <summary>发出停止通知并等待所有登记的工作器收尾；一个失败不妨碍其余工作器停止。</summary>
	/// <param name="cancellation">收尾令牌，不随宿主取消等待而取消。</param>
	/// <returns>所有停止完成的任务；多个故障一起传播。</returns>
	protected virtual async Task OnStoppingAsync(CancellationToken cancellation)
	{
		List<Exception> errors = null;

		try { this.OnStopping(); }
		catch(Exception exception) { (errors ??= []).Add(exception); }

		var workers = _workers.Where(worker => worker != null)
			.Distinct<Components.IWorker>(ReferenceEqualityComparer.Instance).ToArray();

		var pending = Task.WhenAll(workers.Select(StopAsync));

		try { await pending.ConfigureAwait(false); }
		catch(Exception exception)
		{
			if(pending.Exception != null)
				(errors ??= []).AddRange(pending.Exception.InnerExceptions);
			else
				(errors ??= []).Add(exception);
		}

		ThrowErrors(errors);

		static async Task StopAsync(Components.IWorker worker) =>
			await worker.StopAsync([], CancellationToken.None).ConfigureAwait(false);
	}
	#endregion

	#region 生命周期协调
	private async Task StartCoreAsync(TaskCompletionSource completion, CancellationTokenSource cancellation)
	{
		var invocation = new Invocation();
		var previous = _invocation.Value;
		_invocation.Value = invocation;

		try
		{
			await this.OnStartedAsync(cancellation.Token).ConfigureAwait(false);
			completion.TrySetResult();
		}
		catch(Exception exception)
		{
			//回滚由启动操作拥有；并发停止在启动结束后观察同一份回滚结果。
			var rollback = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			_rollback = rollback.Task;

			try
			{
				await this.OnStoppingAsync(CancellationToken.None).ConfigureAwait(false);
				rollback.TrySetResult();
			}
			catch(Exception cleanup)
			{
				SetException(rollback, cleanup);
				exception = new AggregateException(exception, cleanup);
			}

			SetException(completion, exception);
		}
		finally
		{
			cancellation.Dispose();
			invocation.Active = false;
			_invocation.Value = previous;
		}
	}

	private Task GetStopping()
	{
		TaskCompletionSource stopping;
		Task starting;
		CancellationTokenSource cancellation;
		bool execute;

		lock(_lifecycleSync)
		{
			execute = _stopping == null;
			stopping = _stopping ??= new(TaskCreationOptions.RunContinuationsAsynchronously);
			starting = _starting?.Task;
			cancellation = _startupCancellation;
		}

		if(execute)
			_ = this.StopCoreAsync(stopping, starting, cancellation);

		return stopping.Task;
	}

	private async Task StopCoreAsync(TaskCompletionSource completion, Task starting, CancellationTokenSource cancellation)
	{
		var invocation = new Invocation();
		var previous = _invocation.Value;
		_invocation.Value = invocation;
		List<Exception> errors = null;

		try
		{
			if(starting is { IsCompleted: false })
			{
				try { await cancellation.CancelAsync().ConfigureAwait(false); }
				catch(ObjectDisposedException) when(starting.IsCompleted) { }
				catch(Exception exception) { (errors ??= []).Add(exception); }
			}

			if(starting != null)
				await starting.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

			try
			{
				if(_rollback != null)
					await _rollback.ConfigureAwait(false);
				else
					await this.OnStoppingAsync(CancellationToken.None).ConfigureAwait(false);
			}
			catch(Exception exception) { (errors ??= []).Add(exception); }

			ThrowErrors(errors);

			completion.TrySetResult();
		}
		catch(Exception exception) { SetException(completion, exception); }
		finally
		{
			invocation.Active = false;
			_invocation.Value = previous;
		}
	}

	private void CheckReentrancy()
	{
		if(_invocation.Value is { Active: true })
			throw new InvalidOperationException();
	}

	private static void SetException(TaskCompletionSource completion, Exception exception)
	{
		if(exception is OperationCanceledException canceled)
			completion.TrySetCanceled(canceled.CancellationToken);
		else
		{
			completion.TrySetException(exception);
			//即使所有调用者已经取消等待，操作所有者仍观察最终故障。
			_ = completion.Task.Exception;
		}
	}

	private static void ThrowErrors(List<Exception> errors)
	{
		if(errors == null || errors.Count == 0)
			return;

		if(errors.Count == 1)
			ExceptionDispatchInfo.Capture(errors[0]).Throw();

		throw new AggregateException(errors);
	}
	#endregion

	#region 释放资源
	/// <summary>等待工作器停止后释放上下文拥有的资源；并发调用共享同一结果。</summary>
	/// <returns>全部释放结束的操作。</returns>
	public ValueTask DisposeAsync()
	{
		this.CheckReentrancy();
		TaskCompletionSource disposing;
		bool execute;

		lock(_lifecycleSync)
		{
			execute = _disposing == null;
			disposing = _disposing ??= new(TaskCreationOptions.RunContinuationsAsynchronously);
		}

		if(execute)
			_ = this.DisposeCoreAsync(disposing);

		return new(disposing.Task);
	}

	/// <summary>释放应用初始化器和模块；服务容器及其共享服务由宿主负责释放。</summary>
	/// <param name="disposing">是否释放托管资源。</param>
	/// <returns>上下文所拥有资源的释放操作。</returns>
	protected virtual async ValueTask DisposeAsync(bool disposing)
	{
		if(!disposing)
			return;

		List<Exception> errors = null;
		var resources = new HashSet<object>(ReferenceEqualityComparer.Instance);
		var initializers = Interlocked.Exchange(ref _initializers, null);

		if(initializers != null)
			resources.UnionWith(initializers);

		resources.UnionWith(this.Modules);
		resources.ExceptWith(_serviceInitializers);
		_serviceInitializers.Clear();

		foreach(var resource in resources)
		{
			try
			{
				if(resource is IAsyncDisposable asynchronous)
					await asynchronous.DisposeAsync().ConfigureAwait(false);
				else if(resource is IDisposable synchronous)
					synchronous.Dispose();
			}
			catch(Exception exception) { (errors ??= []).Add(exception); }
		}

		this.Modules.Clear();
		ThrowErrors(errors);
	}

	private async Task DisposeCoreAsync(TaskCompletionSource completion)
	{
		var invocation = new Invocation();
		var previous = _invocation.Value;
		_invocation.Value = invocation;
		List<Exception> errors = null;

		try
		{
			try { await this.GetStopping().ConfigureAwait(false); }
			catch(Exception exception) { (errors ??= []).Add(exception); }

			try { this.NotifyStopped(); }
			catch(Exception exception) { (errors ??= []).Add(exception); }

			try { await this.DisposeAsync(true).ConfigureAwait(false); }
			catch(Exception exception) { (errors ??= []).Add(exception); }

			_disposed = 1;
			GC.SuppressFinalize(this);
			ThrowErrors(errors);
			completion.TrySetResult();
		}
		catch(Exception exception) { SetException(completion, exception); }
		finally
		{
			invocation.Active = false;
			_invocation.Value = previous;
		}
	}
	#endregion

	#region 嵌套类型
	private sealed class Invocation
	{
		public volatile bool Active = true;
	}
	#endregion
}
