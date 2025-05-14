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

public partial class Subscriber : IEquatable<Subscriber>, IEnumerable<Subscriber.Entry>, IAsyncDisposable
{
	#region 成员字段
	private Subscription _subscription;
	private Action<Subscriber, Entry, object> _consumer;
	#endregion

	#region 构造函数
	internal Subscriber(SubscriberOptions options, Action<Subscriber, Entry, object> consumer)
	{
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
		try
		{
			//清空订阅的监视条目
			this.Entries.Clear();
		}
		catch { }

		try
		{
			//从服务器中删除当前订阅
			await _subscription?.DeleteAsync(true, cancellation);
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

		try
		{
			cancellation.Cancel();
			cancellation.Dispose();
		}
		catch { }
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
}
