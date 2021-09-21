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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Services;
using Zongsoft.Components;
using Zongsoft.Configuration;
using Zongsoft.Communication;

using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Client.Receiving;
using MQTTnet.Client.Publishing;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Extensions.ManagedClient;

namespace Zongsoft.Messaging.Mqtt
{
	[Service(typeof(IMessageTopic<TopicMessage>))]
	public class MqttQueue : IMessageTopic<TopicMessage>, IAsyncDisposable
	{
		#region 工厂字段
		private static readonly MqttFactory Factory = new MqttFactory();
		#endregion

		#region 成员字段
		private readonly IManagedMqttClient _client;
		private readonly IDictionary<string, MqttSubscriber> _subscribers;
		#endregion

		#region 构造函数
		public MqttQueue(string name)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			this.Name = name.Trim();
			this.Options = GetOptions(this.Name);
			_client = Factory.CreateManagedMqttClient();
			_subscribers = new Dictionary<string, MqttSubscriber>();

			_client.UseApplicationMessageReceivedHandler(OnHandleAsync);
		}
		#endregion

		#region 公共属性
		public string Name { get; }
		public IMessageTopicOptions Options { get; set; }
		public IHandler<TopicMessage> Handler { get; set; }
		public ICollection<MqttSubscriber> Subscribers { get => _subscribers.Values; }
		#endregion

		#region 订阅方法
		public bool Subscribe(string topic, string tags, MessageTopicSubscriptionOptions options = null)
		{
			if(!string.IsNullOrEmpty(tags))
				throw new ArgumentException($"The tags is not supported.");

			_client.SubscribeAsync(new MqttTopicFilter()
			{
				Topic = topic,
				QualityOfServiceLevel = options.Reliability.ToQoS(),
			}).GetAwaiter().GetResult();

			if(_subscribers.TryAdd(GetSubscriberKey(topic, tags), new MqttSubscriber(this, topic, tags)))
			{
				_client.EnsureStart(this.Options).GetAwaiter().GetResult();
				return true;
			}

			return false;
		}

		public async Task<bool> SubscribeAsync(string topic, string tags, MessageTopicSubscriptionOptions options = null)
		{
			if(!string.IsNullOrEmpty(tags))
				throw new ArgumentException($"The tags is not supported.");

			await _client.SubscribeAsync(new MqttTopicFilter()
			{
				Topic = topic,
				QualityOfServiceLevel = options.Reliability.ToQoS(),
			});

			if(_subscribers.TryAdd(GetSubscriberKey(topic, tags), new MqttSubscriber(this, topic, tags)))
			{
				await _client.EnsureStart(this.Options);
				return true;
			}

			return false;
		}

		internal Task UnsubscribeAsync(string topic) => _subscribers.Remove(topic) ? _client.UnsubscribeAsync(topic) : Task.CompletedTask;
		#endregion

		#region 处理方法
		private void OnHandle(MqttApplicationMessageReceivedEventArgs args)
		{
			var message = new TopicMessage(args.ApplicationMessage.Topic, args.ApplicationMessage.Payload)
			{
				Identity = args.ClientId
			};

			if(this.Handle(ref message))
				args.AcknowledgeAsync(CancellationToken.None).GetAwaiter().GetResult();
		}

		private async Task OnHandleAsync(MqttApplicationMessageReceivedEventArgs args)
		{
			var message = new TopicMessage(args.ApplicationMessage.Topic, args.ApplicationMessage.Payload)
			{
				Identity = args.ClientId
			};

			if(await this.HandleAsync(ref message))
				await args.AcknowledgeAsync(CancellationToken.None);
		}

		public virtual bool Handle(ref TopicMessage message) => this.Handler?.Handle(message) ?? false;
		public virtual Task<bool> HandleAsync(ref TopicMessage message, CancellationToken cancellation = default) => this.Handler?.HandleAsync(message, cancellation) ?? Task.FromResult(false);
		#endregion

		#region 发布方法
		public void Publish(ReadOnlySpan<byte> data, string topic, string tags, MessageTopicPublishOptions options = null)
		{
			var message = new MqttApplicationMessage()
			{
				Topic = topic,
				Payload = data.ToArray(),
				QualityOfServiceLevel = options == null ? MQTTnet.Protocol.MqttQualityOfServiceLevel.AtMostOnce : options.Reliability.ToQoS(),
			};

			var result = _client.EnsureStart(this.Options)
				.ContinueWith
				(
					(task, arg) => _client.PublishAsync((MqttApplicationMessage)arg),
					message
				).GetAwaiter().GetResult();
		}

		public Task PublishAsync(ReadOnlySpan<byte> data, string topic, string tags = null, MessageTopicPublishOptions options = null, CancellationToken cancellation = default)
		{
			var message = new MqttApplicationMessage()
			{
				Topic = topic,
				Payload = data.ToArray(),
				QualityOfServiceLevel = options == null ? MQTTnet.Protocol.MqttQualityOfServiceLevel.AtMostOnce : options.Reliability.ToQoS(),
			};

			return _client.EnsureStart()
				.ContinueWith
				(
					(task, arg) => _client.PublishAsync((MqttApplicationMessage)arg),
					message,
					cancellation
				);
		}
		#endregion

		#region 私有方法
		private static string GetSubscriberKey(string topic, string tags) => string.IsNullOrEmpty(tags) ? topic : topic + ':' + tags;
		private static IMessageTopicOptions GetOptions(string name) => ApplicationContext.Current?.Configuration.GetOption<IMessageTopicOptions>("/Messaging/Mqtt/" + name);
		#endregion

		#region 处置方法
		public async ValueTask DisposeAsync()
		{
			await _client.StopAsync();
		}
		#endregion
	}
}
