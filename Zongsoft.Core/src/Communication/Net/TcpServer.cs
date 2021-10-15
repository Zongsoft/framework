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
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Components;
using Zongsoft.Collections;

namespace Zongsoft.Communication.Net
{
	public class TcpServer<TPackage> : ListenerBase<TPackage>
	{
		#region 成员字段
		private Socket _socket;
		private readonly int _bufferSize;
		private TcpServerChannelManager<TPackage> _channels;
		#endregion

		#region 构造函数
		public TcpServer(int bufferSize = 1024) : this("TcpServer", bufferSize) { }
		public TcpServer(string name, int bufferSize = 1024) : base(name)
		{
			if(bufferSize > 64 * 1024 * 1024)
				throw new ArgumentNullException(nameof(bufferSize));

			_bufferSize = Math.Max(64, bufferSize);
		}
		#endregion

		#region 公共属性
		public TcpServerChannelManager<TPackage> Channels { get => _channels; }
		#endregion

		#region 保护属性
		protected Socket Socket { get => _socket; }
		#endregion

		#region 重写方法
		protected override void OnStart(string[] args)
		{
			var address = IPAddress.Any;
			var port = 7969;

			if(args != null || args.Length > 0 && args[0] != null)
			{
				var index = args[0].IndexOf(':');

				if(index > 0 && index < args[0].Length - 1)
				{
					if(IPAddress.TryParse(args[0].AsSpan(0, index), out var ip))
						address = ip;

					if(int.TryParse(args[0].AsSpan(index + 1), out var number) && number > 0)
						port = number;
				}
				else
				{
					if(IPAddress.TryParse(args[0], out var ip))
						address = ip;
				}
			}

			_socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			_socket.Bind(new IPEndPoint(address, port));
			_socket.Listen();

			_channels = new TcpServerChannelManager<TPackage>(_socket, this.Handler, this.Packetizer, _bufferSize);
			_channels.Accept();
		}

		protected override void OnStop(string[] args)
		{
			var manager = Interlocked.Exchange(ref _channels, null);

			if(manager != null)
				manager.Dispose();
		}
		#endregion
	}

	public class TcpServerChannelManager<TPackage> : IEnumerable<TcpServerChannel<TPackage>>, IDisposable
	{
		#region 成员字段
		private Socket _socket;
		private int _identifier;
		private readonly int _maximum;
		private readonly TcpServerChannelPool _pool;
		private readonly ConcurrentDictionary<int, TcpServerChannel<TPackage>> _channels;
		private readonly int _bufferSize;
		private readonly IHandler<TPackage> _handler;
		private readonly TcpPacketizer<TPackage> _packetizer;
		#endregion

		#region 构造函数
		public TcpServerChannelManager(Socket socket, IHandler<TPackage> handler, IPacketizer<TPackage> packetizer, int bufferSize, int maximum = 10000)
		{
			//单机连接数不能大于100万
			if(maximum > 100_0000)
				throw new ArgumentOutOfRangeException(nameof(maximum));

			_socket = socket;
			_handler = handler;
			_packetizer = new TcpPacketizer<TPackage>(packetizer);
			_maximum = Math.Max(100, maximum);
			_bufferSize = Math.Max(512, bufferSize);
			_pool = new TcpServerChannelPool(this, maximum);
			_channels = new ConcurrentDictionary<int, TcpServerChannel<TPackage>>();
		}
		#endregion

		#region 公共属性
		public int Count { get => _channels.Count; }
		public int Limit { get => _maximum; }

		public TcpServerChannel<TPackage> this[int identifier]
		{
			get => _channels.TryGetValue(identifier, out var value) ? value : null;
		}
		#endregion

		#region 接受连接
		public void Accept()
		{
			var argument = new SocketAsyncEventArgs();
			argument.Completed += new EventHandler<SocketAsyncEventArgs>(SocketAsyncEventArgs_Completed);
			this.Accept(argument);
		}

		private void Accept(SocketAsyncEventArgs argument)
		{
			argument.AcceptSocket = null;

			if(!_socket.AcceptAsync(argument))
				this.OnAccept(argument);
		}

		private void OnAccept(SocketAsyncEventArgs argument)
		{
			//从通道池中获取一个通道对象（如果满则堵塞），作为刚受理的通道
			var channel = _pool.Get();

			//将受理的通道对象加入到在线通道列表中
			_channels.TryAdd(channel.ChannelId, channel);

			//更新受理的通道的Soket对象
			channel.Accept(argument.AcceptSocket);

			//开启接收数据
			channel.Receive();

			//继续受理连接
			this.Accept(argument);
		}

		private void SocketAsyncEventArgs_Completed(object sender, SocketAsyncEventArgs args)
		{
			this.OnAccept(args);
		}
		#endregion

		#region 打包拆包
		internal protected virtual IList<ArraySegment<byte>> Pack(ReadOnlySpan<byte> data) => _packetizer.Pack(data);
		internal protected virtual IEnumerable<TPackage> Unpack(ReadOnlyMemory<byte> data) => _packetizer.Unpack(data);
		#endregion

		#region 数据处理
		internal protected virtual bool Handle(TcpServerChannel<TPackage> channel, TPackage package) => _handler.Handle(package);
		internal protected virtual Task<bool> HandleAsync(TcpServerChannel<TPackage> channel, TPackage package, CancellationToken cancellation) => _handler.HandleAsync(package, cancellation);
		#endregion

		#region 虚拟方法
		protected virtual TcpServerChannel<TPackage> CreateChannel()
		{
			var index = Interlocked.Increment(ref _identifier);
			return new TcpServerChannel<TPackage>(this, index + 1, this.GetBuffer(index, _bufferSize));
		}
		protected virtual Memory<byte> GetBuffer(int index, int size) => new byte[size];
		#endregion

		#region 处置关闭
		internal void CloseChannel(TcpServerChannel<TPackage> channel)
		{
			if(_channels.TryRemove(channel.ChannelId, out _))
				_pool.Return(channel);
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			var socket = Interlocked.Exchange(ref _socket, null);

			if(socket != null)
				socket.Dispose();

			_pool.Dispose();

			foreach(var id in _channels.Keys)
			{
				if(_channels.TryRemove(id, out var channel))
					channel.Close();
			}
		}
		#endregion

		#region 枚举遍历
		public IEnumerator<TcpServerChannel<TPackage>> GetEnumerator() => _channels.Values.GetEnumerator();
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => this.GetEnumerator();
		#endregion

		#region 嵌套子类
		private class TcpServerChannelPool : ObjectPool<TcpServerChannel<TPackage>>
		{
			private readonly TcpServerChannelManager<TPackage> _manager;

			public TcpServerChannelPool(TcpServerChannelManager<TPackage> manager, int limit) : base(limit)
			{
				_manager = manager;
			}

			protected override TcpServerChannel<TPackage> OnCreate() => _manager.CreateChannel();
		}
		#endregion
	}

	public class TcpServerChannel<TPackage> : ChannelBase
	{
		#region 成员字段
		private Socket _socket;
		private long _totalReceivedBytes;
		private long _totalReceivedPackages;
		private readonly SocketAsyncEventArgs _socketArgument;
		private readonly TcpServerChannelManager<TPackage> _manager;
		#endregion

		#region 构造函数
		public TcpServerChannel(TcpServerChannelManager<TPackage> manager, int channelId, Memory<byte> buffer) : base(channelId)
		{
			_manager = manager ?? throw new ArgumentNullException(nameof(manager));
			_socketArgument = new SocketAsyncEventArgs();
			_socketArgument.Completed += SocketAsyncEventArgs_Completed;
			_socketArgument.SetBuffer(buffer);
		}
		#endregion

		#region 公共属性
		public Socket Socket { get => _socket; }
		public SocketAsyncEventArgs SocketArgument { get => _socketArgument; }
		public override bool IsIdled => _socket == null;
		#endregion

		#region 受理连接
		internal void Accept(Socket socket)
		{
			_socket = socket ?? throw new ArgumentNullException(nameof(socket));

			if(!socket.ReceiveAsync(_socketArgument))
				this.OnReceived();
		}
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
				socket.Send(_manager.Pack(data));
		}

		public override Task SendAsync(ReadOnlySpan<byte> data, CancellationToken cancellation = default)
		{
			var socket = _socket;
			return socket.SendAsync(_manager.Pack(data), SocketFlags.None);
		}
		#endregion

		#region 接收处理
		protected override void OnReceive(ReadOnlySpan<byte> data)
		{
			var packages = _manager.Unpack(data.ToArray());

			if(packages != null)
			{
				foreach(var package in packages)
				{
					Interlocked.Increment(ref _totalReceivedPackages);
					_manager.Handle(this, package);
				}
			}
		}

		protected override Task OnReceiveAsync(ReadOnlySpan<byte> data, CancellationToken cancellation)
		{
			var packages = _manager.Unpack(data.ToArray());

			if(packages != null)
			{
				foreach(var package in packages)
				{
					Interlocked.Increment(ref _totalReceivedPackages);
					_manager.HandleAsync(this, package, cancellation);
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
			_manager.CloseChannel(this);
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
