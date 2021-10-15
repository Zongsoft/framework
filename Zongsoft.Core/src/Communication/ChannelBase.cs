/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
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

namespace Zongsoft.Communication
{
	/// <summary>
	/// 定义通道基本功能的抽象基类。
	/// </summary>
	public abstract class ChannelBase : IChannel, IReceiver, ISender, IDisposable
	{
		#region 事件定义
		public event EventHandler Closed;
		public event EventHandler Closing;
		#endregion

		#region 构造函数
		protected ChannelBase(int channelId)
		{
			this.ChannelId = channelId;
		}
		#endregion

		#region 公共属性
		/// <summary>获取当前通道的唯一编号。</summary>
		public int ChannelId { get; }

		/// <summary>
		/// 获取当前通道是否为空闲状态。
		/// </summary>
		/// <remarks>
		///		<para>对子类实现者：应该确保该属性能即时反应当前通道的真实状态。</para>
		/// </remarks>
		public abstract bool IsIdled { get; }
		#endregion

		#region 发送方法
		public abstract void Send(ReadOnlySpan<byte> data);
		public abstract Task SendAsync(ReadOnlySpan<byte> data, CancellationToken cancellation = default);
		#endregion

		#region 接收方法
		protected abstract void OnReceive(ReadOnlySpan<byte> data);
		protected abstract Task OnReceiveAsync(ReadOnlySpan<byte> data, CancellationToken cancellation);

		void IReceiver.Receive(ReadOnlySpan<byte> data) => this.OnReceive(data);
		Task IReceiver.ReceiveAsync(ReadOnlySpan<byte> data, CancellationToken cancellation) => this.OnReceiveAsync(data, cancellation);
		#endregion

		#region 激发事件
		protected virtual void OnClosed() => this.Closed?.Invoke(this, EventArgs.Empty);
		protected virtual void OnClosing() => this.Closing?.Invoke(this, EventArgs.Empty);
		#endregion

		#region 关闭方法
		/// <summary>当前通道被关闭时候由子类实现。</summary>
		protected virtual void OnClose() { }

		/// <summary>
		/// 关闭当前通道。
		/// </summary>
		/// <remarks>
		///		<para>注意：该方法不允许线程重入，即在多线程调用中，本方法内部会以同步机制运行。</para>
		///		<para>如果当前通道是空闲的(即<seealso cref="IsIdled"/>属性为真)，则该方法不执行任何操作。</para>
		/// </remarks>
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
		public void Close()
		{
			//如果当前通道是空闲的，则无需关闭。
			//注意：该判断可避免关闭方法被多线程重入。
			if(this.IsIdled)
				return;

			//激发“Closing”关闭前事件
			this.OnClosing();

			//执行子类实现的真正关闭动作
			this.OnClose();

			//激发“Closed”关闭后事件
			this.OnClosed();
		}
		#endregion

		#region 处置方法
		protected virtual void Dispose(bool disposing)
		{
			this.Close();
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		#endregion
	}
}
