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

using Zongsoft.Components;
using Zongsoft.Communication;

using Pipelines.Sockets.Unofficial;

namespace Zongsoft.Net
{
	public static class TcpClient
	{
		public static readonly TcpClient<IMemoryOwner<byte>> Headless = new HeadlessClient();
		public static readonly TcpClient<ReadOnlySequence<byte>> Headed = new HeadedClient();

		private class HeadlessClient : TcpClient<IMemoryOwner<byte>>, ISender
		{
			public HeadlessClient() : base(HeadlessPacketizer.Instance) => this.Address = new IPEndPoint(IPAddress.Loopback, 7969);
			public void Send(ReadOnlySpan<byte> data) => this.SendAsync(data.ToArray());
			public new ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellation = default) => base.SendAsync(data, cancellation);
		}

		private class HeadedClient : TcpClient<ReadOnlySequence<byte>>, ISender
		{
			public HeadedClient() : base(HeadedPacketizer.Instance) => this.Address = new IPEndPoint(IPAddress.Loopback, 7969);
			public void Send(ReadOnlySpan<byte> data) => this.SendAsync(data.ToArray());
			public new ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellation = default) => base.SendAsync(data, cancellation);

			protected override TcpClientChannel<ReadOnlySequence<byte>> CreateChannel(SocketConnection connection, EndPoint address) => new SizedChannel(this, connection, address);

			private class SizedChannel(TcpClient<ReadOnlySequence<byte>> client, SocketConnection connection, EndPoint address) : TcpClientChannel<ReadOnlySequence<byte>>(client, connection, address)
			{
				protected override ValueTask<FlushResult> OnSendAsync(PipeWriter writer, ReadOnlyMemory<byte> data, CancellationToken cancellation)
				{
					return HeadedPacketizer.Instance.PackAsync(writer, new ReadOnlySequence<byte>(data), cancellation);
				}
			}
		}
	}

	public class TcpClient<T> : IHandleable<T>, ISender<T>
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
		public IHandler<T> Handler { get; set; }
		public IPacketizer<T> Packetizer { get; }
		public long TotalBytesSent => _channel?.TotalBytesSent ?? 0;
		public long TotalBytesReceived => _channel?.TotalBytesReceived ?? 0;
		IHandler IHandleable.Handler
		{
			get => this.Handler;
			set => this.Handler = value as IHandler<T> ?? throw new ArgumentException($"The specified ‘{value}’ handler does not match.");
		}
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

				await _channel.DisposeAsync();
			}

			_channel = this.CreateChannel(await SocketConnection.ConnectAsync(address), address);
		}

		public async void DisconnectAsync(CancellationToken cancellation = default)
		{
			var channel = _channel;

			if(channel != null && !channel.IsClosed)
				await channel.CloseAsync(cancellation);
		}

		protected virtual TcpClientChannel<T> CreateChannel(SocketConnection connection, EndPoint address) => new(this, connection, address);
		#endregion

		#region 发送方法
		public void Send(in T package) => this.SendAsync(package).ConfigureAwait(false);
		public async ValueTask SendAsync(T package, CancellationToken cancellation = default)
		{
			await this.ConnectAsync();
			await _channel.SendAsync(package, cancellation);
		}
		protected async ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellation = default)
		{
			await this.ConnectAsync();
			await _channel.SendAsync(data, cancellation);
		}
		#endregion

		#region 接收处理
		internal async ValueTask OnHandleAsync(T payload, CancellationToken cancellation = default)
		{
			var handler = this.Handler;

			if(handler != null)
				await handler.HandleAsync(payload, cancellation);
		}
		#endregion
	}
}
