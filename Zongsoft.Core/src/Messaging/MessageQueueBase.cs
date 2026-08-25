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
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Zongsoft.Components;
using Zongsoft.Collections;

namespace Zongsoft.Messaging;

public abstract class MessageQueueBase<TSubscriber> : IMessageQueue where TSubscriber : IMessageConsumer
{
	#region 常量定义
	private const int DISPOSED = -1;
	private const int DISPOSING = 1;
	#endregion

	#region 成员字段
	private volatile int _disposing;
	private readonly CancellationTokenSource _cancellation = new();
	#endregion

	#region 构造函数
	protected MessageQueueBase(string name)
	{
		this.Name = name ?? string.Empty;
		this.Subscribers = new SubscriberCollection();
	}
	#endregion

	#region 公共属性
	public string Name { get; }
	public SubscriberCollection Subscribers { get; }
	public bool IsDisposed => _disposing == DISPOSED;
	#endregion

	#region 生产方法
	public ValueTask<string> ProduceAsync(ReadOnlyMemory<byte> data, MessageEnqueueOptions options = null, CancellationToken cancellation = default) =>
		this.ProduceAsyncCore(this.GetTopic(null), null, data, options, cancellation);

	public ValueTask<string> ProduceAsync(ReadOnlyMemory<char> data, MessageEnqueueOptions options = null, CancellationToken cancellation = default) =>
		this.ProduceAsync(null, null, data, Encoding.UTF8, options, cancellation);

	public ValueTask<string> ProduceAsync(ReadOnlyMemory<char> data, Encoding encoding, MessageEnqueueOptions options = null, CancellationToken cancellation = default)=>
		this.ProduceAsync(null, null, data, encoding, options, cancellation);

	public ValueTask<string> ProduceAsync(string topic, ReadOnlyMemory<byte> data, MessageEnqueueOptions options = null, CancellationToken cancellation = default) =>
		this.ProduceAsyncCore(this.GetTopic(topic), null, data, options, cancellation);

	public ValueTask<string> ProduceAsync(string topic, string tags, ReadOnlyMemory<byte> data, MessageEnqueueOptions options = null, CancellationToken cancellation = default) =>
		this.ProduceAsyncCore(this.GetTopic(topic), tags, data, options, cancellation);

	public ValueTask<string> ProduceAsync(string topic, ReadOnlyMemory<char> data, MessageEnqueueOptions options = null, CancellationToken cancellation = default) =>
		this.ProduceAsync(topic, null, data, Encoding.UTF8, options, cancellation);

	public ValueTask<string> ProduceAsync(string topic, ReadOnlyMemory<char> data, Encoding encoding, MessageEnqueueOptions options = null, CancellationToken cancellation = default) =>
		this.ProduceAsync(topic, null, data, encoding, options, cancellation);

	public ValueTask<string> ProduceAsync(string topic, string tags, ReadOnlyMemory<char> data, MessageEnqueueOptions options = null, CancellationToken cancellation = default) =>
		this.ProduceAsync(topic, tags, data, Encoding.UTF8, options, cancellation);

	public ValueTask<string> ProduceAsync(string topic, string tags, ReadOnlyMemory<char> data, Encoding encoding, MessageEnqueueOptions options = null, CancellationToken cancellation = default) =>
		this.ProduceAsyncCore(this.GetTopic(topic), tags, encoding.GetBytes(data.ToString()), options, cancellation);

	private ValueTask<string> ProduceAsyncCore(string topic, string tags, ReadOnlyMemory<byte> data, MessageEnqueueOptions options, CancellationToken cancellation)
	{
		if(_disposing != 0)
			throw new ObjectDisposedException(this.GetType().Name);

		var reliability = options?.Reliability ?? MessageReliability.MostOnce;
		if(reliability > this.Reliability)
			throw new NotSupportedException(string.Format(Properties.Resources.Messaging_ReliabilityNotSupported_Message, reliability, this.GetType().Name));

		return this.OnProduceAsync(topic, tags, data, options, cancellation);
	}

	protected abstract ValueTask<string> OnProduceAsync(string topic, string tags, ReadOnlyMemory<byte> data, MessageEnqueueOptions options, CancellationToken cancellation);
	#endregion

	#region 订阅方法
	async ValueTask<IMessageConsumer> IMessageQueue.SubscribeAsync(Action<Message> handler, CancellationToken cancellation) => await this.SubscribeAsync(null, null, new HandlerAdapter(handler), null, cancellation);
	async ValueTask<IMessageConsumer> IMessageQueue.SubscribeAsync(Action<Message> handler, MessageSubscribeOptions options, CancellationToken cancellation) => await this.SubscribeAsync(null, null, new HandlerAdapter(handler), options, cancellation);
	async ValueTask<IMessageConsumer> IMessageQueue.SubscribeAsync(IHandler<Message> handler, CancellationToken cancellation) => await this.SubscribeAsync(null, null, handler, null, cancellation);
	async ValueTask<IMessageConsumer> IMessageQueue.SubscribeAsync(IHandler<Message> handler, MessageSubscribeOptions options, CancellationToken cancellation) => await this.SubscribeAsync(null, null, handler, options, cancellation);
	async ValueTask<IMessageConsumer> IMessageQueue.SubscribeAsync(string topic, Action<Message> handler, CancellationToken cancellation) => await this.SubscribeAsync(topic, null, new HandlerAdapter(handler), null, cancellation);
	async ValueTask<IMessageConsumer> IMessageQueue.SubscribeAsync(string topic, Action<Message> handler, MessageSubscribeOptions options, CancellationToken cancellation) => await this.SubscribeAsync(topic, null, new HandlerAdapter(handler), options, cancellation);
	async ValueTask<IMessageConsumer> IMessageQueue.SubscribeAsync(string topic, IHandler<Message> handler, CancellationToken cancellation) =>await this.SubscribeAsync(topic, null, handler, null, cancellation);
	async ValueTask<IMessageConsumer> IMessageQueue.SubscribeAsync(string topic, IHandler<Message> handler, MessageSubscribeOptions options, CancellationToken cancellation) => await this.SubscribeAsync(topic, null, handler, options, cancellation);
	async ValueTask<IMessageConsumer> IMessageQueue.SubscribeAsync(string topic, string tags, Action<Message> handler, CancellationToken cancellation) => await this.SubscribeAsync(topic, tags, new HandlerAdapter(handler), null, cancellation);
	async ValueTask<IMessageConsumer> IMessageQueue.SubscribeAsync(string topic, string tags, Action<Message> handler, MessageSubscribeOptions options, CancellationToken cancellation) => await this.SubscribeAsync(topic, tags, new HandlerAdapter(handler), options, cancellation);
	async ValueTask<IMessageConsumer> IMessageQueue.SubscribeAsync(string topic, string tags, IHandler<Message> handler, CancellationToken cancellation) => await this.SubscribeAsync(topic, tags, handler, null, cancellation);
	async ValueTask<IMessageConsumer> IMessageQueue.SubscribeAsync(string topic, string tags, IHandler<Message> handler, MessageSubscribeOptions options, CancellationToken cancellation) => await this.SubscribeAsync(topic, tags, handler, options, cancellation);

	public ValueTask<TSubscriber> SubscribeAsync(Action<Message> handler, CancellationToken cancellation = default) => this.SubscribeAsync(null, null, new HandlerAdapter(handler), null, cancellation);
	public ValueTask<TSubscriber> SubscribeAsync(Action<Message> handler, MessageSubscribeOptions options, CancellationToken cancellation = default) => this.SubscribeAsync(null, null, new HandlerAdapter(handler), options, cancellation);
	public ValueTask<TSubscriber> SubscribeAsync(IHandler<Message> handler, CancellationToken cancellation = default) => this.SubscribeAsync(null, null, handler, null, cancellation);
	public ValueTask<TSubscriber> SubscribeAsync(IHandler<Message> handler, MessageSubscribeOptions options, CancellationToken cancellation = default) => this.SubscribeAsync(null, null, handler, options, cancellation);
	public ValueTask<TSubscriber> SubscribeAsync(string topic, Action<Message> handler, CancellationToken cancellation = default) => this.SubscribeAsync(topic, null, new HandlerAdapter(handler), null, cancellation);
	public ValueTask<TSubscriber> SubscribeAsync(string topic, Action<Message> handler, MessageSubscribeOptions options, CancellationToken cancellation = default) => this.SubscribeAsync(topic, null, new HandlerAdapter(handler), options, cancellation);
	public ValueTask<TSubscriber> SubscribeAsync(string topic, IHandler<Message> handler, CancellationToken cancellation = default) => this.SubscribeAsync(topic, null, handler, null, cancellation);
	public ValueTask<TSubscriber> SubscribeAsync(string topic, IHandler<Message> handler, MessageSubscribeOptions options, CancellationToken cancellation = default) => this.SubscribeAsync(topic, null, handler, options, cancellation);
	public ValueTask<TSubscriber> SubscribeAsync(string topic, string tags, Action<Message> handler, CancellationToken cancellation = default) => this.SubscribeAsync(topic, tags, new HandlerAdapter(handler), null, cancellation);
	public ValueTask<TSubscriber> SubscribeAsync(string topic, string tags, Action<Message> handler, MessageSubscribeOptions options, CancellationToken cancellation = default) => this.SubscribeAsync(topic, tags, new HandlerAdapter(handler), options, cancellation);
	public ValueTask<TSubscriber> SubscribeAsync(string topic, string tags, IHandler<Message> handler, CancellationToken cancellation = default) => this.SubscribeAsync(topic, tags, handler, null, cancellation);

	public async ValueTask<TSubscriber> SubscribeAsync(string topic, string tags, IHandler<Message> handler, MessageSubscribeOptions options, CancellationToken cancellation = default)
	{
		if(_disposing != 0)
			throw new ObjectDisposedException(this.GetType().Name);

		var reliability = options?.Reliability ?? MessageReliability.MostOnce;
		var fallback = options?.FallbackBehavior ?? MessageFallbackBehavior.Backoff;
		if(reliability > this.Reliability)
			throw new NotSupportedException(string.Format(Properties.Resources.Messaging_ReliabilityNotSupported_Message, reliability, this.GetType().Name));

		//确保主题不为空
		topic = this.GetTopic(topic) ?? string.Empty;
		options = new MessageSubscribeOptions(reliability, fallback);

		var candidate = new Subscription(handler, Slice(tags), options, entry => InitializeSubscriberAsync(topic, tags, handler, options, entry));
		var subscription = this.Subscribers.GetOrAdd(topic, _ => candidate);
		if(!subscription.Matches(handler, candidate.Tags, options))
			throw new InvalidOperationException(string.Format(Properties.Resources.Messaging_SubscriptionConflict_Message, topic));

		var task = subscription.GetTask();

		return cancellation.CanBeCanceled ?
			await task.WaitAsync(cancellation) :
			await task;

		static string[] Slice(string text) => string.IsNullOrWhiteSpace(text) ? [] :
			text.Split([',', ';'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

		async Task<TSubscriber> InitializeSubscriberAsync(string topic, string tags, IHandler<Message> handler, MessageSubscribeOptions options, Subscription subscription)
		{
			TSubscriber subscriber = default;

			try
			{
				subscriber = await this.CreateSubscriberAsync(topic, tags, handler, options, _cancellation.Token);
				if(subscriber == null)
					return default;

				subscriber.Closed += OnClosed;

				if(!await this.OnSubscribeAsync(subscriber, _cancellation.Token) || subscriber.IsClosed)
				{
					subscriber.Closed -= OnClosed;
					await subscriber.DisposeAsync();
					return default;
				}

				if(!subscription.TryActivate(subscriber))
				{
					subscriber.Closed -= OnClosed;
					await subscriber.DisposeAsync();
					return this.Subscribers.TryGetValue(topic, out var existing) ? existing : default;
				}

				if(subscriber.IsClosed)
				{
					subscriber.Closed -= OnClosed;

					if(this.Remove(topic, subscription, out var removed))
						this.OnUnsubscribed(removed);

					await subscriber.DisposeAsync();
					return default;
				}

				return subscriber;
			}
			catch
			{
				if(subscriber != null)
				{
					subscriber.Closed -= OnClosed;

					if(this.Remove(topic, subscription, out var removed))
						this.OnUnsubscribed(removed);

					await subscriber.DisposeAsync();
				}

				throw;
			}
			finally
			{
				if(!subscription.IsActive)
				{
					this.Subscribers.Remove(topic, subscription);
					subscription.TryRemove(out _);
				}
			}

			void OnClosed(object sender, EventArgs args)
			{
				if(sender is not TSubscriber consumer)
					return;

				consumer.Closed -= OnClosed;

				if(this.Remove(topic, subscription, out var removed))
					this.OnUnsubscribed(removed);
			}
		}
	}

	protected abstract ValueTask<bool> OnSubscribeAsync(TSubscriber subscriber, CancellationToken cancellation);
	protected abstract ValueTask<TSubscriber> CreateSubscriberAsync(string topic, string tags, IHandler<Message> handler, MessageSubscribeOptions options, CancellationToken cancellation);
	protected virtual void OnUnsubscribed(TSubscriber subscriber) { }
	#endregion

	#region 虚拟方法
	protected virtual string GetTopic(string topic) => topic ?? string.Empty;
	protected virtual MessageReliability Reliability => MessageReliability.MostOnce;
	#endregion

	#region 重写方法
	public override string ToString() => $"[{this.GetType().Name}]{this.Name}";
	#endregion

	#region 私有方法
	private bool Remove(string topic, Subscription subscription, out TSubscriber subscriber)
	{
		if(!this.Subscribers.Remove(topic, subscription))
		{
			subscriber = default;
			return false;
		}

		return subscription.TryRemove(out subscriber);
	}
	#endregion

	#region 资源释放
	protected virtual void Dispose(bool disposing) { }
	public void Dispose()
	{
		var disposing = Interlocked.CompareExchange(ref _disposing, DISPOSING, 0);
		if(disposing != 0)
			return;

		try
		{
			_cancellation.Cancel();
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		finally
		{
			_cancellation.Dispose();
			_disposing = DISPOSED;
		}
	}
	#endregion

	#region 嵌套子类
	public sealed class SubscriberCollection : IReadOnlyCollection<TSubscriber>
	{
		#region 成员字段
		private readonly ConcurrentDictionary<string, Subscription> _entries = new();
		#endregion

		#region 公共属性
		public int Count
		{
			get
			{
				var count = 0;

				foreach(var subscription in _entries.Values)
				{
					if(subscription.TryGetValue(out _))
						count++;
				}

				return count;
			}
		}

		public TSubscriber this[string topic] => this.TryGetValue(topic, out var subscriber) ? subscriber : throw new KeyNotFoundException();
		#endregion

		#region 公共方法
		public bool TryGetValue(string topic, out TSubscriber subscriber)
		{
			ArgumentNullException.ThrowIfNull(topic);

			if(_entries.TryGetValue(topic, out var subscription))
				return subscription.TryGetValue(out subscriber);

			subscriber = default;
			return false;
		}
		#endregion

		#region 内部方法
		internal Subscription GetOrAdd(string topic, Func<string, Subscription> factory) => _entries.GetOrAdd(topic, factory);
		internal bool Remove(string topic, Subscription subscription) => _entries.TryRemove(new KeyValuePair<string, Subscription>(topic, subscription));
		#endregion

		#region 枚举遍历
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		public IEnumerator<TSubscriber> GetEnumerator()
		{
			foreach(var subscription in _entries.Values)
			{
				if(subscription.TryGetValue(out var subscriber))
					yield return subscriber;
			}
		}
		#endregion
	}

	internal sealed class Subscription
	{
		#region 状态常量
		private const int INITIALIZING = 0;
		private const int ACTIVE = 1;
		private const int REMOVED = 2;
		#endregion

		#region 成员字段
		private int _state;
		private int _observed;
		private TSubscriber _subscriber;
		private readonly Lazy<Task<TSubscriber>> _initialization;
		#endregion

		#region 构造函数
		public Subscription(IHandler<Message> handler, string[] tags, MessageSubscribeOptions options, Func<Subscription, Task<TSubscriber>> initialize)
		{
			ArgumentNullException.ThrowIfNull(initialize);
			this.Handler = handler;
			this.Tags = tags ?? [];
			this.Options = options ?? MessageSubscribeOptions.Default;
			_initialization = new Lazy<Task<TSubscriber>>(() => initialize(this), LazyThreadSafetyMode.ExecutionAndPublication);
		}
		#endregion

		#region 公共属性
		public bool IsActive => Volatile.Read(ref _state) == ACTIVE;
		public string[] Tags { get; }
		public IHandler<Message> Handler { get; }
		public MessageSubscribeOptions Options { get; }
		#endregion

		#region 公共方法
		public Task<TSubscriber> GetTask()
		{
			var task = _initialization.Value;

			if(Interlocked.Exchange(ref _observed, 1) == 0)
			{
				_ = task.ContinueWith(
					completed => _ = completed.Exception,
					CancellationToken.None,
					TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnFaulted,
					TaskScheduler.Default);
			}

			return task;
		}

		public bool Matches(IHandler<Message> handler, string[] tags, MessageSubscribeOptions options)
		{
			if(!HandlerEquals(this.Handler, handler) || this.Options.Reliability != options.Reliability || this.Options.FallbackBehavior != options.FallbackBehavior)
				return false;

			tags ??= [];
			if(this.Tags.Length != tags.Length)
				return false;

			for(var index = 0; index < tags.Length; index++)
			{
				if(!string.Equals(this.Tags[index], tags[index], StringComparison.OrdinalIgnoreCase))
					return false;
			}

			return true;

			static bool HandlerEquals(IHandler<Message> x, IHandler<Message> y) =>
				ReferenceEquals(x, y) || x is HandlerAdapter first && y is HandlerAdapter second && first.Handler == second.Handler;
		}

		public bool TryActivate(TSubscriber subscriber)
		{
			_subscriber = subscriber;
			return Interlocked.CompareExchange(ref _state, ACTIVE, INITIALIZING) == INITIALIZING;
		}

		public bool TryGetValue(out TSubscriber subscriber)
		{
			if(Volatile.Read(ref _state) == ACTIVE)
			{
				subscriber = _subscriber;
				return true;
			}

			subscriber = default;
			return false;
		}

		public bool TryRemove(out TSubscriber subscriber)
		{
			if(Interlocked.Exchange(ref _state, REMOVED) == ACTIVE)
			{
				subscriber = _subscriber;
				return true;
			}

			subscriber = default;
			return false;
		}
		#endregion
	}

	private sealed class HandlerAdapter(Action<Message> handler) : HandlerBase<Message>
	{
		private readonly Action<Message> _handler = handler ?? throw new ArgumentNullException(nameof(handler));
		public Action<Message> Handler => _handler;

		protected override ValueTask OnHandleAsync(Message argument, Parameters parameters, CancellationToken cancellation)
		{
			_handler.Invoke(argument);
			return ValueTask.CompletedTask;
		}
	}
	#endregion
}

public abstract class MessageQueueBase<TSubscriber, TSettings>(string name, TSettings settings = default) : MessageQueueBase<TSubscriber>(name)
	where TSubscriber : IMessageConsumer
	where TSettings : IMessageQueueSettings
{
	#region 公共属性
	public TSettings Settings { get; set; } = settings;
	#endregion

	#region 重写方法
	protected override string GetTopic(string topic) => string.IsNullOrEmpty(topic) ? this.Settings["Topic"] ?? string.Empty : topic;
	public override string ToString() => this.Settings is null ?
		$"[{this.GetType().Name}]{this.Name}" :
		$"[{this.GetType().Name}]{this.Name}({this.Settings.Value})";
	#endregion
}
