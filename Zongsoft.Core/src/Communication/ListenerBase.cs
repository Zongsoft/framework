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
		public virtual bool IsListening
		{
			get => this.State == WorkerState.Running;
		}

		public virtual IProtocolResolver<T> Resolver { get; }

		public IHandler<T> Handler
		{
			get => _handler;
			set => _handler = value ?? throw new ArgumentNullException();
		}
		#endregion

		#region 虚拟方法
		protected virtual bool OnReceive(ReadOnlySpan<byte> data, object parameter) => this.TryResolve(data, parameter, out var value) && this.OnHandle(value);
		protected virtual Task<bool> OnReceiveAsync(ReadOnlySpan<byte> data, object parameter, CancellationToken cancellation) => this.TryResolve(data, parameter, out var value) ? this.OnHandleAsync(value, cancellation) : Task.FromResult(false);
		protected virtual bool OnHandle(T package) => this.Handler.Handle(package);
		protected virtual Task<bool> OnHandleAsync(T package, CancellationToken cancellation) => this.Handler.HandleAsync(package, cancellation);
		#endregion

		#region 协议转换
		protected virtual bool TryResolve(ReadOnlySpan<byte> data, object parameter, out T result)
		{
			var resolver = this.Resolver;

			if(resolver == null)
			{
				result = default;
				return false;
			}

			return resolver.TryResolve(data, parameter, out result);
		}
		#endregion

		#region 显式实现
		void IListener<T>.Handle(T package) => this.OnHandle(package);
		Task IListener<T>.HandleAsync(T package, CancellationToken cancellation) => this.OnHandleAsync(package, cancellation);
		bool IReceiver.Receive(ReadOnlySpan<byte> data, object parameter) => this.OnReceive(data, parameter);
		Task<bool> IReceiver.ReceiveAsync(ReadOnlySpan<byte> data, object parameter, CancellationToken cancellation) => this.OnReceiveAsync(data, parameter, cancellation);
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
