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

namespace Zongsoft.Communication;

/// <summary>
/// 定义通道基本功能的抽象基类。
/// </summary>
public abstract class ChannelBase : IChannel, IAsyncDisposable
{
	#region 事件定义
	public event EventHandler Closed;
	public event EventHandler Closing;
	#endregion

	#region 常量定义
	private const int NONE_STATUS = 0;
	private const int CLOSED_STATUS = 1;
	private const int CLOSING_STATUS = 2;
	#endregion

	#region 私有变量
	private int _status;
	private volatile AutoResetEvent _semaphore;
	#endregion

	#region 构造函数
	protected ChannelBase()
	{
		_semaphore = new AutoResetEvent(true);
	}
	#endregion

	#region 公共属性
	/// <summary>获取一个值，指示当前通道是否已经关闭。</summary>
	public bool IsClosed => _status == CLOSED_STATUS;
	/// <summary>获取一个值，指示当前通道是否已经被释放。</summary>
	public bool IsDisposed => _semaphore == null;
	#endregion

	#region 激发事件
	protected virtual void OnClosed() => this.Closed?.Invoke(this, EventArgs.Empty);
	protected virtual void OnClosing() => this.Closing?.Invoke(this, EventArgs.Empty);
	#endregion

	#region 关闭方法
	/// <summary>当前通道被关闭时候由子类实现。</summary>
	protected abstract ValueTask OnCloseAsync(CancellationToken cancellation);

	/// <summary>关闭当前通道。</summary>
	/// <remarks>
	///		<para>注意：该方法不允许线程重入，即在多线程调用中，本方法内部会以同步机制运行。</para>
	///		<para>如果当前通道是已关闭的(即<seealso cref="IsClosed"/>属性为真)，则该方法不执行任何操作。</para>
	/// </remarks>
	public async ValueTask CloseAsync(CancellationToken cancellation = default)
	{
		var semaphore = _semaphore ?? throw new ObjectDisposedException(this.GetType().Name);

		try
		{
			semaphore.WaitOne();

			//将状态设置为关闭中
			_status = CLOSING_STATUS;

			//激发“Closing”关闭前事件
			this.OnClosing();

			//执行子类实现的真正关闭动作
			await this.OnCloseAsync(cancellation);

			//设置状态为已关闭
			Interlocked.Exchange(ref _status, CLOSED_STATUS);

			//激发“Closed”关闭后事件
			this.OnClosed();
		}
		finally
		{
			//如果状态不是已关闭则恢复到未关闭状态
			if(_status != CLOSED_STATUS)
				_status = NONE_STATUS;

			_semaphore.Set();
		}
	}
	#endregion

	#region 处置方法
	protected virtual async ValueTask DisposeAsync(bool disposing)
	{
		if(disposing)
			await this.CloseAsync();

		var semaphore = Interlocked.Exchange(ref _semaphore, null);
		if(semaphore != null)
			semaphore.Dispose();
	}

	public async ValueTask DisposeAsync()
	{
		await this.DisposeAsync(true);
		GC.SuppressFinalize(this);
	}
	#endregion
}
