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
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

using MQTTnet.Server;
using MQTTnet.Diagnostics.Logger;

using Zongsoft.Components;
using Zongsoft.Communication;

namespace Zongsoft.Messaging.Mqtt;

public partial class MqttQueueServer : ListenerBase<Message>
{
	#region 常量定义
	/// <summary>MQTT Broker 的默认侦听端口号。</summary>
	public const ushort PORT = 1883;
	#endregion

	#region 成员字段
	private ushort _port;
	private MqttServer _server;
	private IMessageStorage _storage;
	#endregion

	#region 构造函数
	public MqttQueueServer(string name = null) : base(name)
	{
		_port = PORT;
		this.Channels = new ChannelCollection();
		this.Sessions = new SessionCollection();
	}
	#endregion

	#region 公共属性
	/// <summary>获取或设置服务器侦听的端口号，默认值为：<see cref="PORT"/>。</summary>
	public ushort Port
	{
		get => _port;
		set
		{
			if(this.State != WorkerState.Stopped)
				throw new InvalidOperationException(Properties.Resources.MqttQueueServer_PortImmutable_Message);

			_port = value > 0 ? value : PORT;
		}
	}

	/// <summary>获取一个值，指示是否启用 MQTTnet 内部日志。</summary>
	public bool Logable { get; set; }

	public IMessageStorage Storage
	{
		get => _storage;
		set
		{
			if(this.State != WorkerState.Stopped)
				throw new InvalidOperationException(Properties.Resources.MqttQueueServer_StorageImmutable_Message);

			_storage = value;
		}
	}

	/// <summary>获取当前连接到服务器的客户端通道集合。</summary>
	public ChannelCollection Channels { get; }

	/// <summary>获取服务器中的 MQTT 会话集合。</summary>
	public SessionCollection Sessions { get; }
	#endregion

	#region 保护属性
	/// <summary>获取当前运行的 MQTTnet Broker 实例。</summary>
	protected MqttServer Server => _server;
	#endregion

	#region 公共方法
	/// <summary>获取指定主题的 MQTT 保留消息。</summary>
	/// <param name="topic">指定要获取的消息主题。</param>
	/// <returns>返回对应的保留消息；如果主题为空、服务器未启动或保留消息不存在则返回空消息。</returns>
	public async ValueTask<Message> GetRetainedMessageAsync(string topic = null)
	{
		if(string.IsNullOrEmpty(topic))
			return default;

		var server = _server;
		if(server == null || !server.IsStarted)
			return default;

		var message = await server.GetRetainedMessageAsync(topic);
		return message == null ? default : new Message(message.Topic, message.GetPayload());
	}

	/// <summary>获取服务器中的所有 MQTT 保留消息。</summary>
	/// <returns>返回转换后的保留消息数组；如果服务器未启动或没有保留消息则返回空数组。</returns>
	public async ValueTask<Message[]> GetRetainedMessagesAsync()
	{
		var server = _server;
		if(server == null || !server.IsStarted)
			return [];

		var messages = await server.GetRetainedMessagesAsync();
		if(messages == null || messages.Count == 0)
			return [];

		var result = new Message[messages.Count];

		for(int i = 0; i < messages.Count; i++)
		{
			var message = messages[i];
			result[i] = message == null ? default : new Message(message.Topic, message.GetPayload());
		}

		return result;
	}
	#endregion

	#region 重写方法
	protected override async Task OnStartAsync(string[] args, CancellationToken cancellation)
	{
		var factory = new MqttServerFactory(this.Logable ? MqttLogger.Instance : MqttNetNullLogger.Instance);
		var options = factory.CreateServerOptionsBuilder()
			.WithDefaultEndpoint()
			.WithDefaultEndpointPort(_port)
			.Build();

		var server = factory.CreateMqttServer(options);
		server.InterceptingPublishAsync += this.OnMessageReceivedAsync;

		try
		{
			await server.StartAsync().WaitAsync(cancellation);
			_server = server;
			await this.Channels.BindAsync(server);
			await this.Sessions.BindAsync(server);
		}
		catch
		{
			_server = null;
			server.InterceptingPublishAsync -= this.OnMessageReceivedAsync;
			await this.Channels.BindAsync(null);
			await this.Sessions.BindAsync(null);
			server.Dispose();
			throw;
		}
	}

	protected override async Task OnStopAsync(string[] args, CancellationToken cancellation)
	{
		var server = Interlocked.Exchange(ref _server, null);

		await this.Channels.BindAsync(null);
		await this.Sessions.BindAsync(null);

		if(server == null)
			return;

		try
		{
			server.InterceptingPublishAsync -= this.OnMessageReceivedAsync;
			await server.StopAsync().WaitAsync(cancellation);
		}
		finally
		{
			server.Dispose();
		}
	}
	#endregion

	#region 消息处理
	private async Task OnMessageReceivedAsync(InterceptingPublishEventArgs args)
	{
		if(args?.ApplicationMessage == null)
			return;

		byte[] payload;
		try { payload = args.ApplicationMessage.GetPayload(); }
		catch(Exception exception)
		{
			await Zongsoft.Diagnostics.Logging.GetLogging<MqttQueueServer>().ErrorAsync(exception);
			return;
		}

		var message = new Message(args.ApplicationMessage.Topic, payload)
		{
			Identity = args.ClientId,
		};

		try
		{
			await this.OnHandleAsync(message, args.CancellationToken);
		}
		catch(OperationCanceledException) when(args.CancellationToken.IsCancellationRequested)
		{
		}
		catch(Exception ex)
		{
			await Zongsoft.Diagnostics.Logging.GetLogging<MqttQueueServer>().ErrorAsync(ex);
		}
	}

	protected override ValueTask OnHandleAsync(Message message, CancellationToken cancellation)
	{
		var handler = this.Handler;
		return handler == null ? ValueTask.CompletedTask : handler.HandleAsync(message, cancellation);
	}
	#endregion

	#region 处置方法
	protected override void Dispose(bool disposing)
	{
		if(disposing)
		{
			try
			{
				this.Stop();
				base.Dispose(disposing);
				this.Channels.BindAsync(null).GetAwaiter().GetResult();
				this.Sessions.BindAsync(null).GetAwaiter().GetResult();

				var server = Interlocked.Exchange(ref _server, null);
				if(server != null)
				{
					server.InterceptingPublishAsync -= this.OnMessageReceivedAsync;
					server.Dispose();
				}
			}
			finally
			{
				DisposeStorage(Interlocked.Exchange(ref _storage, null));
			}
		}
	}

	private static void DisposeStorage(IMessageStorage storage)
	{
		if(storage is IAsyncDisposable asynchronous)
			asynchronous.DisposeAsync().AsTask().GetAwaiter().GetResult();
		else if(storage is IDisposable disposable)
			disposable.Dispose();
	}
	#endregion
}
