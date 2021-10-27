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
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Services;
using Zongsoft.Components;

namespace Zongsoft.Communication
{
	/// <summary>
	/// 提供通讯侦听功能的抽象基类。
	/// </summary>
	public abstract class ListenerBase<T> : WorkerBase, IListener<T>, IReceiver, IHandleable<T>, IHandleable
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
		public IHandler<T> Handler { get => _handler; set => _handler = value; }
		IHandler IHandleable.Handler { get => _handler; set => _handler = value as IHandler<T> ?? throw new ArgumentException($"The specified ‘{value}’ handler does not match."); }
		#endregion

		#region 虚拟方法
		protected virtual ValueTask OnReceiveAsync(in ReadOnlySequence<byte> data, CancellationToken cancellation)
		{
			var message = data;

			if(this.OnDeserialize(ref message, out var value))
			{
				var task = this.OnHandleAsync(value, cancellation);

				if(!task.IsCompletedSuccessfully)
					return new ValueTask(task.AsTask());
			}

			return ValueTask.CompletedTask;
		}
		protected virtual ValueTask<bool> OnHandleAsync(T package, CancellationToken cancellation) => this.Handler?.HandleAsync(this, package, cancellation) ?? ValueTask.FromResult(false);
		#endregion

		#region 协议转换
		protected virtual bool OnDeserialize(ref ReadOnlySequence<byte> data, out T result)
		{
			var packetizer = this.Packetizer;

			if(packetizer == null)
				throw new InvalidOperationException("Missing the required packetizer for the receive operation.");

			return packetizer.Unpack(ref data, out result);
		}
		#endregion

		#region 显式实现
		ValueTask IListener<T>.HandleAsync(T package, CancellationToken cancellation)
		{
			var task = this.OnHandleAsync(package, cancellation);
			return task.IsCompletedSuccessfully ? ValueTask.CompletedTask : new ValueTask(task.AsTask());
		}
		ValueTask IReceiver.ReceiveAsync(in ReadOnlySequence<byte> data, CancellationToken cancellation) => this.OnReceiveAsync(data, cancellation);
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
