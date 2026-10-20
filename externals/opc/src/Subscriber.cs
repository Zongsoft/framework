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
using System.Threading.Channels;
using System.Collections;
using System.Collections.Generic;

using Opc.Ua;
using Opc.Ua.Client;

namespace Zongsoft.Externals.Opc;

public partial class Subscriber : IEquatable<Subscriber>, IEnumerable<Subscriber.Entry>, IAsyncDisposable
{
	#region 成员字段
	private Subscription _subscription;
	private Action<Subscriber, Entry, object> _consumer;
	private Func<Subscriber, Entry, object, CancellationToken, ValueTask> _asyncConsumer;
	private readonly Channel<Notification> _notifications;
	private readonly Task _dispatch;
	private TaskCompletionSource _disposal;
	private Exception _failure;
	#endregion

	#region 构造函数
	internal Subscriber(SubscriberOptions options, Action<Subscriber, Entry, object> consumer = null)
	{
		_subscription = new Subscription((ITelemetryContext)null)
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

		_consumer = consumer;
		_notifications = Channel.CreateBounded<Notification>(new BoundedChannelOptions(options?.NotificationCapacity > 0 ? options.NotificationCapacity : SubscriberOptions.QUEUE_SIZE)
		{
			SingleReader = true,
			SingleWriter = false,
			AllowSynchronousContinuations = false,
			FullMode = BoundedChannelFullMode.Wait,
		});
		_dispatch = this.DispatchAsync();
		this.Options = options;
		this.Statistics = new();
		this.Entries = new EntryCollection(this);
	}
	#endregion

	#region 公共属性
	public object Subscription => _subscription;
	public SubscriberOptions Options { get; }
	public uint Identifier => _subscription?.Id ?? 0;
	public string Description { get; set; }
	public SubscriberStatistics Statistics { get; }
	public bool Registered => _subscription?.Created ?? false;
	public EntryCollection Entries { get; }
	public Entry this[string name] => this.Entries[name];
	internal Entry this[NodeId identifier] => this.Entries[identifier];
	#endregion

	#region 重写方法
	public bool Equals(Subscriber other) => other is not null && this.Identifier == other.Identifier;
	public override bool Equals(object obj) => this.Equals(obj as Subscriber);
	public override int GetHashCode() => HashCode.Combine(this.Identifier);
	public override string ToString() => $"{nameof(Subscriber)}#{this.Identifier}";
	#endregion

	#region 公共方法
	public void Consume(Action<Subscriber, Entry, object> consumer = null)
	{
		ObjectDisposedException.ThrowIf(_disposal != null, this);
		_asyncConsumer = null;
		_consumer = consumer;
	}

	/// <summary>注册异步通知处理器；通知由此订阅拥有的有界队列顺序处理。</summary>
	/// <param name="consumer">异步处理器；处理失败通过 FlushAsync 或 DisposeAsync 传播。</param>
	public void Consume(Func<Subscriber, Entry, object, CancellationToken, ValueTask> consumer)
	{
		ObjectDisposedException.ThrowIf(_disposal != null, this);
		_consumer = null;
		_asyncConsumer = consumer;
	}

	/// <summary>等待此前接受的异步通知处理完成。</summary>
	/// <param name="cancellation">取消等待的标记。</param>
	/// <returns>通知处理完成的任务。</returns>
	public async ValueTask FlushAsync(CancellationToken cancellation = default)
	{
		ObjectDisposedException.ThrowIf(_disposal != null, this);
		var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		using var registration = cancellation.Register(() => completion.TrySetCanceled(cancellation));
		await _notifications.Writer.WriteAsync(new(null, null, null, completion), cancellation);
		await completion.Task;
	}
	#endregion

	#region 处置方法
	private readonly CancellationTokenSource _cancellation = new();
	public Microsoft.Extensions.Primitives.IChangeToken Disposed => Common.Notification.GetToken(_cancellation);
	public bool IsDisposed => _disposal != null;

	public ValueTask DisposeAsync()
	{
		var completion = Volatile.Read(ref _disposal);
		if(completion == null)
		{
			var candidate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			completion = Interlocked.CompareExchange(ref _disposal, candidate, null) ?? candidate;
			if(ReferenceEquals(completion, candidate))
				_ = this.DisposeCoreAsync(candidate);
		}

		return new(completion.Task);
	}

	private async Task DisposeCoreAsync(TaskCompletionSource completion)
	{
		try
		{
			await this.DisposeAsync(true);
			GC.SuppressFinalize(this);
			completion.TrySetResult();
		}
		catch(Exception exception) { completion.TrySetException(exception); }
	}

	protected virtual async ValueTask DisposeAsync(bool disposing)
	{
		if(!disposing)
			return;

		var subscription = _subscription;
		subscription.StateChanged -= this.Subscription_StateChanged;
		subscription.PublishStatusChanged -= this.Subscription_PublishStatusChanged;
		_notifications.Writer.TryComplete();
		List<Exception> errors = null;

		try { await this.Entries.ClearAsync(); }
		catch(Exception exception) { (errors ??= []).Add(exception); }

		try
		{
			if(subscription.Created)
				await subscription.DeleteAsync(true, CancellationToken.None);
		}
		catch(Exception exception) { (errors ??= []).Add(exception); }

		await _dispatch;
		var failure = Interlocked.Exchange(ref _failure, null);

		if(failure != null)
			(errors ??= []).Add(failure);

		try { subscription.Dispose(); }
		catch(Exception exception) { (errors ??= []).Add(exception); }

		_subscription = null;
		_consumer = null;
		_asyncConsumer = null;

		try { await _cancellation.CancelAsync(); }
		catch(Exception exception) { (errors ??= []).Add(exception); }
		finally { _cancellation.Dispose(); }

		if(errors != null)
			throw new AggregateException(errors);
	}
	#endregion

	#region 事件处理
	private void Subscription_PublishStatusChanged(Subscription subscription, PublishStateChangedEventArgs args) { }
	private void Subscription_StateChanged(Subscription subscription, SubscriptionStateChangedEventArgs args) { }

	internal void OnNotification(MonitoredItem monitoredItem, MonitoredItemNotificationEventArgs args)
	{
		if(this.IsDisposed)
			return;

		this.Statistics.IncreaseNotificationCount();
		var consumer = _consumer;
		var asynchronous = _asyncConsumer;
		var entry = monitoredItem.Handle as Entry;

		if(entry == null || (consumer == null && asynchronous == null))
			return;

		var value = args.NotificationValue is MonitoredItemNotification notification ?
			Utility.GetValue(notification.Value) : args.NotificationValue;

		if(asynchronous != null)
		{
			if(!_notifications.Writer.TryWrite(new(asynchronous, entry, value, null)) && !this.IsDisposed)
				Interlocked.CompareExchange(ref _failure, new InvalidOperationException(Properties.Resources.Subscription_QueueFull_Message), null);
		}
		else
			consumer(this, entry, value);
	}

	private async Task DispatchAsync()
	{
		await foreach(var notification in _notifications.Reader.ReadAllAsync())
		{
			if(notification.Completion != null)
			{
				var failure = Volatile.Read(ref _failure);
				if(failure == null)
					notification.Completion.TrySetResult();
				else if(notification.Completion.TrySetException(failure))
					Interlocked.CompareExchange(ref _failure, null, failure);

				continue;
			}

			try { await notification.Consumer(this, notification.Entry, notification.Value, CancellationToken.None); }
			catch(Exception exception) { Interlocked.CompareExchange(ref _failure, exception, null); }
		}
	}
	#endregion

	#region 嵌套类型
	private readonly record struct Notification(Func<Subscriber, Entry, object, CancellationToken, ValueTask> Consumer, Entry Entry, object Value, TaskCompletionSource Completion);
	#endregion

	#region 枚举遍历
	IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
	public IEnumerator<Entry> GetEnumerator() => this.Entries.GetEnumerator();
	#endregion
}
