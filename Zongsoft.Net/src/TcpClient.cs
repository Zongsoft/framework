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

using Zongsoft.Communication;

using Pipelines.Sockets.Unofficial;

namespace Zongsoft.Net
{
	public class TcpClient : TcpClient<ReadOnlySequence<byte>>
	{
		public TcpClient() : base(TcpPacketizer.Instance) => this.Address = new IPEndPoint(IPAddress.Loopback, 7969);

		public ValueTask SendAsync(string text, CancellationToken cancellation = default) => this.SendAsync(text, null, cancellation);
		public async ValueTask SendAsync(string text, System.Text.Encoding encoding, CancellationToken cancellation = default)
		{
			await this.ConnectAsync();
			var buffer = Zongsoft.Common.Buffer.Encode(text, encoding);

			try
			{
				await this.SendAsync(new ReadOnlySequence<byte>(buffer.Memory), cancellation);
			}
			finally { buffer?.Dispose(); }
		}
	}

	public class TcpClient<T>
	{
		#region 成员字段
		private TcpClientChannel<T> _channel;
		#endregion

		#region 构造函数
		public TcpClient(IPacketizer<T> packetizer)
		{
			this.Packetizer = packetizer ?? throw new ArgumentNullException(nameof(packetizer));
		}
		#endregion

		#region 公共属性
		public EndPoint Address { get; set; }
		public Components.IHandler<T> Handler { get; set; }
		public IPacketizer<T> Packetizer { get; }
		public long TotalBytesSent { get => _channel?.TotalBytesSent ?? 0; }
		public long TotalBytesReceived { get => _channel?.TotalBytesReceived ?? 0; }
		#endregion

		#region 连接方法
		public async Task ConnectAsync(EndPoint address = null)
		{
			if(address == null)
				address = this.Address ?? throw new InvalidOperationException("The destination address to connect to is not specified.");

			if(_channel != null)
			{
				if(address.Equals(_channel.Address) && !_channel.IsClosed)
					return;

				_channel.Dispose();
			}

			_channel = new TcpClientChannel<T>(this, await SocketConnection.ConnectAsync(address), address);
		}

		public void Disconnect()
		{
			var channel = _channel;

			if(channel != null && !channel.IsClosed)
				channel.Close();
		}
		#endregion

		#region 发送方法
		public void Send(in T package) => this.SendAsync(package).ConfigureAwait(false);
		public async ValueTask SendAsync(T package, CancellationToken cancellation = default)
		{
			await this.ConnectAsync();
			await _channel.SendAsync(package, cancellation);
		}
		#endregion

		#region 接收处理
		internal Task OnHandleAsync(in T payload, CancellationToken cancellation = default)
		{
			var handler = this.Handler;

			if(handler != null)
				return handler.HandleAsync(payload, cancellation);

			return Task.CompletedTask;
		}
		#endregion
	}

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
		protected sealed override ValueTask OnReceiveAsync(in T payload)
		{
			static void DisposeOnCompletion(Task task, in T message)
			{
				task.ContinueWith((t, m) => ((IMemoryOwner<byte>)m)?.Dispose(), message);
			}

			try
			{
				var pendingAction = _client.OnHandleAsync(payload, CancellationToken.None);

				if(!pendingAction.IsCompletedSuccessfully)
					DisposeOnCompletion(pendingAction, in payload);
			}
			finally
			{
				if(payload is IDisposable disposable)
					disposable.Dispose();
			}

			return default;
		}
		#endregion
	}
}
