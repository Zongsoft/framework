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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

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

		var adapter = new Adapter(this);
		var subscribers = new List<ZeroSubscriber>();

		try
		{
			foreach(var handler in this.Handlers)
			{
				var urls = handler.GetUrls();

				if(urls == null || urls.Length == 0)
					continue;

				for(int i = 0; i < urls.Length; i++)
				{
					var subscriber = await queue.SubscribeAsync(urls[i], adapter, cancellation);
					if(subscriber != null)
						subscribers.Add(subscriber);
				}
			}

			//全部订阅成功后再发布到实例字段，避免失败启动留下半初始化状态。
			_subscribers = subscribers;
		}
		catch
		{
			//启动中途失败时回滚已经建立的订阅，防止队列中残留无主 subscriber。
			await UnsubscribeAsync(subscribers, CancellationToken.None);
			throw;
		}
	}

	protected override async Task OnStopAsync(string[] args, CancellationToken cancellation)
	{
		var subscribers = _subscribers;
		_subscribers = null;
		await UnsubscribeAsync(subscribers, cancellation);
	}

	private static async ValueTask UnsubscribeAsync(List<ZeroSubscriber> subscribers, CancellationToken cancellation)
	{
		if(subscribers == null || subscribers.Count == 0)
			return;

		foreach(var subscriber in subscribers)
			await subscriber.UnsubscribeAsync(cancellation);

		subscribers.Clear();
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
