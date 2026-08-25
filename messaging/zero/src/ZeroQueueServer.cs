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

using NetMQ;
using NetMQ.Sockets;

using Zongsoft.Services;
using Zongsoft.Components;
using Zongsoft.Configuration;

namespace Zongsoft.Messaging.ZeroMQ;

public sealed class ZeroQueueServer : WorkerBase
{
	#region 公共常量
	public const ushort PORT = 7969;
	#endregion

	#region 内部常量
	internal const string PROTOCOL_VERSION = "1.0";
	internal const string WELCOME_MESSAGE = $"\0Zongsoft.Messaging.ZeroMQ\nProtocol-Version:{PROTOCOL_VERSION}\0";
	#endregion

	#region 成员字段
	private ushort _port;
	private ServerAgent _agent;
	#endregion

	#region 构造函数
	public ZeroQueueServer(string name = null) : base(name) => _port = PORT;
	#endregion

	#region 公共属性
	public ushort Port
	{
		get => _port;
		set
		{
			if(this.State != WorkerState.Stopped)
				throw new InvalidOperationException(Properties.Resources.ZeroQueueServer_PortImmutable_Message);

			_port = value > 0 ? value : PORT;
		}
	}
	#endregion

	#region 重写方法
	protected override async Task OnStartAsync(string[] args, CancellationToken cancellation)
	{
		var (incoming, outgoing) = GetPorts(this.Name, args);
		ValidatePorts(_port, incoming, outgoing);

		var agent = new ServerAgent(_port, incoming, outgoing);
		try
		{
			await agent.StartAsync(cancellation);
			_agent = agent;
		}
		catch
		{
			await agent.DisposeAsync();
			throw;
		}

		static void ValidatePorts(ushort discovery, int incoming, int outgoing)
		{
			if(incoming is < 0 or > ushort.MaxValue || outgoing is < 0 or > ushort.MaxValue)
				throw new ArgumentOutOfRangeException(nameof(incoming), string.Format(Properties.Resources.ZeroQueueServer_DataPortOutOfRange_Message, ushort.MaxValue));

			if(incoming > 0 && outgoing > 0 && incoming == outgoing)
				throw new ArgumentException(Properties.Resources.ZeroQueueServer_DataPortsConflict_Message);

			if(incoming == discovery || outgoing == discovery)
				throw new ArgumentException(Properties.Resources.ZeroQueueServer_DiscoveryPortConflict_Message);
		}
	}

	protected override async Task OnStopAsync(string[] args, CancellationToken cancellation)
	{
		var agent = Interlocked.Exchange(ref _agent, null);
		if(agent != null)
			await agent.DisposeAsync();
	}
	#endregion

	#region 释放资源
	protected override void Dispose(bool disposing)
	{
		if(disposing)
		{
			base.Dispose(disposing);
			Interlocked.Exchange(ref _agent, null)?.DisposeAsync().AsTask().GetAwaiter().GetResult();
		}
	}
	#endregion

	#region 私有方法
	private static (int incoming, int outgoing) GetPorts(string name, string[] args)
	{
		var incoming = 0;
		var outgoing = 0;

		if(args != null)
		{
			for(var index = 0; index < args.Length; index++)
			{
				var parts = args[index].Split(['=', ':'], 2, StringSplitOptions.TrimEntries);
				if(parts.Length != 2)
					continue;

				var key = parts[0].StartsWith("--", StringComparison.Ordinal) ? parts[0][2..] : parts[0];
				if(!key.Equals("incoming", StringComparison.OrdinalIgnoreCase) && !key.Equals("outgoing", StringComparison.OrdinalIgnoreCase))
					continue;

				if(!int.TryParse(parts[1], out var port) || port is < 0 or > ushort.MaxValue)
					throw new ArgumentException(string.Format(Properties.Resources.ZeroQueueServer_NamedPortOutOfRange_Message, key, ushort.MaxValue), nameof(args));

				if(key.Equals("incoming", StringComparison.OrdinalIgnoreCase))
					incoming = port;
				else
					outgoing = port;
			}
		}

		if(incoming > 0 || outgoing > 0)
			return (incoming, outgoing);

		var servers = ApplicationContext.Current?.Configuration.GetOption<Configuration.ServerOptionsCollection>("/Messaging/ZeroMQ/Servers");
		if(servers == null)
			return default;

		if(name != null && servers.TryGetValue(name, out var server))
			return (server.Port.Incoming, server.Port.Outgoing);

		return (servers.Port.Incoming, servers.Port.Outgoing);
	}
	#endregion

	#region 嵌套子类
	private sealed class ServerAgent : IAsyncDisposable
	{
		#region 私有成员
		private readonly ushort _port;
		private readonly int _incoming;
		private readonly int _outgoing;
		private readonly NetMQQueue<Command> _commands;
		private readonly NetMQPoller _poller;
		private readonly Task _runner;
		private ResponseSocket _responser;
		private XPublisherSocket _publisher;
		private XSubscriberSocket _subscriber;
		private int _publisherPort;
		private int _subscriberPort;
		private int _disposed;
		#endregion

		#region 构造函数
		public ServerAgent(ushort port, int incoming, int outgoing)
		{
			_port = port;
			_incoming = incoming;
			_outgoing = outgoing;
			_commands = new NetMQQueue<Command>();
			_commands.ReceiveReady += this.OnCommandReady;
			_poller = new NetMQPoller() { _commands };
			_runner = Task.Factory.StartNew(() =>
			{
				if(Thread.CurrentThread.Name == null)
					Thread.CurrentThread.Name = $"{nameof(ZeroQueueServer)}#{port}.Actor";

				_poller.Run(new SynchronizationContext());
			}, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
		}
		#endregion

		#region 公共方法
		public ValueTask StartAsync(CancellationToken cancellation)
		{
			var command = new StartCommand(cancellation);
			_commands.Enqueue(command);
			return new ValueTask(command.Completion.Task);
		}
		#endregion

		#region 事件处理
		private void OnPublisherReady(object sender, NetMQSocketEventArgs args) => this.Forward(args.Socket, _subscriber);
		private void OnSubscriberReady(object sender, NetMQSocketEventArgs args) => this.Forward(args.Socket, _publisher);

		private void OnReceiveReady(object sender, NetMQSocketEventArgs args)
		{
			var command = args.Socket.ReceiveFrameString();
			if(string.IsNullOrEmpty(command) || command is "port" or "ports")
				args.Socket.SendFrame($"Publisher={_publisherPort};Subscriber={_subscriberPort}");
			else
				args.Socket.SendFrameEmpty();
		}

		private void OnCommandReady(object sender, NetMQQueueEventArgs<Command> args)
		{
			while(args.Queue.TryDequeue(out var command, TimeSpan.Zero))
			{
				try
				{
					if(command.Cancellation.IsCancellationRequested)
						command.Completion.TrySetCanceled(command.Cancellation);
					else if(command is StartCommand)
					{
						this.Start();
						command.Completion.TrySetResult();
					}
					else
					{
						this.Stop();
						command.Completion.TrySetResult();
					}
				}
				catch(Exception exception)
				{
					command.Completion.TrySetException(exception);
				}
			}
		}
		#endregion

		#region 命令执行
		private void Forward(NetMQSocket source, NetMQSocket destination)
		{
			try
			{
				var message = new NetMQMessage();
				while(source.TryReceiveMultipartMessage(ref message))
				{
					destination.SendMultipartMessage(message);
					message = new NetMQMessage();
				}
			}
			catch(Exception exception) { Diagnostics.Logging.GetLogging(this).Error(exception); }
		}

		private void Start()
		{
			_responser = new ResponseSocket();
			_publisher = new XPublisherSocket();
			_subscriber = new XSubscriberSocket();

			try
			{
				_responser.ReceiveReady += this.OnReceiveReady;
				_publisher.ReceiveReady += this.OnPublisherReady;
				_subscriber.ReceiveReady += this.OnSubscriberReady;
				_publisher.SetWelcomeMessage(WELCOME_MESSAGE);
				_responser.Bind($"tcp://*:{_port}");
				_publisherPort = Bind(_publisher, _outgoing);
				_subscriberPort = Bind(_subscriber, _incoming);
				_poller.Add(_responser);
				_poller.Add(_publisher);
				_poller.Add(_subscriber);
			}
			catch
			{
				this.ReleaseSockets();
				throw;
			}
		}

		private void Stop() => this.ReleaseSockets();
		private void ReleaseSockets()
		{
			Release(ref _responser, socket => socket.ReceiveReady -= this.OnReceiveReady);
			Release(ref _publisher, socket => socket.ReceiveReady -= this.OnPublisherReady);
			Release(ref _subscriber, socket => socket.ReceiveReady -= this.OnSubscriberReady);

			_publisherPort = 0;
			_subscriberPort = 0;

			void Release<TSocket>(ref TSocket socket, Action<TSocket> releasing = null) where TSocket : NetMQSocket
			{
				var current = socket;
				socket = null;
				if(current == null || current.IsDisposed)
					return;

				releasing?.Invoke(current);
				_poller.RemoveAndDispose(current);
			}
		}
		#endregion

		#region 私有方法
		private static int Bind(NetMQSocket socket, int port)
		{
			if(port > 0)
			{
				socket.Bind($"tcp://*:{port}");
				return port;
			}

			return socket.BindRandomPort("tcp://*");
		}
		#endregion

		#region 异步释放
		public async ValueTask DisposeAsync()
		{
			if(Interlocked.Exchange(ref _disposed, 1) != 0)
				return;

			var command = new StopCommand();
			_commands.Enqueue(command);
			await command.Completion.Task;

			if(_poller.IsRunning)
				_poller.Stop();

			await _runner;

			_commands.ReceiveReady -= this.OnCommandReady;
			_poller.Remove(_commands);
			_commands.Dispose();
			_poller.Dispose();
		}
		#endregion

		#region 嵌套子类
		private abstract class Command
		{
			protected Command(CancellationToken cancellation = default)
			{
				this.Cancellation = cancellation;
				this.Completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
			}

			public CancellationToken Cancellation { get; }
			public TaskCompletionSource Completion { get; }
		}

		private sealed class StopCommand : Command;
		private sealed class StartCommand(CancellationToken cancellation) : Command(cancellation);
		#endregion
	}
	#endregion
}
