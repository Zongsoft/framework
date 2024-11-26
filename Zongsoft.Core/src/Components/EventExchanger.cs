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
 * Copyright (C) 2010-2023 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Common;
using Zongsoft.Services;
using Zongsoft.Messaging;
using Zongsoft.Serialization;
using Zongsoft.Collections;

namespace Zongsoft.Components;

public class EventExchanger : WorkerBase
{
	#region 常量定义
	private const string DEFAULT_TOPIC = "Events";
	#endregion

	#region 静态字段
	private static readonly uint _instance = Randomizer.GenerateUInt32();
	#endregion

	#region 成员字段
	private string _topic = DEFAULT_TOPIC;
	private IMessageQueue _queue;
	private IMessageConsumer _subscriber;
	#endregion

	#region 公共属性
	[System.ComponentModel.TypeConverter(typeof(MessageQueueConverter))]
	public IMessageQueue Queue
	{
		get => _queue;
		set => _queue = value ?? throw new ArgumentNullException(nameof(value));
	}

	public string Topic
	{
		get => _topic;
		set => _topic = value ?? throw new ArgumentNullException(nameof(value));
	}
	#endregion

	#region 公共方法
	public static Task BroadcastAsync(EventContext context, CancellationToken cancellation)
	{
		var tasks = ApplicationContext.Current.Workers
			.Where(worker => worker.Enabled && worker.State == WorkerState.Running)
			.OfType<EventExchanger>()
			.Select(exchanger => exchanger.ExchangeAsync(context, cancellation).AsTask());

		return Task.WhenAll(tasks);
	}

	public async ValueTask ExchangeAsync(EventContext context, CancellationToken cancellation)
	{
		if(this.State != WorkerState.Running)
			return;

		var queue = _queue;
		if(queue == null)
			return;

		var ticket = new ExchangingTicket(_instance, context.QualifiedName, Events.Marshaler.Marshal(context));
		var json = await Serializer.Json.SerializeAsync(ticket, null, cancellation);
		await queue.ProduceAsync(this.Topic, Encoding.UTF8.GetBytes(json), MessageEnqueueOptions.Default, cancellation);
	}
	#endregion

	#region 重写方法
	protected override async Task OnStartAsync(string[] args, CancellationToken cancellation)
	{
		if(_queue == null)
			throw new InvalidOperationException($"Missing required message queue.");

		//订阅消息队列中的事件主题
		_subscriber = await _queue.SubscribeAsync(this.Topic, Handler.Instance, cancellation);
	}

	protected override async Task OnStopAsync(string[] args, CancellationToken cancellation)
	{
		var subscriber = Interlocked.Exchange(ref _subscriber, null);

		if(subscriber != null)
		{
			await subscriber.UnsubscribeAsync(cancellation);
			subscriber.Dispose();
		}
	}
	#endregion

	#region 嵌套结构
	private struct ExchangingTicket(uint instance, string identifier, byte[] data)
	{
		/// <summary>表示事件交换器的实例号。</summary>
		public uint Instance = instance;
		/// <summary>表示事件的标识，即事件限定名称。</summary>
		public string Identifier = identifier;
		/// <summary>表示事件上下文对象的序列化数据。</summary>
		public byte[] Data = data;

		public override string ToString() => $"{this.Identifier}@{this.Instance}";
	}

	private sealed class Handler : HandlerBase<Message>
	{
		public static readonly Handler Instance = new();

		protected override async ValueTask OnHandleAsync(Message message, Parameters _, CancellationToken cancellation)
		{
			if(message.IsEmpty)
			{
				await message.AcknowledgeAsync(cancellation);
				return;
			}

			//反序列化事件上下文
			var ticket = await Serializer.Json.DeserializeAsync<ExchangingTicket>(message.Data, null, cancellation);

			//如果接收到的事件来源自自身则忽略该事件
			if(ticket.Instance == _instance || string.IsNullOrEmpty(ticket.Identifier))
			{
				await message.AcknowledgeAsync(cancellation);
				return;
			}

			//根据事件标识获取对应的事件描述器
			var descriptor = Events.GetEvent(ticket.Identifier, out var registry);

			//还原事件参数
			(var argument, var parameters) = Events.Marshaler.Unmarshal(descriptor, ticket.Data);

			//重放事件
			await registry.RaiseAsync(descriptor, registry.GetContext(descriptor.Name, argument, parameters), default);

			//应答消息
			await message.AcknowledgeAsync(cancellation);
		}
	}
	#endregion
}
