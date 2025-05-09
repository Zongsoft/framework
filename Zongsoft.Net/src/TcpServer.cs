﻿/*
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
			public void Send(ReadOnlySpan<byte> data) => this.SendAsync(data.ToArray()).AsTask().GetAwaiter().GetResult();
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
			public void Send(ReadOnlySpan<byte> data) => this.SendAsync(data.ToArray()).AsTask().GetAwaiter().GetResult();
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

			private class SizedChannel(TcpServerChannelManager<ReadOnlySequence<byte>> manager, IDuplexPipe transport, IPEndPoint address) : TcpServerChannel<ReadOnlySequence<byte>>(manager, transport, address)
			{
				protected override ValueTask<FlushResult> OnSendAsync(PipeWriter writer, ReadOnlyMemory<byte> data, CancellationToken cancellation)
				{
					return HeadedPacketizer.Instance.PackAsync(writer, new ReadOnlySequence<byte>(data), cancellation);
				}
			}
		}
	}

	public class TcpServer<T> : ListenerBase<T>, ISender<T>
	{
		#region 成员字段
		private readonly IPacketizer<T> _packetizer;
		private TcpServerChannelManager<T> _channels;
		#endregion

		#region 构造函数
		public TcpServer(IPacketizer<T> packetizer) : this(nameof(TcpServer<T>), packetizer) { }
		public TcpServer(string name, IPacketizer<T> packetizer) : base(name)
		{
			_packetizer = packetizer ?? throw new ArgumentNullException(nameof(packetizer));
			_channels = this.CreateChannels();
		}
		#endregion

		#region 公共属性
		public IPEndPoint Address { get; set; }
		public override IPacketizer<T> Packetizer => _packetizer;
		public TcpServerChannelManager<T> Channels => _channels;
		#endregion

		#region 接受方法
		public Task AcceptAsync(IDuplexPipe transport, IPEndPoint address, CancellationToken cancellation = default)
		{
			if(transport == null)
				throw new ArgumentNullException(nameof(transport));

			var channel = this.CreateChannel(transport, address);
			return channel.ReceiveAsync(cancellation);
		}
		#endregion

		#region 广播方法
		public async ValueTask<int> BroadcastAsync(T package, CancellationToken cancellation = default)
		{
			int count = 0;

			foreach(var channel in _channels)
			{
				try
				{
					await channel.SendAsync(package, cancellation);
					count++;
				}
				catch { }
			}

			return count;
		}

		protected async ValueTask<int> BroadcastAsync(ReadOnlyMemory<byte> data, CancellationToken cancellation = default)
		{
			int count = 0;

			foreach(var channel in _channels)
			{
				try
				{
					await channel.SendAsync(data, cancellation);
					count++;
				}
				catch { }
			}

			return count;
		}

		async ValueTask ISender<T>.SendAsync(T package, CancellationToken cancellation)
		{
			foreach(var channel in _channels)
			{
				try
				{
					await channel.SendAsync(package, cancellation);
				}
				catch { }
			}
		}
		#endregion

		#region 虚拟方法
		protected virtual TcpServerChannelManager<T> CreateChannels() => new(this);
		internal protected virtual TcpServerChannel<T> CreateChannel(IDuplexPipe transport, IPEndPoint address) => new(_channels, transport, address);
		#endregion

		#region 重写方法
		protected override Task OnStartAsync(string[] args, CancellationToken cancellation)
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

			//返回完成任务
			return Task.CompletedTask;
		}

		protected override Task OnStopAsync(string[] args, CancellationToken cancellation)
		{
			_channels?.Stop();
			return Task.CompletedTask;
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
