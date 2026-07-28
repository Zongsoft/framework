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
using System.Collections;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using MQTTnet.Server;

namespace Zongsoft.Messaging.Mqtt;

public partial class MqttQueueServer
{
	#region 嵌套子类
	/// <summary>表示 MQTT 服务器中的客户端会话。</summary>
	public sealed class Session
	{
		#region 成员字段
		private volatile MqttSessionStatus _status;
		#endregion

		#region 构造函数
		internal Session(MqttSessionStatus status) => _status = status ?? throw new ArgumentNullException(nameof(status));
		#endregion

		#region 公共属性
		public string Identifier => _status.Id;
		public DateTime CreatedTimestamp => _status.CreatedTimestamp;
		public DateTime? DisconnectedTimestamp => _status.DisconnectedTimestamp;
		public uint ExpiryInterval => _status.ExpiryInterval;
		public long PendingApplicationMessagesCount => _status.PendingApplicationMessagesCount;
		public IDictionary Items => _status.Items;
		#endregion

		#region 公共方法
		/// <summary>废弃并删除当前 MQTT 会话。</summary>
		/// <returns>返回删除会话的异步任务。</returns>
		public Task Abandon() => _status.DeleteAsync();
		#endregion

		#region 内部方法
		internal void Update(MqttSessionStatus status) => _status = status ?? throw new ArgumentNullException(nameof(status));
		#endregion

		#region 重写方法
		public override string ToString() => this.DisconnectedTimestamp.HasValue ?
			$"[Disconnected]{this.Identifier}@{this.DisconnectedTimestamp:O}" :
			$"[Connected]{this.Identifier}@{this.CreatedTimestamp:O}";
		#endregion
	}

	/// <summary>表示 MQTT 客户端会话的键控集合。</summary>
	public sealed class SessionCollection : KeyedCollection<string, Session>, IEnumerable<Session>
	{
		#region 成员字段
		private readonly object _syncRoot;
		private readonly SemaphoreSlim _semaphore;
		private MqttServer _server;
		#endregion

		#region 构造函数
		public SessionCollection() : base(StringComparer.Ordinal)
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

		public new Session this[string key]
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

		public new IEnumerator<Session> GetEnumerator()
		{
			lock(_syncRoot)
				return new List<Session>(this.Items).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		IEnumerator<Session> IEnumerable<Session>.GetEnumerator() => this.GetEnumerator();
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
						_server.SessionDeletedAsync -= this.OnSessionDeletedAsync;
					}

					_server = server;

					if(server != null)
					{
						server.ClientConnectedAsync += this.OnClientChangedAsync;
						server.ClientDisconnectedAsync += this.OnClientChangedAsync;
						server.SessionDeletedAsync += this.OnSessionDeletedAsync;
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
		private async Task OnSessionDeletedAsync(SessionDeletedEventArgs _) => await this.RefreshAsync();

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
			IList<MqttSessionStatus> statuses = null;

			try
			{
				if(server != null && server.IsStarted)
					statuses = await server.GetSessionsAsync();
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
					var session = this.Items[i];

					if(entries == null || !entries.Remove(session.Identifier, out var status))
						base.RemoveItem(i);
					else
						session.Update(status);
				}

				if(entries == null)
					return;

				foreach(var status in entries.Values)
					base.InsertItem(this.Items.Count, new Session(status));
			}
		}
		#endregion

		#region 重写方法
		protected override string GetKeyForItem(Session session) => session.Identifier;

		protected override void InsertItem(int index, Session item)
		{
			lock(_syncRoot)
				base.InsertItem(index, item);
		}

		protected override void RemoveItem(int index)
		{
			lock(_syncRoot)
				base.RemoveItem(index);
		}

		protected override void SetItem(int index, Session item)
		{
			lock(_syncRoot)
				base.SetItem(index, item);
		}

		protected override void ClearItems()
		{
			lock(_syncRoot)
				base.ClearItems();
		}
		#endregion
	}
	#endregion
}
