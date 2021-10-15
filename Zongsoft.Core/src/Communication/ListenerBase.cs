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

using Zongsoft.Services;
using Zongsoft.Components;

namespace Zongsoft.Communication
{
	/// <summary>
	/// 提供通讯侦听功能的抽象基类。
	/// </summary>
	public abstract class ListenerBase<T> : WorkerBase, IListener<T>, IReceiver
	{
		#region 成员变量
		private IHandler<T> _handler;
		#endregion

		#region 构造函数
		protected ListenerBase(string name) : base(name) { }
		#endregion

		#region 公共属性
		public virtual bool IsListening { get => this.State == WorkerState.Running; }
		public virtual IPacketizer<T> Packetizer { get; }

		public IHandler<T> Handler
		{
			get => _handler;
			set => _handler = value ?? throw new ArgumentNullException();
		}
		#endregion

		#region 虚拟方法
		protected virtual void OnReceive(ReadOnlySpan<byte> data) { if(this.OnDeserialize(data, out var value)) this.OnHandle(value); }
		protected virtual Task OnReceiveAsync(ReadOnlySpan<byte> data, CancellationToken cancellation) => this.OnDeserialize(data, out var value) ? this.OnHandleAsync(value, cancellation) : Task.CompletedTask;
		protected virtual bool OnHandle(T package) => this.Handler.Handle(package);
		protected virtual Task<bool> OnHandleAsync(T package, CancellationToken cancellation) => this.Handler.HandleAsync(package, cancellation);
		#endregion

		#region 协议转换
		protected virtual bool OnDeserialize(ReadOnlySpan<byte> data, out T result)
		{
			var packetizer = this.Packetizer;

			if(packetizer == null)
				throw new InvalidOperationException("Missing the required packetizer for the receive operation.");

			return packetizer.TryUnpack(data, out result);
		}
		#endregion

		#region 显式实现
		void IListener<T>.Handle(T package) => this.OnHandle(package);
		Task IListener<T>.HandleAsync(T package, CancellationToken cancellation) => this.OnHandleAsync(package, cancellation);
		void IReceiver.Receive(ReadOnlySpan<byte> data) => this.OnReceive(data);
		Task IReceiver.ReceiveAsync(ReadOnlySpan<byte> data, CancellationToken cancellation) => this.OnReceiveAsync(data, cancellation);
		#endregion

		#region 释放资源
		protected override void Dispose(bool disposing)
		{
			var handler = Interlocked.Exchange(ref _handler, null);

			if(handler is IDisposable disposable)
				disposable.Dispose();
		}
		#endregion
	}
}
