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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Text;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

using MQTTnet;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Diagnostics.Logger;

using Zongsoft.Components;

namespace Zongsoft.Messaging.Mqtt;

public class MqttQueue : MessageQueueBase<MqttSubscriber, Configuration.MqttConnectionSettings>
{
	#region 常量定义
	private static readonly int HandlerConcurrency = Math.Clamp(Environment.ProcessorCount * 2, 4, 256);
	#endregion

	#region 成员字段
	private readonly ConnectionManager _connection;
	private readonly SemaphoreSlim _dispatchers;
	private readonly CancellationTokenSource _cancellation;
	#endregion

	#region 构造函数
	public MqttQueue(string name, Configuration.MqttConnectionSettings settings) : base(name, settings)
	{
		ArgumentNullException.ThrowIfNull(settings);

		_dispatchers = new SemaphoreSlim(HandlerConcurrency, HandlerConcurrency);
		_cancellation = new CancellationTokenSource();
		this.Features.Add(MessageQueueFeature.Compression);
		_connection = new ConnectionManager(this, settings, _cancellation.Token);
	}
	#endregion

	#region 订阅方法
	protected override async ValueTask<bool> OnSubscribeAsync(MqttSubscriber subscriber, CancellationToken cancellation = default)
	{
		try
		{
			var client = await _connection.AcquireAsync(cancellation);

			try
			{
				var result = await client.SubscribeAsync(subscriber.Subscription, cancellation);
				return result.IsSuccessful();
			}
			finally
			{
				_connection.Release();
			}
		}
		catch(Exception ex)
		{
			await Zongsoft.Diagnostics.Logging.GetLogging<MqttQueue>().ErrorAsync(ex, cancellation);
			return false;
		}
	}

	protected override ValueTask<MqttSubscriber> CreateSubscriberAsync(string topic, string tags, IHandler<Message> handler, MessageSubscribeOptions options, CancellationToken cancellation)
	{
		var subscriber = new MqttSubscriber(this, topic, handler, options);

		subscriber.Subscription.TopicFilters.Add(new MqttTopicFilter()
		{
			Topic = topic,
			NoLocal = true,
			QualityOfServiceLevel = (options?.Reliability ?? MessageReliability.MostOnce).ToQoS(),
		});

		return ValueTask.FromResult(subscriber);
	}

	internal async ValueTask UnsubscribeAsync(MqttSubscriber subscriber, CancellationToken cancellation)
	{
		var client = await _connection.AcquireAsync(cancellation);

		try
		{
			await client.UnsubscribeAsync(subscriber.Topic, cancellation);
		}
		finally
		{
			_connection.Release();
		}
	}
	#endregion

	#region 发布方法
	protected override async ValueTask<string> OnProduceAsync(string topic, string tags, ReadOnlyMemory<byte> data, MessageEnqueueOptions options, CancellationToken cancellation)
	{
		if(string.IsNullOrEmpty(topic))
			throw new ArgumentNullException(nameof(topic));

		var builder = new MqttApplicationMessageBuilder()
			.WithTopic(topic)
			.WithQualityOfServiceLevel((options?.Reliability ?? MessageReliability.MostOnce).ToQoS());
		builder.SetPayload(data, options?.Compression ?? default, _connection.Options.ProtocolVersion == MQTTnet.Formatter.MqttProtocolVersion.V500);

		if(options != null && _connection.Options.ProtocolVersion == MQTTnet.Formatter.MqttProtocolVersion.V500)
		{
			if(options.Expiration > TimeSpan.Zero)
				builder.WithMessageExpiryInterval((uint)Math.Min(uint.MaxValue, Math.Ceiling(options.Expiration.TotalSeconds)));

			if(options.Properties != null && options.Properties.HasValue)
			{
				foreach(var property in options.Properties)
				{
					if(property.Key == null || property.Value == null)
						continue;

					builder.WithUserProperty(property.Key.ToString(), Encoding.UTF8.GetBytes(property.Value.ToString()));
				}
			}
		}

		var client = await _connection.AcquireAsync(cancellation);

		try
		{
			var result = await client.PublishAsync(builder.Build(), cancellation);
			if(!result.IsSuccess)
				throw new InvalidOperationException(string.Format(Properties.Resources.MqttQueue_PublishFailed_Message, result.ReasonCode, result.ReasonString));

			return result.PacketIdentifier?.ToString();
		}
		finally
		{
			_connection.Release();
		}
	}
	#endregion

	#region 事件处理
	private async Task OnReceivedAsync(MqttApplicationMessageReceivedEventArgs args)
	{
		//关闭自动应答，由 Message.AcknowledgeAsync() 显式完成 MQTT QoS 应答。
		args.AutoAcknowledge = false;

		byte[] payload;
		try { payload = args.ApplicationMessage.GetPayload(); }
		catch(Exception exception)
		{
			Diagnostics.Logging.GetLogging(this).Error(exception);
			return;
		}

		var message = new Message(args.ApplicationMessage.Topic, payload, AcknowledgeAsync)
		{
			Identity = args.ClientId,
			Identifier = args.PacketIdentifier == 0 ? null : args.PacketIdentifier.ToString(),
		};

		var subscriber = FindSubscriber(message.Topic);
		if(subscriber == null || subscriber.IsClosed)
		{
			await args.AcknowledgeAsync(_cancellation.Token);
			return;
		}

		if(subscriber.Handler == null)
		{
			await args.AcknowledgeAsync(_cancellation.Token);
			return;
		}

		//MQTTnet 为保证顺序会等待消息回调完成；这里使用有界并发派发，
		//避免慢消费者阻塞网络接收，同时限制在途消息数量以提供反压。
		await _dispatchers.WaitAsync(_cancellation.Token);
		_ = Task.Run(() => this.DispatchAsync(subscriber.Handler, message, args, _cancellation.Token));

		ValueTask AcknowledgeAsync(CancellationToken cancellation)
		{
			args.IsHandled = true;
			return new ValueTask(args.AcknowledgeAsync(cancellation));
		}

		MqttSubscriber FindSubscriber(string topic)
		{
			if(this.Subscribers.TryGetValue(topic, out var subscriber))
				return subscriber;

			foreach(var candidate in this.Subscribers)
			{
				if(MqttTopicFilterComparer.Compare(topic, candidate.Topic) == MqttTopicFilterCompareResult.IsMatch)
					return candidate;
			}

			return null;
		}
	}

	#endregion

	#region 消息派发
	private async Task DispatchAsync(IHandler<Message> handler, Message message, MqttApplicationMessageReceivedEventArgs args, CancellationToken cancellation)
	{
		try
		{
			await handler.HandleAsync(message, cancellation);
		}
		catch(OperationCanceledException) when(cancellation.IsCancellationRequested) { }
		catch(Exception ex)
		{
			args.ProcessingFailed = true;
			await Zongsoft.Diagnostics.Logging.GetLogging<MqttQueue>().ErrorAsync(ex);
		}
		finally
		{
			_dispatchers.Release();
		}
	}
	#endregion

	#region 重写方法
	protected override MessageReliability Reliability => MessageReliability.ExactlyOnce;

	public override string ToString()
	{
		var settings = this.Settings;
		return settings == null ? this.Name : $"{this.Name}{Environment.NewLine}Server={settings.Server};Client={settings.Client}";
	}
	#endregion

	#region 处置方法
	protected override void Dispose(bool disposing)
	{
		if(!disposing)
			return;

		_cancellation.Cancel();
		_connection.Dispose();
		var handlersCompleted = this.WaitForHandlers(TimeSpan.FromSeconds(5));

		if(handlersCompleted)
			_dispatchers.Dispose();

		_cancellation.Dispose();
	}

	private bool WaitForHandlers(TimeSpan timeout)
	{
		var deadline = DateTime.UtcNow + timeout;
		var acquired = 0;

		while(acquired < HandlerConcurrency)
		{
			var remaining = deadline - DateTime.UtcNow;
			if(remaining <= TimeSpan.Zero || !_dispatchers.Wait(remaining))
				break;

			acquired++;
		}

		if(acquired == HandlerConcurrency)
			return true;

		if(acquired > 0)
			_dispatchers.Release(acquired);

		return false;
	}
	#endregion

	#region 嵌套子类
	private sealed class ConnectionManager : IDisposable
	{
		#region 成员字段
		private int _disposed;
		private int _readerCount;
		private IMqttClient _client;
		private readonly MqttQueue _queue;
		private readonly MqttClientOptions _options;
		private readonly TimeSpan _reconnectInterval;
		private readonly SemaphoreSlim _lifecycle;
		private readonly SemaphoreSlim _readers;
		private readonly Task _maintenance;
		private readonly CancellationToken _cancellation;
		#endregion

		#region 构造函数
		public ConnectionManager(MqttQueue queue, Configuration.MqttConnectionSettings settings, CancellationToken cancellation)
		{
			_queue = queue ?? throw new ArgumentNullException(nameof(queue));
			ArgumentNullException.ThrowIfNull(settings);

			if(settings.ReconnectInterval <= TimeSpan.Zero)
				throw new ArgumentOutOfRangeException(nameof(settings.ReconnectInterval), settings.ReconnectInterval, "The reconnect interval must be greater than zero.");

			_options = settings.GetOptions();
			_reconnectInterval = settings.ReconnectInterval;
			_lifecycle = new SemaphoreSlim(1, 1);
			_readers = new SemaphoreSlim(1, 1);
			_cancellation = cancellation;

			var factory = new MqttClientFactory(settings.Logable ? MqttLogger.Instance : MqttNetNullLogger.Instance);
			_client = factory.CreateMqttClient();
			_client.DisconnectedAsync += this.OnDisconnectedAsync;
			_client.ApplicationMessageReceivedAsync += queue.OnReceivedAsync;

			//MQTTnet 5 不再提供 ManagedClient，按官方建议由独立后台任务维护连接；
			//连接生命周期由 _lifecycle 独占，而发布和订阅等操作仍可并行使用 MQTTnet 的并发安全实现。
			_maintenance = Task.Run(() => this.MaintainAsync(cancellation));
		}
		#endregion

		#region 公共属性
		public MqttClientOptions Options => _options;
		#endregion

		#region 公共方法
		/// <summary>获取一个已连接且受共享生命周期锁保护的客户端。</summary>
		public async ValueTask<IMqttClient> AcquireAsync(CancellationToken cancellation)
		{
			while(true)
			{
				await this.EnsureConnectedAsync(cancellation);
				await this.EnterAsync(cancellation);

				var client = _client;
				if(client != null && client.IsConnected)
					return client;

				this.Release();
			}
		}

		/// <summary>释放当前客户端的共享生命周期锁。</summary>
		public void Release()
		{
			_readers.Wait();

			try
			{
				if(--_readerCount == 0)
					_lifecycle.Release();
			}
			finally
			{
				_readers.Release();
			}
		}
		#endregion

		#region 私有方法
		/// <summary>按照指定的重连间隔周期检查并维护客户端连接。</summary>
		private async Task MaintainAsync(CancellationToken cancellation)
		{
			using var timer = new PeriodicTimer(_reconnectInterval);

			do
			{
				try
				{
					await this.EnsureConnectedAsync(cancellation);
				}
				catch(OperationCanceledException) when(cancellation.IsCancellationRequested)
				{
					return;
				}
				catch(ObjectDisposedException) when(cancellation.IsCancellationRequested)
				{
					return;
				}
				catch(Exception ex)
				{
					await Zongsoft.Diagnostics.Logging.GetLogging<MqttQueue>().WarnAsync(ex, cancellation);
				}
			}
			while(await timer.WaitForNextTickAsync(cancellation));
		}

		/// <summary>确保客户端已连接，如果建立了新连接则恢复全部订阅。</summary>
		private async ValueTask EnsureConnectedAsync(CancellationToken cancellation)
		{
			var client = _client;
			if(client == null)
				throw new ObjectDisposedException(nameof(MqttQueue));

			if(client.IsConnected)
				return;

			await _lifecycle.WaitAsync(cancellation);

			try
			{
				client = _client;
				if(client == null)
					throw new ObjectDisposedException(nameof(MqttQueue));

				if(client.IsConnected)
					return;

				var result = await client.ConnectAsync(_options, cancellation);
				if(result.ResultCode != MqttClientConnectResultCode.Success)
					throw new InvalidOperationException(string.Format(Properties.Resources.MqttQueue_ConnectFailed_Message, result.ResultCode, result.ReasonString));

				foreach(var subscriber in _queue.Subscribers)
				{
					if(subscriber.IsClosed)
						continue;

					var subscription = await client.SubscribeAsync(subscriber.Subscription, cancellation);
					if(!subscription.IsSuccessful())
						throw new InvalidOperationException(string.Format(Properties.Resources.MqttQueue_SubscriptionRestoreFailed_Message, subscriber.Topic));
				}
			}
			finally
			{
				_lifecycle.Release();
			}
		}

		/// <summary>进入客户端共享生命周期锁。</summary>
		private async ValueTask EnterAsync(CancellationToken cancellation)
		{
			await _readers.WaitAsync(cancellation);

			try
			{
				_readerCount++;

				if(_readerCount == 1)
				{
					try
					{
						await _lifecycle.WaitAsync(cancellation);
					}
					catch
					{
						_readerCount--;
						throw;
					}
				}
			}
			finally
			{
				_readers.Release();
			}
		}
		#endregion

		#region 事件处理
		private async Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs args)
		{
			if(_cancellation.IsCancellationRequested)
				return;

			await Zongsoft.Diagnostics.Logging.GetLogging<MqttQueue>().TraceAsync(
				$"MQTT client '{_options.ClientId}' disconnected: {args.Reason}({args.ReasonString}).");
		}
		#endregion

		#region 处置方法
		public void Dispose()
		{
			if(Interlocked.Exchange(ref _disposed, 1) != 0)
				return;

			try { _maintenance.GetAwaiter().GetResult(); }
			catch(OperationCanceledException) { }

			var client = Interlocked.Exchange(ref _client, null);
			_lifecycle.Wait();

			try
			{
				if(client != null)
				{
					client.ApplicationMessageReceivedAsync -= _queue.OnReceivedAsync;
					client.DisconnectedAsync -= this.OnDisconnectedAsync;

					if(client.IsConnected)
					{
						try
						{
							using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
							client.DisconnectAsync(cancellationToken: cancellation.Token).GetAwaiter().GetResult();
						}
						catch { }
					}

					client.Dispose();
				}
			}
			finally
			{
				_lifecycle.Release();
			}

			_readers.Dispose();
			_lifecycle.Dispose();
		}
		#endregion
	}
	#endregion
}
