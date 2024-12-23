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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
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

using Zongsoft.Common;
using Zongsoft.Components;
using Zongsoft.Collections;
using Zongsoft.Communication;

namespace Zongsoft.Messaging.ZeroMQ;

[System.Reflection.DefaultMember(nameof(Handlers))]
[System.ComponentModel.DefaultProperty(nameof(Handlers))]
public class ZeroRequester : IRequester
{
	#region 私有字段
	private ZeroQueue _queue;
	private readonly Adapter _adapter;
	private readonly ConcurrentDictionary<string, Token> _tokens;
	#endregion

	#region 构造函数
	public ZeroRequester()
	{
		_adapter = new Adapter(this);
		_tokens = new ConcurrentDictionary<string, Token>();
		this.Handlers = new List<IHandler>();
	}
	#endregion

	#region 公共属性
	[System.ComponentModel.TypeConverter(typeof(MessageQueueConverter))]
	public ZeroQueue Queue
	{
		get => _queue;
		set => _queue = value ?? throw new ArgumentNullException(nameof(value));
	}

	public ICollection<IHandler> Handlers { get; }
	#endregion

	#region 公共方法
	public async ValueTask<IRequestToken> RequestAsync(string url, ReadOnlyMemory<byte> data, CancellationToken cancellation = default)
	{
		var request = new ZeroRequest(url, data);

		await _queue.SubscribeAsync(url + "/reply", _adapter, cancellation);
		await _queue.ProduceAsync(url, request.Pack(), null, cancellation);

		var token = new Token(request);
		return _tokens.TryAdd(request.Identifier, token) ? token : null;
	}

	ValueTask IRequester.OnRespondedAsync(IResponse response, CancellationToken cancellation) => this.OnRespondedAsync(response as ZeroResponse, cancellation);
	private async ValueTask OnRespondedAsync(ZeroResponse response, CancellationToken cancellation)
	{
		if(response != null && _tokens.TryRemove(response.Request.Identifier, out var token))
		{
			//设置请求令牌对应的响应
			token.Response(response);

			//必须通过请求令牌重新获取响应，因为可能在此之前已经有其他线程获取并处理了该响应
			response = (ZeroResponse)await token.GetResponseAsync(cancellation);

			if(response != null)
			{
				var handler = HandlerSelector.Default.GetHandler(this.Handlers, token.Request.Url);

				if(handler != null)
					await handler.HandleAsync(response, cancellation);
			}
		}
	}
	#endregion

	#region 私有方法
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

	private sealed class Token(ZeroRequest request) : IRequestToken
	{
		private const int PENDING = 0;
		private const int AVAILABLE = 1;
		private const int UNAVAILABLE = 2;

		private volatile int _state;
		private volatile ZeroResponse _response;

		IRequest IRequestToken.Request => this.Request;
		public ZeroRequest Request { get; } = request;

		internal void Response(ZeroResponse response)
		{
			var state = Interlocked.CompareExchange(ref _state, (response == null ? UNAVAILABLE : AVAILABLE), PENDING);
			if(state == PENDING)
				_response = response;
		}

		public ValueTask<IResponse> GetResponseAsync(CancellationToken cancellation = default) => this.GetResponseAsync(TimeSpan.Zero, cancellation);
		public ValueTask<IResponse> GetResponseAsync(TimeSpan timeout, CancellationToken cancellation = default)
		{
			if(_state == UNAVAILABLE)
				return ValueTask.FromResult<IResponse>(null);

			var state = Interlocked.CompareExchange(ref _state, UNAVAILABLE, AVAILABLE);
			if(state == AVAILABLE)
			{
				var response = Interlocked.Exchange(ref _response, null);
				return ValueTask.FromResult<IResponse>(response);
			}

			if(timeout > TimeSpan.Zero)
			{
				var source = new TaskCompletionSource<IResponse>();
				var registration = cancellation.Register(() => source.TrySetCanceled(), false);

				Task.Delay(timeout, cancellation).ContinueWith(task =>
				{
					if(Interlocked.CompareExchange(ref _state, UNAVAILABLE, AVAILABLE) == AVAILABLE)
					{
						var response = Interlocked.Exchange(ref _response, null);
						source.TrySetResult(response);
					}
				}, TaskContinuationOptions.ExecuteSynchronously);

				return new ValueTask<IResponse>(source.Task);
			}

			return new ValueTask<IResponse>(Task.FromResult<IResponse>(null));
		}
	}
	#endregion
}
