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
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Pipelines.Sockets.Unofficial;

namespace Zongsoft.Net;

public class TcpServerChannelManager<T> : SocketServer, IReadOnlyCollection<TcpServerChannel<T>>
{
	#region 成员字段
	private readonly ConcurrentDictionary<IPEndPoint, ChannelEntry> _channels;
	#endregion

	#region 构造函数
	public TcpServerChannelManager(TcpServer<T> server)
	{
		this.Server = server ?? throw new ArgumentNullException(nameof(server));
		_channels = new ConcurrentDictionary<IPEndPoint, ChannelEntry>();
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
	internal void Pack(PipeWriter writer, in T package) => this.Server.Packetizer.Pack(writer, package);
	internal bool Unpack(ref ReadOnlySequence<byte> data, out T package) => this.Server.Packetizer.Unpack(ref data, out package);
	internal ValueTask HandleAsync(TcpServerChannel<T> channel, in T package, CancellationToken cancellation) => this.Server.Handler?.HandleAsync(package, cancellation) ?? ValueTask.FromCanceled(cancellation);
	#endregion

	#region 内部方法
	internal void Add(TcpServerChannel<T> channel)
	{
		if(channel?.Address is IPEndPoint address)
			_channels.TryAdd(address, new ChannelEntry(channel));
	}

	internal bool Remove(TcpServerChannel<T> channel)
	{
		if(channel?.Address is not IPEndPoint address ||
		   !_channels.TryGetValue(address, out var entry) ||
		   !ReferenceEquals(entry.Channel, channel))
			return false;

		return this.TryRemove(address, entry);
	}

	private bool TryRemove(IPEndPoint address, ChannelEntry entry) =>
		((ICollection<KeyValuePair<IPEndPoint, ChannelEntry>>)_channels).Remove(new(address, entry));
	#endregion

	#region 枚举遍历
	IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
	public IEnumerator<TcpServerChannel<T>> GetEnumerator()
	{
		foreach(var entry in _channels.Values)
			yield return entry.Channel;
	}
	#endregion

	#region 处置方法
	protected override void Dispose(bool disposing)
	{
		foreach(var entry in _channels)
		{
			if(this.TryRemove(entry.Key, entry.Value))
				DisposeAsync(entry.Value.Channel);
		}

		base.Dispose(disposing);

		static async void DisposeAsync(TcpServerChannel<T> channel)
		{
			if(channel == null || channel.IsDisposed)
				return;

			try
			{
				await channel.DisposeAsync();
			}
			catch { }
		}
	}
	#endregion

	#region 嵌套类型
	private sealed class ChannelEntry(TcpServerChannel<T> channel)
	{
		public readonly TcpServerChannel<T> Channel = channel;
	}
	#endregion
}
