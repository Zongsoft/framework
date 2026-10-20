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
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using NetMQ;
using NetMQ.Sockets;

using Zongsoft.Components;
using Zongsoft.Communication;

namespace Zongsoft.Messaging.ZeroMQ;

public sealed partial class ZeroQueueServer : WorkerBase
{
	#region 公共常量
	public const ushort PORT = 7969;
	#endregion

	#region 成员字段
	private NetworkAddress _discovery;
	private NetworkAddress _control;
	private NetworkAddress _incoming;
	private NetworkAddress _outgoing;
	private string _storageName;
	private ServerAgent _agent;
	private IMessageStorage _storage;
	private IMessageStorageFactory _storages;
	#endregion

	#region 构造函数
	public ZeroQueueServer(string name = null) : base(name)
	{
		_discovery = NetworkAddress.Parse($"tcp://0.0.0.0:{PORT}");
		_incoming = NetworkAddress.Parse("tcp://0.0.0.0:32101");
		_outgoing = NetworkAddress.Parse("tcp://0.0.0.0:32102");
	}
	#endregion

	#region 公共属性
	/// <summary>获取或设置发现服务的完整 TCP 侦听地址。</summary>
	public string Discovery
	{
		get => _discovery.ToString()[..^1];
		set => _discovery = this.GetAddress(value);
	}

	/// <summary>获取或设置可靠投递控制的完整 TCP 侦听地址；为空时禁用可靠投递。</summary>
	public string Control
	{
		get => _control?.ToString()[..^1];
		set => _control = this.GetAddress(value, true);
	}

	/// <summary>获取或设置消息流入的完整 TCP 侦听地址。</summary>
	public string Incoming
	{
		get => _incoming.ToString()[..^1];
		set => _incoming = this.GetAddress(value);
	}

	/// <summary>获取或设置消息流出的完整 TCP 侦听地址。</summary>
	public string Outgoing
	{
		get => _outgoing.ToString()[..^1];
		set => _outgoing = this.GetAddress(value);
	}

	/// <summary>获取或设置可靠投递使用的存储配置名，与服务器名称相互独立。</summary>
	public string Storage
	{
		get => _storageName;
		set
		{
			if(this.State != WorkerState.Stopped)
				throw new InvalidOperationException(Properties.Resources.ZeroQueueServer_StorageImmutable_Message);

			value = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
			if(string.Equals(_storageName, value, StringComparison.Ordinal))
				return;

			_storageName = value;
		}
	}

	public IMessageStorageFactory Storages
	{
		get => _storages;
		set
		{
			if(this.State != WorkerState.Stopped)
				throw new InvalidOperationException(Properties.Resources.ZeroQueueServer_StorageImmutable_Message);

			if(ReferenceEquals(_storages, value))
				return;

			_storages = value;
		}
	}
	#endregion

	#region 重写方法
	/// <summary>加载可靠消息并启动服务器的 Actor。</summary>
	/// <param name="args">启动参数。</param>
	/// <param name="cancellation">启动取消令牌。</param>
	/// <returns>存储加载和服务器启动完成的任务。</returns>
	protected override async Task OnStartAsync(string[] args, CancellationToken cancellation)
	{
		cancellation.ThrowIfCancellationRequested();
		this.Validate();

		if(_control != null)
			_storage = _storages.Create(_storageName) ?? throw new InvalidOperationException(string.Format(Properties.Resources.ZeroQueueServer_StorageMissing_Message, _storageName));

		var pending = new System.Collections.Generic.List<Message>();
		if(_storage != null)
		{
			await foreach(var entry in _storage.GetAsync(cancellation))
				pending.Add(entry);
		}

		_agent = new ServerAgent(_discovery, _control, _incoming, _outgoing, _storage, pending);
		await _agent.StartAsync(cancellation);
	}

	/// <summary>等待服务器 Actor、传输资源及自有存储释放。</summary>
	/// <param name="args">停止参数。</param>
	/// <param name="cancellation">停止阶段令牌；清理开始后等待其完成。</param>
	/// <returns>服务器停止完成的任务。</returns>
	protected override async Task OnStopAsync(string[] args, CancellationToken cancellation)
	{
		if(_agent != null)
		{
			await _agent.DisposeAsync();
			_agent = null;
		}

		await DisposeStorageAsync(_storage);
		_storage = null;
	}
	#endregion

	#region 私有方法
	/// <summary>优先异步释放服务器拥有的存储。</summary>
	/// <param name="storage">待释放的存储，可为空。</param>
	/// <returns>存储释放完成的操作。</returns>
	private static async ValueTask DisposeStorageAsync(IMessageStorage storage)
	{
		if(storage is IAsyncDisposable asynchronous)
			await asynchronous.DisposeAsync();
		else if(storage is IDisposable disposable)
			disposable.Dispose();
	}

	/// <summary>在停止状态解析并验证服务器端点。</summary>
	/// <param name="text">完整的 TCP 地址。</param>
	/// <param name="optional">是否允许空地址。</param>
	/// <returns>有效地址，允许空地址时可为空。</returns>
	private NetworkAddress GetAddress(string text, bool optional = false)
	{
		if(this.State != WorkerState.Stopped)
			throw new InvalidOperationException(Properties.Resources.ZeroQueueServer_AddressImmutable_Message);

		if(optional && string.IsNullOrWhiteSpace(text))
			return null;

		if(!NetworkAddress.TryParse(text, out var address) ||
		   address.Scheme != "tcp" || address.Path != "/" ||
		   address.HostType == UriHostNameType.Dns ||
		   !IPAddress.TryParse(address.Zone == null ? address.Host : $"{address.Host}%{address.Zone}", out _))
			throw new ArgumentException(Properties.Resources.ZeroQueueServer_AddressInvalid_Message, nameof(text));

		return address;
	}

	/// <summary>验证端口唯一性及可靠投递的存储配置。</summary>
	private void Validate()
	{
		if(_discovery.Port == _incoming.Port || _discovery.Port == _outgoing.Port || _incoming.Port == _outgoing.Port ||
		   _control != null && (_control.Port == _discovery.Port || _control.Port == _incoming.Port || _control.Port == _outgoing.Port))
			throw new ArgumentException(Properties.Resources.ZeroQueueServer_PortsConflict_Message);

		if((_control != null) != (_storageName != null) || _control != null && _storages == null)
			throw new InvalidOperationException(Properties.Resources.ZeroQueueServer_ControlStorageRequired_Message);
	}
	#endregion

	#region 嵌套子类
	private sealed partial class ServerAgent : IAsyncDisposable
	{
		#region 私有成员
		private readonly NetworkAddress _discovery;
		private readonly NetMQQueue<Command> _commands;
		private readonly NetMQPoller _poller;
		private readonly ZeroBroadcastServer _broadcast;
		private readonly ZeroControlServer _control;
		private readonly Task _runner;
		private readonly string _epoch;
		private ResponseSocket _responser;
		private int _disposed;
		#endregion

		#region 构造函数
		public ServerAgent(NetworkAddress discovery, NetworkAddress control, NetworkAddress incoming, NetworkAddress outgoing, IMessageStorage storage, System.Collections.Generic.IReadOnlyList<Message> pendingEntries)
		{
			_discovery = discovery;
			_epoch = Guid.NewGuid().ToString("N");
			_commands = new NetMQQueue<Command>();
			_commands.ReceiveReady += this.OnCommandReady;
			_poller = new NetMQPoller() { _commands };
			_broadcast = new ZeroBroadcastServer(incoming, outgoing, _epoch, _poller);
			_control = new ZeroControlServer(control, storage, pendingEntries, _poller, action => _commands.Enqueue(new ActionCommand(action)));
			_runner = Task.Factory.StartNew(() =>
			{
				if(Thread.CurrentThread.Name == null)
					Thread.CurrentThread.Name = $"{nameof(ZeroQueueServer)}#{discovery.Port}.Actor";

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
		private void OnReceiveReady(object sender, NetMQSocketEventArgs args)
		{
			var request = args.Socket.ReceiveFrameString();
			if(!Protocol.TryParseDiscoveryRequest(request))
			{
				args.Socket.SendFrame(Protocol.GetDiscoveryError(Protocol.Errors.InvalidRequest));
				return;
			}

			args.Socket.SendFrame(Protocol.GetDiscoveryResponse(_epoch, _control.Port, _broadcast.Incoming, _broadcast.Outgoing));
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
					else if(command is ActionCommand action)
					{
						action.Action();
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
					if(command is ActionCommand)
						Diagnostics.Logging.GetLogging(this).Error(exception);
					command.Completion.TrySetException(exception);
				}
			}
		}
		#endregion

		#region 命令执行
		private void Start()
		{
			_responser = new ResponseSocket();

			try
			{
				_responser.ReceiveReady += this.OnReceiveReady;
				Bind(_responser, _discovery);
				_poller.Add(_responser);
				_broadcast.Start();
				_control.Start();
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
			_control.Stop();
			_broadcast.Stop();
			Release(ref _responser, socket => socket.ReceiveReady -= this.OnReceiveReady);

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
		private static int Bind(NetMQSocket socket, NetworkAddress address)
		{
			socket.Options.IPv4Only = address.HostType != UriHostNameType.IPv6;
			var host = address.Zone == null ? address.Host : $"{address.Host}%{address.Zone}";
			socket.Bind(address.HostType == UriHostNameType.IPv6 ? $"tcp://[{host}]:{address.Port}" : $"tcp://{host}:{address.Port}");
			return address.Port;
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

			await _control.DisposeAsync();
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
		private sealed class ActionCommand(Action action) : Command
		{
			public Action Action { get; } = action ?? throw new ArgumentNullException(nameof(action));
		}
		#endregion
	}
	#endregion
}
