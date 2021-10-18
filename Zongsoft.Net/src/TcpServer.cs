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
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Zongsoft.Communication;

using Pipelines.Sockets.Unofficial;

namespace Zongsoft.Net
{
	public class TcpServer : TcpServer<ReadOnlySequence<byte>>
	{
		public TcpServer() : base(nameof(TcpServer),  TcpPacketizer.Instance) { }
		public TcpServer(string name) : base(name, TcpPacketizer.Instance) { }
	}

	public class TcpServer<T> : ListenerBase<T>
	{
		#region 成员字段
		private IPacketizer<T> _packetizer;
		private TcpServerChannelManager<T> _channels;
		#endregion

		#region 构造函数
		public TcpServer(IPacketizer<T> packetizer) : this(nameof(TcpServer<T>), packetizer) { }
		public TcpServer(string name, IPacketizer<T> packetizer) : base(name)
		{
			_packetizer = packetizer ?? throw new ArgumentNullException(nameof(packetizer));
			_channels = new TcpServerChannelManager<T>(this);
		}
		#endregion

		#region 公共属性
		public IPEndPoint Address { get; set; }
		public new IPacketizer<T> Packetizer { get => _packetizer; }
		public TcpServerChannelManager<T> Channels { get => _channels; }
		#endregion

		#region 重写方法
		protected override void OnStart(string[] args)
		{
			var channels = _channels;

			if(channels == null)
				throw new ObjectDisposedException(this.Name);

			int port = 7969, value;
			var address = IPAddress.Any;

			if(args != null && args.Length > 0)
			{
				switch(args.Length)
				{
					case 1:
						if(int.TryParse(args[0], out value))
							port = value;

						break;
					case 2:
						if(int.TryParse(args[1], out value))
							port = value;

						if(!string.IsNullOrWhiteSpace(args[0]) && IPAddress.TryParse(args[0], out var ip))
							address = ip;

						break;
				}
			}

			channels.Listen(this.Address = new IPEndPoint(address, port));
		}

		protected override void OnStop(string[] args)
		{
			_channels?.Stop();
		}
		#endregion

		#region 处置方法
		protected override void Dispose(bool disposing)
		{
			var channels = Interlocked.Exchange(ref _channels, null);

			if(channels != null)
				channels.Dispose();
		}
		#endregion
	}

	public class TcpServerChannelManager<T> : SocketServer, IReadOnlyCollection<TcpServerChannel<T>>, IDisposable
	{
		#region 成员字段
		private readonly ConcurrentBag<TcpServerChannel<T>> _channels;
		#endregion

		#region 构造函数
		public TcpServerChannelManager(TcpServer<T> server)
		{
			this.Server = server ?? throw new ArgumentNullException(nameof(server));
			_channels = new ConcurrentBag<TcpServerChannel<T>>();
		}
		#endregion

		#region 公共属性
		public TcpServer<T> Server { get; }
		public int Count => _channels.Count;
		#endregion

		#region 连接受理
		protected override Task OnClientConnectedAsync(in ClientConnection client)
		{
			var channel = new TcpServerChannel<T>(this, client.Transport, client.RemoteEndPoint as IPEndPoint);
			return channel.AcceptAsync(CancellationToken.None);
		}
		#endregion

		#region 数据处理
		internal void Pack(PipeWriter writer, in ReadOnlyMemory<byte> data) => this.Server.Packetizer.Pack(writer, new ReadOnlySequence<byte>(data));
		internal bool Unpack(ref ReadOnlySequence<byte> data, out T package) => this.Server.Packetizer.Unpack(ref data, out package);
		internal Task<bool> HandleAsync(in T package, CancellationToken cancellation) => this.Server.Handler.HandleAsync(package, cancellation);
		#endregion

		#region 内部方法
		internal void Add(TcpServerChannel<T> channel) { if(channel != null) _channels.Add(channel); }
		internal bool Remove(TcpServerChannel<T> channel) => channel != null && _channels.TryTake(out _);
		#endregion

		#region 枚举遍历
		public IEnumerator<TcpServerChannel<T>> GetEnumerator() => _channels.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => _channels.GetEnumerator();
		#endregion

		#region 处置方法
		protected override void Dispose(bool disposing)
		{
			foreach(var channel in _channels)
				channel.Dispose();

			_channels.Clear();
			base.Dispose(disposing);
		}
		#endregion
	}

	public class TcpServerChannel<T> : TcpChannelBase<T>, IEquatable<TcpServerChannel<T>>
	{
		#region 成员字段
		private readonly TcpServerChannelManager<T> _manager;
		#endregion

		#region 构造函数
		public TcpServerChannel(TcpServerChannelManager<T> manager, IDuplexPipe pipe, IPEndPoint address) : base(pipe, address)
		{
			_manager = manager ?? throw new ArgumentNullException(nameof(manager));
		}
		#endregion

		#region 开始接收
		public Task AcceptAsync(CancellationToken cancellationToken) => this.StartReceiveLoopAsync(cancellationToken);
		#endregion

		#region 发送方法
		public ValueTask SendAsync(IMemoryOwner<byte> data, CancellationToken cancellation = default) => this.WriteAsync(data, cancellation);
		public ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellation = default) => this.WriteAsync(data, cancellation);
		#endregion

		#region 接收数据
		protected override ValueTask PackAsync(PipeWriter writer, in ReadOnlyMemory<byte> data, CancellationToken cancellation)
		{
			_manager.Pack(writer, data);
			return ValueTask.CompletedTask;
		}

		protected override bool TryUnpack(ref ReadOnlySequence<byte> data, out T package)
		{
			return _manager.Unpack(ref data, out package);
		}

		protected sealed override ValueTask OnReceiveAsync(in T payload)
		{
			static void DisposeOnCompletion(Task task, in T message)
			{
				task.ContinueWith((t, s) => ((IMemoryOwner<byte>)s)?.Dispose(), message);
			}

			try
			{
				var pendingAction = _manager.HandleAsync(payload, CancellationToken.None);

				if(!pendingAction.IsCompletedSuccessfully )
					DisposeOnCompletion(pendingAction, in payload);
			}
			finally
			{
				if(payload is IDisposable disposable)
					disposable.Dispose();
			}

			return default;
		}

		protected override ValueTask OnStartReceiveLoopAsync()
		{
			_manager.Add(this);
			return ValueTask.CompletedTask;
		}

		protected override ValueTask OnEndReceiveLoopAsync()
		{
			_manager.Remove(this);
			return ValueTask.CompletedTask;
		}
		#endregion

		#region 重写方法
		public bool Equals(TcpServerChannel<T> channel) => channel != null && this.Address.Equals(channel.Address);
		public override bool Equals(object obj) => obj is TcpServerChannel<T> channel && this.Equals(channel);
		public override int GetHashCode() => this.Address.GetHashCode();
		public override string ToString() => this.Address.ToString();
		#endregion
	}
}
