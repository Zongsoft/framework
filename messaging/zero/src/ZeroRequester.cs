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
 * This file is part of Zongsoft.Messaging.ZeroMQ library.
 *
 * The Zongsoft.Messaging.ZeroMQ is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Messaging.ZeroMQ is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Messaging.ZeroMQ library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Zongsoft.Caching;
using Zongsoft.Components;
using Zongsoft.Collections;
using Zongsoft.Communication;

namespace Zongsoft.Messaging.ZeroMQ;

[System.Reflection.DefaultMember(nameof(Handlers))]
[System.ComponentModel.DefaultProperty(nameof(Handlers))]
public class ZeroRequester : IRequester, IDisposable, IAsyncDisposable
{
	#region 静态常量
	private static readonly TimeSpan PENDING_EXPIRATION = TimeSpan.FromSeconds(600);
	#endregion

	#region 成员字段
	private ZeroQueue _queue;
	private readonly Adapter _adapter;
	private readonly MemoryCache _pending;
	private readonly ConcurrentDictionary<string, Token> _tokens;
	private readonly ConcurrentDictionary<string, Task<ZeroSubscriber>> _subscriptions;
	private int _disposed;
	#endregion

	#region 构造函数
	public ZeroRequester()
	{
		_adapter = new Adapter(this);
		_subscriptions = new();
		_tokens = new();
		_pending = new();
		_pending.Evicted += this.OnEvicted;
		this.Handlers = new List<IHandler>();
	}
	#endregion

	#region 公共属性
	[System.ComponentModel.TypeConverter(typeof(MessageQueueConverter))]
	public ZeroQueue Queue
	{
		get => _queue;
		set
		{
			if(Volatile.Read(ref _disposed) != 0)
				throw new ObjectDisposedException(nameof(ZeroRequester));

			if(value == null)
				throw new ArgumentNullException(nameof(value));
			var current = _queue;

			if(current != null && !ReferenceEquals(current, value) && !_subscriptions.IsEmpty)
				throw new InvalidOperationException(Properties.Resources.ZeroRequester_QueueReplacementNotAllowed_Message);

			_queue = value;
		}
	}

	public ICollection<IHandler> Handlers { get; }
	#endregion

	#region 公共方法
	public async ValueTask<IRequestToken> RequestAsync(string url, ReadOnlyMemory<byte> data, CancellationToken cancellation = default)
	{
		if(Volatile.Read(ref _disposed) != 0)
			throw new ObjectDisposedException(nameof(ZeroRequester));

		var queue = this.Queue;
		if(queue == null)
			return null;

		var request = new ZeroRequest(url, data);
		var token = new Token(request, request => this.Remove(request.Identifier));

		if(!_tokens.TryAdd(request.Identifier, token))
			return null;

		try
		{
			await this.SubscribeAsync(queue, url + "/reply", cancellation);
			await queue.ProduceAsync(url, request.Pack(), null, cancellation);
			return token;
		}
		catch
		{
			token.Dispose();
			throw;
		}
	}
	#endregion

	#region 应答处理
	ValueTask IRequester.OnRespondedAsync(IResponse response, CancellationToken cancellation) => this.OnRespondedAsync(response as ZeroResponse, cancellation);
	private async ValueTask OnRespondedAsync(ZeroResponse response, CancellationToken cancellation)
	{
		var identifier = response?.Request?.Identifier;
		if(identifier == null || !_tokens.TryGetValue(identifier, out var token))
			return;

		token.Response(response);
		_pending.SetValue(identifier, (object)null, PENDING_EXPIRATION);

		var handler = HandlerSelector.Default.GetHandler(this.Handlers, response.Url);
		if(handler != null)
			await handler.HandleAsync(response, cancellation);
	}
	#endregion

	#region 私有方法
	private async ValueTask SubscribeAsync(ZeroQueue queue, string topic, CancellationToken cancellation)
	{
		var task = _subscriptions.GetOrAdd(topic, key => queue.SubscribeAsync(key, _adapter, CancellationToken.None).AsTask());

		try
		{
			if(cancellation.CanBeCanceled)
				await task.WaitAsync(cancellation);
			else
				await task;
		}
		catch
		{
			if(task.IsFaulted || task.IsCanceled)
				_subscriptions.TryRemove(new KeyValuePair<string, Task<ZeroSubscriber>>(topic, task));

			throw;
		}
	}

	private void Remove(string identifier)
	{
		if(identifier == null)
			return;

		_pending.Remove(identifier);
		_tokens.Remove(identifier, out _);
	}

	private void OnEvicted(object sender, CacheEvictedEventArgs args) => this.Remove(args.Key as string);
	private ZeroRequest GetRequest(string identifier) => identifier != null && _tokens.TryGetValue(identifier, out var token) ? token.Request : null;
	#endregion

	#region 释放资源
	public void Dispose() => this.DisposeAsync().AsTask().GetAwaiter().GetResult();
	public async ValueTask DisposeAsync()
	{
		if(Interlocked.Exchange(ref _disposed, 1) != 0)
			return;

		foreach(var token in _tokens.Values)
			token.Dispose();

		_tokens.Clear();

		foreach(var subscription in _subscriptions.Values)
		{
			try
			{
				var subscriber = await subscription;
				if(subscriber != null)
					await subscriber.DisposeAsync();
			}
			catch { }
		}

		_subscriptions.Clear();
		_pending.Evicted -= this.OnEvicted;
		_pending.Dispose();
		_queue = null;
		GC.SuppressFinalize(this);
	}
	#endregion

	private sealed class Adapter(ZeroRequester requester) : HandlerBase<Message>
	{
		protected override ValueTask OnHandleAsync(Message message, Parameters parameters, CancellationToken cancellation)
		{
			if(message.IsEmpty)
				return ValueTask.CompletedTask;

			(var identifier, var data) = ZeroResponse.Unpack(message.Data);
			if(string.IsNullOrEmpty(identifier))
				return ValueTask.CompletedTask;

			var request = requester.GetRequest(identifier);
			return request == null ? ValueTask.CompletedTask : requester.OnRespondedAsync(request.Response(message.Topic, data), cancellation);
		}
	}

	private sealed class Token : IRequestToken, IDisposable
	{
		private Action<ZeroRequest> _disposed;
		private ConcurrentQueue<ZeroResponse> _responses = new();
		private readonly SemaphoreSlim _signal = new(0);

		public Token(ZeroRequest request, Action<ZeroRequest> disposed)
		{
			this.Request = request ?? throw new ArgumentNullException(nameof(request));
			_disposed = disposed ?? throw new ArgumentNullException(nameof(disposed));
		}

		IRequest IRequestToken.Request => this.Request;
		public ZeroRequest Request { get; }

		internal void Response(ZeroResponse response)
		{
			var responses = _responses;
			if(response == null || responses == null)
				return;

			responses.Enqueue(response);
			_signal.Release();
		}

		public IEnumerable<IResponse> GetResponses(CancellationToken cancellation = default) => this.GetResponses(TimeSpan.Zero, cancellation);
		public IEnumerable<IResponse> GetResponses(TimeSpan timeout, CancellationToken cancellation = default)
		{
			var responses = _responses;
			if(responses == null || cancellation.IsCancellationRequested)
				yield break;

			if(timeout <= TimeSpan.Zero)
			{
				while(responses.TryDequeue(out var response))
				{
					_signal.Wait(0);
					yield return response;
				}

				yield break;
			}

			var deadline = DateTime.UtcNow + timeout;
			while(_responses != null && !cancellation.IsCancellationRequested)
			{
				if(responses.TryDequeue(out var response))
				{
					_signal.Wait(0);
					yield return response;
					continue;
				}

				var remaining = deadline - DateTime.UtcNow;
				if(remaining <= TimeSpan.Zero)
					yield break;

				bool signaled;
				try { signaled = _signal.Wait(remaining, cancellation); }
				catch(OperationCanceledException) { yield break; }

				if(!signaled)
					yield break;
			}
		}

		public void Dispose()
		{
			var responses = Interlocked.Exchange(ref _responses, null);
			if(responses == null)
				return;

			responses.Clear();
			_signal.Release();
			Interlocked.Exchange(ref _disposed, null)?.Invoke(this.Request);
		}
	}
}
