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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Messaging.Mqtt library.
 *
 * The Zongsoft.Messaging.Mqtt is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Messaging.Mqtt is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Messaging.Mqtt library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Net;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using MQTTnet.Server;
using MQTTnet.Formatter;

using Zongsoft.Communication;

namespace Zongsoft.Messaging.Mqtt;

public partial class MqttQueueServer
{
	#region 嵌套子类
	/// <summary>表示连接到 MQTT 服务器的客户端通道。</summary>
	public sealed class Channel : IChannel
	{
		#region 事件定义
		public event EventHandler Closed;
		#endregion

		#region 成员字段
		private volatile MqttClientStatus _status;
		private readonly SemaphoreSlim _semaphore;
		private int _closed;
		private int _disposed;
		#endregion

		#region 构造函数
		internal Channel(MqttClientStatus status)
		{
			_status = status ?? throw new ArgumentNullException(nameof(status));
			_semaphore = new SemaphoreSlim(1, 1);
			this.Session = status.Session == null ? null : new Session(status.Session);
		}
		#endregion

		#region 公共属性
		public bool IsClosed => Volatile.Read(ref _closed) != 0 || this.Session?.DisconnectedTimestamp.HasValue == true;
		public bool IsDisposed => Volatile.Read(ref _disposed) != 0;
		public string Identifier => _status.Id;
		public EndPoint Address => _status.RemoteEndPoint;
		public MqttProtocolVersion ProtocolVersion => _status.ProtocolVersion;
		public DateTime ConnectedTimestamp => _status.ConnectedTimestamp;
		public DateTime LastPacketSentTimestamp => _status.LastPacketSentTimestamp;
		public DateTime LastPacketReceivedTimestamp => _status.LastPacketReceivedTimestamp;
		public DateTime LastNonKeepAlivePacketReceivedTimestamp => _status.LastNonKeepAlivePacketReceivedTimestamp;
		public long SentPacketsCount => _status.SentPacketsCount;
		public long ReceivedPacketsCount => _status.ReceivedPacketsCount;
		public long SentApplicationMessagesCount => _status.SentApplicationMessagesCount;
		public long ReceivedApplicationMessagesCount => _status.ReceivedApplicationMessagesCount;
		public long BytesSent => _status.BytesSent;
		public long BytesReceived => _status.BytesReceived;
		public Session Session { get; private set; }
		#endregion

		#region 内部方法
		internal void Update(MqttClientStatus status)
		{
			_status = status ?? throw new ArgumentNullException(nameof(status));

			if(status.Session == null)
				this.Session = null;
			else if(this.Session == null || this.Session.Identifier != status.Session.Id)
				this.Session = new Session(status.Session);
			else
				this.Session.Update(status.Session);
		}

		internal void Close()
		{
			if(Interlocked.Exchange(ref _closed, 1) == 0)
				this.Closed?.Invoke(this, EventArgs.Empty);
		}
		#endregion

		#region 关闭方法
		/// <summary>断开当前客户端通道。</summary>
		/// <param name="cancellation">指定关闭操作的取消标记，该标记仅在发起断开前生效。</param>
		public async ValueTask CloseAsync(CancellationToken cancellation = default)
		{
			await _semaphore.WaitAsync(cancellation);

			try
			{
				if(this.IsClosed)
					return;

				cancellation.ThrowIfCancellationRequested();
				await _status.DisconnectAsync(new MqttServerClientDisconnectOptions());
				this.Close();
			}
			finally
			{
				_semaphore.Release();
			}
		}
		#endregion

		#region 处置方法
		public async ValueTask DisposeAsync()
		{
			if(Interlocked.Exchange(ref _disposed, 1) != 0)
				return;

			try
			{
				await this.CloseAsync();
				GC.SuppressFinalize(this);
			}
			catch
			{
				Interlocked.Exchange(ref _disposed, 0);
				throw;
			}
		}
		#endregion

		#region 重写方法
		public override string ToString() => $"{this.Identifier}@{this.Address}";
		#endregion
	}

	/// <summary>表示 MQTT 客户端通道的键控集合。</summary>
	public sealed class ChannelCollection : KeyedCollection<string, Channel>, IEnumerable<Channel>
	{
		#region 成员字段
		private readonly object _syncRoot;
		private readonly SemaphoreSlim _semaphore;
		private MqttServer _server;
		#endregion

		#region 构造函数
		public ChannelCollection() : base(StringComparer.Ordinal)
		{
			_syncRoot = new object();
			_semaphore = new SemaphoreSlim(1, 1);
		}
		#endregion

		#region 公共属性
		public new int Count
		{
			get
			{
				lock(_syncRoot)
					return base.Count;
			}
		}

		public new Channel this[string key]
		{
			get
			{
				lock(_syncRoot)
					return base[key];
			}
		}
		#endregion

		#region 公共方法
		public new bool Contains(string key)
		{
			lock(_syncRoot)
				return base.Contains(key);
		}

		public new IEnumerator<Channel> GetEnumerator()
		{
			lock(_syncRoot)
				return new List<Channel>(this.Items).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		IEnumerator<Channel> IEnumerable<Channel>.GetEnumerator() => this.GetEnumerator();
		#endregion

		#region 内部方法
		internal async Task BindAsync(MqttServer server)
		{
			await _semaphore.WaitAsync();

			try
			{
				if(!ReferenceEquals(_server, server))
				{
					if(_server != null)
					{
						_server.ClientConnectedAsync -= this.OnClientChangedAsync;
						_server.ClientDisconnectedAsync -= this.OnClientChangedAsync;
					}

					_server = server;

					if(server != null)
					{
						server.ClientConnectedAsync += this.OnClientChangedAsync;
						server.ClientDisconnectedAsync += this.OnClientChangedAsync;
					}
				}

				await this.SynchronizeAsync(server);
			}
			finally
			{
				_semaphore.Release();
			}
		}
		#endregion

		#region 私有方法
		private async Task OnClientChangedAsync(ClientConnectedEventArgs _) => await this.RefreshAsync();
		private async Task OnClientChangedAsync(ClientDisconnectedEventArgs _) => await this.RefreshAsync();

		private async Task RefreshAsync()
		{
			await _semaphore.WaitAsync();

			try
			{
				await this.SynchronizeAsync(_server);
			}
			finally
			{
				_semaphore.Release();
			}
		}

		private async Task SynchronizeAsync(MqttServer server)
		{
			IList<MqttClientStatus> statuses = null;

			try
			{
				if(server != null && server.IsStarted)
					statuses = await server.GetClientsAsync();
			}
			catch(ObjectDisposedException)
			{
			}
			catch(InvalidOperationException) when(server == null || !server.IsStarted)
			{
			}

			if(!ReferenceEquals(_server, server))
				return;

			lock(_syncRoot)
			{
				var entries = statuses?.Where(status => status != null && !string.IsNullOrEmpty(status.Id))
					.ToDictionary(status => status.Id, StringComparer.Ordinal);

				for(int i = this.Items.Count - 1; i >= 0; i--)
				{
					var channel = this.Items[i];

					if(entries == null || !entries.Remove(channel.Identifier, out var status))
					{
						channel.Close();
						base.RemoveItem(i);
					}
					else
					{
						channel.Update(status);
					}
				}

				if(entries == null)
					return;

				foreach(var status in entries.Values)
					base.InsertItem(this.Items.Count, new Channel(status));
			}
		}
		#endregion

		#region 重写方法
		protected override string GetKeyForItem(Channel channel) => channel.Identifier;

		protected override void InsertItem(int index, Channel item)
		{
			lock(_syncRoot)
				base.InsertItem(index, item);
		}

		protected override void RemoveItem(int index)
		{
			lock(_syncRoot)
				base.RemoveItem(index);
		}

		protected override void SetItem(int index, Channel item)
		{
			lock(_syncRoot)
				base.SetItem(index, item);
		}

		protected override void ClearItems()
		{
			lock(_syncRoot)
			{
				foreach(var channel in this.Items)
					channel.Close();

				base.ClearItems();
			}
		}
		#endregion
	}
	#endregion
}
