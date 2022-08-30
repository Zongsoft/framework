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
	public class MqttQueue : IMessageTopic<MessageTopicMessage>, IAsyncDisposable
	{
		#region 工厂字段
		private static readonly MqttFactory Factory = new MqttFactory();
		#endregion

		#region 成员字段
		private readonly IManagedMqttClient _client;
		private readonly IDictionary<string, MqttSubscriber> _subscribers;
		#endregion

		#region 构造函数
		public MqttQueue(string name, IConnectionSetting connectionSetting)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			this.Name = name.Trim();
			this.ConnectionSetting = connectionSetting;

			_client = Factory.CreateManagedMqttClient();
			_subscribers = new Dictionary<string, MqttSubscriber>();

			_client.ApplicationMessageReceivedAsync += OnHandleAsync;
			//_client.UseApplicationMessageReceivedHandler(OnHandleAsync);
		}
		#endregion

		#region 公共属性
		public string Name { get; }
		public IConnectionSetting ConnectionSetting { get; set; }
		public IHandler<MessageTopicMessage> Handler { get; set; }
		public ICollection<MqttSubscriber> Subscribers { get => _subscribers.Values; }
		IEnumerable<IMessageSubscriber> IMessageTopic.Subscribers => this.Subscribers;
		#endregion

		#region 订阅方法
		public async ValueTask<bool> SubscribeAsync(string topic, IEnumerable<string> tags, MessageTopicSubscriptionOptions options = null)
		{
			if(tags != null && tags.Any())
				throw new ArgumentException($"The tags is not supported.");
			var qos = options == null ? MqttQualityOfServiceLevel.AtMostOnce : options.Reliability.ToQoS();

			await _client.SubscribeAsync(new[]{
				new MqttTopicFilter()
				{
					Topic = topic,
					QualityOfServiceLevel = qos,
				}
			});

			if(_subscribers.TryAdd(GetSubscriberKey(topic, tags), new MqttSubscriber(this, topic, tags)))
			{
				await _client.EnsureStart(this.ConnectionSetting);
				return true;
			}

			return false;
		}

		internal ValueTask UnsubscribeAsync(string topic) => _subscribers.Remove(topic) ? new ValueTask(_client.UnsubscribeAsync(topic)) : ValueTask.CompletedTask;
		#endregion

		#region 处理方法
		private async Task OnHandleAsync(MqttApplicationMessageReceivedEventArgs args)
		{
			//关闭自动应答
			args.AutoAcknowledge = false;

			var message = new MessageTopicMessage(args.ApplicationMessage.Topic, args.ApplicationMessage.Payload, (cancellation) => new ValueTask(args.AcknowledgeAsync(cancellation)))
			{
				Identity = args.ClientId
			};

			if((await this.HandleAsync(ref message)).Succeed)
				await args.AcknowledgeAsync(CancellationToken.None);
		}

		public virtual ValueTask<OperationResult> HandleAsync(ref MessageTopicMessage message, CancellationToken cancellation = default) => this.Handler?.HandleAsync(this, message, cancellation) ?? ValueTask.FromResult(OperationResult.Fail());
		#endregion

		#region 发布方法
		public string Publish(ReadOnlySpan<byte> data, string topic, IEnumerable<string> tags, MessageTopicPublishOptions options = null)
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

		public ValueTask<string> PublishAsync(ReadOnlyMemory<byte> data, string topic, IEnumerable<string> tags = null, MessageTopicPublishOptions options = null, CancellationToken cancellation = default)
		{
			return this.PublishAsync(data.ToArray(), topic, tags, options, cancellation);
		}

		public async ValueTask<string> PublishAsync(byte[] data, string topic, IEnumerable<string> tags = null, MessageTopicPublishOptions options = null, CancellationToken cancellation = default)
		{
			var message = new MqttApplicationMessage()
			{
				Topic = topic,
				Payload = data,
				QualityOfServiceLevel = options == null ? MqttQualityOfServiceLevel.AtMostOnce : options.Reliability.ToQoS(),
			};

			await _client.EnsureStart(this.ConnectionSetting);
			var result = await _client.InternalClient.PublishAsync(message, cancellation);
			return result.PacketIdentifier.HasValue ? result.PacketIdentifier.ToString() : null;
		}
		#endregion

		#region 私有方法
		private static string GetSubscriberKey(string topic, IEnumerable<string> tags) => tags != null && tags.Any() ? topic + ':' + string.Join(',', tags) : topic;
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
		public async ValueTask DisposeAsync()
		{
			_client.ApplicationMessageReceivedAsync -= OnHandleAsync;
			await _client.StopAsync();
		}
		#endregion
	}
}
