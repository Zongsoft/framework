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

namespace Zongsoft.Net
{
	public static class TcpServer
	{
		public static readonly TcpServer<IMemoryOwner<byte>> Headless = new HeadlessServer();
		public static readonly TcpServer<ReadOnlySequence<byte>> Headed = new HeadedServer();

		private class HeadlessServer : TcpServer<IMemoryOwner<byte>>, ISender
		{
			public HeadlessServer() : base(HeadlessPacketizer.Instance) => this.Address = new IPEndPoint(IPAddress.Loopback, 7969);
			public void Send(ReadOnlySpan<byte> data) => this.SendAsync(data.ToArray());
			public ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellation = default)
			{
				var task = base.BroadcastAsync(data, cancellation);

				if(task.IsCompletedSuccessfully)
				{
					task.GetAwaiter().GetResult();
					return ValueTask.CompletedTask;
				}

				return new ValueTask(task.AsTask());
			}
		}

		private class HeadedServer : TcpServer<ReadOnlySequence<byte>>, ISender
		{
			public HeadedServer() : base(HeadedPacketizer.Instance) => this.Address = new IPEndPoint(IPAddress.Loopback, 7969);
			public void Send(ReadOnlySpan<byte> data) => this.SendAsync(data.ToArray());
			public ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellation = default)
			{
				var task = base.BroadcastAsync(data, cancellation);

				if(task.IsCompletedSuccessfully)
				{
					task.GetAwaiter().GetResult();
					return ValueTask.CompletedTask;
				}

				return new ValueTask(task.AsTask());
			}

			internal protected override TcpServerChannel<ReadOnlySequence<byte>> CreateChannel(IDuplexPipe transport, IPEndPoint address) => new SizedChannel(this.Channels, transport, address);

			private class SizedChannel : TcpServerChannel<ReadOnlySequence<byte>>
			{
				public SizedChannel(TcpServerChannelManager<ReadOnlySequence<byte>> manager, IDuplexPipe transport, IPEndPoint address) : base(manager, transport, address) { }
				protected override ValueTask<FlushResult> OnSendAsync(PipeWriter writer, ReadOnlyMemory<byte> data, CancellationToken cancellation)
				{
					return HeadedPacketizer.Instance.PackAsync(writer, new ReadOnlySequence<byte>(data), cancellation);
				}
			}
		}
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
		public override IPacketizer<T> Packetizer { get => _packetizer; }
		public TcpServerChannelManager<T> Channels { get => _channels; }
		#endregion

		#region 接受方法
		public Task AcceptAsync(IDuplexPipe transport, IPEndPoint address, CancellationToken cancellation = default)
		{
			if(transport == null)
				throw new ArgumentNullException(nameof(transport));

			var channel = this.CreateChannel(transport, address);
			_channels.Add(channel);
			return channel.ReceiveAsync(cancellation);
		}

		internal protected virtual TcpServerChannel<T> CreateChannel(IDuplexPipe transport, IPEndPoint address) => new TcpServerChannel<T>(_channels, transport, address);
		#endregion

		#region 广播方法
		public async ValueTask<int> BroadcastAsync(T package, CancellationToken cancellation = default)
		{
			int count = 0;

			foreach(var client in _channels)
			{
				try
				{
					await client.SendAsync(package, cancellation);
					count++;
				}
				catch { }
			}

			return count;
		}

		protected async ValueTask<int> BroadcastAsync(ReadOnlyMemory<byte> data, CancellationToken cancellation = default)
		{
			int count = 0;

			foreach(var client in _channels)
			{
				try
				{
					await client.SendAsync(data, cancellation);
					count++;
				}
				catch { }
			}

			return count;
		}
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
}
