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
 * This file is part of Zongsoft.Net library.
 *
 * The Zongsoft.Net is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Net is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Net library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Net;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Net
{
	public class TcpServerChannel<T> : TcpChannelBase<T>, IEquatable<TcpServerChannel<T>>
	{
		#region 成员字段
		private readonly TcpServerChannelManager<T> _manager;
		#endregion

		#region 构造函数
		public TcpServerChannel(TcpServerChannelManager<T> manager, IDuplexPipe transport, IPEndPoint address) : base(transport, address)
		{
			_manager = manager ?? throw new ArgumentNullException(nameof(manager));
		}
		#endregion

		#region 保护属性
		protected TcpServerChannelManager<T> Manager { get => _manager; }
		#endregion

		#region 开启接收
		public new Task ReceiveAsync(CancellationToken cancellationToken = default) => base.ReceiveAsync(cancellationToken);
		#endregion

		#region 协议解析
		protected override ValueTask PackAsync(PipeWriter writer, in T package, CancellationToken cancellation)
		{
			_manager.PackAsync(writer, package, cancellation);
			return ValueTask.CompletedTask;
		}

		protected override bool Unpack(ref ReadOnlySequence<byte> data, out T package)
		{
			return _manager.Unpack(ref data, out package);
		}
		#endregion

		#region 接收数据
		protected override void OnReceiving() => _manager.Add(this);
		protected sealed override ValueTask OnReceiveAsync(in T package)
		{
			static void DisposeOnCompletion(ValueTask task, in T message)
			{
				if(message is IDisposable)
					task.AsTask().ContinueWith((t, m) => ((IDisposable)m)?.Dispose(), message);
			}

			try
			{
				var pendingAction = _manager.HandleAsync(this, package, CancellationToken.None);

				if(!pendingAction.IsCompletedSuccessfully)
					DisposeOnCompletion(pendingAction, in package);
			}
			finally
			{
				if(package is IDisposable disposable)
					disposable.Dispose();
			}

			return default;
		}
		#endregion

		#region 发送方法
		internal ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellation = default) => base.SendAsync(data, cancellation);
		#endregion

		#region 关闭处理
		protected override void OnClosed() => _manager.Remove(this);
		#endregion

		#region 重写方法
		public bool Equals(TcpServerChannel<T> channel) => channel != null && this.Address.Equals(channel.Address);
		public override bool Equals(object obj) => obj is TcpServerChannel<T> channel && this.Equals(channel);
		public override int GetHashCode() => this.Address.GetHashCode();
		public override string ToString() => this.Address.ToString();
		#endregion
	}
}
