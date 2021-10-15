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
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Zongsoft.Communication.Net
{
	public class TcpClient<TPackage> : ISender
	{
		#region 成员变量
		private Socket _socket;
		private SocketAsyncEventArgs _socketArgument;
		private TcpPacketizer<TPackage> _packetizer;
		private TcpClientChannel<TPackage> _channel;
		private List<ArraySegment<byte>> _cache;
		#endregion

		#region 构造函数
		public TcpClient()
		{
			_cache = new List<ArraySegment<byte>>();
			_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			_socketArgument = new SocketAsyncEventArgs()
			{
				RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, 7969),
			};
			_socketArgument.Completed += SocketAsyncEventArgs_Completed;
		}
		#endregion

		#region 公共属性
		public TcpClientChannel<TPackage> Channel { get => _channel; }
		public Components.IHandler<TPackage> Handler { get; set; }
		public IPacketizer<TPackage> Packetizer { get => _packetizer?.Packetizer; set => _packetizer = new TcpPacketizer<TPackage>(value); }
		public EndPoint Address { get => _socketArgument.RemoteEndPoint; set => _socketArgument.RemoteEndPoint = value; }
		#endregion

		#region 保护属性
		protected Socket Socket { get => _socket; }
		#endregion

		#region 连接处理
		private bool Connect()
		{
			if(_socket.Connected && _channel != null)
				return true;

			if(!_socket.ConnectAsync(_socketArgument))
			{
				this.OnConnect(_socketArgument);
				return true;
			}

			return false;
		}

		private void OnConnect(SocketAsyncEventArgs argument)
		{
			var channel = Interlocked.Exchange(ref _channel, null);

			if(channel != null)
				channel.Dispose();

			channel = _channel = new TcpClientChannel<TPackage>(this, argument.ConnectSocket, 64);

			if(_cache != null && _cache.Count > 0)
			{
				foreach(var data in _cache)
					channel.Send(data);
			}

			//开启接收数据
			channel.Receive();
		}

		private void SocketAsyncEventArgs_Completed(object sender, SocketAsyncEventArgs args)
		{
			this.OnConnect(args);
		}
		#endregion

		#region 打包拆包
		internal protected virtual IList<ArraySegment<byte>> Pack(ReadOnlySpan<byte> data) => _packetizer.Pack(data);
		internal protected virtual IEnumerable<TPackage> Unpack(ReadOnlyMemory<byte> data) => _packetizer.Unpack(data);
		#endregion

		#region 数据处理
		internal protected virtual bool Handle(TcpClientChannel<TPackage> channel, TPackage package) => this.Handler?.Handle(package) ?? false;
		internal protected virtual Task<bool> HandleAsync(TcpClientChannel<TPackage> channel, TPackage package, CancellationToken cancellation) => this.Handler?.HandleAsync(package, cancellation) ?? Task.FromResult(false);
		#endregion

		#region 发送方法
		public void Send(ReadOnlySpan<byte> data)
		{
			if(this.Connect())
				_channel.Send(data);
			else
				_cache.Add(data.ToArray());
		}

		public Task SendAsync(ReadOnlySpan<byte> data, CancellationToken cancellation = default)
		{
			if(this.Connect())
				return _channel.SendAsync(data, cancellation);

			_cache.Add(data.ToArray());
			return Task.CompletedTask;
		}
		#endregion

		#region 私有方法
		internal void CloseChannel() => _channel = null;
		#endregion
	}

	public class TcpClientChannel<TPackage> : ChannelBase
	{
		#region 成员字段
		private Socket _socket;
		private long _totalReceivedBytes;
		private long _totalReceivedPackages;
		private readonly SocketAsyncEventArgs _socketArgument;
		private readonly TcpClient<TPackage> _client;
		#endregion

		#region 构造函数
		public TcpClientChannel(TcpClient<TPackage> client, Socket socket, int bufferSize) : base(1)
		{
			_client = client ?? throw new ArgumentNullException(nameof(client));
			_socket = socket ?? throw new ArgumentNullException(nameof(socket));

			_socketArgument = new SocketAsyncEventArgs();
			_socketArgument.Completed += SocketAsyncEventArgs_Completed;
			_socketArgument.SetBuffer(new byte[bufferSize]);
		}
		#endregion

		#region 公共属性
		public Socket Socket { get => _socket; }
		public SocketAsyncEventArgs SocketArgument { get => _socketArgument; }
		public override bool IsIdled => _socket == null;
		#endregion

		#region 接收数据
		public void Receive()
		{
			if(!_socket.ReceiveAsync(_socketArgument))
				this.OnReceived();
		}

		private void OnReceived()
		{
			var argument = _socketArgument;

			if(argument.BytesTransferred > 0 && argument.SocketError == SocketError.Success)
			{
				Interlocked.Add(ref _totalReceivedBytes, argument.BytesTransferred);
				this.OnReceive(argument.MemoryBuffer.Span);
				this.Receive();
			}
			else
			{
				this.Close();
			}
		}
		#endregion

		#region 发送完成
		private void OnSent()
		{
		}
		#endregion

		#region 收发事件
		private void SocketAsyncEventArgs_Completed(object sender, SocketAsyncEventArgs args)
		{
			switch(args.LastOperation)
			{
				case SocketAsyncOperation.Receive:
					this.OnReceived();
					break;
				case SocketAsyncOperation.Send:
					this.OnSent();
					break;
				default:
					throw new ArgumentException("The last operation completed on the socket was not a receive or send.");
			}
		}
		#endregion

		#region 发送方法
		public override void Send(ReadOnlySpan<byte> data)
		{
			var socket = _socket;

			if(socket != null)
				socket.Send(_client.Pack(data));
		}

		public override Task SendAsync(ReadOnlySpan<byte> data, CancellationToken cancellation = default)
		{
			var socket = _socket;
			return socket.SendAsync(_client.Pack(data), SocketFlags.None);
		}
		#endregion

		#region 接收处理
		protected override void OnReceive(ReadOnlySpan<byte> data)
		{
			var packages = _client.Unpack(data.ToArray());

			if(packages != null)
			{
				foreach(var package in packages)
				{
					Interlocked.Increment(ref _totalReceivedPackages);
					_client.Handle(this, package);
				}
			}
		}

		protected override Task OnReceiveAsync(ReadOnlySpan<byte> data, CancellationToken cancellation)
		{
			var packages = _client.Unpack(data.ToArray());

			if(packages != null)
			{
				foreach(var package in packages)
				{
					Interlocked.Increment(ref _totalReceivedPackages);
					_client.HandleAsync(this, package, cancellation);
				}
			}

			return Task.CompletedTask;
		}
		#endregion

		#region 通道关闭
		protected override void OnClose()
		{
			var socket = Interlocked.Exchange(ref _socket, null);

			if(socket == null)
				return;

			CloseSocket(socket);
			_client.CloseChannel();
		}

		internal static void CloseSocket(Socket socket)
		{
			if(socket == null)
				return;

			try
			{
				socket.Shutdown(SocketShutdown.Send);
			}
			catch { }

			socket.Close();
		}
		#endregion
	}
}
