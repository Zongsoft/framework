/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2021 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Messaging.Mqtt library.
 *
 * The Zongsoft.Messaging.Mqtt is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Messaging.Mqtt is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Messaging.Mqtt library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Zongsoft.Common;
using Zongsoft.Components;
using Zongsoft.Configuration;

using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Extensions.ManagedClient;

namespace Zongsoft.Messaging.Mqtt
{
	public class MqttQueue : MessageQueueBase
	{
		#region 工厂字段
		private static readonly MqttFactory Factory = new MqttFactory();
		#endregion

		#region 成员字段
		private IManagedMqttClient _client;
		private readonly ConcurrentDictionary<string, ISet<MqttSubscriber>> _subscribers;
		#endregion

		#region 构造函数
		public MqttQueue(string name, IConnectionSetting connectionSetting) : base(name, connectionSetting)
		{
			_client = Factory.CreateManagedMqttClient();
			_subscribers = new ConcurrentDictionary<string, ISet<MqttSubscriber>>();

			//挂载消息接收事件
			_client.ApplicationMessageReceivedAsync += OnReceivedAsync;
		}
		#endregion

		#region 公共属性
		public IEnumerable<MqttSubscriber> Subscribers { get => _subscribers.SelectMany(subscriber => subscriber.Value); }
		#endregion

		#region 订阅方法
		public override async ValueTask<IMessageConsumer> SubscribeAsync(string topics, string tags, IMessageHandler handler, MessageSubscribeOptions options, CancellationToken cancellation = default)
		{
			if(tags != null && tags.Any())
				throw new ArgumentException($"The tags is not supported.");

			var qos = options == null ? MqttQualityOfServiceLevel.AtMostOnce : options.Reliability.ToQoS();
			var subscriber = new MqttSubscriber(this, topics, tags, handler, options);
			var parts = topics.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

			foreach(var part in parts)
			{
				if(_subscribers.TryGetValue(part, out var hashset))
				{
					hashset.Add(subscriber);
				}
				else
				{
					if(_subscribers.TryAdd(part, new HashSet<MqttSubscriber>() { subscriber }))
					{
						await _client.SubscribeAsync(new[]
						{
							new MqttTopicFilter()
							{
								Topic = part,
								QualityOfServiceLevel = qos,
							}
						});
 
						await _client.EnsureStart(this.ConnectionSetting);
					}
					else
					{
						_subscribers[part].Add(subscriber);
					}
				}
			}

			//调用消费者的订阅方法，以更新其订阅状态
			await subscriber.SubscribeAsync(cancellation);

			return subscriber;
		}

		internal async ValueTask UnsubscribeAsync(IEnumerable<string> topics)
		{
			if(topics == null || !topics.Any())
				topics = _subscribers.Keys;

			foreach(var topic in topics)
				await this.UnsubscribeAsync(topic);
		}

		internal ValueTask UnsubscribeAsync(string topic) => _subscribers.TryRemove(topic, out var _) ?
			new ValueTask(_client.UnsubscribeAsync(topic)) :
			ValueTask.CompletedTask;
		#endregion

		#region 接收处理
		private async Task OnReceivedAsync(MqttApplicationMessageReceivedEventArgs args)
		{
			//关闭自动应答
			args.AutoAcknowledge = false;

			var message = new Message(args.ApplicationMessage.Topic, args.ApplicationMessage.Payload, (cancellation) => new ValueTask(args.AcknowledgeAsync(cancellation)))
			{
				Identity = args.ClientId
			};

			if(_subscribers.TryGetValue(message.Topic, out var subscribers))
			{
				foreach(var subscriber in subscribers)
				{
					if(subscriber.IsSubscribed)
						await subscriber.Handler?.HandleAsync(message).AsTask();
				}

				//没有异常则应答
				await args.AcknowledgeAsync(CancellationToken.None);
			}
		}
		#endregion

		#region 发布方法
		public string Produce(string topic, ReadOnlySpan<byte> data, MessageEnqueueOptions options = null)
		{
			var message = new MqttApplicationMessage()
			{
				Topic = topic,
				Payload = data.ToArray(),
				QualityOfServiceLevel = options == null ? MqttQualityOfServiceLevel.AtMostOnce : options.Reliability.ToQoS(),
			};

			var result = _client.EnsureStart(this.ConnectionSetting)
				.ContinueWith
				(
					(task, arg) => _client.InternalClient.PublishAsync((MqttApplicationMessage)arg),
					message
				).GetAwaiter().GetResult().Result;

			return result.PacketIdentifier.HasValue ? result.PacketIdentifier.ToString() : null;
		}

		public override async ValueTask<string> ProduceAsync(string topic, string tags, ReadOnlyMemory<byte> data, MessageEnqueueOptions options = null, CancellationToken cancellation = default)
		{
			var message = new MqttApplicationMessage()
			{
				Topic = topic,
				Payload = data.ToArray(),
				QualityOfServiceLevel = options == null ? MqttQualityOfServiceLevel.AtMostOnce : options.Reliability.ToQoS(),
			};

			await _client.EnsureStart(this.ConnectionSetting);
			var result = await _client.InternalClient.PublishAsync(message, cancellation);
			return result.PacketIdentifier.HasValue ? result.PacketIdentifier.ToString() : null;
		}
		#endregion

		#region 重写方法
		public override string ToString()
		{
			var setting = this.ConnectionSetting;

			if(setting == null)
				return this.Name;

			return $"{this.Name}{Environment.NewLine}Server={setting.Values.Server};Instance={setting.Values.Instance};Client={setting.Values.Client}";
		}
		#endregion

		#region 处置方法
		protected override void Dispose(bool disposing)
		{
			var client = Interlocked.Exchange(ref _client, null);

			if(client != null)
			{
				client.ApplicationMessageReceivedAsync -= OnReceivedAsync;
				client.Dispose();
			}
		}
		#endregion
	}
}
