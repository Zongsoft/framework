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
public class ZeroRequester : IRequester
{
	#region 常量定义
	//表示待删除令牌的缓存过期时长
	private static readonly TimeSpan PENDING_EXPIRATION = TimeSpan.FromSeconds(600);
	private static readonly TimeSpan SUBSCRIPTION_DELAY = TimeSpan.FromMilliseconds(100);
	#endregion

	#region 私有字段
	private ZeroQueue _queue;
	private readonly Adapter _adapter;
	private readonly ConcurrentDictionary<string, Token> _tokens;
	private readonly ConcurrentDictionary<string, Task> _subscriptions;
	private readonly MemoryCache _pending;
	#endregion

	#region 构造函数
	public ZeroRequester()
	{
		_adapter = new Adapter(this);
		_tokens = new ConcurrentDictionary<string, Token>();
		_subscriptions = new ConcurrentDictionary<string, Task>();
		this.Handlers = new List<IHandler>();

		_pending = new MemoryCache();
		_pending.Evicted += this.OnEvicted;
	}
	#endregion

	#region 公共属性
	[System.ComponentModel.TypeConverter(typeof(MessageQueueConverter))]
	public ZeroQueue Queue
	{
		get => _queue;
		set
		{
			_queue = value ?? throw new ArgumentNullException(nameof(value));
			_subscriptions.Clear();
		}
	}

	public ICollection<IHandler> Handlers { get; }
	#endregion

	#region 公共方法
	public async ValueTask<IRequestToken> RequestAsync(string url, ReadOnlyMemory<byte> data, CancellationToken cancellation = default)
	{
		var queue = this.Queue;
		if(queue == null)
			return null;

		var request = new ZeroRequest(url, data);
		var reply = url + "/reply";
		var token = new Token(request, request => this.Remove(request.Identifier));

		if(!_tokens.TryAdd(request.Identifier, token))
			return null;

		try
		{
			await this.SubscribeAsync(queue, reply, cancellation);
			await queue.ProduceAsync(url, request.Pack(), null, cancellation);
			return token;
		}
		catch
		{
			token.Dispose();
			throw;
		}
	}

	ValueTask IRequester.OnRespondedAsync(IResponse response, CancellationToken cancellation) => this.OnRespondedAsync(response as ZeroResponse, cancellation);
	private async ValueTask OnRespondedAsync(ZeroResponse response, CancellationToken cancellation)
	{
		var identifier = response?.Request?.Identifier;
		if(identifier == null)
			return;

		if(_tokens.TryGetValue(identifier, out var token))
		{
			//设置请求令牌对应的响应
			token.Response(response);

			//将当前响应对应的请求令牌加入到待删除缓存中
			_pending.SetValue(identifier, (object)null, PENDING_EXPIRATION);

			//获取响应的处理器
			var handler = HandlerSelector.Default.GetHandler(this.Handlers, response.Url);

			if(handler != null)
				await handler.HandleAsync(response, cancellation);
		}
	}
	#endregion

	#region 私有方法
	private async ValueTask SubscribeAsync(ZeroQueue queue, string topic, CancellationToken cancellation)
	{
		var task = _subscriptions.GetOrAdd(topic, topic =>
		{
			var task = SubscribeAsync(queue, topic, _adapter, cancellation);

			//首次订阅任务被取消或失败后不能永久缓存，否则后续请求无法重新订阅该回复主题。
			_ = task.ContinueWith(task =>
			{
				if(task.IsCanceled || task.IsFaulted)
					_subscriptions.TryRemove(new KeyValuePair<string, Task>(topic, task));
			}, TaskScheduler.Default);

			return task;
		});

		try
		{
			await task.WaitAsync(cancellation);
		}
		catch
		{
			if(task.IsFaulted || task.IsCanceled)
				_subscriptions.TryRemove(new KeyValuePair<string, Task>(topic, task));

			throw;
		}

		static async Task SubscribeAsync(ZeroQueue queue, string topic, IHandler<Message> handler, CancellationToken cancellation)
		{
			await queue.SubscribeAsync(topic, handler, cancellation);
			await Task.Delay(SUBSCRIPTION_DELAY, cancellation);
		}
	}

	private void Remove(string identifier)
	{
		if(identifier != null)
		{
			_pending.Remove(identifier);
			_tokens.Remove(identifier, out _);
		}
	}

	private void OnEvicted(object sender, CacheEvictedEventArgs args) => this.Remove(args.Key as string);
	private ZeroRequest GetRequest(string identifier) => identifier != null && _tokens.TryGetValue(identifier, out var token) ? token.Request : null;
	#endregion

	#region 嵌套子类
	private sealed class Adapter(ZeroRequester requester) : HandlerBase<Message>
	{
		public readonly ZeroRequester _requester = requester;

		protected override ValueTask OnHandleAsync(Message message, Parameters parameters, CancellationToken cancellation)
		{
			if(message.IsEmpty)
				return ValueTask.CompletedTask;

			(var identifier, var data) = ZeroResponse.Unpack(message.Data);
			if(string.IsNullOrEmpty(identifier))
				return ValueTask.CompletedTask;

			var request = _requester.GetRequest(identifier);
			if(request == null)
				return ValueTask.CompletedTask;

			return _requester.OnRespondedAsync(request.Response(message.Topic, data), cancellation);
		}
	}

	private sealed class Token : IRequestToken, IDisposable
	{
		#region 私有字段
		private Action<ZeroRequest> _disposed;
		private CancellationTokenSource _cancellation;
		private ConcurrentBag<ZeroResponse> _responses;
		#endregion

		#region 构造函数
		public Token(ZeroRequest request, Action<ZeroRequest> disposed)
		{
			this.Request = request ?? throw new ArgumentNullException(nameof(request));
			_disposed = disposed ?? throw new ArgumentNullException(nameof(disposed));
			_responses = new();
		}
		#endregion

		#region 公共属性
		IRequest IRequestToken.Request => this.Request;
		public ZeroRequest Request { get; }
		#endregion

		#region 内部方法
		internal void Response(ZeroResponse response)
		{
			var responses = _responses;

			if(response != null && responses != null)
				responses.Add(response);
		}
		#endregion

		#region 公共方法
		public IEnumerable<IResponse> GetResponses(CancellationToken cancellation = default) => this.GetResponses(TimeSpan.Zero, cancellation);
		public IEnumerable<IResponse> GetResponses(TimeSpan timeout, CancellationToken cancellation = default)
		{
			if(cancellation.IsCancellationRequested)
				yield break;

			var source = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
			var original = Interlocked.Exchange(ref _cancellation, source);
			original?.Dispose();

			try
			{
				if(timeout > TimeSpan.Zero)
				{
					source.CancelAfter(timeout);

					while(!source.IsCancellationRequested)
					{
						var responses = _responses;

						if(responses == null)
							yield break;

						if(responses.TryTake(out var response))
							yield return response;
						else
							SpinWait.SpinUntil(() => !responses.IsEmpty || source.IsCancellationRequested, 1);
					}
				}
				else
				{
					while(!source.IsCancellationRequested)
					{
						var responses = _responses;

						if(responses == null || !responses.TryTake(out var response))
							yield break;

						yield return response;
					}
				}
			}
			finally
			{
				if(Interlocked.CompareExchange(ref _cancellation, null, source) == source)
					source.Dispose();
			}
		}
		#endregion

		#region 处置方法
		public void Dispose()
		{
			var cancellation = Interlocked.Exchange(ref _cancellation, null);

			if(cancellation != null)
			{
				cancellation.Cancel();
				cancellation.Dispose();
			}

			var responses = Interlocked.Exchange(ref _responses, null);
			responses?.Clear();

			var disposed = Interlocked.Exchange(ref _disposed, null);
			disposed?.Invoke(this.Request);
		}
		#endregion
	}
	#endregion
}
