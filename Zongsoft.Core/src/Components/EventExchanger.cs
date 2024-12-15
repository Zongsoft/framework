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
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Common;
using Zongsoft.Services;
using Zongsoft.Messaging;
using Zongsoft.Collections;
using Zongsoft.Serialization;

namespace Zongsoft.Components;

public class EventExchanger : WorkerBase
{
	#region 常量定义
	private const string DEFAULT_TOPIC = "Events";
	#endregion

	#region 私有变量
	private readonly MessageEnqueueOptions _mostOnce = new(MessageReliability.MostOnce);
	private readonly MessageEnqueueOptions _leastOnce = new(MessageReliability.LeastOnce);
	private readonly MessageEnqueueOptions _exactlyOnce = new(MessageReliability.ExactlyOnce);
	#endregion

	#region 成员字段
	private readonly uint _identifier;
	private readonly Handler _handler;

	private EventExchangerOptions _options;
	private IMessageQueue _queue;
	private IMessageConsumer _subscriber;
	private Func<string, (EventRegistryBase Registry, EventDescriptor Descriptor)> _locator;
	#endregion

	#region 构造函数
	public EventExchanger() : this(null, null) { }
	public EventExchanger(EventExchangerOptions options) : this(null, options) { }
	public EventExchanger(IMessageQueue queue, EventExchangerOptions options = null)
	{
		_queue = queue;
		_options = options ?? new();
		_identifier = Randomizer.GenerateUInt32();
		_locator = Locate;
		_handler = new Handler(this);
	}
	#endregion

	#region 公共属性
	/// <summary>获取事件交换器的实例编号。</summary>
	public uint Identifier => _identifier;

	/// <summary>获取或设置进行事件交换的消息队列。</summary>
	[System.ComponentModel.TypeConverter(typeof(MessageQueueConverter))]
	public IMessageQueue Queue
	{
		get => _queue;
		set => _queue = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <summary>获取或设置进行事件交换器的设置选项。</summary>
	public EventExchangerOptions Options
	{
		get => _options;
		set => _options = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <summary>获取或设置事件处理程序定位器。</summary>
	public Func<string, (EventRegistryBase Registry, EventDescriptor Descriptor)> Locator
	{
		get => _locator;
		set => _locator = value ?? Locate;
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

		var reliability = this.Options.Reliability switch
		{
			MessageReliability.MostOnce => _mostOnce,
			MessageReliability.LeastOnce => _leastOnce,
			MessageReliability.ExactlyOnce => _exactlyOnce,
			_ => _leastOnce,
		};

		var ticket = new ExchangingTicket(_identifier, context.QualifiedName, Events.Marshaler.Marshal(context));
		var json = await Serializer.Json.SerializeAsync(ticket, null, cancellation);
		await queue.ProduceAsync(this.Options.Topic, Encoding.UTF8.GetBytes(json), reliability, cancellation);
	}
	#endregion

	#region 事件定位
	private static (EventRegistryBase Registry, EventDescriptor Descriptor) Locate(string qualifiedName)
	{
		var descriptor = Events.GetEvent(qualifiedName, out var registry);
		return (registry, descriptor);
	}
	#endregion

	#region 重写方法
	protected override async Task OnStartAsync(string[] args, CancellationToken cancellation)
	{
		if(_queue == null)
			throw new InvalidOperationException($"Missing required message queue.");

		//订阅消息队列中的事件主题
		_subscriber = await _queue.SubscribeAsync(this.Options.Topic, _handler, new MessageSubscribeOptions(_options.Reliability), cancellation);
	}

	protected override async Task OnStopAsync(string[] args, CancellationToken cancellation)
	{
		var subscriber = Interlocked.Exchange(ref _subscriber, null);

		if(subscriber != null)
		{
			await subscriber.CloseAsync(cancellation);
			await subscriber.DisposeAsync();
		}
	}
	#endregion

	#region 嵌套结构
	private struct ExchangingTicket
	{
		#region 常量定义
		//表示是否启用数据压缩的阈值
		private const int COMPRESSION_THRESHOLD = 4 * 1024;
		#endregion

		#region 构造函数
		public ExchangingTicket(uint id, string @event, byte[] data)
		{
			this.Exchanger = id;
			this.Event = @event;
			this.Data = Compress(data, out var compressed);
			this.Compressed = compressed;
		}
		#endregion

		#region 公共属性
		/// <summary>表示事件交换器的实例号。</summary>
		public uint Exchanger { get; set; }
		/// <summary>表示事件的标识，即事件限定名称。</summary>
		public string Event { get; set; }
		/// <summary>表示事件数据的压缩器名称。</summary>
		public bool Compressed { get; set; }
		/// <summary>表示事件上下文对象的序列化数据。</summary>
		public byte[] Data { get; set; }
		#endregion

		#region 压缩方法
		private static byte[] Compress(byte[] data, out bool compressed)
		{
			if(data == null || data.Length < COMPRESSION_THRESHOLD)
			{
				compressed = false;
				return data;
			}

			using var source = new MemoryStream(data);
			using var destination = new MemoryStream();
			using var compression = new BrotliStream(destination, CompressionMode.Compress, true);
			source.CopyTo(compression);
			compression.Close();

			destination.Seek(0, SeekOrigin.Begin);
			compressed = true;
			return destination.ToArray();
		}

		internal readonly byte[] Decompress()
		{
			if(!this.Compressed || this.Data == null || this.Data.Length == 0)
				return this.Data;

			using var source = new MemoryStream(this.Data);
			using var destination = new MemoryStream();
			using var compression = new BrotliStream(source, CompressionMode.Decompress);
			compression.CopyTo(destination);
			destination.Seek(0, SeekOrigin.Begin);
			return destination.ToArray();
		}
		#endregion

		#region 重写方法
		public readonly override string ToString() => $"{this.Event}@{this.Exchanger}";
		#endregion
	}

	private sealed class Handler(EventExchanger exchanger) : HandlerBase<Message>
	{
		private readonly EventExchanger _exchanger = exchanger;

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
			if(ticket.Exchanger == _exchanger.Identifier || string.IsNullOrEmpty(ticket.Event))
			{
				//await message.AcknowledgeAsync(cancellation);
				return;
			}

			//根据事件标识获取对应的事件登记簿及对应的事件描述器
			(var registry, var descriptor) = _exchanger.Locator.Invoke(ticket.Event);
			if(descriptor == null || registry == null)
				return;

			//尝试解压缩
			var data = ticket.Decompress();

			//还原事件参数
			(var argument, var parameters) = Events.Marshaler.Unmarshal(descriptor, data);

			//重放事件
			await registry.RaiseAsync(descriptor, registry.GetContext(descriptor.Name, argument, parameters), cancellation);

			//应答消息
			await message.AcknowledgeAsync(cancellation);
		}
	}
	#endregion
}
