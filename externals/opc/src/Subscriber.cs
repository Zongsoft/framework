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
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Opc.Ua;
using Opc.Ua.Client;

namespace Zongsoft.Externals.Opc;

public class Subscriber : IEquatable<Subscriber>, IEnumerable<Subscriber.Entry>, IAsyncDisposable
{
	#region 成员字段
	private Session _session;
	private Subscription _subscription;
	private Action<Subscriber, Entry, object> _consumer;
	#endregion

	#region 构造函数
	internal Subscriber(Session session, SubscriberOptions options, Action<Subscriber, Entry, object> consumer)
	{
		_session = session ?? throw new ArgumentNullException(nameof(session));
		_consumer = consumer ?? throw new ArgumentNullException(nameof(consumer));

		this.Options = options;
		_subscription = new Subscription()
		{
			Handle = this,
			PublishingEnabled = true,
			PublishingInterval = options.GetPublishingInterval(),
			MinLifetimeInterval = options.GetMinLifetimeInterval(),
			KeepAliveCount = (uint)options.GetKeepAliveCount(),
			LifetimeCount = (uint)options.GetLifetimeCount(),
			TimestampsToReturn = TimestampsToReturn.Server,
		};

		_subscription.StateChanged += this.Subscription_StateChanged;
		_subscription.PublishStatusChanged += this.Subscription_PublishStatusChanged;

		this.Entries = new EntryCollection(this);
	}
	#endregion

	#region 公共属性
	public object Subscription => _subscription;
	public SubscriberOptions Options { get; }
	public uint Identifier => _subscription?.Id ?? 0;
	public string Description { get; set; }
	public bool Registered => _subscription?.Created ?? false;
	public EntryCollection Entries { get; }

	public Entry this[int index] => this.Entries[index];
	public Entry this[string name] => this.Entries[name];
	#endregion

	#region 重写方法
	public bool Equals(Subscriber other) => other is not null && this.Identifier == other.Identifier;
	public override bool Equals(object obj) => this.Equals(obj as Subscriber);
	public override int GetHashCode() => HashCode.Combine(this.Identifier);
	public override string ToString() => $"{nameof(Subscriber)}#{this.Identifier}";
	#endregion

	#region 取消订阅
	private async ValueTask UnsubscribeAsync(CancellationToken cancellation = default)
	{
		var subscription = _subscription;
		if(subscription == null)
			return;

		this.Entries.Clear();

		try
		{
			//从服务器中删除当前订阅
			await subscription.DeleteAsync(true, cancellation);

			var session = _session;

			if(session != null && session.Connected)
				await session.RemoveSubscriptionAsync(subscription, cancellation);
		}
		catch { }
	}
	#endregion

	#region 处置方法
	private CancellationTokenSource _cancellation = new();
	public Microsoft.Extensions.Primitives.IChangeToken Disposed => Common.Notification.GetToken(_cancellation);

	public bool IsDisposed => _cancellation?.IsCancellationRequested ?? true;

	public async ValueTask DisposeAsync()
	{
		await this.DisposeAsync(true);
		GC.SuppressFinalize(this);
	}

	protected virtual async ValueTask DisposeAsync(bool disposing)
	{
		var cancellation = Interlocked.Exchange(ref _cancellation, null);
		if(cancellation == null)
			return;

		_subscription.StateChanged -= this.Subscription_StateChanged;
		_subscription.PublishStatusChanged -= this.Subscription_PublishStatusChanged;

		if(disposing)
		{
			//取消订阅
			await this.UnsubscribeAsync();

			//处置订阅
			_subscription.Dispose();
		}

		_subscription = null;
		_consumer = null;
		_session = null;

		cancellation.Cancel();
		cancellation.Dispose();
	}
	#endregion

	#region 事件处理
	private void Subscription_PublishStatusChanged(Subscription subscription, PublishStateChangedEventArgs args) { }
	private void Subscription_StateChanged(Subscription subscription, SubscriptionStateChangedEventArgs args) { }

	internal void OnNotification(MonitoredItem monitoredItem, MonitoredItemNotificationEventArgs args)
	{
		if(this.Entries.TryGetValue(monitoredItem.StartNodeId.ToString(), out var entry))
		{
			var value = args.NotificationValue is MonitoredItemNotification notification ?
				notification.Value.Value :
				args.NotificationValue;

			_consumer?.Invoke(this, entry, value);
		}
	}
	#endregion

	#region 枚举遍历
	public IEnumerator<Entry> GetEnumerator() => this.Entries.GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
	#endregion

	public class Entry : IEquatable<Entry>
	{
		#region 构造函数
		public Entry(string name, Type type, string label, string description = null)
		{
			this.Name = name;
			this.Type = type;
			this.Label = label;
			this.Description = description;
		}
		#endregion

		#region 公共属性
		public string Name { get; }
		public Type Type { get; set; }
		public string Label { get; set; }
		public string Description { get; set; }
		#endregion

		#region 内部属性
		internal MonitoredItem Monitor { get; set; }
		#endregion

		#region 重写方法
		public bool Equals(Entry other) => other is not null && string.Equals(this.Name, other.Name);
		public override bool Equals(object obj) => this.Equals(obj as Entry);
		public override int GetHashCode() => HashCode.Combine(this.Name);
		public override string ToString() => this.Type == null ? this.Name : $"{this.Name}@{Common.TypeAlias.GetAlias(this.Type)}";
		#endregion
	}

	public class EntryCollection : KeyedCollection<string, Entry>
	{
		#region 成员字段
		private readonly Subscriber _subscriber;
		private readonly Subscription _subscription;
		#endregion

		#region 构造函数
		public EntryCollection(Subscriber subscriber, params IEnumerable<Entry> monitors) : base(StringComparer.OrdinalIgnoreCase)
		{
			_subscriber = subscriber ?? throw new ArgumentNullException(nameof(subscriber));
			_subscription = (Subscription)subscriber.Subscription;

			if(monitors == null)
			{
				foreach(var monitor in monitors)
					this.Add(monitor);
			}
		}
		#endregion

		#region 公共方法
		public bool Add(string name)
		{
			if(string.IsNullOrEmpty(name))
				return false;

			if(this.Contains(name))
				return false;

			this.Add(new Entry(name, null, name));
			return true;
		}

		public bool TryRemove(string key, out Entry result)
		{
			if(key != null && this.TryGetValue(key, out result))
				return this.Remove(key);

			result = null;
			return false;
		}
		#endregion

		#region 重写方法
		protected override string GetKeyForItem(Entry entry) => entry.Name;
		protected override void SetItem(int index, Entry item) => throw new NotSupportedException();

		protected override void InsertItem(int index, Entry item)
		{
			if(item == null || string.IsNullOrEmpty(item.Name))
				throw new ArgumentNullException(nameof(item));

			var monitored = new MonitoredItem(_subscription.DefaultItem)
			{
				Handle = item,
				StartNodeId = NodeId.Parse(item.Name),
				AttributeId = Attributes.Value,
				SamplingInterval = _subscriber.Options.GetSamplingInterval(),
				DiscardOldest = true,
				QueueSize = (uint)_subscriber.Options.GetQueueSize(),
				DisplayName = string.IsNullOrEmpty(item.Label) ? item.Name : item.Label,
			};

			item.Monitor = monitored;
			base.InsertItem(index, item);

			monitored.Notification += _subscriber.OnNotification;
			_subscription.AddItem(monitored);

			//从服务器中执行订阅变更操作
			if(_subscription.Created)
				_subscription.ApplyChanges();
		}

		protected override void RemoveItem(int index)
		{
			var item = this[index];

			if(item != null && item.Monitor != null)
			{
				_subscription.RemoveItem(item.Monitor);
				item.Monitor.Notification -= _subscriber.OnNotification;
				item.Monitor = null;
			}

			//调用基类同名方法
			base.RemoveItem(index);

			//从服务器中删除已取消的订阅项目
			if(_subscription.Created)
				_subscription.DeleteItems();
		}

		protected override void ClearItems()
		{
			foreach(var item in this.Items)
			{
				if(item == null || item.Monitor == null)
					continue;

				_subscription.RemoveItem(item.Monitor);
				item.Monitor.Notification -= _subscriber.OnNotification;
				item.Monitor = null;
			}

			//调用基类同名方法
			base.ClearItems();

			//从服务器中删除已取消的订阅项目
			if(_subscription.Created)
				_subscription.DeleteItems();
		}
		#endregion
	}
}
