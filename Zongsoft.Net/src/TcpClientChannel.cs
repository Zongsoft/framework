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

using Pipelines.Sockets.Unofficial;

namespace Zongsoft.Net
{
	public class TcpClientChannel<T> : TcpChannelBase<T>
	{
		#region 成员字段
		private readonly TcpClient<T> _client;
		#endregion

		#region 构造函数
		public TcpClientChannel(TcpClient<T> client, SocketConnection connection, EndPoint address) : base(connection, address)
		{
			_client = client ?? throw new ArgumentNullException(nameof(client));

			this.ReceiveAsync()
				.ContinueWith(task => GC.KeepAlive(task.Exception), TaskContinuationOptions.OnlyOnFaulted);
		}
		#endregion

		#region 公共属性
		public TcpClient<T> Client { get => _client; }
		#endregion

		#region 协议解析
		protected override ValueTask PackAsync(PipeWriter writer, in T package, CancellationToken cancellation)
		{
			return _client.Packetizer.PackAsync(writer, package, cancellation);
		}

		protected override bool Unpack(ref ReadOnlySequence<byte> data, out T package)
		{
			return _client.Packetizer.Unpack(ref data, out package);
		}
		#endregion

		#region 接收数据
		protected sealed override ValueTask OnReceiveAsync(in T package)
		{
			static void DisposeOnCompletion(ValueTask task, in T message)
			{
				if(message is IDisposable)
					task.AsTask().ContinueWith((t, m) => ((IDisposable)m)?.Dispose(), message);
			}

			try
			{
				var pendingAction = _client.OnHandleAsync(package, CancellationToken.None);

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
	}
}
