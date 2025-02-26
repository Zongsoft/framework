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

using Zongsoft.Components;
using Zongsoft.Collections;
using Zongsoft.Communication;

namespace Zongsoft.Messaging.ZeroMQ;

partial class ZeroQueue
{
	private class EventChannel : ChannelBase, IEventChannel
	{
		#region 常量定义
		private const string TOPIC = "Events";
		#endregion

		#region 成员字段
		private ZeroQueue _queue;
		private ZeroSubscriber _subscriber;
		private readonly MessageEnqueueOptions _options;
		#endregion

		#region 构造函数
		internal EventChannel(ZeroQueue queue)
		{
			_queue = queue ?? throw new ArgumentNullException(nameof(queue));
			_options = new MessageEnqueueOptions();
			_options.Properties[Packetizer.Options.Compressive] = 4 * 1024; //开启压缩的阈值(4KB)
		}
		#endregion

		#region 公共方法
		public async ValueTask OpenAsync(EventExchanger exchanger, CancellationToken cancellation = default)
		{
			if(_queue == null)
				throw new ObjectDisposedException(this.GetType().Name);

			_subscriber = await _queue.SubscribeAsync(TOPIC, new Handler(exchanger), cancellation);
		}

		public async ValueTask SendAsync(EventContext context, CancellationToken cancellation = default)
		{
			if(_queue == null)
				throw new ObjectDisposedException(this.GetType().Name);

			await _queue.ProduceAsync($"{TOPIC}/{context.QualifiedName}", Events.Marshaler.Marshal(context), _options, cancellation);
		}
		#endregion

		#region 重写方法
		protected override ValueTask OnCloseAsync(CancellationToken cancellation) => _subscriber.CloseAsync(cancellation);
		protected override ValueTask DisposeAsync(bool disposing)
		{
			_queue = null;
			return base.DisposeAsync(disposing);
		}

		public override string ToString() => _queue == null ? $"{this.GetType().Name}(Disposed)" : $"{_queue.Name}#{_queue.Instance} ({_queue.Settings?.Server})";
		#endregion

		#region 嵌套子类
		private sealed class Handler(EventExchanger exchanger) : HandlerBase<Message>()
		{
			private readonly EventExchanger _exchanger = exchanger ?? throw new ArgumentNullException(nameof(exchanger));

			protected override ValueTask OnHandleAsync(Message message, Parameters parameters, CancellationToken cancellation)
			{
				if(message.IsEmpty)
					return ValueTask.CompletedTask;

				//从主题中提取所属的事件名称(完整限定名)
				var name = GetEventName(message.Topic);
				if(string.IsNullOrEmpty(name))
					return ValueTask.CompletedTask;

				//将消息内容还原成事件参数对象
				(var argument, parameters) = Events.Marshaler.Unmarshal(name, message.Data);

				//通过事件交换器重放接收到事件
				return _exchanger.RaiseAsync(name, argument, parameters, cancellation);
			}

			private static string GetEventName(string topic) => topic != null && topic.Length > TOPIC.Length + 1 && topic.StartsWith(TOPIC) ? topic[(TOPIC.Length + 1)..] : null;
		}
		#endregion
	}
}