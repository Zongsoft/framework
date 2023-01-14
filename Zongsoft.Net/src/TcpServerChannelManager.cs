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

using Pipelines.Sockets.Unofficial;

namespace Zongsoft.Net
{
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
			var channel = this.Server.CreateChannel(client.Transport, client.RemoteEndPoint as IPEndPoint);
			return channel.ReceiveAsync(CancellationToken.None);
		}
		#endregion

		#region 数据处理
		internal ValueTask PackAsync(PipeWriter writer, in T package, CancellationToken cancellation) => this.Server.Packetizer.PackAsync(writer, package, cancellation);
		internal bool Unpack(ref ReadOnlySequence<byte> data, out T package) => this.Server.Packetizer.Unpack(ref data, out package);
		internal ValueTask<object> HandleAsync(TcpServerChannel<T> channel, in T package, CancellationToken cancellation) => this.Server.Handler?.HandleAsync(channel, package, cancellation) ?? ValueTask.FromCanceled<object>(cancellation);
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
}
