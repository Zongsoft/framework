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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.Opc library.
 *
 * The Zongsoft.Externals.Opc is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Opc is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Opc library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Opc.Ua;
using Opc.Ua.Server;

namespace Zongsoft.Externals.Opc;

partial class OpcServer
{
	public sealed class Channel
	{
		#region 成员字段
		private readonly ISession _session;
		private readonly string _identifier;
		private readonly DateTime _creation;
		private readonly ChannelStatistics _statistics;
		#endregion

		#region 构造函数
		internal Channel(ISession session)
		{
			_session = session;
			_creation = DateTime.UtcNow;
			_identifier = session.Id.ToString();
			_statistics = new ChannelStatistics();
		}
		#endregion

		#region 公共属性
		public string Identifier => _identifier;
		public string Name => _session.SessionDiagnostics.SessionName;
		public bool Activated => _session.Activated;
		public DateTime Creation => _creation;
		public DateTime Timestamp => _session.ClientLastContactTime;
		public ChannelStatistics Statistics => _statistics.Update(_session.SessionDiagnostics);
		public Security.AuthenticationIdentity Identity => Security.AuthenticationIdentity.GetIdentity(_session.IdentityToken);
		public System.Security.Cryptography.X509Certificates.X509Certificate Certificate => _session.ClientCertificate;
		#endregion

		#region 重写方法
		public override string ToString() => this.Activated ?
			$"[Activated] {this.Identifier}@{this.Timestamp}" :
			$"[Deactived] {this.Identifier}@{this.Timestamp}";
		#endregion
	}

	public sealed class ChannelStatistics
	{
		public ChannelStatistics Update(SessionDiagnosticsDataType diagnostics)
		{
			if(diagnostics != null)
			{
				this.MonitoredCount = diagnostics.CurrentMonitoredItemsCount;
				this.SubscriptionCount = diagnostics.CurrentSubscriptionsCount;
				this.Readings = new(diagnostics.ReadCount);
				this.Writings = new(diagnostics.WriteCount);
				this.Requests = new(diagnostics.TotalRequestCount);
				this.Browsings = new(diagnostics.BrowseCount);
			}

			return this;
		}

		public Count Requests { get; private set; }
		public Count Readings { get; private set; }
		public Count Writings { get; private set; }
		public Count Browsings { get; private set; }
		public uint MonitoredCount { get; private set; }
		public uint SubscriptionCount { get; private set; }

		public readonly struct Count
		{
			public Count(uint total, uint error)
			{
				this.Total = total;
				this.Error = error;
			}

			internal Count(ServiceCounterDataType counter)
			{
				this.Total = counter.TotalCount;
				this.Error = counter.ErrorCount;
			}

			public readonly uint Total;
			public readonly uint Error;

			public override string ToString() => $"{this.Error}/{this.Total}";
		}
	}

	public sealed class ChannelCollection : IReadOnlyCollection<Channel>
	{
		#region 成员字段
		private readonly ConcurrentDictionary<string, Channel> _channels = new();
		#endregion

		#region 公共方法
		public int Count => _channels.Count;
		public bool Contains(string id) => _channels.ContainsKey(id);
		public bool TryGetValue(string id, out Channel channel) => _channels.TryGetValue(id, out channel);
		#endregion

		#region 内部方法
		internal bool Remove(string id) => id != null && _channels.Remove(id, out _);
		internal void Clear() => _channels.Clear();
		internal bool Add(ISession session) => _channels.TryAdd(session.Id.ToString(), new Channel(session));
		internal bool Add(Channel channel) => channel != null && _channels.TryAdd(channel.Identifier, channel);
		#endregion

		#region 枚举遍历
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		public IEnumerator<Channel> GetEnumerator() => _channels.Values.GetEnumerator();
		#endregion
	}
}
