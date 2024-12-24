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

using Zongsoft.Services;
using Zongsoft.Components;
using Zongsoft.Collections;
using Zongsoft.Communication;

namespace Zongsoft.Messaging.ZeroMQ;

[System.Reflection.DefaultMember(nameof(Handlers))]
[System.ComponentModel.DefaultProperty(nameof(Handlers))]
public class ZeroResponder : WorkerBase, IResponder
{
	#region 私有字段
	private ZeroQueue _queue;
	private Adapter _adapter;
	private List<ZeroSubscriber> _subscribers;
	#endregion

	#region 构造函数
	public ZeroResponder(string name = null) : base(name)
	{
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

	#region 重写方法
	protected override async Task OnStartAsync(string[] args, CancellationToken cancellation)
	{
		var queue = _queue;
		if(queue == null)
			return;

		_adapter = new Adapter(this);
		_subscribers = new List<ZeroSubscriber>();

		foreach(var handler in this.Handlers)
		{
			var urls = handler.GetUrls();

			if(urls == null || urls.Length == 0)
				continue;

			for(int i = 0; i < urls.Length; i++)
			{
				var subscriber = await queue.SubscribeAsync(urls[i], _adapter, cancellation);
				if(subscriber != null)
					_subscribers.Add(subscriber);
			}
		}
	}

	protected override async Task OnStopAsync(string[] args, CancellationToken cancellation)
	{
		if(_subscribers == null || _subscribers.Count == 0)
			return;

		foreach(var subscriber in _subscribers)
		{
			await subscriber.UnsubscribeAsync(cancellation);
		}

		_subscribers.Clear();
	}
	#endregion

	#region 公共方法
	public ValueTask OnRequested(IRequest request, CancellationToken cancellation)
	{
		//获取请求对应的处理器
		var handler = HandlerSelector.Default.GetHandler(this.Handlers, request.Url);

		if(handler != null)
			return handler.HandleAsync(request, Parameters.Parameter<IResponder>(this), cancellation);

		return ValueTask.CompletedTask;
	}

	public async ValueTask RespondAsync(IResponse response, CancellationToken cancellation = default)
	{
		if(response == null)
			throw new ArgumentNullException(nameof(response));

		await _queue.ProduceAsync($"{response.Url}", ZeroResponse.Pack(response), null, cancellation);
	}
	#endregion

	#region 嵌套子类
	private sealed class Adapter(ZeroResponder responder) : HandlerBase<Message>
	{
		private readonly ZeroResponder _responder = responder;

		protected override ValueTask OnHandleAsync(Message message, Parameters parameters, CancellationToken cancellation)
		{
			if(message.IsEmpty)
				return ValueTask.CompletedTask;

			var request = ZeroRequest.Unpack(message.Topic, message.Data);
			return request != null ? _responder.OnRequested(request, cancellation) : ValueTask.CompletedTask;
		}
	}
	#endregion
}
