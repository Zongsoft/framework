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
using System.Threading;
using System.Threading.Tasks;

using Opc.Ua;
using Opc.Ua.Client;

namespace Zongsoft.Externals.Opc;

partial class Subscriber
{
	public class Entry : IEquatable<Entry>
	{
		#region 成员字段
		private readonly NodeId _nodeId;
		private MonitoredItem _monitor;
		#endregion

		#region 构造函数
		public Entry(string name, object tag = null) : this(name, null, tag, null, null) { }
		public Entry(string name, Type type, object tag = null, string label = null, string description = null)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			this.Name = name;
			this.Type = type;
			this.Tag = tag;
			this.Label = label;
			this.Description = description;
			_nodeId = NodeId.Parse(name);
		}
		#endregion

		#region 公共属性
		public string Name { get; }
		public Type Type { get; set; }
		public object Tag { get; set; }
		public string Label { get; set; }
		public string Description { get; set; }
		#endregion

		#region 内部属性
		internal NodeId Identifier => this.Monitor?.StartNodeId ?? _nodeId;
		internal MonitoredItem Monitor => _monitor;
		internal void Monitored(MonitoredItem monitor, MonitoredItemNotificationEventHandler handler)
		{
			if(handler != null)
			{
				var older = _monitor;
				if(older != null)
					older.Notification -= handler;

				if(monitor != null)
					monitor.Notification += handler;
			}

			_monitor = monitor;
		}
		#endregion

		#region 重写方法
		public bool Equals(Entry other) => other is not null && string.Equals(this.Name, other.Name);
		public override bool Equals(object obj) => this.Equals(obj as Entry);
		public override int GetHashCode() => HashCode.Combine(this.Name);
		public override string ToString() => this.Type == null ? this.Name : $"{this.Name}@{Common.TypeAlias.GetAlias(this.Type)}";
		#endregion
	}

	public class EntryCollection : IEnumerable<Entry>
	{
		#region 成员字段
		private readonly Subscriber _subscriber;
		private readonly Subscription _subscription;
		private readonly Dictionary<NodeId, Entry> _dictionary;
		#endregion

		#region 构造函数
		public EntryCollection(Subscriber subscriber)
		{
			_subscriber = subscriber ?? throw new ArgumentNullException(nameof(subscriber));
			_subscription = (Subscription)subscriber.Subscription;
			_dictionary = new Dictionary<NodeId, Entry>();
		}
		#endregion

		#region 公共属性
		public int Count => _dictionary.Count;
		public Entry this[string name] => string.IsNullOrEmpty(name) ? null : _dictionary[NodeId.Parse(name)];
		internal Entry this[NodeId identifier] => identifier == null ? null : _dictionary[identifier];
		#endregion

		#region 公共方法
		public bool Contains(string name) => name != null && _dictionary.ContainsKey(NodeId.Parse(name));
		internal bool Contains(NodeId identifier) => identifier != null && _dictionary.ContainsKey(identifier);

		public ValueTask<bool> AddAsync(string name, object tag = null, CancellationToken cancellation = default) =>
			string.IsNullOrEmpty(name) ?
			ValueTask.FromResult(false) :
			this.AddAsync(new Entry(name, tag), cancellation);

		public async ValueTask<bool> AddAsync(Entry entry, CancellationToken cancellation = default)
		{
			if(entry == null)
				return false;

			if(_dictionary.TryAdd(entry.Identifier, entry))
			{
				var monitored = new MonitoredItem(_subscription.DefaultItem)
				{
					Handle = entry,
					StartNodeId = entry.Identifier,
					AttributeId = Attributes.Value,
					SamplingInterval = _subscriber.Options.GetSamplingInterval(),
					DiscardOldest = true,
					QueueSize = (uint)_subscriber.Options.GetQueueSize(),
					DisplayName = string.IsNullOrEmpty(entry.Label) ? entry.Name : entry.Label,
				};

				entry.Monitored(monitored, _subscriber.OnNotification);
				_subscription.AddItem(monitored);

				//从服务器中执行订阅变更操作
				if(_subscription.Created)
					await _subscription.ApplyChangesAsync(cancellation);

				return true;
			}

			return false;
		}

		public async ValueTask ClearAsync(CancellationToken cancellation = default)
		{
			foreach(var entry in _dictionary.Values)
			{
				if(entry == null || entry.Monitor == null)
					continue;

				_subscription.RemoveItem(entry.Monitor);
				entry.Monitored(null, _subscriber.OnNotification);
			}

			//调用基类同名方法
			_dictionary.Clear();

			//从服务器中删除已取消的订阅项目
			if(_subscription.Created)
				await _subscription.DeleteItemsAsync(cancellation);
		}

		public bool TryGetValue(string name, out Entry value)
		{
			if(!string.IsNullOrEmpty(name))
				return _dictionary.TryGetValue(NodeId.Parse(name), out value);

			value = null;
			return false;
		}

		internal bool TryGetValue(NodeId identifier, out Entry value)
		{
			if(identifier != null)
				return _dictionary.TryGetValue(identifier, out value);

			value = null;
			return false;
		}

		public async ValueTask<Entry> RemoveAsync(string name, CancellationToken cancellation = default)
		{
			if(!string.IsNullOrEmpty(name) && _dictionary.Remove(NodeId.Parse(name), out var result))
			{
				await this.OnRemovedAsync(result, cancellation);
				return result;
			}

			return default;
		}

		internal async ValueTask<Entry> RemoveAsync(NodeId identifier, CancellationToken cancellation = default)
		{
			if(identifier != null && _dictionary.Remove(identifier, out var result))
			{
				await this.OnRemovedAsync(result, cancellation);
				return result;
			}

			return default;
		}
		#endregion

		#region 私有方法
		private async ValueTask OnRemovedAsync(Entry entry, CancellationToken cancellation)
		{
			if(entry != null && entry.Monitor != null)
			{
				_subscription.RemoveItem(entry.Monitor);
				entry.Monitored(null, _subscriber.OnNotification);
			}

			//从服务器中删除已取消的订阅项目
			if(_subscription.Created)
				await _subscription.DeleteItemsAsync(cancellation);
		}
		#endregion

		#region 枚举遍历
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		public IEnumerator<Entry> GetEnumerator() => _dictionary.Values.GetEnumerator();
		#endregion
	}
}
