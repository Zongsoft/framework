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
using System.Text;
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
using MQTTnet.Exceptions;

namespace Zongsoft.Messaging.Mqtt
{
	public class MqttQueue : MessageQueueBase<MqttSubscriber>
	{
		#region 工厂字段
		private static readonly MqttFactory Factory = new();
		#endregion

		#region 成员字段
		private IMqttClient _client;
		private MqttClientOptions _options;
		private AutoResetEvent _semaphore;
		#endregion

		#region 构造函数
		public MqttQueue(string name, IConnectionSettings connectionSettings) : base(name, connectionSettings)
		{
			_client = Factory.CreateMqttClient();
			_options = MqttUtility.GetOptions(connectionSettings);
			_semaphore = new AutoResetEvent(true);

			//挂载消息接收事件
			_client.ApplicationMessageReceivedAsync += this.OnReceivedAsync;
			_client.DisconnectedAsync += this.OnDisconnectedAsync;
		}
		#endregion

		#region 订阅方法
		protected override async ValueTask<bool> OnSubscribeAsync(MqttSubscriber subscriber, CancellationToken cancellation = default)
		{
			try
			{
				//确保连接完成
				await this.EnsureConnectAsync(cancellation);

				//执行订阅操作
				var result = await _client.SubscribeAsync(subscriber.Subscription, cancellation);

				//返回订阅结果
				return result.Items != null && result.Items.Count > 0;
			}
			catch(Exception ex)
			{
				Zongsoft.Diagnostics.Logger.GetLogger<MqttQueue>().Error(ex);
				return false;
			}
		}

		protected override MqttSubscriber CreateSubscriber(string topic, string tags, IHandler<Message> handler, MessageSubscribeOptions options)
		{
			var subscriber = new MqttSubscriber(this, topic, handler, options);

			subscriber.Subscription.TopicFilters.Add(new MqttTopicFilter()
			{
				Topic = topic,
				NoLocal = true,
				QualityOfServiceLevel = options == null ? MqttQualityOfServiceLevel.AtLeastOnce : options.Reliability.ToQoS(),
			});

			return subscriber;
		}

		internal ValueTask UnsubscribeAsync(MqttSubscriber subscriber) => new(_client.UnsubscribeAsync(subscriber.Topic));
		#endregion

		#region 发布方法
		public override async ValueTask<string> ProduceAsync(string topic, string tags, ReadOnlyMemory<byte> data, MessageEnqueueOptions options = null, CancellationToken cancellation = default)
		{
			var message = new MqttApplicationMessage()
			{
				Topic = topic,
				PayloadSegment = data.ToArray(),
				QualityOfServiceLevel = options == null ? MqttQualityOfServiceLevel.AtMostOnce : options.Reliability.ToQoS(),
			};

			//确保连接完成
			await this.EnsureConnectAsync(cancellation);

			try
			{
				var result = await _client.PublishAsync(message, cancellation);
				return result.IsSuccess && result.PacketIdentifier.HasValue ? result.PacketIdentifier.ToString() : null;
			}
			catch(Exception ex)
			{
				Zongsoft.Diagnostics.Logger.GetLogger<MqttQueue>().Error(ex);
				return null;
			}
		}
		#endregion

		#region 事件处理
		private async Task OnReceivedAsync(MqttApplicationMessageReceivedEventArgs args)
		{
			//关闭自动应答
			args.AutoAcknowledge = false;

			var data = args.ApplicationMessage.PayloadSegment.ToArray();
			var message = new Message(args.ApplicationMessage.Topic, data, AcknowledgeAsync)
			{
				Identity = args.ClientId
			};

			if(this.Subscribers.TryGetValue(message.Topic, out var subscriber) && !subscriber.IsUnsubscribed)
				await InvokeHandler(subscriber.Handler, message);

			ValueTask AcknowledgeAsync(CancellationToken cancellation)
			{
				args.IsHandled = true;
				return new ValueTask(args.AcknowledgeAsync(cancellation));
			}

			static async ValueTask InvokeHandler(IHandler<Message> handler, Message message)
			{
				if(handler == null)
					return;

				try
				{
					await handler.HandleAsync(message);
				}
				catch(Exception ex)
				{
					Zongsoft.Diagnostics.Logger.GetLogger<MqttQueue>().Error(ex);
				}
			}
		}

		private async Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs args)
		{
			var logger = Zongsoft.Diagnostics.Logger.GetLogger<MqttQueue>();

			logger.Error($"MQTT is Disconnected. (ThreadId:{Environment.CurrentManagedThreadId})" + Environment.NewLine + GetDisconnectedMessage(args));

			try
			{
				_semaphore.WaitOne();

				if(_client.IsConnected)
				{
					logger.Error($"MQTT does not need to reconnect again because other threads have already reconnected successfully. (ThreadId:{Environment.CurrentManagedThreadId})");
					return;
				}

				var connection = await _client.ConnectAsync(_options);
				if(connection.ResultCode == MqttClientConnectResultCode.Success)
					SpinWait.SpinUntil(() => _client.IsConnected);

				if(connection.ResultCode != MqttClientConnectResultCode.Success)
				{
					logger.Error($"MQTT Reconnected Failed. (ThreadId:{Environment.CurrentManagedThreadId})" + Environment.NewLine + GetConnectResultInfo(connection, false));
					return;
				}

				logger.Error($"MQTT Reconnected Succeed. (ThreadId:{Environment.CurrentManagedThreadId})");

				foreach(var subscriber in this.Subscribers)
				{
					logger.Error($"MQTT is ready to start resubscription:“{subscriber.Topic}”. (ThreadId:{Environment.CurrentManagedThreadId})");
					var subscription = await _client.SubscribeAsync(subscriber.Subscription);
					logger.Error($"MQTT resubscription complete. (ThreadId:{Environment.CurrentManagedThreadId})" + Environment.NewLine + GetSubscribeResultInfo(subscription));
				}
			}
			finally
			{
				_semaphore.Set();
			}

			static string GetDisconnectedMessage(MqttClientDisconnectedEventArgs args)
			{
				if(args == null)
					return string.Empty;

				return $"Reason: {args.Reason}({args.ReasonString})" + Environment.NewLine +
					$"ClientWasConnected: {args.ClientWasConnected}" + Environment.NewLine +
					$"Exception: {args.Exception}" + Environment.NewLine +
					"Result: {" + Environment.NewLine +
						GetConnectResultInfo(args.ConnectResult, true) + Environment.NewLine +
					"}";
			}

			static string GetConnectResultInfo(MqttClientConnectResult result, bool indented)
			{
				if(result == null)
					return string.Empty;

				var indent = indented ? "\t" : string.Empty;

				return $"{indent}ClientId: {result.AssignedClientIdentifier}" + Environment.NewLine +
					$"{indent}ResultCode: {result.ResultCode}" + Environment.NewLine +
					$"{indent}Reason: {result.ReasonString}" + Environment.NewLine +
					$"{indent}IsSessionPresent: {result.IsSessionPresent}" + Environment.NewLine;
			}

			static string GetSubscribeResultInfo(MqttClientSubscribeResult result)
			{
				if(result == null)
					return string.Empty;

				var text = new StringBuilder($"PacketId: {result.PacketIdentifier}" + Environment.NewLine +
					$"Reason: {result.ReasonString}" + Environment.NewLine +
					$"Items:" + Environment.NewLine +
					"{");

				if(result.Items != null && result.Items.Count > 0)
				{
					foreach(var item in result.Items)
					{
						text.AppendLine($"\tResultCode:{item.ResultCode}, Topic:{item.TopicFilter?.Topic}");
					}
				}

				text.Append('}');
				return text.ToString();
			}
		}
		#endregion

		#region 重写方法
		public override string ToString()
		{
			var settings = this.ConnectionSettings;
			return settings == null ? this.Name : $"{this.Name}{Environment.NewLine}Server={settings.Server};Instance={settings.Instance};Client={settings.Client}";
		}
		#endregion

		#region 私有方法
		private async ValueTask EnsureConnectAsync(CancellationToken cancellation)
		{
			if(_client.IsConnected)
				return;

			try
			{
				_semaphore.WaitOne();

				if(_client.IsConnected)
					return;

				var result = await _client.ConnectAsync(_options, cancellation);
				if(result.ResultCode == MqttClientConnectResultCode.Success)
					SpinWait.SpinUntil(() => _client.IsConnected, TimeSpan.FromSeconds(1));
			}
			catch(InvalidOperationException)
			{
				SpinWait.SpinUntil(() => _client.IsConnected, TimeSpan.FromSeconds(1));
			}
			finally
			{
				_semaphore.Set();
			}
		}
		#endregion

		#region 处置方法
		protected override void Dispose(bool disposing)
		{
			var client = Interlocked.Exchange(ref _client, null);

			if(client != null)
			{
				client.ApplicationMessageReceivedAsync -= this.OnReceivedAsync;
				client.DisconnectedAsync -= this.OnDisconnectedAsync;
			}

			client.Dispose();
		}
		#endregion
	}
}
