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
using System.Collections.Generic;

namespace Zongsoft.Communication;

/// <summary>统一管理通道关闭和资源释放的抽象基类。</summary>
/// <remarks>并发调用共享完成结果；事件中可以请求关闭，但不能同步等待包含自身的关闭或释放操作。</remarks>
public abstract class ChannelBase : IChannel
{
	#region 事件声明
	/// <summary>表示通道传输已经关闭。</summary>
	public event EventHandler Closed;
	/// <summary>表示通道开始关闭。</summary>
	public event EventHandler Closing;
	#endregion

	#region 成员字段
	private int _closed;
	private int _disposed;
	private TaskCompletionSource _closing;
	private TaskCompletionSource _disposing;
	#endregion

	#region 公共属性
	/// <summary>获取一个值，指示通道关闭流程已经结束，即使收尾曾发生异常。</summary>
	public bool IsClosed => Volatile.Read(ref _closed) != 0;
	/// <summary>获取一个值，指示资源释放流程已经结束。</summary>
	public bool IsDisposed => Volatile.Read(ref _disposed) != 0;
	/// <summary>获取一个值，指示通道已开始关闭，不再接受新操作。</summary>
	protected bool IsClosing => Volatile.Read(ref _closing) != null;
	#endregion

	#region 关闭方法
	/// <summary>请求关闭通道，并等待唯一的关闭操作。</summary>
	/// <param name="cancellation">开始前取消请求；开始后仅取消本调用方的等待。</param>
	/// <returns>共享关闭结果；异常不会阻止其余关闭阶段执行。</returns>
	public ValueTask CloseAsync(CancellationToken cancellation = default)
	{
		cancellation.ThrowIfCancellationRequested();
		var closing = Volatile.Read(ref _closing);

		if(closing == null)
		{
			var candidate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			closing = Interlocked.CompareExchange(ref _closing, candidate, null) ?? candidate;

			if(ReferenceEquals(closing, candidate))
				_ = this.CloseCoreAsync(candidate);
		}

		return new(cancellation.CanBeCanceled ? closing.Task.WaitAsync(cancellation) : closing.Task);
	}

	/// <summary>关闭派生类型拥有的传输。基类确保仅执行一次。</summary>
	/// <param name="cancellation">资源收尾令牌，不随单个关闭调用者取消。</param>
	/// <returns>传输关闭操作。</returns>
	protected abstract ValueTask OnCloseAsync(CancellationToken cancellation);

	/// <summary>执行每个关闭阶段，即使通知或派生关闭抛错也继续后续清理。</summary>
	/// <param name="completion">已发布的共享完成信号。</param>
	/// <returns>唯一关闭执行任务。</returns>
	private async Task CloseCoreAsync(TaskCompletionSource completion)
	{
		List<Exception> errors = null;

		try { this.OnClosing(); }
		catch(Exception exception) { (errors ??= []).Add(exception); }

		try { await this.OnCloseAsync(CancellationToken.None).ConfigureAwait(false); }
		catch(Exception exception) { (errors ??= []).Add(exception); }

		Volatile.Write(ref _closed, 1);

		try { this.OnClosed(); }
		catch(Exception exception) { (errors ??= []).Add(exception); }

		if(errors == null)
			completion.TrySetResult();
		else
			completion.TrySetException(errors);
	}
	#endregion

	#region 事件通知
	/// <summary>通知通道开始关闭。</summary>
	protected virtual void OnClosing() => this.Closing?.Invoke(this, EventArgs.Empty);
	/// <summary>通知通道关闭流程结束。</summary>
	protected virtual void OnClosed() => this.Closed?.Invoke(this, EventArgs.Empty);
	#endregion

	#region 释放资源
	/// <summary>关闭完成后释放派生类型拥有的资源；注入的共享服务不由通道释放。</summary>
	/// <param name="disposing">是否释放托管资源。</param>
	/// <returns>派生资源释放操作。</returns>
	protected virtual ValueTask DisposeAsync(bool disposing) => ValueTask.CompletedTask;

	/// <summary>关闭通道并释放资源。并发调用等待同一结果。</summary>
	/// <returns>完整的资源释放操作。</returns>
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

	/// <summary>在关闭后释放派生资源并聚合两个阶段的错误。</summary>
	/// <param name="completion">已发布的共享完成信号。</param>
	/// <returns>唯一释放执行任务。</returns>
	private async Task DisposeCoreAsync(TaskCompletionSource completion)
	{
		List<Exception> errors = null;

		try { await this.CloseAsync().ConfigureAwait(false); }
		catch(Exception exception)
		{
			errors = _closing.Task.Exception is { } failure ? [.. failure.InnerExceptions] : [exception];
		}

		try { await this.DisposeAsync(true).ConfigureAwait(false); }
		catch(Exception exception) { (errors ??= []).Add(exception); }

		Volatile.Write(ref _disposed, 1);
		GC.SuppressFinalize(this);

		if(errors == null)
			completion.TrySetResult();
		else
			completion.TrySetException(errors);
	}
	#endregion
}
